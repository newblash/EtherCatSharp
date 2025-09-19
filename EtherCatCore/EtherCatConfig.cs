using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace EtherCatSharp.EtherCatCore
{
    public partial class EtherCatCore
    {
        public void ecx_init_context(ecx_contextt context)
        {
            int lp;
            context.slavecount = 0;
            /* clear slave eeprom cache, does not actually read any eeprom */
            ecx_siigetbyte(context, 0, EC_MAXEEPBUF);
            for (lp = 0; lp < EC_MAXGROUP; lp++)
            {
                /* default start address per group entry */
                context.grouplist[lp].logstartaddr = (uint)(lp << EC_LOGGROUPOFFSET);
                //ecx_initmbxqueue(context, (byte)lp);
            }
        }

        /// <summary>
        /// 查找从站
        /// </summary>
        /// <returns></returns>
        public int ecx_detect_slaves(ecx_contextt context)
        {
            byte b;
            ushort w;
            int wkc;

            //使特殊的预初始化寄存器写入
            b = 0x00;
            //忽略别名寄存器
            ecx_BWR(context.port, 0x0000, ECT_REG_DLALIAS, sizeof(byte), [b], EC_TIMEOUTRET3);
            w = (EC_STATE_INIT | EC_STATE_ACK);
            byte[] bytes = BitConverter.GetBytes(w);
            //复位所有从站为init状态
            ecx_BWR(context.port, 0x0000, ECT_REG_ALCTL, sizeof(ushort), bytes, EC_TIMEOUTRET3);
            //重复复位所有从站为init状态
            ecx_BWR(context.port, 0x0000, ECT_REG_ALCTL, sizeof(ushort), bytes, EC_TIMEOUTRET3);
            //查找从站数量
            wkc = ecx_BRD(context.port, 0x0000, ECT_REG_TYPE, sizeof(ushort), bytes, EC_TIMEOUTSAFE);
            if (wkc > 0)
            {
                if (wkc > EC_MAXSLAVE)
                {
                    return EC_SLAVECOUNTEXCEEDED;
                }
                else
                {
                    context.slavecount = wkc;
                }
            }
            return wkc;
        }
        /// <summary>
        /// 设置所有从站为默认状态
        /// </summary>
        public void ecx_set_slaves_to_default(ecx_contextt context)
        {
            byte[] b;
            ushort w;
            byte[] zbuf = new byte[64];
            b = [0x00];
            ecx_BWR(context.port, 0x0000, ECT_REG_DLPORT, sizeof(byte), b, EC_TIMEOUTRET3);     /* deact loop manual */
            w = htoes(0x0004);
            ecx_BWR(context.port, 0x0000, ECT_REG_IRQMASK, sizeof(ushort), BitConverter.GetBytes(w), EC_TIMEOUTRET3);     /* set IRQ mask */
            ecx_BWR(context.port, 0x0000, ECT_REG_RXERR, 8, zbuf, EC_TIMEOUTRET3);  /* reset CRC counters */
            ecx_BWR(context.port, 0x0000, ECT_REG_FMMU0, 16 * 3, zbuf, EC_TIMEOUTRET3);  /* reset FMMU's */
            ecx_BWR(context.port, 0x0000, ECT_REG_SM0, 8 * 4, zbuf, EC_TIMEOUTRET3);  /* reset SyncM */
            b = [0x00];
            ecx_BWR(context.port, 0x0000, ECT_REG_DCSYNCACT, sizeof(byte), b, EC_TIMEOUTRET3);     /* reset activation register */
            ecx_BWR(context.port, 0x0000, ECT_REG_DCSYSTIME, 4, zbuf, EC_TIMEOUTRET3);  /* reset system time+ofs */
            w = htoes(0x1000);
            ecx_BWR(context.port, 0x0000, ECT_REG_DCSPEEDCNT, sizeof(ushort), BitConverter.GetBytes(w), EC_TIMEOUTRET3);     /* DC speedstart */
            w = htoes(0x0c00);
            ecx_BWR(context.port, 0x0000, ECT_REG_DCTIMEFILT, sizeof(ushort), BitConverter.GetBytes(w), EC_TIMEOUTRET3);     /* DC filt expr */
            b = [0x00];
            ecx_BWR(context.port, 0x0000, ECT_REG_DLALIAS, sizeof(byte), b, EC_TIMEOUTRET3);     /* Ignore Alias register */
            w = (ushort)(EC_STATE_INIT | EC_STATE_ACK);
            ecx_BWR(context.port, 0x0000, ECT_REG_ALCTL, sizeof(ushort), BitConverter.GetBytes(w), EC_TIMEOUTRET3);     /* Reset all slaves to Init */
            b = [0x02];
            ecx_BWR(context.port, 0x0000, ECT_REG_EEPCFG, sizeof(byte), b, EC_TIMEOUTRET3);     /* force Eeprom from PDI */
            b = [0x00];
            ecx_BWR(context.port, 0x0000, ECT_REG_EEPCFG, sizeof(byte), b, EC_TIMEOUTRET3);     /* set Eeprom to master */
        }
        public int ecx_lookup_prev_sii(ecx_contextt context, ushort slave)
        {
            int i, nSM;
            if ((slave > 1) && (context.slavecount > 0))
            {
                i = 1;
                while (((context.slavelist[i].eep_man != context.slavelist[slave].eep_man) ||
                        (context.slavelist[i].eep_id != context.slavelist[slave].eep_id) ||
                        (context.slavelist[i].eep_rev != context.slavelist[slave].eep_rev)) &&
                       (i < slave))
                {
                    i++;
                }
                if (i < slave)
                {
                    context.slavelist[slave].CoEdetails = context.slavelist[i].CoEdetails;
                    context.slavelist[slave].FoEdetails = context.slavelist[i].FoEdetails;
                    context.slavelist[slave].EoEdetails = context.slavelist[i].EoEdetails;
                    context.slavelist[slave].SoEdetails = context.slavelist[i].SoEdetails;
                    if (context.slavelist[i].blockLRW > 0)
                    {
                        context.slavelist[slave].blockLRW = 1;
                        context.slavelist[0].blockLRW++;
                    }
                    context.slavelist[slave].Ebuscurrent = context.slavelist[i].Ebuscurrent;
                    context.slavelist[0].Ebuscurrent += context.slavelist[slave].Ebuscurrent;
                    context.slavelist[slave].name = context.slavelist[i].name;
                    for (nSM = 0; nSM < EC_MAXSM; nSM++)
                    {
                        context.slavelist[slave].SM[nSM].StartAddr = context.slavelist[i].SM[nSM].StartAddr;
                        context.slavelist[slave].SM[nSM].SMlength = context.slavelist[i].SM[nSM].SMlength;
                        context.slavelist[slave].SM[nSM].SMflags = context.slavelist[i].SM[nSM].SMflags;
                    }
                    context.slavelist[slave].FMMU0func = context.slavelist[i].FMMU0func;
                    context.slavelist[slave].FMMU1func = context.slavelist[i].FMMU1func;
                    context.slavelist[slave].FMMU2func = context.slavelist[i].FMMU2func;
                    context.slavelist[slave].FMMU3func = context.slavelist[i].FMMU3func;
                    Debug.WriteLine("Copy SII slave %d from %d.\n", slave, i);
                    return 1;
                }
            }
            return 0;
        }
        /// <summary>
        /// 初始化配置
        /// </summary>
        /// <param name="usetable"></param>
        /// <returns></returns>
        public int ecx_config_init(ecx_contextt context, bool usetable)
        {
            ushort slave, ADPh, configadr, ssigen = 0;
            ushort topology, estat = 0;
            short topoc, slavec, aliasadr = 0;
            byte b, h;
            byte SMc;
            uint eedat = 0;
            int wkc, nSM;
            ushort val16;
            ecx_init_context(context);
            wkc = ecx_detect_slaves(context);//查找从站数量
            if (wkc > 0)
            {
                //创建从站列表
                context.slavelist = Enumerable.Range(0, wkc + 1).Select(_ => new ec_slavet()).ToArray();
                //context.slavelist = new ec_slavet[wkc + 1];
                ecx_set_slaves_to_default(context);
                for (slave = 1; slave <= context.slavecount; slave++)
                {
                    ADPh = (ushort)(1 - slave);
                    //Console.WriteLine("ADPh:" + ADPh);
                    //读取从机的接口类型
                    val16 = ecx_APRDw(context.port, ADPh, ECT_REG_PDICTL, EC_TIMEOUTRET3);
                    context.slavelist[slave].Itype = etohs(val16);
                    /*节点偏移量用于提高网络帧的可读性*/
                    /*这对可寻址的slave的数量没有影响（自动环绕）*/
                    ecx_APWRw(context.port, ADPh, ECT_REG_STADR, htoes((ushort)(slave + EC_NODEOFFSET)), EC_TIMEOUTRET3); /*设置从机节点地址*/
                    if (slave == 1)
                    {
                        b = 1; /*删除第一个从机的非ecat帧*/
                    }
                    else
                    {
                        b = 0; /*传递下列从设备的所有帧*/
                    }
                    ecx_APWRw(context.port, ADPh, ECT_REG_DLCTL, htoes(b), EC_TIMEOUTRET3);/*设置非ecat帧行为*/
                    configadr = ecx_APRDw(context.port, ADPh, ECT_REG_STADR, EC_TIMEOUTRET3);
                    configadr = etohs(configadr);
                    //configadr = 4097;
                    context.slavelist[slave].configadr = configadr;
                    byte[] aliasadr_t = BitConverter.GetBytes(aliasadr);
                    ecx_FPRD(context.port, configadr, ECT_REG_ALIAS, sizeof(ushort), aliasadr_t, EC_TIMEOUTRET3);
                    aliasadr = BitConverter.ToInt16(aliasadr_t, 0);
                    context.slavelist[slave].aliasadr = etohs((ushort)aliasadr);
                    byte[] estat_t = BitConverter.GetBytes(estat);
                    ecx_FPRD(context.port, configadr, ECT_REG_EEPSTAT, sizeof(ushort), estat_t, EC_TIMEOUTRET3);
                    estat = BitConverter.ToUInt16(estat_t, 0);
                    estat = etohs(estat);
                    if ((estat & EC_ESTAT_R64) > 0)/*检查从机是否可以读取8字节块*/
                    {
                        context.slavelist[slave].eep_8byte = 1;
                    }
                    ecx_readeeprom1(context, slave, ECT_SII_MANUF); /*手动*/

                }
                for (slave = 1; slave <= context.slavecount; slave++)
                {
                    eedat = ecx_readeeprom2(context, slave, EC_TIMEOUTEEP); /* Manuf */
                    context.slavelist[slave].eep_man = etohl(eedat);
                    ecx_readeeprom1(context, slave, ECT_SII_SN); /* serial # */
                }
                for (slave = 1; slave <= context.slavecount; slave++)
                {
                    eedat = ecx_readeeprom2(context, slave, EC_TIMEOUTEEP); /* serial # */
                    context.slavelist[slave].eep_sn = etohl(eedat);
                    ecx_readeeprom1(context, slave, ECT_SII_ID); /* ID */
                }
                for (slave = 1; slave <= context.slavecount; slave++)
                {
                    eedat = ecx_readeeprom2(context, slave, EC_TIMEOUTEEP); /* ID */
                    context.slavelist[slave].eep_id = etohl(eedat);
                    ecx_readeeprom1(context, slave, ECT_SII_REV); /* revision */
                }

                for (slave = 1; slave <= context.slavecount; slave++)
                {
                    eedat = ecx_readeeprom2(context, slave, EC_TIMEOUTEEP); /* revision */
                    context.slavelist[slave].eep_rev = etohl(eedat);
                    ecx_readeeprom1(context, slave, ECT_SII_RXMBXADR); /* write mailbox address + mailboxsize */
                }
                for (slave = 1; slave <= context.slavecount; slave++)
                {
                    eedat = ecx_readeeprom2(context, slave, EC_TIMEOUTEEP); /* write mailbox address and mailboxsize */
                    context.slavelist[slave].mbx_wo = LO_WORD(etohl(eedat));
                    context.slavelist[slave].mbx_l = HI_WORD(etohl(eedat));
                    if (context.slavelist[slave].mbx_l > 0)
                    {
                        ecx_readeeprom1(context, slave, ECT_SII_TXMBXADR); /* read mailbox offset */
                    }
                }
                for (slave = 1; slave <= context.slavecount; slave++)
                {
                    if (context.slavelist[slave].mbx_l > 0)
                    {
                        eedat = ecx_readeeprom2(context, slave, EC_TIMEOUTEEP); /* read mailbox offset */
                        context.slavelist[slave].mbx_ro = LO_WORD(etohl(eedat)); /* read mailbox offset */
                        context.slavelist[slave].mbx_rl = HI_WORD(etohl(eedat)); /*read mailbox length */
                        if (context.slavelist[slave].mbx_rl == 0)
                        {
                            context.slavelist[slave].mbx_rl = context.slavelist[slave].mbx_l;
                        }
                        ecx_readeeprom1(context, slave, ECT_SII_MBXPROTO);
                    }
                    configadr = context.slavelist[slave].configadr;
                    val16 = ecx_FPRDw(context.port, configadr, ECT_REG_ESCSUP, EC_TIMEOUTRET3);
                    if ((etohs(val16) & 0x04) > 0)  /* Supcontext.port DC? */
                    {
                        context.slavelist[slave].hasdc = true;
                    }
                    else
                    {
                        context.slavelist[slave].hasdc = false;
                    }
                    topology = ecx_FPRDw(context.port, configadr, ECT_REG_DLSTAT, EC_TIMEOUTRET3); /* extract topology from DL status */
                    topology = etohs(topology);
                    h = 0;
                    b = 0;
                    if ((topology & 0x0300) == 0x0200) /* context.port0 open and communication established */
                    {
                        h++;
                        b |= 0x01;
                    }
                    if ((topology & 0x0c00) == 0x0800) /* context.port1 open and communication established */
                    {
                        h++;
                        b |= 0x02;
                    }
                    if ((topology & 0x3000) == 0x2000) /* context.port2 open and communication established */
                    {
                        h++;
                        b |= 0x04;
                    }
                    if ((topology & 0xc000) == 0x8000) /* context.port3 open and communication established */
                    {
                        h++;
                        b |= 0x08;
                    }
                    /* ptype = Physical type*/
                    val16 = ecx_FPRDw(context.port, configadr, ECT_REG_PORTDES, EC_TIMEOUTRET3);
                    context.slavelist[slave].ptype = LO_BYTE(etohs(val16));
                    context.slavelist[slave].topology = h;
                    context.slavelist[slave].activeports = b;
                    /* 0=no links, not possible             */
                    /* 1=1 link  , end of line              */
                    /* 2=2 links , one before and one after */
                    /* 3=3 links , split point              */
                    /* 4=4 links , cross point              */
                    /* search for parent */
                    context.slavelist[slave].parent = 0; /* parent is master */
                    if (slave > 1)
                    {
                        topoc = 0;
                        slavec = (short)(slave - 1);
                        do
                        {
                            topology = context.slavelist[slavec].topology;
                            if (topology == 1)
                            {
                                topoc--; /* endpoint found */
                            }
                            if (topology == 3)
                            {
                                topoc++; /* split found */
                            }
                            if (topology == 4)
                            {
                                topoc += 2; /* cross found */
                            }
                            if (((topoc >= 0) && (topology > 1)) ||
                                (slavec == 1)) /* parent found */
                            {
                                context.slavelist[slave].parent = (ushort)slavec;
                                slavec = 1;
                            }
                            slavec--;
                        }
                        while (slavec > 0);
                    }
                    ecx_statecheck(context, slave, EC_STATE_INIT, EC_TIMEOUTSTATE); //* check state change Init */

                    /* set default mailbox configuration if slave has mailbox */
                    if (context.slavelist[slave].mbx_l > 0)
                    {
                        //初始化SMtype,结构体初始化的时候,没有初始化
                        context.slavelist[slave].SMtype = new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                        context.slavelist[slave].SM = Enumerable.Range(0, 8).Select(_ => new ec_smt()).ToArray();
                        context.slavelist[slave].SMtype[0] = 1;
                        context.slavelist[slave].SMtype[1] = 2;
                        context.slavelist[slave].SMtype[2] = 3;
                        context.slavelist[slave].SMtype[3] = 4;
                        context.slavelist[slave].SM[0].StartAddr = htoes(context.slavelist[slave].mbx_wo);
                        context.slavelist[slave].SM[0].SMlength = htoes(context.slavelist[slave].mbx_l);
                        context.slavelist[slave].SM[0].SMflags = htoel(EC_DEFAULTMBXSM0);
                        context.slavelist[slave].SM[1].StartAddr = htoes(context.slavelist[slave].mbx_ro);
                        context.slavelist[slave].SM[1].SMlength = htoes(context.slavelist[slave].mbx_rl);
                        context.slavelist[slave].SM[1].SMflags = htoel(EC_DEFAULTMBXSM1);
                        eedat = ecx_readeeprom2(context, slave, EC_TIMEOUTEEP);
                        context.slavelist[slave].mbx_proto = (ushort)etohl(eedat);
                    }

                    /* slave not in configuration table, find out via SII */
                    if (ecx_lookup_prev_sii(context, slave) == 0)
                    {
                        ssigen = (ushort)ecx_siifind(context, slave, ECT_SII_GENERAL);
                        /* SII general section */
                        if (ssigen != 0)
                        {
                            context.slavelist[slave].CoEdetails = ecx_siigetbyte(context, slave, (ushort)(ssigen + 0x07));
                            context.slavelist[slave].FoEdetails = ecx_siigetbyte(context, slave, (ushort)(ssigen + 0x08));
                            context.slavelist[slave].EoEdetails = ecx_siigetbyte(context, slave, (ushort)(ssigen + 0x09));
                            context.slavelist[slave].SoEdetails = ecx_siigetbyte(context, slave, (ushort)(ssigen + 0x0a));
                            if ((ecx_siigetbyte(context, slave, (ushort)(ssigen + 0x0d)) & 0x02) > 0)
                            {
                                context.slavelist[slave].blockLRW = 1;
                                context.slavelist[0].blockLRW++;
                            }
                            context.slavelist[slave].Ebuscurrent = ecx_siigetbyte(context, slave, (ushort)(ssigen + 0x0e));
                            context.slavelist[slave].Ebuscurrent += (short)(ecx_siigetbyte(context, slave, (ushort)(ssigen + 0x0f)) << 8);
                            context.slavelist[0].Ebuscurrent += context.slavelist[slave].Ebuscurrent;
                        }
                        /* SII strings section */
                        if (ecx_siifind(context, slave, ECT_SII_STRING) > 0)
                        {
                            string str = "";
                            ecx_siistring(context, ref str, slave, 1);
                            context.slavelist[slave].name = str;
                        }
                        else
                        {
                            Console.WriteLine(context.slavelist[slave].name + context.slavelist[slave].eep_man + context.slavelist[slave].eep_id);
                        }


                        /* SII SM section */
                        nSM = ecx_siiSM(context, slave, context.eepSM);
                        if (nSM > 0)
                        {
                            context.slavelist[slave].SM[0].StartAddr = htoes(context.eepSM.PhStart);
                            context.slavelist[slave].SM[0].SMlength = htoes(context.eepSM.Plength);
                            context.slavelist[slave].SM[0].SMflags = htoel((uint)((context.eepSM.Creg) + (context.eepSM.Activate << 16)));
                            SMc = 1;
                            while ((SMc < EC_MAXSM) && ecx_siiSMnext(context, slave, context.eepSM, SMc) > 0)
                            {
                                context.slavelist[slave].SM[SMc].StartAddr = htoes(context.eepSM.PhStart);
                                context.slavelist[slave].SM[SMc].SMlength = htoes(context.eepSM.Plength);
                                context.slavelist[slave].SM[SMc].SMflags = htoel((uint)((context.eepSM.Creg) + (context.eepSM.Activate << 16)));
                                SMc++;
                            }
                        }
                        /* SII FMMU section */
                        if (ecx_siiFMMU(context, slave, context.eepFMMU) > 0)
                        {
                            if (context.eepFMMU.FMMU0 != 0xff)
                            {
                                context.slavelist[slave].FMMU0func = context.eepFMMU.FMMU0;
                            }
                            if (context.eepFMMU.FMMU1 != 0xff)
                            {
                                context.slavelist[slave].FMMU1func = context.eepFMMU.FMMU1;
                            }
                            if (context.eepFMMU.FMMU2 != 0xff)
                            {
                                context.slavelist[slave].FMMU2func = context.eepFMMU.FMMU2;
                            }
                            if (context.eepFMMU.FMMU3 != 0xff)
                            {
                                context.slavelist[slave].FMMU3func = context.eepFMMU.FMMU3;
                            }
                        }
                    }

                    if (context.slavelist[slave].mbx_l > 0)
                    {
                        if (context.slavelist[slave].SM[0].StartAddr == 0x0000) /* should never happen */
                        {
                            context.slavelist[slave].SM[0].StartAddr = htoes(0x1000);
                            context.slavelist[slave].SM[0].SMlength = htoes(0x0080);
                            context.slavelist[slave].SM[0].SMflags = htoel(EC_DEFAULTMBXSM0);
                            context.slavelist[slave].SMtype[0] = 1;
                        }
                        if (context.slavelist[slave].SM[1].StartAddr == 0x0000) /* should never happen */
                        {
                            context.slavelist[slave].SM[1].StartAddr = htoes(0x1080);
                            context.slavelist[slave].SM[1].SMlength = htoes(0x0080);
                            context.slavelist[slave].SM[1].SMflags = htoel(EC_DEFAULTMBXSM1);
                            context.slavelist[slave].SMtype[1] = 2;
                        }
                        /* program SM0 mailbox in and SM1 mailbox out for slave */
                        /* writing both SM in one datagram will solve timing issue in old NETX */
                        var sms = ToBytes(context.slavelist[slave].SM[0]);
                        var sms1 = ToBytes(context.slavelist[slave].SM[1]);
                        byte[] combined = new byte[sms.Length * 2];
                        Array.Copy(sms, 0, combined, 0, sms.Length);
                        Array.Copy(sms1, 0, combined, sms1.Length, sms1.Length);
                        //Console.WriteLine("测试data:");
                        ecx_FPWR(context.port, configadr, ECT_REG_SM0, (ushort)combined.Length, combined, EC_TIMEOUTRET3);
                    }
                    /* some slaves need eeprom available to PDI in init.preop transition */
                    ecx_eeprom2pdi(context, slave);
                    /* User may override automatic state change */
                    if (context.manualstatechange == 0)
                    {
                        /* request pre_op for slave */
                        ecx_FPWRw(context.port, configadr, ECT_REG_ALCTL, htoes(EC_STATE_PRE_OP | EC_STATE_ACK), EC_TIMEOUTRET3); /* set preop status */
                    }
                }
            }
            return wkc;
        }
        public int ecx_lookup_mapping(ecx_contextt context, ushort slave, ref uint Osize, ref uint Isize)
        {
            int i, nSM;
            if ((slave > 1) && (context.slavecount > 0))
            {
                i = 1;
                while (((context.slavelist[i].eep_man != context.slavelist[slave].eep_man) ||
                        (context.slavelist[i].eep_id != context.slavelist[slave].eep_id) ||
                        (context.slavelist[i].eep_rev != context.slavelist[slave].eep_rev)) &&
                       (i < slave))
                {
                    i++;
                }
                if (i < slave)
                {
                    for (nSM = 0; nSM < EC_MAXSM; nSM++)
                    {
                        context.slavelist[slave].SM[nSM].SMlength = context.slavelist[i].SM[nSM].SMlength;
                        context.slavelist[slave].SMtype[nSM] = context.slavelist[i].SMtype[nSM];
                    }
                    Osize = context.slavelist[i].Obits;
                    Isize = context.slavelist[i].Ibits;
                    context.slavelist[slave].Obits = (ushort)Osize;
                    context.slavelist[slave].Ibits = (ushort)Isize;
                    return 1;
                }
            }
            return 0;
        }
        public int ecx_map_coe_soe(ecx_contextt context, ushort slave, int thread_n)
        {
            uint Isize, Osize;
            int rval;

            ecx_statecheck(context, slave, EC_STATE_PRE_OP, EC_TIMEOUTSTATE); /* check state change pre-op */

            /* execute special slave configuration hook Pre-Op to Safe-OP */
            //if (context.slavelist[slave].PO2SOconfig) /* only if registered */
            //{
            //    context->slavelist[slave].PO2SOconfig(slave);
            //}
            //if (context->slavelist[slave].PO2SOconfigx) /* only if registered */
            //{
            //    context->slavelist[slave].PO2SOconfigx(context, slave);
            //}
            /* if slave not found in configlist find IO mapping in slave self */
            if (context.slavelist[slave].configindex == 0)
            {
                Isize = 0;
                Osize = 0;
                if ((context.slavelist[slave].mbx_proto & ECT_MBXPROT_COE) > 0) /* has CoE */
                {
                    rval = 0;
                    if ((context.slavelist[slave].CoEdetails & ECT_COEDET_SDOCA) > 0) /* has Complete Access */
                    {
                        /* read PDO mapping via CoE and use Complete Access */
                        rval = ecx_readPDOmapCA(context, slave, thread_n, ref Osize, ref Isize);
                    }
                    if (rval == 0) /* CA not available or not succeeded */
                    {
                        /* read PDO mapping via CoE */
                        rval = ecx_readPDOmap(context, slave, ref Osize, ref Isize);
                    }
                }
                //不检测SOE
                //if ((Isize == 0&& Osize ==0) && (context.slavelist[slave].mbx_proto & ECT_MBXPROT_SOE) > 0) /* has SoE */
                //{
                //    /* read AT / MDT mapping via SoE */
                //    rval = ecx_readIDNmap(context, slave, ref Osize, ref Isize);
                //    context.slavelist[slave].SM[2].SMlength = htoes((ushort)((Osize + 7) / 8));
                //    context.slavelist[slave].SM[3].SMlength = htoes((ushort)((Isize + 7) / 8));
                //    Console.WriteLine("  SoE Osize:%u Isize:%u\n", Osize, Isize);
                //}
                context.slavelist[slave].Obits = (ushort)Osize;
                context.slavelist[slave].Ibits = (ushort)Isize;
            }

            return 1;
        }
        public int ecx_map_sii(ecx_contextt context, ushort slave)
        {
            uint Isize, Osize;
            int nSM;
            ec_eepromPDOt eepPDO = new ec_eepromPDOt();

            Osize = context.slavelist[slave].Obits;
            Isize = context.slavelist[slave].Ibits;

            if (Isize == 0 && Osize == 0) /* find PDO in previous slave with same ID */
            {
                ecx_lookup_mapping(context, slave, ref Osize, ref Isize);
            }
            if (Isize == 0 && Osize == 0) /* find PDO mapping by SII */
            {
                Isize = (uint)ecx_siiPDO(context, slave, eepPDO, 0);
                for (nSM = 0; nSM < EC_MAXSM; nSM++)
                {
                    if (eepPDO.SMbitsize[nSM] > 0)
                    {
                        context.slavelist[slave].SM[nSM].SMlength = htoes((ushort)((eepPDO.SMbitsize[nSM] + 7) / 8));
                        context.slavelist[slave].SMtype[nSM] = 4;
                    }
                }
                Osize = (uint)ecx_siiPDO(context, slave, eepPDO, 1);
                for (nSM = 0; nSM < EC_MAXSM; nSM++)
                {
                    if (eepPDO.SMbitsize[nSM] > 0)
                    {
                        context.slavelist[slave].SM[nSM].SMlength = htoes((ushort)((eepPDO.SMbitsize[nSM] + 7) / 8));
                        context.slavelist[slave].SMtype[nSM] = 3;
                    }
                }
            }
            context.slavelist[slave].Obits = (ushort)Osize;
            context.slavelist[slave].Ibits = (ushort)Isize;

            return 1;
        }
        public int ecx_map_sm(ecx_contextt context, ushort slave)
        {
            ushort configadr;
            int nSM;

            configadr = context.slavelist[slave].configadr;

            if (context.slavelist[slave].mbx_l == 0 && context.slavelist[slave].SM[0].StartAddr == 0)
            {
                byte[] bytes = ToBytes(context.slavelist[slave].SM[0]);
                ecx_FPWR(context.port, configadr, ECT_REG_SM0, (ushort)bytes.Length, bytes, EC_TIMEOUTRET3);
                context.slavelist[slave].SM[0] = FromBytes<ec_smt>(bytes);
            }
            if (context.slavelist[slave].mbx_l == 0 && context.slavelist[slave].SM[1].StartAddr == 0)
            {
                byte[] bytes = ToBytes(context.slavelist[slave].SM[1]);
                ecx_FPWR(context.port, configadr, ECT_REG_SM1, (ushort)bytes.Length, bytes, EC_TIMEOUTRET3);
                context.slavelist[slave].SM[1] = FromBytes<ec_smt>(bytes);
            }
            /* program SM2 to SMx */
            for (nSM = 2; nSM < EC_MAXSM; nSM++)
            {
                if (context.slavelist[slave].SM[nSM].StartAddr > 0)
                {
                    /* check if SM length is zero -> clear enable flag */
                    if (context.slavelist[slave].SM[nSM].SMlength == 0)
                    {
                        context.slavelist[slave].SM[nSM].SMflags =
                           htoel(etohl(context.slavelist[slave].SM[nSM].SMflags) & EC_SMENABLEMASK);
                    }
                    /* if SM length is non zero always set enable flag */
                    else
                    {
                        context.slavelist[slave].SM[nSM].SMflags =
                           htoel(etohl(context.slavelist[slave].SM[nSM].SMflags) | ~EC_SMENABLEMASK);
                    }
                    byte[] bytes = ToBytes(context.slavelist[slave].SM[nSM]);
                    ecx_FPWR(context.port, configadr, (ushort)(ECT_REG_SM0 + (nSM * bytes.Length)), (ushort)bytes.Length, bytes, EC_TIMEOUTRET3);
                    context.slavelist[slave].SM[nSM] = FromBytes<ec_smt>(bytes);
                }
            }
            if (context.slavelist[slave].Ibits > 7)
            {
                context.slavelist[slave].Ibytes = (uint)((context.slavelist[slave].Ibits + 7) / 8);
            }
            if (context.slavelist[slave].Obits > 7)
            {
                context.slavelist[slave].Obytes = (uint)((context.slavelist[slave].Obits + 7) / 8);
            }

            return 1;
        }
        public void ecx_config_find_mappings(ecx_contextt context, byte group)
        {
            ushort slave;

            /* find CoE and SoE mapping of slaves in multiple threads */
            for (slave = 1; slave <= context.slavecount; slave++)
            {
                if (group <= 0 || (group == context.slavelist[slave].group))
                {
                    /* serialised version */
                    ecx_map_coe_soe(context, slave, 0);
                }
            }

            /* find SII mapping of slave and program SM */
            for (slave = 1; slave <= context.slavecount; slave++)
            {
                if (group <= 0 || (group == context.slavelist[slave].group))
                {
                    ecx_map_sii(context, slave);
                    ecx_map_sm(context, slave);
                }
            }
        }
        public void ecx_config_create_input_mappings(ecx_contextt context, Span<byte> pIOmap, byte group, ushort slave, ref uint LogAddr, ref byte BitPos)
        {
            int BitCount = 0;
            int FMMUdone = 0;
            ushort ByteCount = 0;
            ushort FMMUsize = 0;
            byte SMc = 0;
            ushort EndAddr;
            ushort SMlength;
            ushort configadr;
            byte FMMUc;

            configadr = context.slavelist[slave].configadr;
            FMMUc = context.slavelist[slave].FMMUunused;
            if (context.slavelist[slave].Obits > 0) /* find free FMMU */
            {
                while (context.slavelist[slave].FMMU[FMMUc].LogStart > 0)
                {
                    FMMUc++;
                }
            }
            /* search for SM that contribute to the input mapping */
            while ((SMc < EC_MAXSM) && (FMMUdone < ((context.slavelist[slave].Ibits + 7) / 8)))
            {
                while ((SMc < (EC_MAXSM - 1)) && (context.slavelist[slave].SMtype[SMc] != 4))
                {
                    SMc++;
                }
                context.slavelist[slave].FMMU[FMMUc].PhysStart = context.slavelist[slave].SM[SMc].StartAddr;
                SMlength = etohs(context.slavelist[slave].SM[SMc].SMlength);
                ByteCount += SMlength;
                BitCount += SMlength * 8;
                EndAddr = (ushort)(etohs(context.slavelist[slave].SM[SMc].StartAddr) + SMlength);
                while ((BitCount < context.slavelist[slave].Ibits) && (SMc < (EC_MAXSM - 1))) /* more SM for input */
                {
                    SMc++;
                    while ((SMc < (EC_MAXSM - 1)) && (context.slavelist[slave].SMtype[SMc] != 4))
                    {
                        SMc++;
                    }
                    /* if addresses from more SM connect use one FMMU otherwise break up in multiple FMMU */
                    if (etohs(context.slavelist[slave].SM[SMc].StartAddr) > EndAddr)
                    {
                        break;
                    }
                    SMlength = etohs(context.slavelist[slave].SM[SMc].SMlength);
                    ByteCount += SMlength;
                    BitCount += SMlength * 8;
                    EndAddr = (ushort)(etohs(context.slavelist[slave].SM[SMc].StartAddr) + SMlength);
                }

                /* bit oriented slave */
                if (context.slavelist[slave].Ibytes == 0)
                {
                    context.slavelist[slave].FMMU[FMMUc].LogStart = htoel((uint)LogAddr);
                    context.slavelist[slave].FMMU[FMMUc].LogStartbit = BitPos;
                    BitPos += (byte)(context.slavelist[slave].Ibits - 1);
                    if (BitPos > 7)
                    {
                        LogAddr += 1;
                        BitPos -= 8;
                    }
                    FMMUsize = (ushort)(LogAddr - etohl(context.slavelist[slave].FMMU[FMMUc].LogStart) + 1);
                    context.slavelist[slave].FMMU[FMMUc].LogLength = htoes(FMMUsize);
                    context.slavelist[slave].FMMU[FMMUc].LogEndbit = BitPos;
                    BitPos += 1;
                    if (BitPos > 7)
                    {
                        LogAddr += 1;
                        BitPos -= 8;
                    }
                }
                /* byte oriented slave */
                else
                {
                    if (BitPos > 0)
                    {
                        LogAddr += 1;
                        BitPos = 0;
                    }
                    context.slavelist[slave].FMMU[FMMUc].LogStart = htoel((uint)LogAddr);
                    context.slavelist[slave].FMMU[FMMUc].LogStartbit = BitPos;
                    BitPos = 7;
                    FMMUsize = ByteCount;
                    if ((FMMUsize + FMMUdone) > (int)context.slavelist[slave].Ibytes)
                    {
                        FMMUsize = (ushort)(context.slavelist[slave].Ibytes - FMMUdone);
                    }
                    LogAddr += FMMUsize;
                    context.slavelist[slave].FMMU[FMMUc].LogLength = htoes(FMMUsize);
                    context.slavelist[slave].FMMU[FMMUc].LogEndbit = BitPos;
                    BitPos = 0;
                }
                FMMUdone += FMMUsize;
                if (context.slavelist[slave].FMMU[FMMUc].LogLength > 0)
                {
                    context.slavelist[slave].FMMU[FMMUc].PhysStartBit = 0;
                    context.slavelist[slave].FMMU[FMMUc].FMMUtype = 1;
                    context.slavelist[slave].FMMU[FMMUc].FMMUactive = 1;
                    /* program FMMU for input */
                    byte[] bytes = ToBytes(context.slavelist[slave].FMMU[FMMUc]);
                    ecx_FPWR(context.port, configadr, (ushort)(ECT_REG_FMMU0 + (bytes.Length * FMMUc)), (ushort)bytes.Length, bytes, EC_TIMEOUTRET3);
                    context.slavelist[slave].FMMU[FMMUc] = FromBytes<ec_fmmut>(bytes);
                }
                if (context.slavelist[slave].inputs != null)
                {
                    if (group > 0)
                    {
                        context.slavelist[slave].inputs = pIOmap.Slice((int)(etohl(context.slavelist[slave].FMMU[FMMUc].LogStart) -
                           context.grouplist[group].logstartaddr)).ToArray();
                    }
                    else
                    {
                        context.slavelist[slave].inputs = pIOmap.Slice((int)(etohl(context.slavelist[slave].FMMU[FMMUc].LogStart))).ToArray();
                    }
                    context.slavelist[slave].Istartbit =
                       context.slavelist[slave].FMMU[FMMUc].LogStartbit;
                }
                FMMUc++;
            }
            context.slavelist[slave].FMMUunused = FMMUc;
        }
        public void ecx_config_create_output_mappings(ecx_contextt context, Span<byte> pIOmap, byte group, ushort slave, ref uint LogAddr, ref byte BitPos)
        {
            int BitCount = 0;
            int FMMUdone = 0;
            ushort ByteCount = 0;
            ushort FMMUsize = 0;
            byte SMc = 0;
            ushort EndAddr;
            ushort SMlength;
            ushort configadr;
            byte FMMUc;


            FMMUc = context.slavelist[slave].FMMUunused;
            configadr = context.slavelist[slave].configadr;

            /* search for SM that contribute to the output mapping */
            while ((SMc < EC_MAXSM) && (FMMUdone < ((context.slavelist[slave].Obits + 7) / 8)))
            {
                while ((SMc < (EC_MAXSM - 1)) && (context.slavelist[slave].SMtype[SMc] != 3))
                {
                    SMc++;
                }
                context.slavelist[slave].FMMU[FMMUc].PhysStart = context.slavelist[slave].SM[SMc].StartAddr;
                SMlength = etohs(context.slavelist[slave].SM[SMc].SMlength);
                ByteCount += SMlength;
                BitCount += SMlength * 8;
                EndAddr = (ushort)(etohs(context.slavelist[slave].SM[SMc].StartAddr) + SMlength);
                while ((BitCount < context.slavelist[slave].Obits) && (SMc < (EC_MAXSM - 1))) /* more SM for output */
                {
                    SMc++;
                    while ((SMc < (EC_MAXSM - 1)) && (context.slavelist[slave].SMtype[SMc] != 3))
                    {
                        SMc++;
                    }
                    /* if addresses from more SM connect use one FMMU otherwise break up in multiple FMMU */
                    if (etohs(context.slavelist[slave].SM[SMc].StartAddr) > EndAddr)
                    {
                        break;
                    }
                    SMlength = etohs(context.slavelist[slave].SM[SMc].SMlength);
                    ByteCount += SMlength;
                    BitCount += SMlength * 8;
                    EndAddr = (ushort)(etohs(context.slavelist[slave].SM[SMc].StartAddr) + SMlength);
                }

                /* bit oriented slave */
                if (context.slavelist[slave].Ibytes == 0)
                {
                    context.slavelist[slave].FMMU[FMMUc].LogStart = htoel((uint)LogAddr);
                    context.slavelist[slave].FMMU[FMMUc].LogStartbit = BitPos;
                    BitPos += (byte)(context.slavelist[slave].Obits - 1);
                    if (BitPos > 7)
                    {
                        LogAddr += 1;
                        BitPos -= 8;
                    }
                    FMMUsize = (ushort)(LogAddr - etohl(context.slavelist[slave].FMMU[FMMUc].LogStart) + 1);
                    context.slavelist[slave].FMMU[FMMUc].LogLength = htoes(FMMUsize);
                    context.slavelist[slave].FMMU[FMMUc].LogEndbit = BitPos;
                    BitPos += 1;
                    if (BitPos > 7)
                    {
                        LogAddr += 1;
                        BitPos -= 8;
                    }
                }
                /* byte oriented slave */
                else
                {
                    if (BitPos > 0)
                    {
                        LogAddr += 1;
                        BitPos = 0;
                    }
                    context.slavelist[slave].FMMU[FMMUc].LogStart = htoel((uint)LogAddr);
                    context.slavelist[slave].FMMU[FMMUc].LogStartbit = BitPos;
                    BitPos = 7;
                    FMMUsize = ByteCount;
                    if ((FMMUsize + FMMUdone) > (int)context.slavelist[slave].Obytes)
                    {
                        FMMUsize = (ushort)(context.slavelist[slave].Obytes - FMMUdone);
                    }
                    LogAddr += FMMUsize;
                    context.slavelist[slave].FMMU[FMMUc].LogLength = htoes(FMMUsize);
                    context.slavelist[slave].FMMU[FMMUc].LogEndbit = BitPos;
                    BitPos = 0;
                }
                FMMUdone += FMMUsize;
                if (context.slavelist[slave].FMMU[FMMUc].LogLength > 0)
                {
                    context.slavelist[slave].FMMU[FMMUc].PhysStartBit = 0;
                    context.slavelist[slave].FMMU[FMMUc].FMMUtype = 2;
                    context.slavelist[slave].FMMU[FMMUc].FMMUactive = 1;
                    /* program FMMU for output */
                    byte[] bytes = ToBytes(context.slavelist[slave].FMMU[FMMUc]);
                    ecx_FPWR(context.port, configadr, (ushort)(ECT_REG_FMMU0 + (bytes.Length * FMMUc)), (ushort)bytes.Length, bytes, EC_TIMEOUTRET3);
                    context.slavelist[slave].FMMU[FMMUc] = FromBytes<ec_fmmut>(bytes);
                }
                if (context.slavelist[slave].outputs == null)
                {
                    if (group > 0)
                    {
                        context.slavelist[slave].outputs =
                            pIOmap.Slice((int)(etohl(context.slavelist[slave].FMMU[FMMUc].LogStart) -
                           context.grouplist[group].logstartaddr)).ToArray();
                    }
                    else
                    {
                        context.slavelist[slave].outputs = pIOmap.Slice((int)(etohl(context.slavelist[slave].FMMU[FMMUc].LogStart))).ToArray();
                    }
                    context.slavelist[slave].Ostartbit =
                       context.slavelist[slave].FMMU[FMMUc].LogStartbit;
                }
                FMMUc++;
            }
            context.slavelist[slave].FMMUunused = FMMUc;
        }
        public int ecx_main_config_map_group(ecx_contextt context, Span<byte> pIOmap, byte group, bool forceByteAlignment)
        {
            ushort slave, configadr = 0;
            byte BitPos;
            uint LogAddr = 0;
            uint oLogAddr = 0;
            uint diff;
            ushort currentsegment = 0;
            uint segmentsize = 0;
            uint segmentmaxsize = (EC_MAXLRWDATA - EC_FIRSTDCDATAGRAM); /* first segment must account for DC overhead */
            if ((context.slavecount > 0) && (group < EC_MAXGROUP))
            {
                LogAddr = context.grouplist[group].logstartaddr;
                oLogAddr = LogAddr;
                BitPos = 0;
                context.grouplist[group].nsegments = 0;
                context.grouplist[group].outputsWKC = 0;
                context.grouplist[group].inputsWKC = 0;

                /* Find mappings and program syncmanagers */
                ecx_config_find_mappings(context, group);

                /* do output mapping of slave and program FMMUs */
                for (slave = 1; slave <= context.slavecount; slave++)
                {
                    configadr = context.slavelist[slave].configadr;

                    if (group <= 0 || (group == context.slavelist[slave].group))
                    {
                        /* create output mapping */
                        if (context.slavelist[slave].Obits > 0)
                        {
                            ecx_config_create_output_mappings(context, pIOmap, group, slave, ref LogAddr, ref BitPos);

                            if (forceByteAlignment)
                            {
                                /* Force byte alignment if the output is < 8 bits */
                                if (BitPos > 0)
                                {
                                    LogAddr++;
                                    BitPos = 0;
                                }
                            }

                            diff = LogAddr - oLogAddr;
                            oLogAddr = LogAddr;
                            if ((segmentsize + diff) > segmentmaxsize && diff <= segmentmaxsize && currentsegment < EC_MAXIOSEGMENTS)
                            {
                                context.grouplist[group].IOsegment[currentsegment++] = segmentsize;
                                segmentsize = 0;
                                segmentmaxsize = EC_MAXLRWDATA; /* can ignore DC overhead after first segment */
                            }
                            segmentsize += diff;
                            while (segmentsize > segmentmaxsize && currentsegment < EC_MAXIOSEGMENTS)
                            {
                                context.grouplist[group].IOsegment[currentsegment++] = segmentmaxsize;
                                segmentsize -= segmentmaxsize;
                                context.grouplist[group].outputsWKC++;
                                segmentmaxsize = EC_MAXLRWDATA; /* can ignore DC overhead after first segment */
                            }
                            /* if this slave added output data and there is a partial segment still outstanding increment the outputwkc */
                            if (segmentsize > 0 && diff > 0)
                                context.grouplist[group].outputsWKC++;
                        }
                    }
                }
                if (BitPos > 0)
                {
                    LogAddr++;
                    oLogAddr = LogAddr;
                    BitPos = 0;
                    if ((segmentsize + 1) > segmentmaxsize && currentsegment < EC_MAXIOSEGMENTS)
                    {
                        context.grouplist[group].IOsegment[currentsegment++] = segmentsize;
                        segmentsize = 0;
                        context.grouplist[group].outputsWKC++;
                        segmentmaxsize = EC_MAXLRWDATA; /* can ignore DC overhead after first segment */
                    }
                    segmentsize += 1;
                }
                context.grouplist[group].outputs = pIOmap.ToArray();
                context.grouplist[group].Obytes = LogAddr - context.grouplist[group].logstartaddr;
                context.grouplist[group].nsegments = (ushort)(currentsegment + 1);
                context.grouplist[group].Isegment = currentsegment;
                context.grouplist[group].Ioffset = (ushort)segmentsize;
                if (group <= 0)
                {
                    context.slavelist[0].outputs = pIOmap.ToArray();
                    context.slavelist[0].Obytes = LogAddr - context.grouplist[group].logstartaddr; /* store output bytes in master record */
                }

                /* do input mapping of slave and program FMMUs */
                for (slave = 1; slave <= context.slavecount; slave++)
                {
                    configadr = context.slavelist[slave].configadr;
                    if (group <= 0 || (group == context.slavelist[slave].group))
                    {
                        /* create input mapping */
                        if (context.slavelist[slave].Ibits > 0)
                        {

                            ecx_config_create_input_mappings(context, pIOmap, group, slave, ref LogAddr, ref BitPos);

                            if (forceByteAlignment)
                            {
                                /* Force byte alignment if the input is < 8 bits */
                                if (BitPos > 0)
                                {
                                    LogAddr++;
                                    BitPos = 0;
                                }
                            }

                            diff = LogAddr - oLogAddr;
                            oLogAddr = LogAddr;
                            if ((segmentsize + diff) > segmentmaxsize && diff <= segmentmaxsize && currentsegment < EC_MAXIOSEGMENTS)
                            {
                                context.grouplist[group].IOsegment[currentsegment++] = segmentsize;
                                segmentsize = 0;
                                segmentmaxsize = EC_MAXLRWDATA; /* can ignore DC overhead after first segment */
                            }
                            segmentsize += diff;
                            while (segmentsize > segmentmaxsize && currentsegment < EC_MAXIOSEGMENTS)
                            {
                                context.grouplist[group].IOsegment[currentsegment++] = segmentmaxsize;
                                segmentsize -= segmentmaxsize;
                                context.grouplist[group].inputsWKC++;
                                segmentmaxsize = EC_MAXLRWDATA; /* can ignore DC overhead after first segment */
                            }
                            /* if this slave added input data and there is a partial segment still outstanding increment the inputwkc */
                            if (segmentsize > 0 && diff > 0)
                                context.grouplist[group].inputsWKC++;
                        }
                        ecx_eeprom2pdi(context, slave); /* set Eeprom control to PDI */
                        /* User may override automatic state change */
                        if (context.manualstatechange == 0)
                        {
                            /* request safe_op for slave */
                            ecx_FPWRw(context.port,
                               configadr,
                               ECT_REG_ALCTL,
                               htoes(EC_STATE_SAFE_OP),
                               EC_TIMEOUTRET3); /* set safeop status */
                        }
                        if (context.slavelist[slave].blockLRW > 0)
                        {
                            context.grouplist[group].blockLRW++;
                        }
                        context.grouplist[group].Ebuscurrent += context.slavelist[slave].Ebuscurrent;

                    }
                }
                if (BitPos > 0)
                {
                    LogAddr++;
                    oLogAddr = LogAddr;
                    BitPos = 0;
                    if ((segmentsize + 1) > segmentmaxsize && currentsegment < EC_MAXIOSEGMENTS)
                    {
                        context.grouplist[group].IOsegment[currentsegment++] = segmentsize;
                        segmentsize = 0;
                        context.grouplist[group].inputsWKC++;
                    }
                    segmentsize += 1;
                }
                context.grouplist[group].IOsegment[currentsegment] = segmentsize;
                context.grouplist[group].nsegments = (ushort)(currentsegment + 1);
                context.grouplist[group].inputs = pIOmap.Slice((int)context.grouplist[0].Obytes).ToArray();
                context.grouplist[group].Ibytes = LogAddr - context.grouplist[group].logstartaddr - context.grouplist[group].Obytes;

                if (group <= 0)
                {
                    context.slavelist[0].inputs = pIOmap.Slice((int)context.slavelist[0].Obytes).ToArray();
                    context.slavelist[0].Ibytes = LogAddr - context.grouplist[group].logstartaddr - context.slavelist[0].Obytes; /* store input bytes in master record */
                }

                return (int)(LogAddr - context.grouplist[group].logstartaddr);
            }

            return 0;
        }
        int ecx_config_overlap_map_group(ecx_contextt context, Span<byte> pIOmap, byte group)
        {
            ushort slave, configadr;
            byte BitPos;
            uint mLogAddr = 0;
            uint siLogAddr = 0;
            uint soLogAddr = 0;
            uint tempLogAddr;
            uint diff;
            ushort currentsegment = 0;
            uint segmentsize = 0;
            uint segmentmaxsize = (EC_MAXLRWDATA - EC_FIRSTDCDATAGRAM);

            if ((context.slavecount > 0) && (group < context.maxgroup))
            {
                mLogAddr = context.grouplist[group].logstartaddr;
                siLogAddr = mLogAddr;
                soLogAddr = mLogAddr;
                BitPos = 0;
                context.grouplist[group].nsegments = 0;
                context.grouplist[group].outputsWKC = 0;
                context.grouplist[group].inputsWKC = 0;

                /* Find mappings and program syncmanagers */
                ecx_config_find_mappings(context, group);

                /* do IO mapping of slave and program FMMUs */
                for (slave = 1; slave <= context.slavecount; slave++)
                {
                    configadr = context.slavelist[slave].configadr;
                    siLogAddr = soLogAddr = mLogAddr;

                    if (group > 0 || (group == context.slavelist[slave].group))
                    {
                        /* create output mapping */
                        if (context.slavelist[slave].Obits > 0)
                        {

                            ecx_config_create_output_mappings(context, pIOmap, group, slave, ref soLogAddr, ref BitPos);
                            if (BitPos > 0)
                            {
                                soLogAddr++;
                                BitPos = 0;
                            }
                        }

                        /* create input mapping */
                        if (context.slavelist[slave].Ibits > 0)
                        {
                            ecx_config_create_input_mappings(context, pIOmap, group, slave, ref siLogAddr, ref BitPos);
                            if (BitPos > 0)
                            {
                                siLogAddr++;
                                BitPos = 0;
                            }
                        }

                        tempLogAddr = (siLogAddr > soLogAddr) ? siLogAddr : soLogAddr;
                        diff = tempLogAddr - mLogAddr;
                        int soLength = (int)(soLogAddr - mLogAddr);
                        int siLength = (int)(siLogAddr - mLogAddr);
                        mLogAddr = tempLogAddr;
                        if ((segmentsize + diff) > segmentmaxsize && diff <= segmentmaxsize && currentsegment < EC_MAXIOSEGMENTS)
                        {
                            context.grouplist[group].IOsegment[currentsegment++] = segmentsize;
                            segmentsize = 0;
                            segmentmaxsize = EC_MAXLRWDATA; /* can ignore DC overhead after first segment */
                        }
                        segmentsize += diff;
                        while (segmentsize > segmentmaxsize && currentsegment < EC_MAXIOSEGMENTS)
                        {
                            context.grouplist[group].IOsegment[currentsegment++] = segmentmaxsize;
                            segmentsize -= segmentmaxsize;
                            context.grouplist[group].inputsWKC += (ushort)(siLength > 0 ? 1 : 0);
                            context.grouplist[group].outputsWKC += (ushort)(soLength > 0 ? 1 : 0);
                            siLength -= (int)segmentmaxsize;
                            soLength -= (int)segmentmaxsize;
                            segmentmaxsize = EC_MAXLRWDATA; /* can ignore DC overhead after first segment */
                        }
                        /* if this slave added data and there is a partial segment still outstanding increment the relevant wkc */
                        if (segmentsize > 0 && diff > 0)
                        {
                            context.grouplist[group].inputsWKC += (ushort)(siLength > 0 ? 1 : 0);
                            context.grouplist[group].outputsWKC += (ushort)(soLength > 0 ? 1 : 0);
                        }

                        ecx_eeprom2pdi(context, slave); /* set Eeprom control to PDI */
                        /* User may override automatic state change */
                        if (context.manualstatechange == 0)
                        {
                            /* request safe_op for slave */
                            ecx_FPWRw(context.port,
                               configadr,
                               ECT_REG_ALCTL,
                               htoes(EC_STATE_SAFE_OP),
                               EC_TIMEOUTRET3);
                        }
                        if (context.slavelist[slave].blockLRW > 0)
                        {
                            context.grouplist[group].blockLRW++;
                        }
                        context.grouplist[group].Ebuscurrent += context.slavelist[slave].Ebuscurrent;

                    }
                }

                context.grouplist[group].IOsegment[currentsegment] = segmentsize;
                context.grouplist[group].nsegments = (ushort)(currentsegment + 1);
                context.grouplist[group].Isegment = 0;
                context.grouplist[group].Ioffset = 0;

                context.grouplist[group].Obytes = soLogAddr - context.grouplist[group].logstartaddr;
                context.grouplist[group].Ibytes = siLogAddr - context.grouplist[group].logstartaddr;
                context.grouplist[group].outputs = pIOmap.ToArray();
                context.grouplist[group].inputs = pIOmap.Slice((int)context.grouplist[group].Obytes).ToArray();

                /* Move calculated inputs with OBytes offset*/
                for (slave = 1; slave <= context.slavecount; slave++)
                {
                    if (group != 0 || (group == context.slavelist[slave].group))
                    {
                        if (context.slavelist[slave].Ibits > 0)
                        {
                            context.slavelist[slave].inputs = context.slavelist[slave].inputs.AsSpan<byte>().Slice((int)context.grouplist[group].Obytes).ToArray();
                        }
                    }
                }

                if (group != 0)
                {
                    /* store output bytes in master record */
                    context.slavelist[0].outputs = pIOmap.ToArray();
                    context.slavelist[0].Obytes = soLogAddr - context.grouplist[group].logstartaddr;
                    context.slavelist[0].inputs = pIOmap.Slice((int)context.slavelist[0].Obytes).ToArray();
                    context.slavelist[0].Ibytes = siLogAddr - context.grouplist[group].logstartaddr;
                }

                return (int)(context.grouplist[group].Obytes + context.grouplist[group].Ibytes);
            }

            return 0;
        }
        int ecx_recover_slave(ecx_contextt context, ushort slave, int timeout)
        {
            int rval;
            int wkc;
            ushort ADPh, configadr, readadr;

            rval = 0;
            configadr = context.slavelist[slave].configadr;
            ADPh = (ushort)(1 - slave);
            /* check if we found another slave than the requested */
            readadr = 0xfffe;
            byte[] bytes = BitConverter.GetBytes(readadr);
            wkc = ecx_APRD(context.port, ADPh, ECT_REG_STADR, sizeof(ushort), bytes, timeout);
            readadr = BitConverter.ToUInt16(bytes, 0);
            /* correct slave found, finished */
            if (readadr == configadr)
            {
                return 1;
            }
            /* only try if no config address*/
            if ((wkc > 0) && (readadr == 0))
            {
                /* clear possible slaves at EC_TEMPNODE */
                ecx_FPWRw(context.port, EC_TEMPNODE, ECT_REG_STADR, htoes(0), 0);
                /* set temporary node address of slave */
                if (ecx_APWRw(context.port, ADPh, ECT_REG_STADR, htoes(EC_TEMPNODE), timeout) <= 0)
                {
                    ecx_FPWRw(context.port, EC_TEMPNODE, ECT_REG_STADR, htoes(0), 0);
                    return 0; /* slave fails to respond */
                }

                context.slavelist[slave].configadr = EC_TEMPNODE; /* temporary config address */
                ecx_eeprom2master(context, slave); /* set Eeprom control to master */

                /* check if slave is the same as configured before */
                if ((ecx_FPRDw(context.port, EC_TEMPNODE, ECT_REG_ALIAS, timeout) ==
                       htoes(context.slavelist[slave].aliasadr)) &&
                    (ecx_readeeprom(context, slave, ECT_SII_ID, EC_TIMEOUTEEP) ==
                       htoel(context.slavelist[slave].eep_id)) &&
                    (ecx_readeeprom(context, slave, ECT_SII_MANUF, EC_TIMEOUTEEP) ==
                       htoel(context.slavelist[slave].eep_man)) &&
                    (ecx_readeeprom(context, slave, ECT_SII_REV, EC_TIMEOUTEEP) ==
                       htoel(context.slavelist[slave].eep_rev)))
                {
                    rval = ecx_FPWRw(context.port, EC_TEMPNODE, ECT_REG_STADR, htoes(configadr), timeout);
                    context.slavelist[slave].configadr = configadr;
                }
                else
                {
                    /* slave is not the expected one, remove config address*/
                    ecx_FPWRw(context.port, EC_TEMPNODE, ECT_REG_STADR, htoes(0), timeout);
                    context.slavelist[slave].configadr = configadr;
                }
            }

            return rval;
        }
        int ecx_reconfig_slave(ecx_contextt context, ushort slave, int timeout)
        {
            int state, nSM, FMMUc;
            ushort configadr;

            configadr = context.slavelist[slave].configadr;
            if (ecx_FPWRw(context.port, configadr, ECT_REG_ALCTL, htoes(EC_STATE_INIT), timeout) <= 0)
            {
                return 0;
            }
            state = 0;
            ecx_eeprom2pdi(context, slave); /* set Eeprom control to PDI */
            /* check state change init */
            state = ecx_statecheck(context, slave, EC_STATE_INIT, EC_TIMEOUTSTATE);
            if (state == EC_STATE_INIT)
            {
                /* program all enabled SM */
                for (nSM = 0; nSM < EC_MAXSM; nSM++)
                {
                    if (context.slavelist[slave].SM[nSM].StartAddr > 0)
                    {
                        byte[] bytes = ToBytes(context.slavelist[slave].SM[nSM]);
                        ecx_FPWR(context.port, configadr, (ushort)(ECT_REG_SM0 + (nSM * bytes.Length)),
                           (ushort)bytes.Length, bytes, timeout);
                        context.slavelist[slave].SM[nSM] = FromBytes<ec_smt>(bytes);
                    }
                }
                ecx_FPWRw(context.port, configadr, ECT_REG_ALCTL, htoes(EC_STATE_PRE_OP), timeout);
                state = ecx_statecheck(context, slave, EC_STATE_PRE_OP, EC_TIMEOUTSTATE); /* check state change pre-op */
                if (state == EC_STATE_PRE_OP)
                {
                    /* execute special slave configuration hook Pre-Op to Safe-OP */
                    //if (context.slavelist[slave].PO2SOconfig) /* only if registered */
                    //{
                    //    context.slavelist[slave].PO2SOconfig(slave);
                    //}
                    //if (context.slavelist[slave].PO2SOconfigx) /* only if registered */
                    //{
                    //    context.slavelist[slave].PO2SOconfigx(context, slave);
                    //}
                    ecx_FPWRw(context.port, configadr, ECT_REG_ALCTL, htoes(EC_STATE_SAFE_OP), timeout); /* set safeop status */
                    state = ecx_statecheck(context, slave, EC_STATE_SAFE_OP, EC_TIMEOUTSTATE); /* check state change safe-op */
                    /* program configured FMMU */
                    for (FMMUc = 0; FMMUc < context.slavelist[slave].FMMUunused; FMMUc++)
                    {
                        byte[] bytes = ToBytes(context.slavelist[slave].FMMU[FMMUc]);
                        ecx_FPWR(context.port, configadr, (ushort)(ECT_REG_FMMU0 + (bytes.Length * FMMUc)), (ushort)bytes.Length, bytes, timeout);
                        context.slavelist[slave].FMMU[FMMUc] = FromBytes<ec_fmmut>(bytes);
                    }
                }
            }

            return state;
        }
















    }
}
