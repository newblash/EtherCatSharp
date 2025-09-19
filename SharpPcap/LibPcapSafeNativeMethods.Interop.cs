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
    ///// <summary>
    ///// Per http://msdn.microsoft.com/en-us/ms182161.aspx 
    ///// </summary>
    //[SuppressUnmanagedCodeSecurity]
    //internal static partial class LibPcapSafeNativeMethods_d
    //{
    //    /// <summary>
    //    /// This default is good enough for .NET Framework and .NET Core on non Windows with Libpcap default config
    //    /// </summary>
    //    internal static readonly Encoding StringEncoding = Encoding.Default;

    //    private static Encoding ConfigureStringEncoding()
    //    {
    //        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    //        {
    //            // libpcap always use UTF-8 when not on Windows
    //            return Encoding.UTF8;
    //        }
    //        try
    //        {
    //            // Try to change Libpcap to UTF-8 mode
    //            const uint PCAP_CHAR_ENC_UTF_8 = 1;
    //            var res = pcap_init(PCAP_CHAR_ENC_UTF_8, out _);
    //            if (res == 0)
    //            {
    //                // We made it
    //                return Encoding.UTF8;
    //            }
    //        }
    //        catch (TypeLoadException)
    //        {
    //            // pcap_init not supported, using old Libpcap
    //        }
    //        // Needed especially in .NET Core, to make sure codepage 0 returns the system default non-unicode code page
    //        //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    //        // In windows by default, system code page is used
    //        return Encoding.GetEncoding(0);
    //    }

    //    //[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    //    //[return: MarshalAs(UnmanagedType.Bool)]
    //    //static extern bool SetDllDirectory(string lpPathName);

    //    static LibPcapSafeNativeMethods()
    //    {
    //        //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    //        //{
    //        //    SetDllDirectory(Path.Combine(Environment.SystemDirectory, "Npcap"));
    //        //}
    //        StringEncoding = ConfigureStringEncoding();
    //    }

    //    internal static PcapError pcap_setbuff(PcapHandle adapter, int bufferSizeInBytes)
    //    {
    //        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
    //            ? _pcap_setbuff(adapter, bufferSizeInBytes)
    //            : PcapError.PlatformNotSupported;
    //    }
    //    internal static PcapError pcap_setmintocopy(PcapHandle adapter, int sizeInBytes)
    //    {
    //        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
    //            ? _pcap_setmintocopy(adapter, sizeInBytes)
    //            : PcapError.PlatformNotSupported;
    //    }
    //    // NOTE: For mono users on non-windows platforms a .config file is used to map
    //    //       the windows dll name to the unix/mac library name
    //    //       This file is called $assembly_name.dll.config and is placed in the
    //    //       same directory as the assembly
    //    //       See http://www.mono-project.com/Interop_with_Native_Libraries#Library_Names
    //    private const string PCAP_DLL = "wpcap";

    //    [DllImport(PCAP_DLL, CallingConvention = CallingConvention.Cdecl)]
    //    internal extern static int pcap_init(
    //        uint opts,
    //        out ErrorBuffer /* char* */ errbuf
    //    );

    //    [DllImport(PCAP_DLL, CallingConvention = CallingConvention.Cdecl)]
    //    internal extern static int pcap_findalldevs(
    //        ref IntPtr /* pcap_if_t** */ alldevs,
    //        out ErrorBuffer /* char* */ errbuf
    //    );

    //    [DllImport(PCAP_DLL, CallingConvention = CallingConvention.Cdecl)]
    //    internal extern static void pcap_freealldevs(IntPtr /* pcap_if_t * */ alldevs);

    //    [DllImport(PCAP_DLL, CallingConvention = CallingConvention.Cdecl)]
    //    internal extern static PcapHandle pcap_open(
    //        string dev,
    //        int packetLen,
    //        int flags,
    //        int read_timeout,
    //        IntPtr rmtauth,
    //        out ErrorBuffer /* char* */ errbuf
    //    );

    //    [DllImport(PCAP_DLL, CallingConvention = CallingConvention.Cdecl)]
    //    internal extern static PcapHandle pcap_create(
    //        string dev,
    //        out ErrorBuffer /* char* */ errbuf
    //    );

    //    [DllImport(PCAP_DLL, CallingConvention = CallingConvention.Cdecl)]
    //    internal extern static PcapError pcap_set_buffer_size(PcapHandle adapter, int bufferSizeInBytes);

    //    [DllImport(PCAP_DLL, CallingConvention = CallingConvention.Cdecl)]
    //    internal extern static PcapError pcap_set_immediate_mode(PcapHandle adapter, int immediate_mode);

    //    /// <summary> close the files associated with p and deallocates resources.</summary>
    //    [DllImport(PCAP_DLL, CallingConvention = CallingConvention.Cdecl)]
    //    internal extern static void pcap_close(IntPtr /*pcap_t * */ adaptHandle);

    //    /// <summary>
    //    /// To avoid callback, this returns one packet at a time
    //    /// </summary>
    //    [DllImport(PCAP_DLL, CallingConvention = CallingConvention.Cdecl)]
    //    internal extern static int pcap_next_ex(PcapHandle adaptHandle, ref IntPtr header, ref IntPtr data);

    //    /// <summary>
    //    /// Send a raw packet.<br/>
    //    /// This function allows to send a raw packet to the network. 
    //    /// The MAC CRC doesn't need to be included, because it is transparently calculated
    //    ///  and added by the network interface driver.
    //    /// </summary>
    //    /// <param name="adaptHandle">the interface that will be used to send the packet</param>
    //    /// <param name="data">contains the data of the packet to send (including the various protocol headers)</param>
    //    /// <param name="size">the dimension of the buffer pointed by data</param>
    //    /// <returns>0 if the packet is succesfully sent, -1 otherwise.</returns>
    //    [DllImport(PCAP_DLL, CallingConvention = CallingConvention.Cdecl)]
    //    internal extern static int pcap_sendpacket(PcapHandle adaptHandle, byte[] data, int size);

    //    /// <summary>
    //    /// Compile a packet filter, converting an high level filtering expression (see Filtering expression syntax) in a program that can be interpreted by the kernel-level filtering engine. 
    //    /// </summary>
    //    [DllImport(PCAP_DLL, CallingConvention = CallingConvention.Cdecl)]
    //    internal extern static int pcap_compile(
    //        PcapHandle adaptHandle, BpfProgram fp,
    //        string str,
    //        int optimize, UInt32 netmask
    //    );

    //    [DllImport(PCAP_DLL, CallingConvention = CallingConvention.Cdecl)]
    //    internal extern static int pcap_setfilter(PcapHandle adaptHandle, BpfProgram fp);
    //    /// <summary>
    //    /// Free up allocated memory pointed to by a bpf_program struct generated by pcap_compile()
    //    /// </summary>
    //    [DllImport(PCAP_DLL, CallingConvention = CallingConvention.Cdecl)]
    //    internal extern static void pcap_freecode(IntPtr fp);

    //    /// <summary>
    //    /// return the error text pertaining to the last pcap library error.
    //    /// </summary>
    //    [DllImport(PCAP_DLL, CallingConvention = CallingConvention.Cdecl)]
    //    internal extern static string pcap_geterr(PcapHandle /*pcap_t * */ adaptHandle);

    //    /// <summary>Returns a pointer to a string giving information about the version of the libpcap library being used; note that it contains more information than just a version number. </summary>
    //    [DllImport(PCAP_DLL, CallingConvention = CallingConvention.Cdecl)]
    //    internal extern static string /* const char * */  pcap_lib_version();


    //    /// <summary> Return the link layer of an adapter. </summary>
    //    [DllImport(PCAP_DLL, CallingConvention = CallingConvention.Cdecl)]
    //    internal extern static int pcap_datalink(PcapHandle adaptHandle);
    //    /// <summary>
    //    /// The delegate declaration for PcapHandler requires an UnmanagedFunctionPointer attribute.
    //    /// Without this it fires for one time and then throws null pointer exception
    //    /// </summary>
    //    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    //    internal delegate void pcap_handler(IntPtr param, IntPtr /* pcap_pkthdr* */ header, IntPtr pkt_data);

    //    /// <summary>
    //    /// Retrieves a selectable file descriptor
    //    /// </summary>
    //    /// <param name="adaptHandle">
    //    /// A <see cref="IntPtr"/>
    //    /// </param>
    //    /// <returns>
    //    /// A <see cref="int"/>
    //    /// </returns>
    //    [DllImport(PCAP_DLL, CallingConvention = CallingConvention.Cdecl)]
    //    internal extern static int pcap_get_selectable_fd(PcapHandle adaptHandle);

    //    /// <summary>
    //    /// pcap_set_snaplen() sets the snapshot length to be used on a capture handle when the handle is activated to snaplen.  
    //    /// </summary>
    //    /// <param name="p">A <see cref="PcapHandle"/></param>
    //    /// <param name="snaplen">A <see cref="int"/></param>
    //    /// <returns>Returns 0 on success or PCAP_ERROR_ACTIVATED if called on a capture handle that has been activated.</returns>
    //    [DllImport(PCAP_DLL, CallingConvention = CallingConvention.Cdecl)]
    //    internal extern static PcapError pcap_set_snaplen(PcapHandle p, int snaplen);

    //    /// <summary>
    //    /// pcap_set_promisc() sets whether promiscuous mode should be set on a capture handle when the handle is activated. 
    //    /// If promisc is non-zero, promiscuous mode will be set, otherwise it will not be set.  
    //    /// </summary>
    //    /// <param name="p">A <see cref="IntPtr"/></param>
    //    /// <param name="promisc">A <see cref="int"/></param>
    //    /// <returns>Returns 0 on success or PCAP_ERROR_ACTIVATED if called on a capture handle that has been activated.</returns>
    //    [DllImport(PCAP_DLL, CallingConvention = CallingConvention.Cdecl)]
    //    internal extern static PcapError pcap_set_promisc(PcapHandle p, int promisc);

    //    /// <summary>
    //    /// pcap_set_timeout() sets the packet buffer timeout that will be used on a capture handle when the handle is activated to to_ms, which is in units of milliseconds.
    //    /// </summary>
    //    /// <param name="p">A <see cref="IntPtr"/></param>
    //    /// <param name="to_ms">A <see cref="int"/></param>
    //    /// <returns>Returns 0 on success or PCAP_ERROR_ACTIVATED if called on a capture handle that has been activated.</returns>
    //    [DllImport(PCAP_DLL, CallingConvention = CallingConvention.Cdecl)]
    //    internal extern static PcapError pcap_set_timeout(PcapHandle p, int to_ms);

    //    /// <summary>
    //    /// pcap_activate() is used to activate a packet capture handle to look at packets on the network, with the options that were set on the handle being in effect.  
    //    /// </summary>
    //    /// <param name="p">A <see cref="PcapHandle"/></param>
    //    /// <returns>Returns 0 on success without warnings, a non-zero positive value on success with warnings, and a negative value on error. A non-zero return value indicates what warning or error condition occurred.</returns>
    //    [DllImport(PCAP_DLL, CallingConvention = CallingConvention.Cdecl)]
    //    internal extern static PcapError pcap_activate(PcapHandle p);

    //    /// <summary>
    //    /// This function is different from <see cref="pcap_set_buffer_size"/>.
    //    /// It's for kernel buffer size, and applicable only for Windows
    //    /// </summary>
    //    /// <param name="adapter"></param>
    //    /// <param name="bufferSizeInBytes"></param>
    //    /// <returns></returns>
    //    [DllImport(PCAP_DLL, EntryPoint = "pcap_setbuff", CallingConvention = CallingConvention.Cdecl)]
    //    private extern static PcapError _pcap_setbuff(PcapHandle adapter, int bufferSizeInBytes);

    //    /// <summary>
    //    /// Windows Only
    //    /// changes the minimum amount of data in the kernel buffer that causes 
    //    /// a read from the application to return (unless the timeout expires)
    //    /// Setting this to zero will put the device in immediate mode in Windows
    //    /// See https://www.tcpdump.org/manpages/pcap_set_immediate_mode.3pcap.html
    //    /// </summary>
    //    /// <param name="adapter">
    //    /// A <see cref="PcapHandle"/>
    //    /// </param>
    //    /// <param name="sizeInBytes">
    //    /// A <see cref="int"/>
    //    /// </param>
    //    /// <returns>
    //    /// A <see cref="int"/>
    //    /// </returns>
    //    [DllImport(PCAP_DLL, EntryPoint = "pcap_setmintocopy", CallingConvention = CallingConvention.Cdecl)]
    //    private extern static PcapError _pcap_setmintocopy(PcapHandle adapter, int sizeInBytes);
    //}

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
        public static readonly LibPcapLibrary Instance = CreateInstance();

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
