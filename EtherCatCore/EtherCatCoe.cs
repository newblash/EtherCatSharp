using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static EtherCatSharp.EtherCatCore.EtherCatCore;

namespace EtherCatSharp.EtherCatCore
{
    public partial class EtherCatCore
    {
        // 序列化为字节数组
        public static unsafe byte[] ec_SDO_ToBytes(ec_SDO_t sdo)
        {
            int size = Marshal.SizeOf<ec_SDO_t>();
            byte[] buffer = new byte[size];

            fixed (byte* bufferPtr = buffer)
            {
                // 使用MemoryCopy进行内存复制（.NET Core 2.1+）
                Buffer.MemoryCopy(&sdo, bufferPtr, size, size);
            }

            return buffer;
        }
        // 从字节数组反序列化
        public static unsafe ec_SDO_t ec_SDO_FromBytes(byte[] data)
        {
            if (data.Length < Marshal.SizeOf<ec_SDO_t>())
                throw new ArgumentException("数据长度不足");

            ec_SDO_t result = new ec_SDO_t();

            fixed (byte* dataPtr = data)
            {
                Buffer.MemoryCopy(dataPtr, &result, Marshal.SizeOf<ec_SDO_t>(), Marshal.SizeOf<ec_SDO_t>());
            }

            return result;
        }
        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        public unsafe struct ec_SDO_t
        {
            // 固定字段部分
            [FieldOffset(0)]
            public ec_mbxheadert MbxHeader;

            [FieldOffset(6)] // 偏移量需要根据ec_mbxheadert的实际大小调整
            public ushort CANOpen;

            [FieldOffset(8)]
            public byte Command;

            [FieldOffset(9)]
            public ushort Index;

            [FieldOffset(11)]
            public byte SubIndex;

            // 联合体部分 - 使用固定大小的缓冲区
            [FieldOffset(12)]
            private fixed byte _data[0x200];

            // 使用Span<byte>访问数据
            public Span<byte> Bdata
            {
                get
                {
                    unsafe
                    {
                        fixed (byte* ptr = _data)
                        {
                            return new Span<byte>(ptr, 0x200);
                        }
                    }
                }
            }

            // 使用Span<ushort>访问数据
            public Span<ushort> Wdata
            {
                get
                {
                    unsafe
                    {
                        fixed (byte* ptr = _data)
                        {
                            return new Span<ushort>((ushort*)ptr, 0x100);
                        }
                    }
                }
            }

            // 使用Span<uint>访问数据
            public Span<uint> Ldata
            {
                get
                {
                    unsafe
                    {
                        fixed (byte* ptr = _data)
                        {
                            return new Span<uint>((uint*)ptr, 0x80);
                        }
                    }
                }
            }



        }

