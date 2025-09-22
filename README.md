# EtherCatSharp
## 目标:仿照SOEM1.4版本创建一个.net版本的ethercat主站
```
代码状态:
  底层网卡通讯:√
  查找从站:√
  配置从站IOmap:√
  配置从站SM:√
  配置从站FMMU:√
  读取从站信息:√
  SDO读写:√
  PDO读写:×
  配置DC:×
```
使用方法:
```
        static void Main(string[] args)
        {
            EtherCatCore etherCat = new EtherCatCore();
            ecx_contextt ctx = new ecx_contextt();
            etherCat.ecx_initmbxpool(ctx);
            byte[] IOmap = new byte[4096];
            if (etherCat.ecx_setupnic(ctx.port, @"\Device\NPF_{70B03C0F-5B64-4F13-9BA4-4E24E2CBE66E}", false) > 0)
            {
                //var ins = etherCat.ecx_detect_slaves(ref ecx_Portt);
                //Console.WriteLine("从站数量:" + ins);
                //etherCat.ecx_set_slaves_to_default(ref ecx_Portt);
                var wkc = etherCat.ecx_config_init(ctx, false);
                //etherCat.ecx_readstate(ctx);
                //Console.WriteLine("从站数量:" + wkc);
                if (wkc > 0)
                {
                    int a = etherCat.ecx_main_config_map_group(ctx, IOmap, 0, false);
                    Console.WriteLine("配置从站数量:" + wkc);
                    int cnt, i, j, nSM = 0;
                    ushort ssigen;
                    int expectedWKC = (ctx.grouplist[0].outputsWKC * 2) + ctx.grouplist[0].inputsWKC;
                    Console.WriteLine($"WKC={expectedWKC}");
                    //Console.WriteLine("检查从站状态");
                    etherCat.ecx_statecheck(ctx, 0, EC_STATE_SAFE_OP, EC_TIMEOUTSTATE * 3);
                    //Console.WriteLine("检查从站状态结束");
                    if (ctx.slavelist[0].state != EC_STATE_SAFE_OP)
                    {
                        Console.WriteLine("Not all slaves reached safe operational state.\n");
                        etherCat.ecx_readstate(ctx);
                        for (i = 1; i <= ctx.slavecount; i++)
                        {
                            if (ctx.slavelist[i].state != EC_STATE_SAFE_OP)
                            {
                                Console.WriteLine($"Slave {i} State={ctx.slavelist[i].state} StatusCode={ctx.slavelist[i].ALstatuscode}");
                            }
                        }
                    }
                    etherCat.ecx_readstate(ctx);
                    for (cnt = 1; cnt <= ctx.slavecount; cnt++)
                    {
                        Console.WriteLine($"\nSlave:{cnt}\n Name:{ctx.slavelist[cnt].name}\n Output size: {ctx.slavelist[cnt].Obits}bits\n Input size: {ctx.slavelist[cnt].Ibits}bits\n State: {ctx.slavelist[cnt].state}\n Delay: {ctx.slavelist[cnt].pdelay}[ns]\n Has DC: {ctx.slavelist[cnt].hasdc}");
                        if (ctx.slavelist[cnt].hasdc)
                            Console.WriteLine($" DCParentport:{ctx.slavelist[cnt].parentport}");
                        Console.WriteLine($" Activeports:{(ctx.slavelist[cnt].activeports & 0x01) > 0}.{(ctx.slavelist[cnt].activeports & 0x02) > 0}.{(ctx.slavelist[cnt].activeports & 0x04) > 0}.{(ctx.slavelist[cnt].activeports & 0x08) > 0}\n");
                        Console.WriteLine($" Configured address: {ctx.slavelist[cnt].configadr:X4}");
                        Console.WriteLine($" Man: {(int)ctx.slavelist[cnt].eep_man:X8} ID: {(int)ctx.slavelist[cnt].eep_id:X8} Rev: {(int)ctx.slavelist[cnt].eep_rev:X8}");
                        for (nSM = 0; nSM < EC_MAXSM; nSM++)
                        {
                            if (ctx.slavelist[cnt].SM[nSM].StartAddr > 0)
                                Console.WriteLine($" SM{nSM:X} A:{etohs(ctx.slavelist[cnt].SM[nSM].StartAddr):X4} L:{etohs(ctx.slavelist[cnt].SM[nSM].SMlength)} F:{etohl(ctx.slavelist[cnt].SM[nSM].SMflags):X8} Type:{ctx.slavelist[cnt].SMtype[nSM]:X}");
                        }
                        for (j = 0; j < ctx.slavelist[cnt].FMMUunused; j++)
                        {
                            Console.WriteLine($" FMMU{j} Ls:{etohl(ctx.slavelist[cnt].FMMU[j].LogStart):X8} Ll:{etohs(ctx.slavelist[cnt].FMMU[j].LogLength)} Lsb:{ctx.slavelist[cnt].FMMU[j].LogStartbit} Leb:{ctx.slavelist[cnt].FMMU[j].LogEndbit} Ps:{etohs(ctx.slavelist[cnt].FMMU[j].PhysStart):X4} Psb:{ctx.slavelist[cnt].FMMU[j].PhysStartBit} Ty:{ctx.slavelist[cnt].FMMU[j].FMMUtype:X2} Act:{ctx.slavelist[cnt].FMMU[j].FMMUactive:X2}");
                        }
                        Console.WriteLine($" FMMUfunc 0:{ctx.slavelist[cnt].FMMU0func} 1:{ctx.slavelist[cnt].FMMU1func} 2:{ctx.slavelist[cnt].FMMU2func} 3:{ctx.slavelist[cnt].FMMU3func}");
                        Console.WriteLine($" MBX length wr: {ctx.slavelist[cnt].mbx_l} rd: {ctx.slavelist[cnt].mbx_rl} MBX protocols : {ctx.slavelist[cnt].mbx_proto}");
                        ssigen = (ushort)etherCat.ecx_siifind(ctx, (ushort)cnt, ECT_SII_GENERAL);
                        /* SII general section */
                        if (ssigen > 0)
                        {
                            ctx.slavelist[cnt].CoEdetails = etherCat.ecx_siigetbyte(ctx, (ushort)cnt, (ushort)(ssigen + 0x07));
                            ctx.slavelist[cnt].FoEdetails = etherCat.ecx_siigetbyte(ctx, (ushort)cnt, (ushort)((ushort)ssigen + 0x08));
                            ctx.slavelist[cnt].EoEdetails = etherCat.ecx_siigetbyte(ctx, (ushort)cnt, (ushort)((ushort)ssigen + 0x09));
                            ctx.slavelist[cnt].SoEdetails = etherCat.ecx_siigetbyte(ctx, (ushort)cnt, (ushort)((ushort)ssigen + 0x0a));
                            if ((etherCat.ecx_siigetbyte(ctx, (ushort)cnt, (ushort)(ssigen + 0x0d)) & 0x02) > 0)
                            {
                                ctx.slavelist[cnt].blockLRW = 1;
                                ctx.slavelist[0].blockLRW++;
                            }
                            ctx.slavelist[cnt].Ebuscurrent = etherCat.ecx_siigetbyte(ctx, (ushort)cnt, (ushort)(ssigen + 0x0e));
                            ctx.slavelist[cnt].Ebuscurrent += (short)(etherCat.ecx_siigetbyte(ctx, (ushort)cnt, (ushort)(ssigen + 0x0f)) << 8);
                            ctx.slavelist[0].Ebuscurrent += ctx.slavelist[cnt].Ebuscurrent;
                        }
                        Console.WriteLine($" CoE details: {ctx.slavelist[cnt].CoEdetails:X2} FoE details: {ctx.slavelist[cnt].FoEdetails:X2} EoE details:{ctx.slavelist[cnt].EoEdetails:X2} SoE details: {ctx.slavelist[cnt].SoEdetails:X2}");
                        Console.WriteLine($" Ebus current: {ctx.slavelist[cnt].Ebuscurrent}[mA]\n only LRD/LWR:{ctx.slavelist[cnt].blockLRW}\n");
                        //if ((ctx.slavelist[cnt].mbx_proto & ECT_MBXPROT_COE) && printSDO)
                        //    si_sdo(cnt);
                        //if (printMAP)
                        //{
                        //    if (ctx.slavelist[cnt].mbx_proto & ECT_MBXPROT_COE)
                        //        si_map_sdo(cnt);
                        //    else
                        //        si_map_sii(cnt);
                        //}
                    }

                }
                else
                {
                    Console.WriteLine("未查找到从站");
                }
            }
            else
            {
                Console.WriteLine("初始化mbx pool错误");
            }


            etherCat.ecx_closenic();
        }
```
