using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace EtherCatSharp.EtherCatCore
{
    public partial class EtherCatCore
    {
        /// <summary>
        /// standard ethercat mailbox header 
        /// 标准Ethercat邮箱头
        /// </summary>
        public struct ec_mbxheadert
        {
            public ushort length { get; set; }
            public ushort address { get; set; }
            public byte priority { get; set; }
            public byte mbxtype { get; set; }
        }
        public struct ec_emcyt
        {
            public ec_mbxheadert MbxHeader { get; set; }
            public ushort CANOpen { get; set; }
            public ushort ErrorCode { get; set; }
            public byte ErrorReg { get; set; }
            public byte bData { get; set; }
            public ushort w1 { get; set; }
            public ushort w2 { get; set; }
        }
        public struct ec_mbxerrort
        {
            public ec_mbxheadert MbxHeader { get; set; }
            public ushort Type { get; set; }
            public ushort Detail { get; set; }
        }

        //从从机旁路缓存读取EEPROM
        public void ecx_readeeprom1(ecx_contextt context, ushort slave, ushort eeproma)
        {
            ushort configadr, estat = 0;
            ec_eepromt ed = new ec_eepromt();
            int wkc, cnt = 0;

            ecx_eeprom2master(context, slave); /* set eeprom control to master */
            configadr = context.slavelist[slave].configadr;
            if (ecx_eeprom_waitnotbusyFP(context, configadr, ref estat, EC_TIMEOUTEEP) == 1)
            {
                if ((estat & EC_ESTAT_EMASK) > 0) /* error bits are set */
                {
                    estat = htoes(EC_ECMD_NOP); /* clear error bits */
                    wkc = ecx_FPWR(context.port, configadr, ECT_REG_EEPCTL, sizeof(ushort), BitConverter.GetBytes(estat).Reverse().ToArray(), EC_TIMEOUTRET3);
                }
                ed.comm = htoes(EC_ECMD_READ);
                ed.addr = htoes(eeproma);
                ed.d2 = 0x0000;
                var eds = ToBytes(ed);
                do
                {
                    wkc = ecx_FPWR(context.port, configadr, ECT_REG_EEPCTL, (ushort)eds.Length, eds, EC_TIMEOUTRET);
                }
                while ((wkc <= 0) && (cnt++ < EC_DEFAULTRETRIES));
            }
        }

        //从从机旁路缓存读取EEPROM。
        public uint ecx_readeeprom2(ecx_contextt context, ushort slave, int timeout)
        {
            ushort estat, configadr;
            uint edat;
            int wkc, cnt = 0;

            configadr = context.slavelist[slave].configadr;
            edat = 0;
            estat = 0x0000;
            byte[] edat_t = BitConverter.GetBytes(edat).Reverse().ToArray();
            if (ecx_eeprom_waitnotbusyFP(context, configadr, ref estat, timeout) == 1)
            {
                do
                {
                    wkc = ecx_FPRD(context.port, configadr, ECT_REG_EEPDAT, sizeof(uint), edat_t, EC_TIMEOUTRET);
                    edat = BitConverter.ToUInt32(edat_t, 0);
                }
                while ((wkc <= 0) && (cnt++ < EC_DEFAULTRETRIES));
            }

            return edat;
        }
        //将eeprom控制设为主机。仅当设置为PDI时。
        public int ecx_eeprom2master(ecx_contextt context, ushort slave)
        {
            int wkc = 1, cnt = 0;
            ushort configadr;
            byte eepctl;

            if (context.slavelist[slave].eep_pdi == 1)
            {
                configadr = context.slavelist[slave].configadr;
                eepctl = 2;
                do
                {
                    wkc = ecx_FPWR(context.port, configadr, ECT_REG_EEPCFG, sizeof(byte), BitConverter.GetBytes(eepctl), EC_TIMEOUTRET); /* force Eeprom from PDI */
                }
                while ((wkc <= 0) && (cnt++ < EC_DEFAULTRETRIES));
                eepctl = 0;
                cnt = 0;
                do
                {
                    wkc = ecx_FPWR(context.port, configadr, ECT_REG_EEPCFG, sizeof(byte), BitConverter.GetBytes(eepctl), EC_TIMEOUTRET); /* set Eeprom to master */
                }
                while ((wkc <= 0) && (cnt++ < EC_DEFAULTRETRIES));
                context.slavelist[slave].eep_pdi = 0;
            }

            return wkc;
        }
        public ushort ecx_eeprom_waitnotbusyFP(ecx_contextt context, ushort configadr, ref ushort estat, int timeout)
        {
            int wkc, cnt = 0;
            ushort retval = 0;
            osalTimer.Start(timeout);
            do
            {
                cnt++;
                osalTimer.osal_usleep(EC_LOCALDELAY);

                estat = 0;
                byte[] estat_t = BitConverter.GetBytes(estat).Reverse().ToArray();
                wkc = ecx_FPRD(context.port, configadr, ECT_REG_EEPSTAT, sizeof(ushort), estat_t, EC_TIMEOUTRET);
                estat = BitConverter.ToUInt16(estat_t, 0);
                estat = etohs(estat);
            }
            while (((wkc <= 0) || ((estat & 0x8000) > 0)) && (osalTimer.IsExpired() == false)); /* wait for eeprom ready */
            if ((estat & 0x8000) == 0)
            {
                retval = 1;
            }

            return retval;
        }

        /// <summary>
        /// 检查实际从机状态。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="slave"></param>
        /// <param name="reqstate"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public ushort ecx_statecheck(ecx_contextt context, ushort slave, ushort reqstate, int timeout)
        {
            ushort configadr, state, rval;
            ec_alstatust slstat = new ec_alstatust();

            if (slave > context.slavecount)
            {
                return 0;
            }
            OsalTimer timer = new OsalTimer();
            timer.Start(timeout);
            configadr = context.slavelist[slave].configadr;
            do
            {
                if (slave < 1)
                {
                    rval = 0;
                    byte[] rval_t = BitConverter.GetBytes(rval).Reverse().ToArray();
                    ecx_BRD(context.port, 0, ECT_REG_ALSTAT, sizeof(ushort), rval_t, EC_TIMEOUTRET);
                    rval = BitConverter.ToUInt16(rval_t, 0);
                    rval = etohs(rval);
                }
                else
                {
                    slstat.alstatus = 0;
                    slstat.alstatuscode = 0;
                    byte[] slstat_t = ToBytes(slstat);
                    ecx_FPRD(context.port, configadr, ECT_REG_ALSTAT, (ushort)slstat_t.Length, slstat_t, EC_TIMEOUTRET);
                    slstat = FromBytes<ec_alstatust>(slstat_t);
                    rval = etohs(slstat.alstatus);
                    context.slavelist[slave].ALstatuscode = etohs(slstat.alstatuscode);
                }
                //Console.WriteLine("rval=" + rval);
                state = (ushort)(rval & 0x000f); /* read slave status */
                //Console.WriteLine("state=" + state);
                if (state != reqstate)
                {
                    timer.osal_usleep(1000);
                }
            }
            while ((state != reqstate) && (timer.IsExpired() == false));//
            context.slavelist[slave].state = rval;
            return state;
        }
        public int ecx_readstate(ecx_contextt context)
        {
            ushort slave, fslave, lslave, configadr, lowest, rval, bitwisestate = 0;
            ec_alstatust[] sl = new ec_alstatust[MAX_FPRD_MULTI];
            ushort[] slca = new ushort[MAX_FPRD_MULTI];
            bool noerrorflag, allslavessamestate;
            bool allslavespresent = false;
            int wkc;

            /* Try to establish the state of all slaves sending only one broadcast datagram.
             * This way a number of datagrams equal to the number of slaves will be sent only if needed.*/
            rval = 0;
            byte[] bytes = BitConverter.GetBytes(rval);
            wkc = ecx_BRD(context.port, 0, ECT_REG_ALSTAT, sizeof(ushort), bytes, EC_TIMEOUTRET);
            rval = BitConverter.ToUInt16(bytes, 0);
            if (wkc >= context.slavecount)
            {
                allslavespresent = true;
            }

            rval = etohs(rval);
            bitwisestate = (ushort)(rval & 0x0f);

            if ((rval & EC_STATE_ERROR) == 0)
            {
                noerrorflag = true;
                context.slavelist[0].ALstatuscode = 0;
            }
            else
            {
                noerrorflag = false;
            }

            switch (bitwisestate)
            {
                /* Note: BOOT State collides with PRE_OP | INIT and cannot be used here */
                case EC_STATE_INIT:
                case EC_STATE_PRE_OP:
                case EC_STATE_SAFE_OP:
                case EC_STATE_OPERATIONAL:
                    allslavessamestate = true;
                    context.slavelist[0].state = bitwisestate;
                    break;
                default:
                    allslavessamestate = false;
                    break;
            }

            if (noerrorflag && allslavessamestate && allslavespresent)
            {
                /* No slave has toggled the error flag so the alstatuscode
                 * (even if different from 0) should be ignored and
                 * the slaves have reached the same state so the internal state
                 * can be updated without sending any datagram. */
                for (slave = 1; slave <= context.slavecount; slave++)
                {
                    context.slavelist[slave].ALstatuscode = 0x0000;
                    context.slavelist[slave].state = bitwisestate;
                }
                lowest = bitwisestate;
            }
            else
            {
                /* Not all slaves have the same state or at least one is in error so one datagram per slave
                 * is needed. */
                context.slavelist[0].ALstatuscode = 0;
                lowest = 0xff;
                fslave = 1;
                do
                {
                    lslave = (ushort)context.slavecount;
                    if ((lslave - fslave) >= MAX_FPRD_MULTI)
                    {
                        lslave = (ushort)(fslave + MAX_FPRD_MULTI - 1);
                    }
                    for (slave = fslave; slave <= lslave; slave++)
                    {
                        ec_alstatust zero = new ec_alstatust() { alstatus = 0, alstatuscode = 0, unused = 0 };

                        configadr = context.slavelist[slave].configadr;
                        slca[slave - fslave] = configadr;
                        sl[slave - fslave] = zero;
                    }
                    ecx_FPRD_multi(context, (lslave - fslave) + 1, slca, sl, EC_TIMEOUTRET3);
                    for (slave = fslave; slave <= lslave; slave++)
                    {
                        configadr = context.slavelist[slave].configadr;
                        rval = etohs(sl[slave - fslave].alstatus);
                        context.slavelist[slave].ALstatuscode = etohs(sl[slave - fslave].alstatuscode);
                        if ((rval & 0xf) < lowest)
                        {
                            lowest = (ushort)(rval & 0xf);
                        }
                        context.slavelist[slave].state = rval;
                        context.slavelist[0].ALstatuscode |= context.slavelist[slave].ALstatuscode;
                    }
                    fslave = (ushort)(lslave + 1);
                } while (lslave < context.slavecount);
                context.slavelist[0].state = lowest;
            }

            return lowest;
        }
        public int ecx_writestate(ecx_contextt context, ushort slave)
        {
            int ret;
            ushort configadr, slstate;

            if (slave == 0)
            {
                slstate = htoes(context.slavelist[slave].state);
                byte[] bytes = BitConverter.GetBytes(slstate);
                ret = ecx_BWR(context.port, 0, ECT_REG_ALCTL, (ushort)bytes.Length, bytes, EC_TIMEOUTRET3);
            }
            else
            {
                configadr = context.slavelist[slave].configadr;
                ret = ecx_FPWRw(context.port, configadr, ECT_REG_ALCTL, htoes(context.slavelist[slave].state), EC_TIMEOUTRET3);
            }
            return ret;
        }
        public int ecx_FPRD_multi(ecx_contextt context, int n, ushort[] configlst, ec_alstatust[] slstatlst, int timeout)
        {
            int wkc;
            byte idx;
            ushort[] sldatapos = new ushort[MAX_FPRD_MULTI];
            int slcnt;

            idx = ecx_getindex(context.port);
            slcnt = 0;
            var ECT = ToBytes(slstatlst[slcnt]);

            ecx_setupdatagram(context.port, context.port.txbuf[idx], EC_CMD_FPRD, idx, configlst[slcnt], ECT_REG_ALSTAT, (ushort)ECT.Length, ECT);

            sldatapos[slcnt] = (ushort)EC_HEADERSIZE;
            while (++slcnt < (n - 1))
            {
                ECT = ToBytes(slstatlst[slcnt]);
                sldatapos[slcnt] = ecx_adddatagram(context.port, context.port.txbuf[idx], EC_CMD_FPRD, idx, true, configlst[slcnt], ECT_REG_ALSTAT, (ushort)ECT.Length, ECT);
            }
            if (slcnt < n)
            {
                ECT = ToBytes(slstatlst[slcnt]);
                sldatapos[slcnt] = ecx_adddatagram(context.port, context.port.txbuf[idx], EC_CMD_FPRD, idx, false, configlst[slcnt], ECT_REG_ALSTAT, (ushort)ECT.Length, ECT);
            }
            wkc = ecx_srconfirm(context.port, idx, timeout);
            if (wkc >= 0)
            {
                for (slcnt = 0; slcnt < n; slcnt++)
                {
                    ECT = ToBytes(slstatlst[slcnt]);
                    memcpy(ECT, slcnt, context.port.rxbuf[idx], sldatapos[slcnt], (ushort)ECT.Length);
                    slstatlst[slcnt] = FromBytes<ec_alstatust>(ECT);
                }
            }
            ecx_setbufstat(context.port, idx, EC_BUF_EMPTY);
            return wkc;
        }
        public byte ec_nextmbxcnt(byte cnt)
        {
            cnt++;
            if (cnt > 7)
            {
                cnt = 1; /* wrap around to 1, not 0 */
            }

            return cnt;
        }
        public void ec_clearmbx(Span<byte> Mbx)
        {
            if (Mbx != null)
                memset(Mbx, 0x00, EC_MAXMBX);
        }
        public int ecx_clearmbxstatus(ecx_contextt context, byte group)
        {
            if (context.grouplist[group].mbxstatus != null && context.grouplist[group].mbxstatuslength > 0)
            {
                var mbx = context.grouplist[group].mbxstatus;
                memset(mbx, 0x00, context.grouplist[group].mbxstatuslength);
                context.grouplist[group].mbxstatus = mbx;
                return 1;
            }
            return 0;
        }
        public int ecx_readmbxstatusex(ecx_contextt context, ushort slave, ref ushort SMstatex)
        {
            ushort hu16 = 0;
            byte[] bytes = BitConverter.GetBytes(hu16);
            ushort configadr = context.slavelist[slave].configadr;
            int wkc = ecx_FPRD(context.port, configadr, ECT_REG_SM1STAT, sizeof(ushort), bytes, EC_TIMEOUTRET);
            hu16 = BitConverter.ToUInt16(bytes, 0);
            SMstatex = etohs(hu16);
            return wkc;
        }
        public int ecx_mbxempty(ecx_contextt context, ushort slave, int timeout)
        {
            ushort configadr;
            byte SMstat;
            int wkc;

            osalTimer.Start(timeout);
            configadr = context.slavelist[slave].configadr;
            do
            {
                SMstat = 0;
                byte[] bytes = BitConverter.GetBytes(SMstat);
                wkc = ecx_FPRD(context.port, configadr, ECT_REG_SM0STAT, sizeof(byte), bytes, EC_TIMEOUTRET);
                SMstat = bytes[0];
                if (((SMstat & 0x08) != 0) && (timeout > EC_LOCALDELAY))
                {
                    osalTimer.osal_usleep(EC_LOCALDELAY);
                }
            } while (((wkc <= 0) || ((SMstat & 0x08) != 0)) && (osalTimer.IsExpired() == false));

            if ((wkc > 0) && ((SMstat & 0x08) == 0))
            {
                return 1;
            }

            return 0;
        }

        // Set eeprom control to PDI. Only if set to master.
        public int ecx_eeprom2pdi(ecx_contextt context, ushort slave)
        {
            int wkc = 1, cnt = 0;
            ushort configadr;
            byte eepctl;

            if (context.slavelist[slave].eep_pdi != 1)
            {
                configadr = context.slavelist[slave].configadr;
                eepctl = 1;
                byte[] eepctl_t = BitConverter.GetBytes(eepctl);
                do
                {
                    wkc = ecx_FPWR(context.port, configadr, ECT_REG_EEPCFG, sizeof(byte), eepctl_t, EC_TIMEOUTRET); /* set Eeprom to PDI */
                }
                while ((wkc <= 0) && (cnt++ < EC_DEFAULTRETRIES));
                context.slavelist[slave].eep_pdi = 1;
            }
            return wkc;
        }
        // Get SM data from SII SM section in slave EEPROM.
        public ushort ecx_siiSM(ecx_contextt context, ushort slave, ec_eepromSMt SM)
        {
            ushort a, w;
            byte eectl = context.slavelist[slave].eep_pdi;

            SM.nSM = 0;
            SM.Startpos = (ushort)ecx_siifind(context, slave, ECT_SII_SM);
            if (SM.Startpos > 0)
            {
                a = SM.Startpos;
                w = ecx_siigetbyte(context, slave, a++);
                w += (ushort)(ecx_siigetbyte(context, slave, a++) << 8);
                SM.nSM = (byte)(w / 4);
                SM.PhStart = ecx_siigetbyte(context, slave, a++);
                SM.PhStart += (ushort)(ecx_siigetbyte(context, slave, a++) << 8);
                SM.Plength = ecx_siigetbyte(context, slave, a++);
                SM.Plength += (ushort)(ecx_siigetbyte(context, slave, a++) << 8);
                SM.Creg = ecx_siigetbyte(context, slave, a++);
                SM.Sreg = ecx_siigetbyte(context, slave, a++);
                SM.Activate = ecx_siigetbyte(context, slave, a++);
                SM.PDIctrl = ecx_siigetbyte(context, slave, a++);
            }
            if (eectl == 1)
            {
                ecx_eeprom2pdi(context, slave); /* if eeprom control was previously pdi then restore */
            }

            return SM.nSM;
        }
        // Read one byte from slave EEPROM via cache.
        public byte ecx_siigetbyte(ecx_contextt context, ushort slave, ushort address)
        {
            ushort configadr, eadr;
            ulong edat64;
            uint edat32;
            int mapw, mapb;
            int lp, cnt;
            byte retval;

            retval = 0xff;
            if (slave != context.esislave) /* not the same slave? */
            {
                context.esislave = slave;
            }
            if (address < EC_MAXEEPBUF)
            {
                mapw = (address >> 5);
                mapb = (address - (mapw << 5));
                if ((context.esimap[mapw] & (1U << mapb)) != 0)
                {
                    /* byte is already in buffer */
                    retval = context.esibuf[address];
                }
                else
                {
                    /* byte is not in buffer, put it there */
                    configadr = context.slavelist[slave].configadr;
                    ecx_eeprom2master(context, slave); /* set eeprom control to master */
                    eadr = (ushort)(address >> 1);
                    edat64 = ecx_readeepromFP(context, configadr, eadr, EC_TIMEOUTEEP);
                    /* 8 byte response */
                    if (context.slavelist[slave].eep_8byte == 1)
                    {
                        PutUnaligned64(edat64, context.esibuf, eadr << 1);
                        cnt = 8;
                    }
                    /* 4 byte response */
                    else
                    {
                        edat32 = (uint)edat64;
                        PutUnaligned32(edat32, context.esibuf, eadr << 1);
                        cnt = 4;
                    }
                    /* find bitmap location */
                    mapw = (eadr >> 4);
                    mapb = ((eadr << 1) - (mapw << 5));
                    for (lp = 0; lp < cnt; lp++)
                    {
                        /* set bitmap for each byte that is read */
                        context.esimap[mapw] |= (1U << mapb);
                        mapb++;
                        if (mapb > 31)
                        {
                            mapb = 0;
                            mapw++;
                        }
                    }
                    retval = context.esibuf[address];
                }
            }

            return retval;
        }
        public short ecx_siifind(ecx_contextt context, ushort slave, ushort cat)
        {
            short a;
            ushort p;
            byte eectl = context.slavelist[slave].eep_pdi;

            a = ECT_SII_START << 1;
            /* read first SII section category */
            p = ecx_siigetbyte(context, slave, (ushort)a);
            a++;
            p += (ushort)(ecx_siigetbyte(context, slave, (ushort)a) << 8);
            a++;
            /* traverse SII while category is not found and not EOF */
            while ((p != cat) && (p != 0xffff))
            {
                /* read section length */
                p = ecx_siigetbyte(context, slave, (ushort)a);
                a++;
                p += (ushort)(ecx_siigetbyte(context, slave, (ushort)a) << 8);
                a++;
                /* locate next section category */
                a += (short)(p << 1);
                /* read section category */
                p = ecx_siigetbyte(context, slave, (ushort)a);
                a++;
                p += (ushort)(ecx_siigetbyte(context, slave, (ushort)a) << 8);
                a++;
            }
            if (p != cat)
            {
                a = 0;
            }
            if (eectl == 1)
            {
                ecx_eeprom2pdi(context, slave); /* if eeprom control was previously pdi then restore */
            }

            return a;
        }
        public ulong ecx_readeepromFP(ecx_contextt context, ushort configadr, ushort eeproma, int timeout)
        {
            ushort estat = 0;
            uint edat32;
            ulong edat64;
            ec_eepromt ed = new ec_eepromt();
            int wkc, cnt, nackcnt = 0;

            edat64 = 0;
            edat32 = 0;
            if (ecx_eeprom_waitnotbusyFP(context, configadr, ref estat, timeout) == 1)
            {
                if ((estat & EC_ESTAT_EMASK) != 0) /* error bits are set */
                {
                    estat = htoes(EC_ECMD_NOP);
                    wkc = ecx_FPWR(context.port, configadr, ECT_REG_EEPCTL, sizeof(ushort), BitConverter.GetBytes(estat).Reverse().ToArray(), EC_TIMEOUTRET3);
                }

                do
                {
                    ed.comm = htoes(EC_ECMD_READ);
                    ed.addr = htoes(eeproma);
                    ed.d2 = 0x0000;
                    cnt = 0;
                    var eds = ToBytes(ed);
                    do
                    {
                        wkc = ecx_FPWR(context.port, configadr, ECT_REG_EEPCTL, (ushort)eds.Length, eds, EC_TIMEOUTRET);
                    }
                    while ((wkc <= 0) && (cnt++ < EC_DEFAULTRETRIES));
                    if (wkc == 1)
                    {
                        osalTimer.osal_usleep(200);
                        estat = 0x0000;
                        if (ecx_eeprom_waitnotbusyFP(context, configadr, ref estat, timeout) == 1)
                        {
                            if ((estat & 0x2000) != 0)
                            {
                                nackcnt++;
                                osalTimer.osal_usleep(200 * 5);
                            }
                            else
                            {
                                nackcnt = 0;
                                if ((estat & 0x0040) != 0)
                                {
                                    byte[] edat64_t = BitConverter.GetBytes(edat64).Reverse().ToArray();
                                    cnt = 0;
                                    do
                                    {
                                        wkc = ecx_FPRD(context.port, configadr, ECT_REG_EEPDAT, sizeof(ulong), edat64_t, EC_TIMEOUTRET);
                                        edat64 = BitConverter.ToUInt32(edat64_t, 0);
                                    }
                                    while ((wkc <= 0) && (cnt++ < EC_DEFAULTRETRIES));
                                }
                                else
                                {
                                    byte[] edat32_t = BitConverter.GetBytes(edat32).Reverse().ToArray();
                                    cnt = 0;
                                    do
                                    {
                                        wkc = ecx_FPRD(context.port, configadr, ECT_REG_EEPDAT, sizeof(uint), edat32_t, EC_TIMEOUTRET);
                                        edat32 = BitConverter.ToUInt32(edat32_t, 0);
                                    }
                                    while ((wkc <= 0) && (cnt++ < EC_DEFAULTRETRIES));
                                    edat64 = (ulong)edat32;
                                }
                            }
                        }
                    }
                }
                while ((nackcnt > 0) && (nackcnt < 3));
            }

            return edat64;
        }
        public ushort ecx_siiSMnext(ecx_contextt context, ushort slave, ec_eepromSMt SM, ushort n)
        {
            ushort a;
            ushort retVal = 0;
            byte eectl = context.slavelist[slave].eep_pdi;

            if (n < SM.nSM)
            {
                a = (ushort)(SM.Startpos + 2 + (n * 8));
                SM.PhStart = ecx_siigetbyte(context, slave, a++);
                SM.PhStart += (ushort)(ecx_siigetbyte(context, slave, a++) << 8);
                SM.Plength = ecx_siigetbyte(context, slave, a++);
                SM.Plength += (ushort)(ecx_siigetbyte(context, slave, a++) << 8);
                SM.Creg = ecx_siigetbyte(context, slave, a++);
                SM.Sreg = ecx_siigetbyte(context, slave, a++);
                SM.Activate = ecx_siigetbyte(context, slave, a++);
                SM.PDIctrl = ecx_siigetbyte(context, slave, a++);
                retVal = 1;
            }
            if (eectl == 1)
            {
                ecx_eeprom2pdi(context, slave); /* if eeprom control was previously pdi then restore */
            }

            return retVal;
        }
        public int ecx_siiPDO(ecx_contextt context, ushort slave, ec_eepromPDOt PDO, byte t)
        {
            ushort a, w, c, e, er, Size;
            byte eectl = context.slavelist[slave].eep_pdi;

            Size = 0;
            PDO.nPDO = 0;
            PDO.Length = 0;
            PDO.Index[1] = 0;
            for (c = 0; c < EC_MAXSM; c++) PDO.SMbitsize[c] = 0;
            if (t > 1)
                t = 1;
            PDO.Startpos = (ushort)ecx_siifind(context, slave, (ushort)(ECT_SII_PDO + t));
            if (PDO.Startpos > 0)
            {
                a = PDO.Startpos;
                w = ecx_siigetbyte(context, slave, a++);
                w += (ushort)(ecx_siigetbyte(context, slave, a++) << 8);
                PDO.Length = w;
                c = 1;
                /* traverse through all PDOs */
                do
                {
                    PDO.nPDO++;
                    PDO.Index[PDO.nPDO] = ecx_siigetbyte(context, slave, a++);
                    PDO.Index[PDO.nPDO] += (ushort)(ecx_siigetbyte(context, slave, a++) << 8);
                    PDO.BitSize[PDO.nPDO] = 0;
                    c++;
                    e = ecx_siigetbyte(context, slave, a++);
                    PDO.SyncM[PDO.nPDO] = ecx_siigetbyte(context, slave, a++);
                    a += 4;
                    c += 2;
                    if (PDO.SyncM[PDO.nPDO] < EC_MAXSM) /* active and in range SM? */
                    {
                        /* read all entries defined in PDO */
                        for (er = 1; er <= e; er++)
                        {
                            c += 4;
                            a += 5;
                            PDO.BitSize[PDO.nPDO] += ecx_siigetbyte(context, slave, a++);
                            a += 2;
                        }
                        PDO.SMbitsize[PDO.SyncM[PDO.nPDO]] += PDO.BitSize[PDO.nPDO];
                        Size += PDO.BitSize[PDO.nPDO];
                        c++;
                    }
                    else /* PDO deactivated because SM is 0xff or > EC_MAXSM */
                    {
                        c += (ushort)(4 * e);
                        a += (ushort)(8 * e);
                        c++;
                    }
                    if (PDO.nPDO >= (EC_MAXEEPDO - 1))
                    {
                        c = PDO.Length; /* limit number of PDO entries in buffer */
                    }
                }
                while (c < PDO.Length);
            }
            if (eectl > 0)
            {
                ecx_eeprom2pdi(context, slave); /* if eeprom control was previously pdi then restore */
            }

            return (Size);
        }

        public ushort ecx_siiFMMU(ecx_contextt context, ushort slave, ec_eepromFMMUt FMMU)
        {
            ushort a;
            byte eectl = context.slavelist[slave].eep_pdi;

            FMMU.nFMMU = 0;
            FMMU.FMMU0 = 0;
            FMMU.FMMU1 = 0;
            FMMU.FMMU2 = 0;
            FMMU.FMMU3 = 0;
            FMMU.Startpos = (ushort)ecx_siifind(context, slave, ECT_SII_FMMU);

            if (FMMU.Startpos > 0)
            {
                a = FMMU.Startpos;
                FMMU.nFMMU = ecx_siigetbyte(context, slave, a++);
                FMMU.nFMMU += (byte)(ecx_siigetbyte(context, slave, a++) << 8);
                FMMU.nFMMU *= 2;
                FMMU.FMMU0 = ecx_siigetbyte(context, slave, a++);
                FMMU.FMMU1 = ecx_siigetbyte(context, slave, a++);
                if (FMMU.nFMMU > 2)
                {
                    FMMU.FMMU2 = ecx_siigetbyte(context, slave, a++);
                    FMMU.FMMU3 = ecx_siigetbyte(context, slave, a++);
                }
            }
            if (eectl == 1)
            {
                ecx_eeprom2pdi(context, slave); /* if eeprom control was previously pdi then restore */
            }

            return FMMU.nFMMU;
        }

        public void ecx_siistring(ecx_contextt context, ref string str, ushort slave, ushort Sn)
        {
            ushort a, i, j, l, n, ba;
            byte eectl = context.slavelist[slave].eep_pdi;

            a = (ushort)ecx_siifind(context, slave, ECT_SII_STRING); /* find string section */
            if (a > 0)
            {
                ba = (ushort)(a + 2);                               /* skip SII section header */
                n = ecx_siigetbyte(context, slave, ba++); /* read number of strings in section */
                if (Sn <= n)                              /* is req string available? */
                {
                    for (i = 1; i <= Sn; i++) /* walk through strings */
                    {
                        l = ecx_siigetbyte(context, slave, ba++); /* length of this string */
                        if (i < Sn)
                        {
                            ba += l;
                        }
                        else
                        {
                            for (j = 1; j <= l; j++) /* copy one string */
                            {
                                if (j <= EC_MAXNAME)
                                {
                                    str += (char)ecx_siigetbyte(context, slave, ba++);
                                }
                                else
                                {
                                    ba++;
                                }
                            }
                        }
                    }
                }
                else
                {
                    str = ""; /* empty string */
                }
            }
            if (eectl == 1)
            {
                ecx_eeprom2pdi(context, slave); /* if eeprom control was previously pdi then restore */
            }
        }
        public int ecx_initmbxqueue(ecx_contextt context, byte group)
        {
            int retval = 0;
            int cnt;
            ec_mbxqueuet mbxqueue = context.grouplist[group].mbxtxqueue;
            mbxqueue.osal_mutext = new object();
            mbxqueue.listhead = 0;
            mbxqueue.listtail = 0;
            mbxqueue.listcount = 0;
            for (cnt = 0; cnt < EC_MBXPOOLSIZE; cnt++)
                mbxqueue.mbxticket[cnt] = -1;
            return retval;
        }
        public int ecx_initmbxpool(ecx_contextt context)
        {
            int retval = 0;
            ec_mbxpoolt mbxpool = context.mbxpool;
            mbxpool.mbxmutex = new object();
            for (int item = 0; item < EC_MBXPOOLSIZE; item++)
            {
                mbxpool.mbxemptylist[item] = item;
            }
            mbxpool.listhead = 0;
            mbxpool.listtail = 0;
            mbxpool.listcount = EC_MBXPOOLSIZE;
            return retval;
        }

        //不安全版本
        unsafe void ecx_esidump(ecx_contextt context, ushort slave, ref byte[] esibuf)
        {
            fixed (byte* esibuf_t = esibuf)
            {
                ushort configadr, address, incr;
                ulong* p64;
                ushort* p16;
                ulong edat;
                byte eectl = context.slavelist[slave].eep_pdi;

                ecx_eeprom2master(context, slave); /* set eeprom control to master */
                configadr = context.slavelist[slave].configadr;
                address = ECT_SII_START;
                p16 = (ushort*)esibuf_t;
                if (context.slavelist[slave].eep_8byte > 0)
                {
                    incr = 4;
                }
                else
                {
                    incr = 2;
                }
                do
                {
                    edat = ecx_readeepromFP(context, configadr, address, EC_TIMEOUTEEP);
                    p64 = (ulong*)p16;
                    *p64 = edat;
                    p16 += incr;
                    address += incr;
                } while ((address <= (EC_MAXEEPBUF >> 1)) && ((uint)edat != 0xffffffff));

                if (eectl > 0)
                {
                    ecx_eeprom2pdi(context, slave); /* if eeprom control was previously pdi then restore */
                }
            }
        }
        //安全版本
        public void ecx_esidump(ecx_contextt context, ushort slave, Span<byte> esibuf)
        {
            ushort configadr, address, incr;
            ulong edat;
            byte eectl = context.slavelist[slave].eep_pdi;
            ecx_eeprom2master(context, slave); /* set eeprom control to master */
            configadr = context.slavelist[slave].configadr;
            address = ECT_SII_START;
            if (context.slavelist[slave].eep_8byte > 0)
            {
                incr = 4;
            }
            else
            {
                incr = 2;
            }
            int byteOffset = 0;
            do
            {
                edat = ecx_readeepromFP(context, configadr, address, EC_TIMEOUTEEP);
                // 使用 Span 写入数据
                if (byteOffset + 8 <= esibuf.Length)
                {
                    // 将 ulong 写入 Span
                    MemoryMarshal.Write(esibuf.Slice(byteOffset), ref edat);
                }
                byteOffset += incr * 2; // 转换为字节偏移
                address += incr;
            } while ((address <= (EC_MAXEEPBUF >> 1)) && ((uint)edat != 0xffffffff));

            if (eectl > 0)
            {
                ecx_eeprom2pdi(context, slave); /* if eeprom control was previously pdi then restore */
            }

        }
        public uint ecx_readeeprom(ecx_contextt context, ushort slave, ushort eeproma, int timeout)
        {
            ushort configadr;

            ecx_eeprom2master(context, slave); /* set eeprom control to master */
            configadr = context.slavelist[slave].configadr;

            return ((uint)ecx_readeepromFP(context, configadr, eeproma, timeout));
        }
        public int ecx_writeeeprom(ecx_contextt context, ushort slave, ushort eeproma, ushort data, int timeout)
        {
            ushort configadr;

            ecx_eeprom2master(context, slave); /* set eeprom control to master */
            configadr = context.slavelist[slave].configadr;
            return (ecx_writeeepromFP(context, configadr, eeproma, data, timeout));
        }
        public ushort ecx_eeprom_waitnotbusyAP(ecx_contextt context, ushort aiadr, ref ushort estat, int timeout)
        {
            int wkc, cnt = 0;
            ushort retval = 0;
            osalTimer.Start(timeout);
            do
            {
                if (cnt++ > 0)
                {
                    osalTimer.osal_usleep(EC_LOCALDELAY);
                }
                estat = 0;
                byte[] bytes = BitConverter.GetBytes(estat);
                wkc = ecx_APRD(context.port, aiadr, ECT_REG_EEPSTAT, sizeof(ushort), bytes, EC_TIMEOUTRET);
                estat = BitConverter.ToUInt16(bytes, 0);
                estat = etohs(estat);
            } while (((wkc <= 0) || ((estat & EC_ESTAT_BUSY) > 0)) && (osalTimer.IsExpired() == false)); /* wait for eeprom ready */
            if ((estat & EC_ESTAT_BUSY) == 0)
            {
                retval = 1;
            }

            return retval;
        }
        public ulong ecx_readeepromAP(ecx_contextt context, ushort aiadr, ushort eeproma, int timeout)
        {
            ushort estat = 0;
            uint edat32 = 0;
            ulong edat64 = 0;
            ec_eepromt ed = new ec_eepromt();
            int wkc, cnt, nackcnt = 0;

            if (ecx_eeprom_waitnotbusyAP(context, aiadr, ref estat, timeout) > 0)
            {
                if ((estat & EC_ESTAT_EMASK) > 0) /* error bits are set */
                {
                    estat = htoes(EC_ECMD_NOP); /* clear error bits */
                    byte[] bytes = BitConverter.GetBytes(estat);
                    wkc = ecx_APWR(context.port, aiadr, ECT_REG_EEPCTL, sizeof(ushort), bytes, EC_TIMEOUTRET3);
                }

                do
                {
                    ed.comm = htoes(EC_ECMD_READ);
                    ed.addr = htoes(eeproma);
                    ed.d2 = 0x0000;
                    cnt = 0;
                    var eds = ToBytes(ed);
                    do
                    {
                        wkc = ecx_APWR(context.port, aiadr, ECT_REG_EEPCTL, (ushort)eds.Length, eds, EC_TIMEOUTRET);
                    } while ((wkc <= 0) && (cnt++ < EC_DEFAULTRETRIES));
                    if (wkc > 0)
                    {
                        osalTimer.osal_usleep(EC_LOCALDELAY);
                        estat = 0x0000;

                        if (ecx_eeprom_waitnotbusyAP(context, aiadr, ref estat, timeout) > 0)
                        {
                            if ((estat & EC_ESTAT_NACK) > 0)
                            {
                                nackcnt++;
                                osalTimer.osal_usleep(EC_LOCALDELAY * 5);
                            }
                            else
                            {
                                nackcnt = 0;
                                if ((estat & EC_ESTAT_R64) > 0)
                                {
                                    cnt = 0;
                                    byte[] bytes = BitConverter.GetBytes(edat64);
                                    do
                                    {
                                        wkc = ecx_APRD(context.port, aiadr, ECT_REG_EEPDAT, sizeof(ulong), bytes, EC_TIMEOUTRET);
                                        edat64 = BitConverter.ToUInt64(bytes, 0);
                                    } while ((wkc <= 0) && (cnt++ < EC_DEFAULTRETRIES));
                                }
                                else
                                {
                                    cnt = 0;
                                    byte[] bytes = BitConverter.GetBytes(edat32);
                                    do
                                    {
                                        wkc = ecx_APRD(context.port, aiadr, ECT_REG_EEPDAT, sizeof(uint), bytes, EC_TIMEOUTRET);
                                        edat32 = BitConverter.ToUInt32(bytes, 0);
                                    } while ((wkc <= 0) && (cnt++ < EC_DEFAULTRETRIES));
                                    edat64 = edat32;
                                }
                            }
                        }
                    }
                } while ((nackcnt > 0) && (nackcnt < 3));
            }

            return edat64;
        }
        public int ecx_writeeepromAP(ecx_contextt context, ushort aiadr, ushort eeproma, ushort data, int timeout)
        {
            ushort estat = 0;
            ec_eepromt ed = new ec_eepromt();
            int wkc, rval = 0, cnt = 0, nackcnt = 0;

            if (ecx_eeprom_waitnotbusyAP(context, aiadr, ref estat, timeout) > 0)
            {
                if ((estat & EC_ESTAT_EMASK) > 0) /* error bits are set */
                {
                    estat = htoes(EC_ECMD_NOP); /* clear error bits */
                    byte[] bytes = BitConverter.GetBytes(estat);
                    wkc = ecx_APWR(context.port, aiadr, ECT_REG_EEPCTL, sizeof(ushort), bytes, EC_TIMEOUTRET3);
                }
                do
                {
                    cnt = 0;
                    byte[] bytes = BitConverter.GetBytes(data);
                    do
                    {
                        wkc = ecx_APWR(context.port, aiadr, ECT_REG_EEPDAT, sizeof(ushort), bytes, EC_TIMEOUTRET);
                    } while ((wkc <= 0) && (cnt++ < EC_DEFAULTRETRIES));

                    ed.comm = EC_ECMD_WRITE;
                    ed.addr = eeproma;
                    ed.d2 = 0x0000;
                    cnt = 0;
                    var eds = ToBytes(ed);
                    do
                    {
                        wkc = ecx_APWR(context.port, aiadr, ECT_REG_EEPCTL, (ushort)eds.Length, eds, EC_TIMEOUTRET);

                    } while ((wkc <= 0) && (cnt++ < EC_DEFAULTRETRIES));
                    if (wkc > 0)
                    {
                        osalTimer.osal_usleep(EC_LOCALDELAY * 2);
                        estat = 0x0000;
                        if (ecx_eeprom_waitnotbusyAP(context, aiadr, ref estat, timeout) > 0)
                        {
                            if ((estat & EC_ESTAT_NACK) > 0)
                            {
                                nackcnt++;
                                osalTimer.osal_usleep(EC_LOCALDELAY * 25);
                            }
                            else
                            {
                                nackcnt = 0;
                                rval = 1;
                            }
                        }
                    }

                } while ((nackcnt > 0) && (nackcnt < 3));
            }

            return rval;
        }
        public int ecx_writeeepromFP(ecx_contextt context, ushort configadr, ushort eeproma, ushort data, int timeout)
        {
            ushort estat = 0;
            ec_eepromt ed = new ec_eepromt();
            int wkc, rval = 0, cnt = 0, nackcnt = 0;

            if (ecx_eeprom_waitnotbusyFP(context, configadr, ref estat, timeout) > 0)
            {
                if ((estat & EC_ESTAT_EMASK) > 0) /* error bits are set */
                {
                    estat = htoes(EC_ECMD_NOP); /* clear error bits */
                    byte[] bytes = BitConverter.GetBytes(estat);
                    wkc = ecx_FPWR(context.port, configadr, ECT_REG_EEPCTL, sizeof(ushort), bytes, EC_TIMEOUTRET3);
                }
                do
                {
                    cnt = 0;
                    byte[] bytes = BitConverter.GetBytes(data);
                    do
                    {
                        wkc = ecx_FPWR(context.port, configadr, ECT_REG_EEPDAT, sizeof(ushort), bytes, EC_TIMEOUTRET);
                    } while ((wkc <= 0) && (cnt++ < EC_DEFAULTRETRIES));
                    ed.comm = EC_ECMD_WRITE;
                    ed.addr = eeproma;
                    ed.d2 = 0x0000;
                    cnt = 0;
                    var eds = ToBytes(ed);
                    do
                    {
                        wkc = ecx_FPWR(context.port, configadr, ECT_REG_EEPCTL, (ushort)eds.Length, eds, EC_TIMEOUTRET);
                    } while ((wkc <= 0) && (cnt++ < EC_DEFAULTRETRIES));
                    if (wkc > 0)
                    {
                        osalTimer.osal_usleep(EC_LOCALDELAY * 2);
                        estat = 0x0000;
                        if (ecx_eeprom_waitnotbusyFP(context, configadr, ref estat, timeout) > 0)
                        {
                            if ((estat & EC_ESTAT_NACK) > 0)
                            {
                                nackcnt++;
                                osalTimer.osal_usleep(EC_LOCALDELAY * 5);
                            }
                            else
                            {
                                nackcnt = 0;
                                rval = 1;
                            }
                        }
                    }
                } while ((nackcnt > 0) && (nackcnt < 3));
            }

            return rval;
        }
        public void ecx_pushindex(ecx_contextt context, byte idx, Span<byte> data, ushort length, ushort DCO)
        {
            if (context.idxstack.pushed < EC_MAXBUF)
            {
                context.idxstack.idx[context.idxstack.pushed] = idx;
                unsafe
                {
                    fixed (byte* dataPtr = data)
                    {
                        context.idxstack.data[context.idxstack.pushed] = (byte)(IntPtr)dataPtr;
                    }
                }
                context.idxstack.length[context.idxstack.pushed] = (ushort)data.Length;
                context.idxstack.dcoffset[context.idxstack.pushed] = DCO;
                context.idxstack.pushed++;

            }
        }
        public int ecx_pullindex(ecx_contextt context)
        {
            int rval = -1;
            if (context.idxstack.pulled < context.idxstack.pushed)
            {
                rval = context.idxstack.pulled;
                context.idxstack.pulled++;
            }
            return rval;
        }
        public void ecx_clearindex(ecx_contextt context)
        {
            context.idxstack.pushed = 0;
            context.idxstack.pulled = 0;
        }
        public void ecx_packeterror(ecx_contextt context, ushort Slave, ushort Index, byte SubIdx, ushort ErrorCode)
        {
            ec_errort Ec = new ec_errort();
            Ec.Time = new ec_timet() { sec = (uint)DateTime.Now.Second, usec = (uint)((DateTime.Now.Ticks / 10) % 1_000_000) };
            Ec.Slave = Slave;
            Ec.Index = Index;
            Ec.SubIdx = SubIdx;
            context.ecaterror = true;
            Ec.Etype = EC_ERR_TYPE_PACKET_ERROR;
            Ec.ErrorCode = ErrorCode;
            ecx_pusherror(context, Ec);
        }
        public void ecx_pusherror(ecx_contextt context, ec_errort Ec)
        {
            context.elist.Error[context.elist.head] = Ec;
            context.elist.Error[context.elist.head].Signal = 1;
            context.elist.head++;
            if (context.elist.head > EC_MAXELIST)
            {
                context.elist.head = 0;
            }
            if (context.elist.head == context.elist.tail)
            {
                context.elist.tail++;
            }
            if (context.elist.tail > EC_MAXELIST)
            {
                context.elist.tail = 0;
            }
            (context.ecaterror) = true;
        }
        public void ecx_mbxerror(ecx_contextt context, ushort Slave, ushort Detail)
        {
            ec_errort Ec = new ec_errort();
            Ec.Time = new ec_timet() { sec = (uint)DateTime.Now.Second, usec = (uint)((DateTime.Now.Ticks / 10) % 1_000_000) };
            Ec.Slave = Slave;
            Ec.Index = 0;
            Ec.SubIdx = 0;
            Ec.Etype = EC_ERR_TYPE_MBX_ERROR;
            Ec.ErrorCode = Detail;
            ecx_pusherror(context, Ec);
        }
        public void ecx_mbxemergencyerror(ecx_contextt context, ushort Slave, ushort ErrorCode, ushort ErrorReg, byte b1, ushort w1, ushort w2)
        {
            ec_errort Ec = new ec_errort();
            Ec.Time = new ec_timet() { sec = (uint)DateTime.Now.Second, usec = (uint)((DateTime.Now.Ticks / 10) % 1_000_000) };
            Ec.Slave = Slave;
            Ec.Index = 0;
            Ec.SubIdx = 0;
            Ec.Etype = EC_ERR_TYPE_EMERGENCY;
            Ec.ErrorCode = ErrorCode;
            Ec.ErrorReg = (byte)ErrorReg;
            Ec.b1 = b1;
            Ec.w1 = w1;
            Ec.w2 = w2;
            ecx_pusherror(context, Ec);
        }
        public int ecx_mbxreceive(ecx_contextt context, ushort slave, Span<byte> mbx, int timeout)
        {
            ushort mbxro, mbxl, configadr;
            int wkc = 0;
            int wkc2;
            ushort SMstat;
            byte SMcontr = 0;
            ec_mbxheadert mbxh;
            ec_emcyt EMp;
            ec_mbxerrort MBXEp;

            configadr = context.slavelist[slave].configadr;
            mbxl = context.slavelist[slave].mbx_rl;
            if ((mbxl > 0) && (mbxl <= EC_MAXMBX))
            {
                OsalTimer timer = new OsalTimer();
                timer.Start(timeout);
                wkc = 0;
                do /* wait for read mailbox available */
                {
                    SMstat = 0;
                    byte[] bytes = BitConverter.GetBytes(SMstat);
                    wkc = ecx_FPRD(context.port, configadr, ECT_REG_SM1STAT, sizeof(ushort), bytes, EC_TIMEOUTRET);
                    SMstat = BitConverter.ToUInt16(bytes, 0);
                    SMstat = etohs(SMstat);
                    if (((SMstat & 0x08) == 0) && (timeout > EC_LOCALDELAY))
                    {
                        timer.osal_usleep(EC_LOCALDELAY);
                    }
                }
                while (((wkc <= 0) || ((SMstat & 0x08) == 0)) && (timer.IsExpired() == false));//

                if ((wkc > 0) && ((SMstat & 0x08) > 0)) /* read mailbox available ? */
                {
                    mbxro = context.slavelist[slave].mbx_ro;
                    mbxh = mbx.ToStruct<ec_mbxheadert>();
                    do
                    {
                        wkc = ecx_FPRD(context.port, configadr, mbxro, mbxl, mbx, EC_TIMEOUTRET); /* get mailbox */
                        if ((wkc > 0) && ((mbxh.mbxtype & 0x0f) == 0x00)) /* Mailbox error response? */
                        {

                            MBXEp = mbx.ToStruct<ec_mbxerrort>();
                            ecx_mbxerror(context, slave, etohs(MBXEp.Detail));
                            wkc = 0; /* prevent emergency to cascade up, it is already handled. */
                        }
                        else if ((wkc > 0) && ((mbxh.mbxtype & 0x0f) == ECT_MBXT_COE)) /* CoE response? */
                        {
                            EMp = mbx.ToStruct<ec_emcyt>();
                            if ((etohs(EMp.CANOpen) >> 12) == 0x01) /* Emergency request? */
                            {
                                ecx_mbxemergencyerror(context, slave, etohs(EMp.ErrorCode), EMp.ErrorReg,
                                        EMp.bData, etohs(EMp.w1), etohs(EMp.w2));
                                wkc = 0; /* prevent emergency to cascade up, it is already handled. */
                            }
                        }
                        //不管EOE协议
                        //else if ((wkc > 0) && ((mbxh.mbxtype & 0x0f) == ECT_MBXT_EOE)) /* EoE response? */
                        //{
                        //    ec_EOEt* eoembx = (ec_EOEt*)mbx;
                        //    ushort frameinfo1 = etohs(eoembx.frameinfo1);
                        //    /* All non fragment data frame types are expected to be handled by
                        //    * slave send/receive API if the EoE hook is set
                        //    */
                        //    if (EOE_HDR_FRAME_TYPE_GET(frameinfo1) == EOE_FRAG_DATA)
                        //    {
                        //        if (context->EOEhook)
                        //        {
                        //            if (context->EOEhook(context, slave, eoembx) > 0)
                        //            {
                        //                /* Fragment handled by EoE hook */
                        //                wkc = 0;
                        //            }
                        //        }
                        //    }
                        //}
                        else
                        {
                            if (wkc <= 0) /* read mailbox lost */
                            {
                                SMstat ^= 0x0200; /* toggle repeat request */
                                SMstat = htoes(SMstat);

                                byte[] bytes = BitConverter.GetBytes(SMstat);
                                wkc = ecx_FPRD(context.port, configadr, ECT_REG_SM1STAT, sizeof(ushort), bytes, EC_TIMEOUTRET);
                                SMstat = BitConverter.ToUInt16(bytes, 0);

                                SMstat = etohs(SMstat);
                                do /* wait for toggle ack */
                                {
                                    bytes = BitConverter.GetBytes(SMcontr);
                                    wkc2 = ecx_FPRD(context.port, configadr, ECT_REG_SM1CONTR, sizeof(byte), bytes, EC_TIMEOUTRET);
                                    SMcontr = bytes[0];
                                } while (((wkc2 <= 0) || ((SMcontr & 0x02) != (HI_BYTE(SMstat) & 0x02))) && (timer.IsExpired() == false));
                                do /* wait for read mailbox available */
                                {
                                    bytes = BitConverter.GetBytes(SMstat);
                                    wkc2 = ecx_FPRD(context.port, configadr, ECT_REG_SM1STAT, sizeof(ushort), bytes, EC_TIMEOUTRET);
                                    SMstat = BitConverter.ToUInt16(bytes, 0);
                                    SMstat = etohs(SMstat);
                                    if (((SMstat & 0x08) == 0) && (timeout > EC_LOCALDELAY))
                                    {
                                        timer.osal_usleep(EC_LOCALDELAY);
                                    }
                                } while (((wkc2 <= 0) || ((SMstat & 0x08) == 0)) && (timer.IsExpired() == false));
                            }
                        }
                    } while ((wkc <= 0) && (timer.IsExpired() == false)); /* if WKC<=0 repeat */
                }
                else /* no read mailbox available */
                {
                    if (wkc > 0)
                        wkc = EC_TIMEOUT;
                }
            }

            return wkc;
        }
        int ecx_mbxsend(ecx_contextt context, ushort slave, Span<byte> mbx, int timeout)
        {
            ushort mbxwo, mbxl, configadr;
            int wkc;

            wkc = 0;
            configadr = context.slavelist[slave].configadr;
            mbxl = context.slavelist[slave].mbx_l;
            if ((mbxl > 0) && (mbxl <= EC_MAXMBX))
            {
                if (ecx_mbxempty(context, slave, timeout) > 0)
                {
                    mbxwo = context.slavelist[slave].mbx_wo;
                    /* write slave in mailbox */
                    wkc = ecx_FPWR(context.port, configadr, mbxwo, mbxl, mbx, EC_TIMEOUTRET3);
                }
                else
                {
                    wkc = 0;
                }
            }

            return wkc;
        }
    }
}
