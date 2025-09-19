// Copyright 2010 Chris Morgan <chmorgan@gmail.com>
//
// SPDX-License-Identifier: MIT

using System;

namespace SharpPcap
{
    public class DeviceConfiguration
    {
        /// <summary>
        /// Defines if the adapter mode. 
        /// </summary>
        public DeviceModes Mode { get; set; }

        public int ReadTimeout { get; set; } = 1000;

        // MAX_PACKET_SIZE (65536) grants that the whole packet will be captured on all the MACs.
        public int Snaplen { get; set; } = 65536;
        public int? KernelBufferSize { get; set; }
        public bool? Immediate { get; set; }

        public int? MinToCopy { get; set; }

        public event EventHandler<ConfigurationFailedEventArgs>? ConfigurationFailed;

        internal void RaiseConfigurationFailed(string property, PcapError error, string message)
        {
            message = message ?? $"Failed to set {property}.";

            if (ConfigurationFailed is null)
            {
                var exception = error == PcapError.PlatformNotSupported ?
                    (Exception)new PlatformNotSupportedException() :
                    new PcapException(message, error);
                throw exception;
            }
            else
            {
                var args = new ConfigurationFailedEventArgs
                {
                    Property = property,
                    Error = error,
                    Message = message,
                };
                ConfigurationFailed.Invoke(this, args);
            }
        }
    }

    public class ConfigurationFailedEventArgs : EventArgs
    {
        public PcapError Error { get; internal set; }
        public  string? Property { get; set; }
        public  string? Message { get; set; }
    }
}
