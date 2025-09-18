using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace EtherCatSharp.EtherCatCore
{
    public partial class EtherCatCore
    {
        /// <summary>
        /// 定义网卡设备
        /// </summary>
        private LibPcapLiveDevice? device;
        /// <summary>
        /// 定时器对象，用于处理超时和延时操作
        /// </summary>
        public OsalTimer osalTimer = new OsalTimer();
        public OsalTimer osalTimer2 = new OsalTimer();
        /// <summary>
        /// 不冗余发送
        /// </summary>
        const byte ECT_RED_NONE = 0;
        /// <summary>
        /// 冗余发送
        /// </summary>
        const byte ECT_RED_DOUBLE = 1;
        /// <summary>
        /// 不冗余发送时使用的mac地址
        /// </summary>
        ushort[] priMAC = { 0x0101, 0x0101, 0x0101 };
        /// <summary>
        /// 冗余时发送的mac地址
        /// </summary>
        ushort[] secMAC = { 0x0404, 0x0404, 0x0404 };
        static char[] errbuf = new char[256];
        public void ecx_clear_rxbufstat(Span<int> rxbufstat)
        {
            int i;
            for (i = 0; i < EC_MAXBUF; i++)
            {
                rxbufstat[i] = EC_BUF_EMPTY;
            }
        }
        public int ecx_setupnic(ecx_portt port, string ifname, bool secondary)
        {
            if (secondary)
            {
                /* secondary port struct available? */
                if (port.redport != null)
                {
                    /* when using secondary socket it is automatically a redundant setup */
                    port.redstate = ECT_RED_DOUBLE;
                    port.redport.stack.txbuf = port.txbuf;
                    port.redport.stack.txbuflength = port.txbuflength;
                    port.redport.stack.tempbuf = port.redport.tempinbuf;
                    port.redport.stack.rxbuf = port.redport.rxbuf;
                    port.redport.stack.rxbufstat = port.redport.rxbufstat;
                    port.redport.stack.rxsa = port.redport.rxsa;
                    ecx_clear_rxbufstat(port.redport.rxbufstat);
                }
                else
                {
                    /* fail */
                    return 0;
                }
            }
            else
            {
                port.lastidx = 0;
                port.redstate = ECT_RED_NONE;
                port.stack.txbuf = port.txbuf;
                port.stack.txbuflength = port.txbuflength;
                port.stack.tempbuf = port.tempinbuf;
                port.stack.rxbuf = port.rxbuf;
                port.stack.rxbufstat = port.rxbufstat;
                port.stack.rxsa = port.rxsa;
                ecx_clear_rxbufstat(port.rxbufstat);
            }


            // 查找设备
            device = LibPcapLiveDeviceList.Instance.FirstOrDefault(d => d.Name == ifname || d.Description == ifname || d.Interface?.FriendlyName == ifname || d.Interface?.Name == ifname);
            if (device == null)
                return -1; // 未找到设备

            // 打开设备（混杂模式，最大响应，无本地捕获）
            device.Open(DeviceModes.Promiscuous | DeviceModes.NoCaptureLocal | DeviceModes.MaxResponsiveness, 1000);
            // 设置过滤器，仅捕获 EtherCAT 帧
            device.Filter = "ether proto 0x88a4";

            for (int i = 0; i < EC_MAXBUF; i++)
            {
                ec_setupheader(port.txbuf[i]);
                port.rxbufstat[i] = EC_BUF_EMPTY;
            }
            ec_setupheader(port.txbuf2);
            return 1;
        }
        public int ecx_closenic()
        {
            try
            {
                if (device != null)
                {
                    device.Close();
                    device = null;
                }
                return 0; // 成功关闭设备
            }
            catch
            {
                // 关闭失败
                return -1;
            }
        }
        public void ec_setupheader(Span<byte> buff)
        {
            ec_etherheadert bp = MemoryMarshal.Read<ec_etherheadert>(buff);
            bp.da0 = htons(0xffff);
            bp.da1 = htons(0xffff);
            bp.da2 = htons(0xffff);
            bp.sa0 = htons(priMAC[0]);
            bp.sa1 = htons(priMAC[1]);
            bp.sa2 = htons(priMAC[2]);
            bp.etype = htons(ETH_P_ECAT);
            var bufft = bp.ToBytes();
            memcpy(buff, 0, bufft, 0, bufft.Length);
        }
        public byte ecx_getindex(ecx_portt port)
        {
            byte idx;
            byte cnt;

            lock (port.getindex_mutex)
            {
                idx = (byte)(port.lastidx + 0x01);
                /* index can't be larger than buffer array */
                if (idx >= EC_MAXBUF)
                {
                    idx = 0;
                }
                cnt = 0;
                /* try to find unused index */
                while ((port.rxbufstat[idx] != EC_BUF_EMPTY) && (cnt < EC_MAXBUF))
                {
                    idx++;
                    cnt++;
                    if (idx >= EC_MAXBUF)
                    {
                        idx = 0;
                    }
                }
                port.rxbufstat[idx] = EC_BUF_ALLOC;
                if (port.redstate != ECT_RED_NONE)
                    port.redport.rxbufstat[idx] = EC_BUF_ALLOC;
                port.lastidx = idx;
            }
            return idx;
        }
        public void ecx_setbufstat(ecx_portt port, byte idx, int bufstat)
        {
            port.rxbufstat[idx] = bufstat;
            if (port.redstate != ECT_RED_NONE)
                port.redport.rxbufstat[idx] = bufstat;
        }
        public int ecx_outframe(ecx_portt port, byte idx, int stacknumber = 0)
        {
            int lp, rval;
            ec_stackT stack;
            if (stacknumber == 0)
            {
                stack = port.stack;
            }
            else
            {
                stack = port.redport.stack;
            }
            lp = stack.txbuflength[idx];
            stack.rxbufstat[idx] = EC_BUF_TX;
            try
            {
                device?.SendPacket(stack.txbuf[idx], lp);
                rval = 0; // 发送成功
            }
            catch
            {
                rval = -1; // 发送失败
            }

            if (rval == -1)
            {
                stack.rxbufstat[idx] = EC_BUF_EMPTY;
            }
            return rval;
        }
        public int ecx_outframe_red(ecx_portt port, byte idx)
        {
            ec_comt datagramP;
            ec_etherheadert ehp;
            int rval;
            ehp = port.txbuf[idx].ToStruct<ec_etherheadert>();
            //ehp = ToStructure<ec_etherheadert>(port.txbuf[idx]);
            /* rewrite MAC source address 1 to primary */
            ehp.sa1 = htons(priMAC[1]);
            /* transmit over primary socket*/
            rval = ecx_outframe(port, idx, 0);
            if (port.redstate != ECT_RED_NONE)
            {
                lock (port.tx_mutex)
                {
                    ehp = port.txbuf2.ToStruct<ec_etherheadert>();
                    /* use dummy frame for secondary socket transmit (BRD) */
                    datagramP = port.txbuf2.ToStruct<ec_comt>(ETH_HEADERSIZE);
                    /* write index to frame */
                    datagramP.index = idx;
                    /* rewrite MAC source address 1 to secondary */
                    ehp.sa1 = htons(secMAC[1]);
                    /* transmit over secondary socket */
                    port.redport.rxbufstat[idx] = EC_BUF_TX;
                    try
                    {
                        device?.SendPacket(port.txbuf2, port.txbuflength2);
                        rval = 0; // 发送成功
                    }
                    catch
                    {
                        rval = -1; // 发送失败
                    }

                    if (rval == -1)
                    {
                        port.redport.rxbufstat[idx] = EC_BUF_EMPTY;
                    }
                }
            }
            return rval;
        }
        public int ecx_recvpkt(ecx_portt port, int stacknumber = 0)
        {
            if (device == null)
            {
                return 0;
            }
            ec_stackT stack;
            if (stacknumber == 0)
            {
                stack = port.stack;
            }
            else
            {
                stack = port.redport.stack;
            }
            int lp = port.tempinbuf.Length;
            var res = device.GetNextPacket(out var rawCapture);
            if (res <= 0)
            {
                port.tempinbufs = 0;
                return 0;
            }
            int bytesRx = rawCapture.Data.Length;
            if (bytesRx > lp)
            {
                bytesRx = lp;
            }
            // 将数据复制到 tempbuf
            memcpy(stack.tempbuf, 0, rawCapture.Data.ToArray(), 0, bytesRx);
            port.tempinbufs = bytesRx;
            return bytesRx > 0 ? 1 : 0;
        }
        public int ecx_inframe(ecx_portt port, byte idx, int stacknumber = 0)
        {
            ushort l;
            int rval;
            byte idxf;
            ec_etherheadert ehp;
            ec_comt ecp;
            byte[] rxbuf;
            ec_stackT stack;
            if (stacknumber == 0)
            {
                stack = port.stack;
            }
            else
            {
                stack = port.redport.stack;
            }
            rval = EC_NOFRAME;
            rxbuf = stack.rxbuf[idx];
            /* check if requested index is already in buffer ? */
            if ((idx < EC_MAXBUF) && (stack.rxbufstat[idx] == EC_BUF_RCVD))
            {
                l = (ushort)(rxbuf[0] + ((ushort)(rxbuf[1] & 0x0f) << 8));
                /* return WKC */
                rval = (rxbuf[l] + (rxbuf[l + 1] << 8));
                /* mark as completed */
                stack.rxbufstat[idx] = EC_BUF_COMPLETE;
            }
            else
            {
                lock (port.rx_mutex)
                {
                    /* check if it is an EtherCAT frame */
                    if (ecx_recvpkt(port, stacknumber) == 1)
                    {
                        rval = EC_OTHERFRAME;
                        ehp = stack.tempbuf.ToStruct<ec_etherheadert>();
                        /* check if it is an EtherCAT frame */
                        if (ehp.etype == htons(ETH_P_ECAT))
                        {
                            ecp = stack.tempbuf.ToStruct<ec_comt>(ETH_HEADERSIZE);
                            l = (ushort)(etohs(ecp.elength) & 0x0fff);
                            Console.WriteLine("L="+l);
                            idxf = ecp.index;
                            /* found index equals requested index ? */
                            if (idxf == idx)
                            {
                                /* yes, put it in the buffer array (strip ethernet header) */
                                //memcpy(rxbuf, 0, stack.tempbuf, 0, stack.txbuflength[idx]);

                                memcpy(rxbuf, 0, stack.tempbuf, ETH_HEADERSIZE, stack.txbuflength[idx]- ETH_HEADERSIZE);
                                /* return WKC */
                               // rval = (rxbuf[l+ EC_HEADERSIZE+EC_ELENGTHSIZE] + ((rxbuf[l +EC_HEADERSIZE + EC_ELENGTHSIZE+ 1]) << 8));

                                rval = (rxbuf[l] + ((rxbuf[l + 1]) << 8));
                                /* mark as completed */
                                stack.rxbufstat[idx] = EC_BUF_COMPLETE;
                                /* store MAC source word 1 for redundant routing info */
                                stack.rxsa[idx] = ntohs(ehp.sa1);
                            }
                            else
                            {
                                /* check if index exist and someone is waiting for it */
                                if (idxf < EC_MAXBUF && (stack.rxbufstat)[idxf] == EC_BUF_TX)
                                {
                                    rxbuf = stack.rxbuf[idxf];
                                    /* put it in the buffer array (strip ethernet header) */
                                    //memcpy(rxbuf, 0, stack.tempbuf, 0, stack.txbuflength[idx]);

                                    memcpy(rxbuf, 0, stack.tempbuf, ETH_HEADERSIZE, stack.txbuflength[idx]- ETH_HEADERSIZE);
                                    /* mark as received */
                                    (stack.rxbufstat)[idxf] = EC_BUF_RCVD;
                                    (stack.rxsa)[idxf] = ntohs(ehp.sa1);
                                }
                                else
                                {
                                    /* strange things happened */
                                }
                            }
                        }
                    }
                }
            }

            /* WKC if matching frame found */
            return rval;
        }
        public int ecx_waitinframe_red(ecx_portt port, byte idx, int timer)
        {
            int wkc = EC_NOFRAME;
            int wkc2 = EC_NOFRAME;
            int primrx, secrx;

            /* if not in redundant mode then always assume secondary is OK */
            if (port.redstate == ECT_RED_NONE)
                wkc2 = 0;
            osalTimer2.Start(timer);
            do
            {
                /* only read frame if not already in */
                if (wkc <= EC_NOFRAME)
                    wkc = ecx_inframe(port, idx, 0);
                /* only try secondary if in redundant mode */
                if (port.redstate != ECT_RED_NONE)
                {
                    /* only read frame if not already in */
                    if (wkc2 <= EC_NOFRAME)
                        wkc2 = ecx_inframe(port, idx, 1);
                }
                /* wait for both frames to arrive or timeout */
            } while (((wkc <= EC_NOFRAME) || (wkc2 <= EC_NOFRAME)) && !osalTimer2.IsExpired());
            /* only do redundant functions when in redundant mode */
            if (port.redstate != ECT_RED_NONE)
            {
                /* primrx if the received MAC source on primary socket */
                primrx = 0;
                if (wkc > EC_NOFRAME) primrx = port.rxsa[idx];
                /* secrx if the received MAC source on psecondary socket */
                secrx = 0;
                if (wkc2 > EC_NOFRAME) secrx = port.redport.rxsa[idx];

                /* primary socket got secondary frame and secondary socket got primary frame */
                /* normal situation in redundant mode */
                if (((primrx == 0x0404) && (secrx == 0x0101)))
                {
                    /* copy secondary buffer to primary */
                    //memcpy(port.rxbuf[idx], ETH_HEADERSIZE, port.redport.rxbuf[idx], ETH_HEADERSIZE, port.txbuflength[idx] - ETH_HEADERSIZE);
                    memcpy(port.rxbuf[idx], 0, port.redport.rxbuf[idx], 0, port.txbuflength[idx] - ETH_HEADERSIZE);
                    wkc = wkc2;
                }
                /* primary socket got nothing or primary frame, and secondary socket got secondary frame */
                /* we need to resend TX packet */
                if (((primrx == 0) && (secrx == 0x0404)) ||
                    ((primrx == 0x0101) && (secrx == 0x0404)))
                {
                    /* If both primary and secondary have partial connection retransmit the primary received
                     * frame over the secondary socket. The result from the secondary received frame is a combined
                     * frame that traversed all slaves in standard order. */
                    if ((primrx == 0x0101) && (secrx == 0x0404))
                    {
                        /* copy primary rx to tx buffer */
                        //memcpy(port.txbuf[idx], ETH_HEADERSIZE, port.rxbuf[idx], ETH_HEADERSIZE, port.txbuflength[idx] - ETH_HEADERSIZE);
                        memcpy(port.txbuf[idx], 0, port.rxbuf[idx], 0, port.txbuflength[idx] - ETH_HEADERSIZE);
                    }
                    osalTimer.Start(EC_TIMEOUTRET);
                    /* resend secondary tx */
                    ecx_outframe(port, idx, 1);
                    do
                    {
                        /* retrieve frame */
                        wkc2 = ecx_inframe(port, idx, 1);
                    } while ((wkc2 <= EC_NOFRAME) && !osalTimer.IsExpired());
                    if (wkc2 > EC_NOFRAME)
                    {
                        /* copy secondary result to primary rx buffer */
                        //memcpy(port.rxbuf[idx], ETH_HEADERSIZE, port.redport.rxbuf[idx], ETH_HEADERSIZE, port.txbuflength[idx] - ETH_HEADERSIZE);
                        memcpy(port.rxbuf[idx], 0, port.redport.rxbuf[idx], 0, port.txbuflength[idx] - ETH_HEADERSIZE);
                        wkc = wkc2;
                    }
                }
            }

            /* return WKC or EC_NOFRAME */
            return wkc;
        }
        public int ecx_waitinframe(ecx_portt port, byte idx, int timeout)
        {
            int wkc;
            wkc = ecx_waitinframe_red(port, idx, timeout);

            return wkc;
        }
        public int ecx_srconfirm(ecx_portt port, byte idx, int timeout)
        {
            int wkc = EC_NOFRAME;
            osalTimer.Start(timeout);
            do
            {
                /* tx frame on primary and if in redundant mode a dummy on secondary */
                ecx_outframe(port, idx);
                if (timeout < EC_TIMEOUTRET)
                {
                    osalTimer2.Start(timeout);
                }
                else
                {
                    /* normally use partial timeout for rx */
                    osalTimer2.Start(EC_TIMEOUTRET);
                }
                /* get frame from primary or if in redundant mode possibly from secondary */
                wkc = ecx_waitinframe_red(port, idx, timeout);
                /* wait for answer with WKC>=0 or otherwise retry until timeout */
            } while ((wkc <= EC_NOFRAME) && !osalTimer.IsExpired());

            return wkc;
        }
    }
}
