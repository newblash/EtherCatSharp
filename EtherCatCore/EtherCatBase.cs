using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace EtherCatSharp.EtherCatCore
{
    public partial class EtherCatCore
    {
        static int count = 0;
        /// <summary>
        /// 向 EtherCAT 数据报写入数据
        /// </summary>
        /// <param name="datagramdata">数据报的数据部分</param>
        /// <param name="com">命令</param>
        /// <param name="length">数据缓冲区长度</param>
        /// <param name="data">要复制到数据报的数据缓冲区</param>
        static void ecx_writedatagramdata(Span<byte> datagramdata, ushort com, int dstoffset, int srcoffset, ushort length, Span<byte> data)
        {
            if (length > 0)
            {
                //Console.Write($"序号:{count} 命令:{com} 数据:");

                //for (int i = srcoffset; i < srcoffset + length; i++)
                //{
                //    Console.Write($"0x{data[i]:X} ");
                //}
                //Console.Write("\n");
                //count++;
                switch (com)
                {
                    case EC_CMD_NOP:
                    case EC_CMD_APRD:
                    case EC_CMD_FPRD:
                    case EC_CMD_BRD:
                    case EC_CMD_LRD:
                        //无需写入数据。初始化数据
                        memset(datagramdata, ETH_HEADERSIZE + EC_HEADERSIZE, length);
                        break;
                    default:
                        //如果是其他命令，则将数据复制到数据报中。
                        memcpy(datagramdata, dstoffset, data, srcoffset, length);
                        break;
                }
            }
        }
        /// <summary>
        /// 在标准以太网帧中生成并设置 EtherCAT 数据报。
        /// </summary>
        /// <param name="frame">帧缓冲区</param>
        /// <param name="com">命令</param>
        /// <param name="idx">用于 TX 和 RX 缓冲区的索引</param>
        /// <param name="ADP">地址位置</param>
        /// <param name="ADO">地址偏移</param>
        /// <param name="length">数据报长度</param>
        /// <param name="data">要复制到数据报的数据缓冲区</param>
        /// <returns>始终为 0</returns>
        public int ecx_setupdatagram(ecx_portt port, Span<byte> frame, byte com, byte idx, ushort ADP, ushort ADO, ushort length, Span<byte> data)
        {
            ec_comt datagramP = frame.ToStruct<ec_comt>(ETH_HEADERSIZE);
            datagramP.elength = htoes((ushort)(EC_ECATTYPE + EC_HEADERSIZE + length));
            datagramP.command = com;
            datagramP.index = idx;
            datagramP.ADP = htoes(ADP);
            datagramP.ADO = htoes(ADO);
            datagramP.dlength = htoes(length);


            var bufft = datagramP.ToBytes();
            memcpy(frame, ETH_HEADERSIZE, bufft, 0, bufft.Length);

            ecx_writedatagramdata(frame, com, ETH_HEADERSIZE + EC_HEADERSIZE, 0, length, data);
            /* set WKC to zero */
            frame[ETH_HEADERSIZE + EC_HEADERSIZE + length] = 0x00;
            frame[ETH_HEADERSIZE + EC_HEADERSIZE + length + 1] = 0x00;
            /* set size of frame in buffer array */
            port.txbuflength[idx] = ETH_HEADERSIZE + EC_HEADERSIZE + EC_WKCSIZE + length;
            return 0;
        }
        /// <summary>
        /// 向已有以太网帧添加 EtherCAT 数据报（多数据报拼接）。
        /// </summary>
        /// <param name="frame">帧缓冲区</param>
        /// <param name="com">命令</param>
        /// <param name="idx">用于 TX 和 RX 缓冲区的索引</param>
        /// <param name="more">如果后续还有数据报则为 TRUE</param>
        /// <param name="ADP">地址位置</param>
        /// <param name="ADO">地址偏移</param>
        /// <param name="length">数据报长度（不包括 EtherCAT 头部）</param>
        /// <param name="data">要复制到数据报的数据缓冲区</param>
        /// <returns>返回 RX 帧中数据的偏移量，可用于接收后获取数据</returns>
        public ushort ecx_adddatagram(ecx_portt port, Span<byte> frame, byte com, byte idx, bool more, ushort ADP, ushort ADO, ushort length, Span<byte> data)
        {
            ushort prevlength = (ushort)port.txbuflength[idx];
            ec_comt datagramP = frame.ToStruct<ec_comt>(ETH_HEADERSIZE);

            /* add new datagram to ethernet frame size */
            datagramP.elength = htoes((ushort)(etohs(datagramP.elength) + EC_HEADERSIZE + length));
            /* add "datagram follows" flag to previous subframe dlength */
            datagramP.dlength = htoes((ushort)(etohs(datagramP.dlength) | EC_DATAGRAMFOLLOWS));
            /* set new EtherCAT header position */
            datagramP = frame.ToStruct<ec_comt>(ETH_HEADERSIZE - EC_ELENGTHSIZE);
            datagramP.command = com;
            datagramP.index = idx;
            datagramP.ADP = htoes(ADP);
            datagramP.ADO = htoes(ADO);
            if (more)
            {
                /* this is not the last datagram to add */
                datagramP.dlength = htoes((ushort)(length | EC_DATAGRAMFOLLOWS));
            }
            else
            {
                /* this is the last datagram in the frame */
                datagramP.dlength = htoes(length);
            }
            ecx_writedatagramdata(frame, com, prevlength + EC_HEADERSIZE - EC_ELENGTHSIZE, 0, length, data);
            /* set WKC to zero */
            frame[prevlength + EC_HEADERSIZE - EC_ELENGTHSIZE + length] = 0x00;
            frame[prevlength + EC_HEADERSIZE - EC_ELENGTHSIZE + length + 1] = 0x00;
            /* set size of frame in buffer array */
            port.txbuflength[idx] = prevlength + EC_HEADERSIZE - EC_ELENGTHSIZE + EC_WKCSIZE + length;
            /* return offset to data in rx frame
               14 bytes smaller than tx frame due to stripping of ethernet header */
            return (ushort)(prevlength + EC_HEADERSIZE - EC_ELENGTHSIZE - ETH_HEADERSIZE);
        }
        /// <summary>
        /// 执行 EtherCAT 广播写操作。
        /// </summary>
        /// <param name="ecPort">端口上下文结构体</param>
        /// <param name="ADP">地址位置，通常为 0</param>
        /// <param name="ADO">地址偏移，从站内存地址</param>
        /// <param name="length">数据缓冲区长度</param>
        /// <param name="data">要写入从站的数据缓冲区</param>
        /// <param name="timeout">超时时间（微秒），标准为 EC_TIMEOUTRET</param>
        /// <returns>工作计数器或 EC_NOFRAME</returns>
        public int ecx_BWR(ecx_portt port, ushort ADP, ushort ADO, ushort length, Span<byte> data, int timeout)
        {
            byte idx;
            int wkc;
            //获取空闲索引
            idx = ecx_getindex(port);
            //设置数据报
            ecx_setupdatagram(port, port.txbuf[idx], EC_CMD_BWR, idx, ADP, ADO, length, data);
            //发送数据并等待回应
            wkc = ecx_srconfirm(port, idx, timeout);
            ecx_setbufstat(port, idx, EC_BUF_EMPTY);
            return wkc;
        }
        /// <summary>
        /// 执行 EtherCAT 广播读操作。
        /// </summary>
        /// <param name="ecPort">端口上下文结构体</param>
        /// <param name="ADP">地址位置，通常为 0</param>
        /// <param name="ADO">地址偏移，从站内存地址</param>
        /// <param name="length">数据缓冲区长度</param>
        /// <param name="data">要写入从站的数据缓冲区</param>
        /// <param name="timeout">超时时间（微秒），标准为 EC_TIMEOUTRET</param>
        /// <returns>工作计数器或 EC_NOFRAME</returns>
        public int ecx_BRD(ecx_portt port, ushort ADP, ushort ADO, ushort length, Span<byte> data, int timeout)
        {
            byte idx;
            int wkc;
            //获取空闲索引
            idx = ecx_getindex(port);
            //设置数据报
            ecx_setupdatagram(port, port.txbuf[idx], EC_CMD_BRD, idx, ADP, ADO, length, data);
            //发送数据并等待回应
            wkc = ecx_srconfirm(port, idx, timeout);
            if (wkc > 0)
            {
                memcpy(data, 0, port.rxbuf[idx], EC_HEADERSIZE, length);
            }
            ecx_setbufstat(port, idx, EC_BUF_EMPTY);
            return wkc;
        }
        /// <summary>
        /// 自动递增读取
        /// </summary>
        /// <param name="ADP"></param>
        /// <param name="ADO"></param>
        /// <param name="length"></param>
        /// <param name="data"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public int ecx_APRD(ecx_portt port, ushort ADP, ushort ADO, ushort length, Span<byte> data, int timeout)
        {
            byte idx;
            int wkc;
            //获取空闲索引
            idx = ecx_getindex(port);
            //设置数据报
            ecx_setupdatagram(port, port.txbuf[idx], EC_CMD_APRD, idx, ADP, ADO, length, data);
            //发送数据并等待回应
            wkc = ecx_srconfirm(port, idx, timeout);
            if (wkc > 0)
            {
                memcpy(data, 0, port.rxbuf[idx], EC_HEADERSIZE, length);
            }
            ecx_setbufstat(port, idx, EC_BUF_EMPTY);
            return wkc;
        }

        /// <summary>
        /// 自动递增读取+多写入
        /// </summary>
        /// <param name="ADP"></param>
        /// <param name="ADO"></param>
        /// <param name="length"></param>
        /// <param name="data"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public int ecx_ARMW(ecx_portt port, ushort ADP, ushort ADO, ushort length, Span<byte> data, int timeout)
        {
            byte idx;
            int wkc;
            //获取空闲索引
            idx = ecx_getindex(port);
            //设置数据报
            ecx_setupdatagram(port, port.txbuf[idx], EC_CMD_APRD, idx, ADP, ADO, length, data);
            //发送数据并等待回应
            wkc = ecx_srconfirm(port, idx, timeout);
            if (wkc > 0)
            {
                memcpy(port.rxbuf[idx], EC_HEADERSIZE, data, 0, length);
            }
            ecx_setbufstat(port, idx, EC_BUF_EMPTY);
            return wkc;
        }
        /// <summary>
        /// 配置地址读取+多写入
        /// </summary>
        /// <param name="ADP"></param>
        /// <param name="ADO"></param>
        /// <param name="length"></param>
        /// <param name="data"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public int ecx_FRMW(ecx_portt port, ushort ADP, ushort ADO, ushort length, Span<byte> data, int timeout)
        {
            byte idx;
            int wkc;
            //获取空闲索引
            idx = ecx_getindex(port);
            //设置数据报
            ecx_setupdatagram(port, port.txbuf[idx], EC_CMD_FRMW, idx, ADP, ADO, length, data);
            //发送数据并等待回应
            wkc = ecx_srconfirm(port, idx, timeout);
            if (wkc > 0)
            {
                memcpy(port.rxbuf[idx], EC_HEADERSIZE, data, 0, length);
            }
            ecx_setbufstat(port, idx, EC_BUF_EMPTY);
            return wkc;
        }
        /// <summary>
        /// 自动递增读取,返回计数器
        /// </summary>
        /// <param name="ADP"></param>
        /// <param name="ADO"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public ushort ecx_APRDw(ecx_portt port, ushort ADP, ushort ADO, int timeout)
        {
            byte[] bytes = new byte[sizeof(ushort)];
            ecx_APRD(port, ADP, ADO, sizeof(ushort), bytes, timeout);
            return BitConverter.ToUInt16(bytes, 0);
        }
        /// <summary>
        /// 配置地址读取
        /// </summary>
        /// <param name="ADP"></param>
        /// <param name="ADO"></param>
        /// <param name="length"></param>
        /// <param name="data"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public int ecx_FPRD(ecx_portt port, ushort ADP, ushort ADO, ushort length, Span<byte> data, int timeout)
        {
            byte idx;
            int wkc;
            //获取空闲索引
            idx = ecx_getindex(port);
            //设置数据报
            ecx_setupdatagram(port, port.txbuf[idx], EC_CMD_FPRD, idx, ADP, ADO, length, data);
            //发送数据并等待回应
            wkc = ecx_srconfirm(port, idx, timeout);
            if (wkc > 0)
            {
                memcpy(data, 0, port.rxbuf[idx], EC_HEADERSIZE, length);
            }
            ecx_setbufstat(port, idx, EC_BUF_EMPTY);
            return wkc;
        }
        /// <summary>
        /// 配置地址读,返回计数器
        /// </summary>
        /// <param name="ADP"></param>
        /// <param name="ADO"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public ushort ecx_FPRDw(ecx_portt port, ushort ADP, ushort ADO, int timeout)
        {
            byte[] bytes = new byte[sizeof(ushort)];
            ecx_FPRD(port, ADP, ADO, sizeof(ushort), bytes, timeout);
            return BitConverter.ToUInt16(bytes, 0);
        }
        /// <summary>
        /// 自动递增地址写入
        /// </summary>
        /// <param name="ADP"></param>
        /// <param name="ADO"></param>
        /// <param name="length"></param>
        /// <param name="data"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public int ecx_APWR(ecx_portt port, ushort ADP, ushort ADO, ushort length, Span<byte> data, int timeout)
        {
            byte idx;
            int wkc;
            //获取空闲索引
            idx = ecx_getindex(port);
            //设置数据报
            ecx_setupdatagram(port, port.txbuf[idx], EC_CMD_APWR, idx, ADP, ADO, length, data);
            //发送数据并等待回应
            wkc = ecx_srconfirm(port, idx, timeout);
            ecx_setbufstat(port, idx, EC_BUF_EMPTY);
            return wkc;
        }
        /// <summary>
        /// 自动递增地址写入,返回计数器
        /// </summary>
        /// <param name="ADP"></param>
        /// <param name="ADO"></param>
        /// <param name="data"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public int ecx_APWRw(ecx_portt port, ushort ADP, ushort ADO, ushort data, int timeout)
        {
            return ecx_APWR(port, ADP, ADO, sizeof(ushort), BitConverter.GetBytes(data), timeout);
        }
        /// <summary>
        /// 配置地址写入
        /// </summary>
        /// <param name="ADP"></param>
        /// <param name="ADO"></param>
        /// <param name="length"></param>
        /// <param name="data"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public int ecx_FPWR(ecx_portt port, ushort ADP, ushort ADO, ushort length, Span<byte> data, int timeout)
        {
            byte idx;
            int wkc;
            //获取空闲索引
            idx = ecx_getindex(port);
            //设置数据报
            ecx_setupdatagram(port, port.txbuf[idx], EC_CMD_FPWR, idx, ADP, ADO, length, data);
            //发送数据并等待回应
            wkc = ecx_srconfirm(port, idx, timeout);
            ecx_setbufstat(port, idx, EC_BUF_EMPTY);
            return wkc;
        }
        /// <summary>
        /// 配置地址写入,返回计数器
        /// </summary>
        /// <param name="ADP"></param>
        /// <param name="ADO"></param>
        /// <param name="data"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public int ecx_FPWRw(ecx_portt port, ushort ADP, ushort ADO, ushort data, int timeout)
        {
            return ecx_FPWR(port, ADP, ADO, sizeof(ushort), BitConverter.GetBytes(data), timeout);
        }
        /// <summary>
        /// 逻辑读写
        /// </summary>
        /// <param name="LogAdr"></param>
        /// <param name="length"></param>
        /// <param name="data"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public int ecx_LRW(ecx_portt port, uint LogAdr, ushort length, Span<byte> data, int timeout)
        {
            byte idx;
            int wkc;
            //获取空闲索引
            idx = ecx_getindex(port);
            //设置数据报
            ecx_setupdatagram(port, port.txbuf[idx], EC_CMD_LRW, idx, LO_WORD(LogAdr), HI_WORD(LogAdr), length, data);
            //发送数据并等待回应
            wkc = ecx_srconfirm(port, idx, timeout);
            if (wkc > 0 && port.rxbuf[idx][EC_CMDOFFSET] == EC_CMD_LRW)
            {
                memcpy(port.rxbuf[idx], EC_HEADERSIZE, data, 0, length);
            }
            ecx_setbufstat(port, idx, EC_BUF_EMPTY);
            return wkc;
        }
        /// <summary>
        /// 逻辑读
        /// </summary>
        /// <param name="LogAdr"></param>
        /// <param name="length"></param>
        /// <param name="data"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public int ecx_LRD(ecx_portt port, uint LogAdr, ushort length, Span<byte> data, int timeout)
        {
            byte idx;
            int wkc;
            //获取空闲索引
            idx = ecx_getindex(port);
            //设置数据报
            ecx_setupdatagram(port, port.txbuf[idx], EC_CMD_LRD, idx, LO_WORD(LogAdr), HI_WORD(LogAdr), length, data);
            //发送数据并等待回应
            wkc = ecx_srconfirm(port, idx, timeout);
            if (wkc > 0 && port.rxbuf[idx][EC_CMDOFFSET] == EC_CMD_LRD)
            {
                memcpy(port.rxbuf[idx], EC_HEADERSIZE, data, 0, length);
            }
            ecx_setbufstat(port, idx, EC_BUF_EMPTY);
            return wkc;
        }
        /// <summary>
        /// 逻辑写
        /// </summary>
        /// <param name="LogAdr"></param>
        /// <param name="length"></param>
        /// <param name="data"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public int ecx_LWR(ecx_portt port, uint LogAdr, ushort length, Span<byte> data, int timeout)
        {
            byte idx;
            int wkc;
            //获取空闲索引
            idx = ecx_getindex(port);
            //设置数据报
            ecx_setupdatagram(port, port.txbuf[idx], EC_CMD_LWR, idx, LO_WORD(LogAdr), HI_WORD(LogAdr), length, data);
            //发送数据并等待回应
            wkc = ecx_srconfirm(port, idx, timeout);
            ecx_setbufstat(port, idx, EC_BUF_EMPTY);
            return wkc;
        }
        /// <summary>
        /// DC同步逻辑读写
        /// </summary>
        /// <param name="LogAdr"></param>
        /// <param name="length"></param>
        /// <param name="data"></param>
        /// <param name="DCrs"></param>
        /// <param name="DCtime"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public int LRWDC(ecx_portt port, uint LogAdr, ushort length, Span<byte> data, ushort DCrs, ref ulong DCtime, int timeout)
        {
            ushort DCtO;
            byte idx;
            int wkc;
            ulong DCtE;

            idx = ecx_getindex(port);
            ecx_setupdatagram(port, port.txbuf[idx], EC_CMD_LWR, idx, LO_WORD(LogAdr), HI_WORD(LogAdr), length, data);
            /* FPRMW in second datagram */
            DCtE = htoell(DCtime);
            DCtO = ecx_adddatagram(port, port.txbuf[idx], EC_CMD_FRMW, idx, false, DCrs, (ushort)ECT_REG_DCSYSTIME, sizeof(ulong), BitConverter.GetBytes(DCtE));
            wkc = ecx_srconfirm(port, idx, timeout);
            if ((wkc > 0) && (port.rxbuf[idx][EC_CMDOFFSET] == EC_CMD_LRW))
            {
                memcpy(port.rxbuf[idx], EC_HEADERSIZE, data, 0, length);
                byte[] wkcs = BitConverter.GetBytes(wkc);
                memcpy(wkcs, EC_HEADERSIZE + length, port.rxbuf[idx], 0, EC_WKCSIZE);
                byte[] DCtEs = BitConverter.GetBytes(DCtE);
                memcpy(DCtEs, DCtO, port.rxbuf[idx], 0, sizeof(ulong));
                DCtime = etohll(BitConverter.ToUInt64(DCtEs, 0));
                wkc = BitConverter.ToInt32(wkcs, 0);
            }
            ecx_setbufstat(port, idx, EC_BUF_EMPTY);
            return wkc;
        }
    }
}