        void ecx_SDOerror(ecx_contextt context, ushort Slave, ushort Index, byte SubIdx, int AbortCode)
        {
            ec_errort Ec = new ec_errort();
            Ec.Time = new ec_timet() { sec = (uint)DateTime.Now.Second, usec = (uint)((DateTime.Now.Ticks / 10) % 1_000_000) };
            Ec.Slave = Slave;
            Ec.Index = Index;
            Ec.SubIdx = SubIdx;
            context.ecaterror = true;
            Ec.Etype = EC_ERR_TYPE_SDO_ERROR;
            Ec.AbortCode = AbortCode;
            ecx_pusherror(context, Ec);
        }
        uint ecx_readPDOassign(ecx_contextt context, ushort Slave, ushort PDOassign)
        {
            ushort idxloop, nidx, subidxloop, rdat, idx, subidx;
            byte subcnt;
            int wkc, rdl;
            int rdat2;
            ushort bsize = 0;
            rdl = sizeof(ushort);
            rdat = 0;
            byte[] bytes = BitConverter.GetBytes(rdat);
            /* read PDO assign subindex 0 ( = number of PDO's) */
            wkc = ecx_SDOread(context, Slave, PDOassign, 0x00, false, ref rdl, bytes, EC_TIMEOUTRXM);
            rdat = BitConverter.ToUInt16(bytes, 0);
            rdat = etohs(rdat);
            /* positive result from slave ? */
            if ((wkc > 0) && (rdat > 0))
            {
                /* number of available sub indexes */
                nidx = rdat;
                bsize = 0;
                /* read all PDO's */
                for (idxloop = 1; idxloop <= nidx; idxloop++)
                {
                    rdl = sizeof(ushort);
                    rdat = 0;
                    bytes = BitConverter.GetBytes(rdat);
                    /* read PDO assign */
                    wkc = ecx_SDOread(context, Slave, PDOassign, (byte)idxloop, false, ref rdl, bytes, EC_TIMEOUTRXM);
                    rdat = BitConverter.ToUInt16(bytes, 0);
                    /* result is index of PDO */
                    idx = etohs(rdat);
                    if (idx > 0)
                    {
                        rdl = sizeof(byte);
                        subcnt = 0;
                        bytes = BitConverter.GetBytes(subcnt);
                        /* read number of subindexes of PDO */
                        wkc = ecx_SDOread(context, Slave, idx, 0x00, false, ref rdl, bytes, EC_TIMEOUTRXM);
                        subcnt = bytes[0];
                        subidx = subcnt;
                        /* for each subindex */
                        for (subidxloop = 1; subidxloop <= subidx; subidxloop++)
                        {
                            rdl = sizeof(int);
                            rdat2 = 0;
                            bytes = BitConverter.GetBytes(rdat2);
                            /* read SDO that is mapped in PDO */
                            wkc = ecx_SDOread(context, Slave, idx, (byte)subidxloop, false, ref rdl, bytes, EC_TIMEOUTRXM);
                            rdat2 = BitConverter.ToInt32(bytes, 0);
                            rdat2 = (int)etohl((uint)rdat2);
                            /* extract bitlength of SDO */
                            if (LO_BYTE((ushort)rdat2) < 0xff)
                            {
                                bsize += LO_BYTE((ushort)rdat2);
                            }
                            else
                            {
                                rdl = sizeof(ushort);
                                rdat = htoes(0xff);
                                /* read Object Entry in Object database */
                                //                  wkc = ec_readOEsingle(idx, (uint8)SubCount, pODlist, pOElist);
                                bsize += etohs(rdat);
                            }
                        }
                    }
                }
            }
            /* return total found bitlength (PDO) */
            return bsize;
        }
        public int ecx_readPDOmapCA(ecx_contextt context, ushort Slave, int Thread_n, ref uint Osize, ref uint Isize)
        {
            int wkc, rdl;
            int retVal = 0;
            byte nSM, iSM, tSM;
            uint Tsize;
            byte SMt_bug_add;

            Isize = 0;
            Osize = 0;
            SMt_bug_add = 0;

            byte[] bytes = ToBytes(context.SMcommtype[Thread_n]);
            rdl = bytes.Length;
            context.SMcommtype[Thread_n].n = 0;
            /* read SyncManager Communication Type object count Complete Access*/
            wkc = ecx_SDOread(context, Slave, ECT_SDO_SMCOMMTYPE, 0x00, true, ref rdl,
                  bytes, EC_TIMEOUTRXM);
            context.SMcommtype[Thread_n] = FromBytes<ec_SMcommtypet>(bytes);
            /* positive result from slave ? */
            if ((wkc > 0) && (context.SMcommtype[Thread_n].n > 2))
            {
                nSM = context.SMcommtype[Thread_n].n;
                /* limit to maximum number of SM defined, if true the slave can't be configured */
                if (nSM > EC_MAXSM)
                {
                    nSM = EC_MAXSM;
                    ecx_packeterror(context, Slave, 0, 0, 10); /* #SM larger than EC_MAXSM */
                }
                /* iterate for every SM type defined */
                for (iSM = 2; iSM < nSM; iSM++)
                {
                    tSM = context.SMcommtype[Thread_n].SMtype[iSM];

                    // start slave bug prevention code, remove if possible
                    if ((iSM == 2) && (tSM == 2)) // SM2 has type 2 == mailbox out, this is a bug in the slave!
                    {
                        SMt_bug_add = 1; // try to correct, this works if the types are 0 1 2 3 and should be 1 2 3 4
                    }
                    if (tSM > 0)
                    {
                        tSM += SMt_bug_add; // only add if SMt > 0
                    }
                    // end slave bug prevention code

                    context.slavelist[Slave].SMtype[iSM] = tSM;
                    /* check if SM is unused -> clear enable flag */
                    if (tSM == 0)
                    {
                        context.slavelist[Slave].SM[iSM].SMflags =
                           htoel(etohl(context.slavelist[Slave].SM[iSM].SMflags) & EC_SMENABLEMASK);
                    }
                    if ((tSM == 3) || (tSM == 4))
                    {
                        /* read the assign PDO */
                        Tsize = ecx_readPDOassignCA(context, Slave, Thread_n,
                              (ushort)(ECT_SDO_PDOASSIGN + iSM));
                        /* if a mapping is found */
                        if (Tsize > 0)
                        {
                            context.slavelist[Slave].SM[iSM].SMlength = htoes((ushort)((Tsize + 7) / 8));
                            if (tSM == 3)
                            {
                                /* we are doing outputs */
                                Osize += Tsize;
                            }
                            else
                            {
                                /* we are doing inputs */
                                Isize += Tsize;
                            }
                        }
                    }
                }
            }

            /* found some I/O bits ? */
            if ((Isize > 0) || (Osize > 0))
            {
                retVal = 1;
            }
            return retVal;
        }
        uint ecx_readPDOassignCA(ecx_contextt context, ushort Slave, int Thread_n, ushort PDOassign)
        {
            ushort idxloop, nidx, subidxloop, idx, subidx;
            int wkc, rdl;
            uint bsize = 0;

            /* find maximum size of PDOassign buffer */
            byte[] bytes = ToBytes(context.PDOassign[Thread_n]);
            rdl = bytes.Length;

            context.PDOassign[Thread_n].n = 0;
            /* read rxPDOassign in CA mode, all subindexes are read in one struct */
            wkc = ecx_SDOread(context, Slave, PDOassign, 0x00, true, ref rdl, bytes, EC_TIMEOUTRXM);
            context.PDOassign[Thread_n] = FromBytes<ec_PDOassignt>(bytes);
            /* positive result from slave ? */
            if ((wkc > 0) && (context.PDOassign[Thread_n].n > 0))
            {
                nidx = context.PDOassign[Thread_n].n;
                bsize = 0;
                /* for each PDO do */
                for (idxloop = 1; idxloop <= nidx; idxloop++)
                {
                    /* get index from PDOassign struct */
                    idx = etohs((ushort)context.PDOassign[Thread_n].index[idxloop - 1]);
                    if (idx > 0)
                    {
                        bytes = ToBytes(context.PDOdesc[Thread_n]);
                        rdl = bytes.Length;
                        context.PDOdesc[Thread_n].n = 0;
                        /* read SDO's that are mapped in PDO, CA mode */
                        wkc = ecx_SDOread(context, Slave, idx, 0x00, true, ref rdl, bytes, EC_TIMEOUTRXM);
                        context.PDOdesc[Thread_n] = FromBytes<ec_PDOdesct>(bytes);
                        subidx = context.PDOdesc[Thread_n].n;
                        /* extract all bitlengths of SDO's */
                        for (subidxloop = 1; subidxloop <= subidx; subidxloop++)
                        {
                            bsize += LO_BYTE((ushort)etohl(context.PDOdesc[Thread_n].PDO[subidxloop - 1]));
                        }
                    }
                }
            }

            /* return total found bitlength (PDO) */
            return bsize;
        }
        int ecx_readPDOmap(ecx_contextt context, ushort Slave, ref uint Osize, ref uint Isize)
        {
            int wkc, rdl;
            int retVal = 0;
            byte nSM, iSM, tSM;
            uint Tsize;
            byte SMt_bug_add;

            Isize = 0;
            Osize = 0;
            SMt_bug_add = 0;
            rdl = sizeof(byte);
            nSM = 0;
            byte[] bytes = BitConverter.GetBytes(nSM);
            /* read SyncManager Communication Type object count */
            wkc = ecx_SDOread(context, Slave, ECT_SDO_SMCOMMTYPE, 0x00, false, ref rdl, bytes, EC_TIMEOUTRXM);
            nSM = bytes[0];
            /* positive result from slave ? */
            if ((wkc > 0) && (nSM > 2))
            {
                /* limit to maximum number of SM defined, if true the slave can't be configured */
                if (nSM > EC_MAXSM)
                    nSM = EC_MAXSM;
                /* iterate for every SM type defined */
                for (iSM = 2; iSM < nSM; iSM++)
                {
                    rdl = sizeof(byte);
                    tSM = 0;
                    /* read SyncManager Communication Type */
                    bytes = BitConverter.GetBytes(tSM);
                    wkc = ecx_SDOread(context, Slave, ECT_SDO_SMCOMMTYPE, (byte)(iSM + 1), false, ref rdl, bytes, EC_TIMEOUTRXM);
                    tSM = bytes[0];
                    if (wkc > 0)
                    {
                        // start slave bug prevention code, remove if possible
                        if ((iSM == 2) && (tSM == 2)) // SM2 has type 2 == mailbox out, this is a bug in the slave!
                        {
                            SMt_bug_add = 1; // try to correct, this works if the types are 0 1 2 3 and should be 1 2 3 4
                        }
                        if (tSM > 0)
                        {
                            tSM += SMt_bug_add; // only add if SMt > 0
                        }
                        if ((iSM == 2) && (tSM == 0)) // SM2 has type 0, this is a bug in the slave!
                        {
                            tSM = 3;
                        }
                        if ((iSM == 3) && (tSM == 0)) // SM3 has type 0, this is a bug in the slave!
                        {
                            tSM = 4;
                        }
                        // end slave bug prevention code

                        context.slavelist[Slave].SMtype[iSM] = tSM;
                        /* check if SM is unused -> clear enable flag */
                        if (tSM == 0)
                        {
                            context.slavelist[Slave].SM[iSM].SMflags =
                               htoel(etohl(context.slavelist[Slave].SM[iSM].SMflags) & EC_SMENABLEMASK);
                        }
                        if ((tSM == 3) || (tSM == 4))
                        {
                            /* read the assign PDO */
                            Tsize = ecx_readPDOassign(context, Slave, (ushort)(ECT_SDO_PDOASSIGN + iSM));
                            /* if a mapping is found */
                            if (Tsize > 0)
                            {
                                context.slavelist[Slave].SM[iSM].SMlength = htoes((ushort)((Tsize + 7) / 8));
                                if (tSM == 3)
                                {
                                    /* we are doing outputs */
                                    Osize += Tsize;
                                }
                                else
                                {
                                    /* we are doing inputs */
                                    Isize += Tsize;
                                }
                            }
                        }
                    }
                }
            }

            /* found some I/O bits ? */
            if ((Isize > 0) || (Osize > 0))
            {
                retVal = 1;
            }

            return retVal;
        }
        int ecx_SDOread(ecx_contextt context, ushort slave, ushort index, byte subindex,
               bool CA, ref int psize, Span<byte> p, int timeout)
        {
            ec_SDO_t SDOp = new ec_SDO_t();
            ec_SDO_t aSDOp = new ec_SDO_t();
            ushort bytesize, Framedatasize;
            int wkc;
            int SDOlen;
            Span<byte> bp;
            Span<byte> hp;
            byte[] MbxIn = new byte[1487];
            byte[] MbxOut = new byte[1487];
            byte cnt, toggle;
            bool NotLast;
            /* Empty slave out mailbox if something is in. Timeout set to 0 */
            wkc = ecx_mbxreceive(context, slave, MbxIn, 0);
            aSDOp = ec_SDO_FromBytes(MbxIn);
            SDOp = ec_SDO_FromBytes(MbxOut);

            SDOp.MbxHeader.length = htoes(0x000a);
            SDOp.MbxHeader.address = htoes(0x0000);
            SDOp.MbxHeader.priority = 0x00;
            /* get new mailbox count value, used as session handle */
            cnt = ec_nextmbxcnt(context.slavelist[slave].mbx_cnt);
            context.slavelist[slave].mbx_cnt = cnt;
            SDOp.MbxHeader.mbxtype = (byte)(ECT_MBXT_COE + MBX_HDR_SET_CNT(cnt)); /* CoE */
            SDOp.CANOpen = htoes(0x000 + (ECT_COES_SDOREQ << 12)); /* number 9bits service upper 4 bits (SDO request) */
            if (CA)
            {
                SDOp.Command = ECT_SDO_UP_REQ_CA; /* upload request complete access */
            }
            else
            {
                SDOp.Command = ECT_SDO_UP_REQ; /* upload request normal */
            }
            SDOp.Index = htoes(index);
            if (CA && (subindex > 1))
            {
                subindex = 1;
            }
            SDOp.SubIndex = subindex;
            //Ldata前四位数据置为0
            SDOp.Ldata[0] = 0;

            MbxOut = SDOp.ToBytes();
            /* send CoE request to slave */
            wkc = ecx_mbxsend(context, slave, MbxOut, EC_TIMEOUTTXM);
            SDOp = ec_SDO_FromBytes(MbxOut);
            if (wkc > 0) /* succeeded to place mailbox in slave ? */
            {
                /* clean mailboxbuffer */
                ec_clearmbx(MbxIn);
                /* read slave response */
                wkc = ecx_mbxreceive(context, slave, MbxIn, timeout);
                aSDOp = ec_SDO_FromBytes(MbxIn);
                if (wkc > 0) /* succeeded to read slave response ? */
                {
                    /* slave response should be CoE, SDO response and the correct index */
                    if (((aSDOp.MbxHeader.mbxtype & 0x0f) == ECT_MBXT_COE) &&((etohs(aSDOp.CANOpen) >> 12) == ECT_COES_SDORES) &&(aSDOp.Index == SDOp.Index))
                    {
                        if ((aSDOp.Command & 0x02) > 0)
                        {
                            /* expedited frame response */
                            bytesize = (ushort)(4 - ((aSDOp.Command >> 2) & 0x03));
                            if (psize >= bytesize) /* parameter buffer big enough ? */
                            {
                                /* copy parameter in parameter buffer */
                                memcpy(p, 0, aSDOp.Ldata, 0, bytesize);
                                /* return the real parameter size */
                                psize = bytesize;
                            }
                            else
                            {
                                wkc = 0;
                                ecx_packeterror(context, slave, index, subindex, 3); /*  data container too small for type */
                            }
                        }
                        else
                        { /* normal frame response */
                            SDOlen = (int)etohl(aSDOp.Ldata[0]);
                            /* Does parameter fit in parameter buffer ? */
                            if (SDOlen <= psize)
                            {
                                bp = p;
                                hp = p;
                                /* calculate mailbox transfer size */
                                Framedatasize = (ushort)(etohs(aSDOp.MbxHeader.length) - 10);
                                if (Framedatasize < SDOlen) /* transfer in segments? */
                                {
                                    /* copy parameter data in parameter buffer */
                                    memcpy(hp, 0, aSDOp.Ldata, 1, Framedatasize);
                                    /* increment buffer pointer */
                                    hp.Slice(Framedatasize);
                                    psize = Framedatasize;
                                    NotLast = true;
                                    toggle = 0x00;
                                    while (NotLast) /* segmented transfer */
                                    {
                                        SDOp = ec_SDO_FromBytes(MbxOut);
                                        SDOp.MbxHeader.length = htoes(0x000a);
                                        SDOp.MbxHeader.address = htoes(0x0000);
                                        SDOp.MbxHeader.priority = 0x00;
                                        cnt = ec_nextmbxcnt(context.slavelist[slave].mbx_cnt);
                                        context.slavelist[slave].mbx_cnt = cnt;
                                        SDOp.MbxHeader.mbxtype = (byte)(ECT_MBXT_COE + MBX_HDR_SET_CNT(cnt)); /* CoE */
                                        SDOp.CANOpen = htoes(0x000 + (ECT_COES_SDOREQ << 12)); /* number 9bits service upper 4 bits (SDO request) */
                                        SDOp.Command = (byte)(ECT_SDO_SEG_UP_REQ + toggle); /* segment upload request */
                                        SDOp.Index = htoes(index);
                                        SDOp.SubIndex = subindex;
                                        
                                        SDOp.Ldata[0] = 0;
                                        MbxOut = SDOp.ToBytes();
                                        /* send segmented upload request to slave */
                                        wkc = ecx_mbxsend(context, slave, MbxOut, EC_TIMEOUTTXM);
                                        SDOp = ec_SDO_FromBytes(MbxOut);
                                        /* is mailbox transferred to slave ? */
                                        if (wkc > 0)
                                        {
                                            ec_clearmbx(MbxIn);
                                            /* read slave response */
                                            wkc = ecx_mbxreceive(context, slave, MbxIn, timeout);
                                            aSDOp = ec_SDO_FromBytes(MbxIn);
                                            /* has slave responded ? */
                                            if (wkc > 0)
                                            {
                                                /* slave response should be CoE, SDO response */
                                                if ((((aSDOp.MbxHeader.mbxtype & 0x0f) == ECT_MBXT_COE) &&
                                                     ((etohs(aSDOp.CANOpen) >> 12) == ECT_COES_SDORES) &&
                                                     ((aSDOp.Command & 0xe0) == 0x00)))
                                                {
                                                    /* calculate mailbox transfer size */
                                                    Framedatasize = (ushort)(etohs(aSDOp.MbxHeader.length) - 3);
                                                    if ((aSDOp.Command & 0x01) > 0)
                                                    { /* last segment */
                                                        NotLast = false;
                                                        if (Framedatasize == 7)
                                                            /* subtract unused bytes from frame */
                                                            Framedatasize = (ushort)(Framedatasize - ((aSDOp.Command & 0x0e) >> 1));
                                                        /* copy to parameter buffer */
                                                        memcpy(hp, 0, BitConverter.GetBytes(aSDOp.Index), 0, Framedatasize);
                                                    }
                                                    else /* segments follow */
                                                    {
                                                        /* copy to parameter buffer */
                                                        memcpy(hp, 0, BitConverter.GetBytes(aSDOp.Index), 0, Framedatasize);
                                                        /* increment buffer pointer */
                                                        hp.Slice(Framedatasize);
                                                    }
                                                    /* update parameter size */
                                                    psize += Framedatasize;
                                                }
                                                /* unexpected frame returned from slave */
                                                else
                                                {
                                                    NotLast = false;
                                                    if ((aSDOp.Command) == ECT_SDO_ABORT) /* SDO abort frame received */
                                                        ecx_SDOerror(context, slave, index, subindex, (int)etohl(aSDOp.Ldata[0]));
                                                    else
                                                        ecx_packeterror(context, slave, index, subindex, 1); /* Unexpected frame returned */
                                                    wkc = 0;
                                                }
                                            }
                                        }
                                        toggle = (byte)(toggle ^ 0x10); /* toggle bit for segment request */
                                    }
                                }
                                /* non segmented transfer */
                                else
                                {
                                    /* copy to parameter buffer */
                                    //memcpy(bp, 0, aSDOp.Ldata, 1, SDOlen); //这个方法不好使了,换成直接赋值
                                    // 获取 ldata 的 Span
                                    Span<uint> ldataSpan = aSDOp.Ldata;

                                    // 获取索引 1 处的 uint 的字节表示
                                    Span<byte> sourceBytes = MemoryMarshal.AsBytes(ldataSpan.Slice(1, SDOlen));

                                    // 只取前 SDOlen 个字节
                                    sourceBytes = sourceBytes.Slice(0, SDOlen);
                                    // 复制到 bp
                                    sourceBytes.CopyTo(bp);
                                    psize = SDOlen;
                                }
                            }
                            /* parameter buffer too small */
                            else
                            {
                                wkc = 0;
                                ecx_packeterror(context, slave, index, subindex, 3); /*  data container too small for type */
                            }
                        }
                    }
                    /* other slave response */
                    else
                    {
                        if ((aSDOp.Command) == ECT_SDO_ABORT) /* SDO abort frame received */
                        {
                            ecx_SDOerror(context, slave, index, subindex, (int)etohl(aSDOp.Ldata[0]));
                        }
                        else
                        {
                            ecx_packeterror(context, slave, index, subindex, 1); /* Unexpected frame returned */
                        }
                        wkc = 0;
                    }
                }
            }
            return wkc;
        }
    }
}
