// Copyright 2010 Chris Morgan <chmorgan@gmail.com>
// Copyright 2021 Ayoub Kaanich <kayoub5@live.com>
//
// SPDX-License-Identifier: MIT

using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SharpPcap
{

    /// <summary>
    /// Item in a list of interfaces.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct pcap_if
    {
        public IntPtr /* pcap_if* */    Next;
        public string Name;           /* name to hand to "pcap_open_live()" */
        public string Description;    /* textual description of interface, or NULL */
        public IntPtr /*pcap_addr * */  Addresses;
        public UInt32 Flags;          /* PCAP_IF_ interface flags */
    };

    /// <summary>
    /// A BPF pseudo-assembly program for packet filtering
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct bpf_program
    {
        public uint bf_len;
        public IntPtr /* bpf_insn **/ bf_insns;
    };
    public class BpfProgram : SafeHandleZeroOrMinusOneIsInvalid
    {

        // pcap_compile() in 1.8.0 and later is newly thread-safe
        // Requires calls to pcap_compile to be non-concurrent to avoid crashes due to known lack of thread-safety
        // See https://github.com/chmorgan/sharppcap/issues/311
        // Problem of thread safety does not affect Windows
        private static readonly object SyncCompile = new object();

        public static BpfProgram? TryCreate(PcapHandle pcapHandle, string filter, int optimize = 1, uint netmask = 0)
        {
            var bpfProgram = new BpfProgram();
            int result;

            result = LibPcapSafeNativeMethods.pcap_compile(pcapHandle, bpfProgram, filter, optimize, netmask);

            if (result < 0)
            {
                // Don't use Dispose since we don't want pcap_freecode to be called here
                Marshal.FreeHGlobal(bpfProgram.handle);
                bpfProgram.SetHandle(IntPtr.Zero);
                bpfProgram = null;
            }
            return bpfProgram;
        }


        public static BpfProgram Create(PcapHandle pcapHandle, string filter, int optimize = 1, uint netmask = 0)
        {
            var bpfProgram = TryCreate(pcapHandle, filter, optimize, netmask);
            if (bpfProgram == null)
            {
                throw new PcapException(PcapDevice.GetLastError(pcapHandle));
            }
            return bpfProgram;
        }
        private BpfProgram()
            : base(true)
        {
            var bpfProgram = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(bpf_program)));
            SetHandle(bpfProgram);
        }

        protected override bool ReleaseHandle()
        {
            LibPcapSafeNativeMethods.pcap_freecode(handle);
            //Alocate an unmanaged buffer
            Marshal.FreeHGlobal(handle);
            return true;
        }
    }
}
