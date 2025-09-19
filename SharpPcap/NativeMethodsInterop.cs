using SharpPcap;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace EtherCatSharp.SharpPcap
{
    /// <summary>
    /// 动态加载 libpcap/wpcap 并绑定函数
    /// </summary>
    public sealed class LibPcapLibrary : IDisposable
    {
        private IntPtr _libraryHandle;
        // 函数委托定义
        public delegate int pcap_init_d(uint opts, out ErrorBuffer errbuf);
        public delegate int pcap_findalldevs_d(ref IntPtr alldevs, out ErrorBuffer errbuf);
        public delegate void pcap_freealldevs_d(IntPtr alldevs);
        public delegate PcapHandle pcap_open_d(string dev, int packetLen, int flags, int read_timeout, IntPtr rmtauth, out ErrorBuffer errbuf);
        public delegate PcapHandle pcap_create_d(string dev, out ErrorBuffer errbuf);
        public delegate PcapError pcap_set_buffer_size_d(PcapHandle  adapter, int bufferSizeInBytes);
        public delegate PcapError pcap_set_immediate_mode_d(PcapHandle  adapter, int immediate_mode);
        public delegate void pcap_close_d(IntPtr adaptHandle);
        public delegate int pcap_next_ex_d(PcapHandle  adaptHandle, ref IntPtr /* **pkt_header */ header, ref IntPtr data);
        public delegate int pcap_sendpacket_d(PcapHandle adaptHandle, byte[] data, int size);
        public delegate int pcap_compile_d(PcapHandle  adaptHandle, BpfProgram fp, string /*char * */str, int optimize, UInt32 netmask);
        public delegate int pcap_setfilter_d(PcapHandle adaptHandle, BpfProgram fp);
        public delegate void pcap_freecode_d(IntPtr fp);
        public delegate string pcap_geterr_d(PcapHandle adaptHandle);
        public delegate int pcap_datalink_d(PcapHandle adaptHandle);
        public delegate int pcap_get_selectable_fd_d(PcapHandle adaptHandle);
        public delegate PcapError pcap_set_snaplen_d(PcapHandle  p, int snaplen);
        public delegate PcapError pcap_set_promisc_d(PcapHandle  p, int promisc);
        public delegate PcapError pcap_set_timeout_d(PcapHandle  p, int to_ms);
        public delegate PcapError pcap_activate_d(PcapHandle p);
        public delegate PcapError pcap_setbuff_d(PcapHandle  adapter, int bufferSizeInBytes);
        public delegate PcapError pcap_setmintocopy_d(PcapHandle  adapter, int sizeInBytes);

        // 函数指针
        public pcap_init_d? pcap_init { get; private set; }
        public pcap_findalldevs_d? pcap_findalldevs { get; private set; }
        public pcap_freealldevs_d? pcap_freealldevs { get; private set; }
        public pcap_open_d? pcap_open { get; private set; }
        public pcap_create_d? pcap_create { get; private set; }
        public pcap_set_buffer_size_d? pcap_set_buffer_size { get; private set; }
        public pcap_set_immediate_mode_d? pcap_set_immediate_mode { get; private set; }
        public pcap_close_d? pcap_close { get; private set; }
        public pcap_next_ex_d? pcap_next_ex { get; private set; }
        public pcap_sendpacket_d? pcap_sendpacket { get; private set; }
        public pcap_compile_d? pcap_compile { get; private set; }
        public pcap_setfilter_d? pcap_setfilter { get; private set; }
        public pcap_freecode_d? pcap_freecode { get; private set; }
        public pcap_geterr_d? pcap_geterr { get; private set; }
        public pcap_datalink_d? pcap_datalink { get; private set; }
        public pcap_get_selectable_fd_d? pcap_get_selectable_fd { get; private set; }
        public pcap_set_snaplen_d? pcap_set_snaplen { get; private set; }
        public pcap_set_promisc_d? pcap_set_promisc { get; private set; }
        public pcap_set_timeout_d? pcap_set_timeout { get; private set; }
        public pcap_activate_d? pcap_activate { get; private set; }
        public pcap_setbuff_d? pcap_setbuff { get; private set; }
        public pcap_setmintocopy_d? pcap_setmintocopy { get; private set; }

        /// <summary>
        /// 加载库并绑定函数
        /// </summary>
        public bool Load()
        {
            _libraryHandle = LoadLibrary();
            if (_libraryHandle == IntPtr.Zero)
                return false;

            try
            {
                pcap_init = GetFunction<pcap_init_d>("pcap_init");
                pcap_findalldevs = GetFunction<pcap_findalldevs_d>("pcap_findalldevs");
                pcap_freealldevs = GetFunction<pcap_freealldevs_d>("pcap_freealldevs");
                pcap_open = GetFunction<pcap_open_d>("pcap_open");
                pcap_create = GetFunction<pcap_create_d>("pcap_create");
                pcap_set_buffer_size = GetFunction<pcap_set_buffer_size_d>("pcap_set_buffer_size");
                pcap_set_immediate_mode = GetFunction<pcap_set_immediate_mode_d>("pcap_set_immediate_mode");
                pcap_close = GetFunction<pcap_close_d>("pcap_close");
                pcap_next_ex = GetFunction<pcap_next_ex_d>("pcap_next_ex");
                pcap_sendpacket = GetFunction<pcap_sendpacket_d>("pcap_sendpacket");
                pcap_compile = GetFunction<pcap_compile_d>("pcap_compile");
                pcap_setfilter = GetFunction<pcap_setfilter_d>("pcap_setfilter");
                pcap_freecode = GetFunction<pcap_freecode_d>("pcap_freecode");
                pcap_geterr = GetFunction<pcap_geterr_d>("pcap_geterr");
                pcap_datalink = GetFunction<pcap_datalink_d>("pcap_datalink");
                pcap_get_selectable_fd = GetFunction<pcap_get_selectable_fd_d>("pcap_get_selectable_fd");
                pcap_set_snaplen = GetFunction<pcap_set_snaplen_d>("pcap_set_snaplen");
                pcap_set_promisc = GetFunction<pcap_set_promisc_d>("pcap_set_promisc");
                pcap_set_timeout = GetFunction<pcap_set_timeout_d>("pcap_set_timeout");
                pcap_activate = GetFunction<pcap_activate_d>("pcap_activate");
                pcap_setbuff = GetFunction<pcap_setbuff_d>("_pcap_setbuff");
                pcap_setmintocopy = GetFunction<pcap_setmintocopy_d>("_pcap_setmintocopy");
                return true;
            }
            catch
            {
                // 加载失败，清理
                FreeLibrarys(_libraryHandle);
                _libraryHandle = IntPtr.Zero;
                return false;
            }
        }
        //安全获取函数
        private T? GetFunction<T>(string functionName) where T : class
        {
            IntPtr funcPtr = GetProcAddres(_libraryHandle, functionName);
            if (funcPtr == IntPtr.Zero)
                return null; // 找不到就返回 null

            return Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(T)) as T;
        }
        public void Dispose()
        {
            if (_libraryHandle != IntPtr.Zero)
            {
                FreeLibrarys(_libraryHandle);
                _libraryHandle = IntPtr.Zero;
            }
        }
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr LoadLibraryA(string lpFileName);

        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("libdl.so.2")]
        private static extern IntPtr dlopen(string filename, int flags);

        [DllImport("libdl.so.2")]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);


        /// <summary>
        /// 动态加载原生库
        /// </summary>
        private static IntPtr LoadLibrary()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return LoadLibraryA("wpcap");
            }
            else if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return dlopen("pcap", 2);
            }
            else
            {
                throw new PlatformNotSupportedException("不支持的操作系统");
            }
        }

        /// <summary>
        /// 获取函数地址
        /// </summary>
        private static IntPtr GetProcAddres(IntPtr libraryHandle, string functionName)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return GetProcAddress(libraryHandle, functionName);
            }
            else if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return dlsym(libraryHandle, functionName);
            }
            else
            {
                throw new PlatformNotSupportedException("不支持的操作系统");
            }
        }

        /// <summary>
        /// 释放库
        /// </summary>
        private static bool FreeLibrarys(IntPtr libraryHandle)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return FreeLibrary(libraryHandle);
            }
            else if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return dlclose(libraryHandle) == 0;
            }
            return false;
        }

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("libdl.so.2")]
        private static extern int dlclose(IntPtr handle);
    }
}
