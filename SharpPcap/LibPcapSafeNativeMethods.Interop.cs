// Copyright 2005 Tamir Gal <tamir@tamirgal.com>
// Copyright 2008-2009 Phillip Lemon <lucidcomms@gmail.com>
// Copyright 2008-2011 Chris Morgan <chmorgan@gmail.com>
//
// SPDX-License-Identifier: MIT

using EtherCatSharp.SharpPcap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace SharpPcap
{

    [StructLayout(LayoutKind.Sequential)]
    public struct ErrorBuffer
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        internal byte[] Data;

        public override string ToString()
        {
            var nbBytes = 0;
            while (Data[nbBytes] != 0)
            {
                nbBytes++;
            }
            return Encoding.UTF8.GetString(Data, 0, nbBytes);
        }
    }


    /// <summary>
    /// 静态包装类，提供对 LibPcapLibrary 的全局访问
    /// </summary>
    public static class LibPcapSafeNativeMethods
    {
        /// <summary>
        /// 单例实例
        /// </summary>
        private static readonly LibPcapLibrary Instance = CreateInstance();

        // ✅ 静态构造函数（只会执行一次）
        static LibPcapSafeNativeMethods()
        {
            // 这里可以加日志或初始化逻辑
        }

        private static LibPcapLibrary CreateInstance()
        {
            var lib = new LibPcapLibrary();
            if (!lib.Load())
            {
                throw new DllNotFoundException(
                    "无法加载 wpcap.dll 或 libpcap.so。请确保：\n" +
                    " - Windows: 安装 Npcap (https://npcap.com)\n" +
                    " - Linux: 安装 libpcap-dev\n" +
                    " - macOS: 安装 libpcap");
            }
            return lib;
        }

        // ✅ 代理函数（可选）：直接暴露常用函数
        //public static int pcap_init(uint opts, out ErrorBuffer errbuf) => Instance.pcap_init(opts, out errbuf);
        public static int pcap_findalldevs(ref IntPtr alldevs, out ErrorBuffer errbuf) => Instance.pcap_findalldevs(ref alldevs, out errbuf);
        public static void pcap_freealldevs(IntPtr alldevs) => Instance.pcap_freealldevs(alldevs);
        public static PcapHandle pcap_open(string dev, int packetLen, int flags, int read_timeout, IntPtr rmtauth, out ErrorBuffer errbuf) => Instance.pcap_open(dev, packetLen, flags, read_timeout, rmtauth, out errbuf);
        public static PcapHandle pcap_create(string dev, out ErrorBuffer errbuf) => Instance.pcap_create(dev, out errbuf);
        public static PcapError pcap_set_buffer_size(PcapHandle adapter, int bufferSizeInBytes) => Instance.pcap_set_buffer_size(adapter, bufferSizeInBytes);
        public static PcapError pcap_set_immediate_mode(PcapHandle adapter, int immediate_mode) => Instance.pcap_set_immediate_mode(adapter, immediate_mode);
        public static void pcap_close(IntPtr adaptHandle) => Instance.pcap_close(adaptHandle);
        public static int pcap_next_ex(PcapHandle adaptHandle, ref IntPtr header, ref IntPtr data) => Instance.pcap_next_ex(adaptHandle, ref header, ref data);
        public static int pcap_sendpacket(PcapHandle adaptHandle, byte[] data, int size) => Instance.pcap_sendpacket(adaptHandle, data, size);
        public static int pcap_compile(PcapHandle adaptHandle, BpfProgram fp, string str, int optimize, UInt32 netmask) => Instance.pcap_compile(adaptHandle, fp, str, optimize, netmask);
        public static int pcap_setfilter(PcapHandle adaptHandle, BpfProgram fp) => Instance.pcap_setfilter(adaptHandle, fp);
        public static void pcap_freecode(IntPtr fp) => Instance.pcap_freecode(fp);
        public static string pcap_geterr(PcapHandle adaptHandle) => Instance.pcap_geterr(adaptHandle);
        public static int pcap_datalink(PcapHandle adaptHandle) => Instance.pcap_datalink(adaptHandle);
        public static int pcap_get_selectable_fd(PcapHandle adaptHandle) => Instance.pcap_get_selectable_fd(adaptHandle);
        public static PcapError pcap_set_snaplen(PcapHandle p, int snaplen) => Instance.pcap_set_snaplen(p, snaplen);
        public static PcapError pcap_set_promisc(PcapHandle p, int promisc) => Instance.pcap_set_promisc(p, promisc);
        public static PcapError pcap_set_timeout(PcapHandle p, int to_ms) => Instance.pcap_set_timeout(p, to_ms);
        public static PcapError pcap_activate(PcapHandle p) => Instance.pcap_activate(p);
        public static PcapError pcap_setbuff(PcapHandle adapter, int bufferSizeInBytes) => Instance.pcap_setbuff(adapter, bufferSizeInBytes);
        public static PcapError pcap_setmintocopy(PcapHandle adapter, int sizeInBytes) => Instance.pcap_setmintocopy(adapter, sizeInBytes);
    }
}
