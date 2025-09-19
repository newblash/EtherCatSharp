// Copyright 2005 Tamir Gal <tamir@tamirgal.com>
// Copyright 2009 Chris Morgan <chmorgan@gmail.com>
//
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.NetworkInformation;

namespace SharpPcap
{
    /// <summary>
    /// managed version of struct pcap_if
    /// NOTE: we can't use pcap_if directly because the class contains
    ///       a pointer to pcap_if that will be freed when the
    ///       device memory is freed, so instead convert the unmanaged structure
    ///       to a managed one to avoid this issue
    /// </summary>
    public class PcapInterface
    {
        /// <value>
        /// Name of the interface. Used internally when passed to pcap_open_live()
        /// </value>
        public string Name { get; internal set; }

        /// <value>
        /// Text description of the interface as given by pcap/npcap
        /// </value>
        public string Description { get; internal set; }

        /// <value>
        /// Pcap interface flags
        /// </value>
        public uint Flags { get; internal set; }

        internal PcapInterface(pcap_if pcapIf, NetworkInterface? networkInterface)
        {
            Name = pcapIf.Name;
            Description = pcapIf.Description;
            Flags = pcapIf.Flags;
        }

        //只调用了这个方法用于获取所有的网卡接口
        static public IReadOnlyList<PcapInterface> GetAllPcapInterfaces()
        {
            var devicePtr = IntPtr.Zero;

            int result = LibPcapSafeNativeMethods.pcap_findalldevs(ref devicePtr, out var errbuf);
            if (result < 0)
            {
                throw new PcapException(errbuf.ToString());
            }
            var pcapInterfaces = GetAllPcapInterfaces(devicePtr);

            // Free unmanaged memory allocation
            LibPcapSafeNativeMethods.pcap_freealldevs(devicePtr);

            return pcapInterfaces;
        }
        static private IReadOnlyList<PcapInterface> GetAllPcapInterfaces(IntPtr devicePtr)
        {
            var list = new List<PcapInterface>();
            var nics = NetworkInterface.GetAllNetworkInterfaces();
            var nextDevPtr = devicePtr;
            while (nextDevPtr != IntPtr.Zero)
            {
                // Marshal pointer into a struct
                var pcap_if_unmanaged = Marshal.PtrToStructure<pcap_if>(nextDevPtr);
                NetworkInterface? networkInterface = null;
                foreach (var nic in nics)
                {
                    // if the name and id match then we have found the NetworkInterface
                    // that matches the PcapDevice
                    if (pcap_if_unmanaged.Name.EndsWith(nic.Id))
                    {
                        networkInterface = nic;
                    }
                }
                var pcap_if = new PcapInterface(pcap_if_unmanaged, networkInterface);
                list.Add(pcap_if);
                nextDevPtr = pcap_if_unmanaged.Next;
            }

            return list;
        }
    }
}
