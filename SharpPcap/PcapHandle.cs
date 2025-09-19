// Copyright 2010 Chris Morgan <chmorgan@gmail.com>
// Copyright 2021 Ayoub Kaanich <kayoub5@live.com>
//
// SPDX-License-Identifier: MIT

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace SharpPcap
{
    public class PcapHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public PcapHandle()
            : base(true)
        {
        }

        private PcapHandle(IntPtr handle)
            : base(true)
        {
            SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            LibPcapSafeNativeMethods.pcap_close(handle);
            return true;
        }

        internal static readonly PcapHandle Invalid = new PcapHandle(IntPtr.Zero);
    }

}
