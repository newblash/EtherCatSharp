using System;
using System.Collections.Generic;
using System.Text;

namespace EtherCatSharp.EtherCatCore
{
    public partial class EtherCatCore
    {
        //public int ecx_readIDNmap(ecx_contextt context, ushort slave, ref uint Osize, ref uint Isize)
        //{
        //    int retVal = 0;
        //    int wkc;
        //    int psize;
        //    byte driveNr;
        //    ushort entries, itemcount;
        //    ec_SoEmappingt SoEmapping;
        //    ec_SoEattributet SoEattribute;

        //    Isize = 0;
        //    Osize = 0;
        //    for (driveNr = 0; driveNr < EC_SOE_MAX_DRIVES; driveNr++)
        //    {
        //        psize = sizeof(SoEmapping);
        //        /* read output mapping via SoE */
        //        wkc = ecx_SoEread(context, slave, driveNr, EC_SOE_VALUE_B, EC_IDN_MDTCONFIG, &psize, &SoEmapping, EC_TIMEOUTRXM);
        //        if ((wkc > 0) && (psize >= 4) && ((entries = etohs(SoEmapping.currentlength) / 2) > 0) && (entries <= EC_SOE_MAXMAPPING))
        //        {
        //            /* command word (uint16) is always mapped but not in list */
        //            *Osize += 16;
        //            for (itemcount = 0; itemcount < entries; itemcount++)
        //            {
        //                psize = sizeof(SoEattribute);
        //                /* read attribute of each IDN in mapping list */
        //                wkc = ecx_SoEread(context, slave, driveNr, EC_SOE_ATTRIBUTE_B, SoEmapping.idn[itemcount], &psize, &SoEattribute, EC_TIMEOUTRXM);
        //                if ((wkc > 0) && (!SoEattribute.list))
        //                {
        //                    /* length : 0 = 8bit, 1 = 16bit .... */
        //                    *Osize += (int)8 << SoEattribute.length;
        //                }
        //            }
        //        }
        //        psize = sizeof(SoEmapping);
        //        /* read input mapping via SoE */
        //        wkc = ecx_SoEread(context, slave, driveNr, EC_SOE_VALUE_B, EC_IDN_ATCONFIG, &psize, &SoEmapping, EC_TIMEOUTRXM);
        //        if ((wkc > 0) && (psize >= 4) && ((entries = etohs(SoEmapping.currentlength) / 2) > 0) && (entries <= EC_SOE_MAXMAPPING))
        //        {
        //            /* status word (uint16) is always mapped but not in list */
        //            Isize += 16;
        //            for (itemcount = 0; itemcount < entries; itemcount++)
        //            {
        //                psize = sizeof(SoEattribute);
        //                /* read attribute of each IDN in mapping list */
        //                wkc = ecx_SoEread(context, slave, driveNr, EC_SOE_ATTRIBUTE_B, SoEmapping.idn[itemcount], &psize, &SoEattribute, EC_TIMEOUTRXM);
        //                if ((wkc > 0) && (!SoEattribute.list))
        //                {
        //                    /* length : 0 = 8bit, 1 = 16bit .... */
        //                    Isize += (int)8 << SoEattribute.length;
        //                }
        //            }
        //        }
        //    }

        //    /* found some I/O bits ? */
        //    if ((Isize > 0) || (Osize > 0))
        //    {
        //        retVal = 1;
        //    }
        //    return retVal;
        //}
    }
}
