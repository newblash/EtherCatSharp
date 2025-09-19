// Copyright 2010 Chris Morgan <chmorgan@gmail.com>
//
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpPcap
{
    /// <summary>
    /// Base class for all pcap devices
    /// </summary>
    public abstract partial class PcapDevice : ICaptureDevice
    {
        /// <summary>
        /// Device name
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Description
        /// </summary>
        public abstract string Description { get; }
        /// <summary>
        /// Return a value indicating if this adapter is opened
        /// </summary>
        public virtual bool Opened
        {
            get { return !(Handle.IsInvalid || Handle.IsClosed); }
        }

        /// <summary>
        /// The file descriptor obtained from pcap_fileno
        /// Used for polling
        /// </summary>
        protected internal int FileDescriptor = -1;

        /// <summary>
        /// The underlying pcap device handle
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public PcapHandle Handle { get; protected set; } = PcapHandle.Invalid;

        /// <summary>
        /// Retrieve the last error string for a given pcap_t* device
        /// </summary>
        /// <param name="deviceHandle">
        /// A <see cref="IntPtr"/>
        /// </param>
        /// <returns>
        /// A <see cref="string"/>
        /// </returns>
        internal static string GetLastError(PcapHandle deviceHandle)
        {
            return LibPcapSafeNativeMethods.pcap_geterr(deviceHandle);
        }

        /// <summary>
        /// The last pcap error associated with this pcap device
        /// </summary>
        public virtual string LastError
        {
            get { return GetLastError(Handle); }
        }
        /// <summary>
        /// Open the device. To start capturing call the 'StartCapture' function
        /// </summary>
        /// <param name="configuration">
        /// A <see cref="DeviceConfiguration"/>
        /// </param>
        public virtual void Open(DeviceConfiguration configuration)
        {
            // Caches linkType value.
            // Open refers to the device being "created"
            // This method is called by sub-classes in the override method
            int dataLink = 0;
            if (Opened)
            {
                dataLink = LibPcapSafeNativeMethods.pcap_datalink(Handle);
            }
        }

        /// <summary>
        /// Closes this adapter
        /// </summary>
        public virtual void Close()
        {
            Handle.Close();
            Handle = PcapHandle.Invalid;
        }
        /// <summary>
        /// Retrieve the next packet data
        /// </summary>
        /// <param name="e">Structure to hold the packet data info</param>
        /// <returns>Status of the operation</returns>
        public virtual GetPacketStatus GetNextPacket(out ReadOnlySpan<byte> e)
        {
            //Pointer to a packet info struct
            IntPtr header = IntPtr.Zero;

            //Pointer to a packet struct
            IntPtr data = IntPtr.Zero;
            e = default;
            // using an invalid PcapHandle can result in an unmanaged segfault
            // so check for that here
            ThrowIfNotOpen("Device must be opened via Open() prior to use");
            int res;
            res = LibPcapSafeNativeMethods.pcap_next_ex(Handle, ref header, ref data);
            if (res == 1)
            {
                int caplen = Marshal.ReadInt32(header + 8);
                int tv_sec = Marshal.ReadInt32(header);
                int tv_usec = Marshal.ReadInt32(header + 4);
                var packetData = new byte[caplen];
                Marshal.Copy(data, packetData, 0, caplen);
                e = packetData.AsSpan();
            }
            return (GetPacketStatus)res;
        }

        #region Filtering
        /// <summary>
        /// Assign a filter to this device given a filterExpression
        /// </summary>
        /// <param name="filterExpression">The filter expression to compile</param>
        protected void SetFilter(string filterExpression)
        {
            // save the filter string
            _filterString = filterExpression;

            int res;

            // pcap_setfilter() requires a valid pcap_t which isn't present if
            // the device hasn't been opened
            ThrowIfNotOpen("device is not open");

            // attempt to compile the program
            var bpfProgram = BpfProgram.Create(Handle, filterExpression);

            //associate the filter with this device
            res = LibPcapSafeNativeMethods.pcap_setfilter(Handle, bpfProgram);

            // Free the program whether or not we were successful in setting the filter
            // we don't want to leak unmanaged memory if we throw an exception.
            bpfProgram.Dispose();

            //watch for errors
            if (res < 0)
            {
                var errorString = string.Format("Can't set filter ({0}) : {1}", filterExpression, LastError);
                throw new PcapException(errorString);
            }
        }

        private string? _filterString;

        /// <summary>
        /// Kernel level filtering expression associated with this device.
        /// For more info on filter expression syntax, see:
        /// https://www.tcpdump.org/manpages/pcap-filter.7.html
        /// </summary>
        public virtual string? Filter
        {
            get
            {
                return _filterString;
            }

            set
            {
                SetFilter(value ?? string.Empty);
            }
        }
        #endregion

        /// <summary>
        /// Most pcap configuration functions have the signature int pcap_set_foo(pcap_t, int)
        /// those functions also set the error buffer, so we read it
        /// This is a helper method to use them and detect/report errors
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="setter"></param>
        /// <param name="property"></param>
        /// <param name="value"></param>
        protected void Configure(DeviceConfiguration configuration,string property, Func<PcapHandle, int, PcapError> setter,int? value
        )
        {
            if (value.HasValue)
            {
                var retval = setter(Handle, value.Value);
                if (retval != 0)
                {
                    configuration.RaiseConfigurationFailed(property, retval, GetLastError(Handle));
                }
            }
        }

        protected internal void ConfigureIfCompatible(bool compatible,DeviceConfiguration configuration,string property,Func<PcapHandle, int, PcapError> setter,int? value
        )
        {
            if (!value.HasValue)
            {
                return;
            }
            if (!compatible)
            {
                configuration.RaiseConfigurationFailed(
                    property, PcapError.Generic,
                    $"Can not configure {property} with current device and selected modes"
                );
            }
            else
            {
                Configure(configuration, property, setter, value);
            }
        }

        /// <summary>
        /// Helper method for checking that the adapter is open, throws an
        /// exception with a string of ExceptionString if the device isn't open
        /// </summary>
        /// <param name="ExceptionString">
        /// A <see cref="string"/>
        /// </param>
        protected void ThrowIfNotOpen(string ExceptionString)
        {
            if (!Opened)
            {
                throw new Exception(ExceptionString);
            }
        }
        ~PcapDevice()
        {
            Close();
        }

        public void Dispose()
        {
            Close();
        }
    }
}
