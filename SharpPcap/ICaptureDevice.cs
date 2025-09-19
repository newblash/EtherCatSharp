// Copyright 2011 Chris Morgan <chmorgan@gmail.com>
//
// SPDX-License-Identifier: MIT

using System;
namespace SharpPcap
{
    /// <summary>
    /// Interfaces for capture devices
    /// </summary>
    public interface ICaptureDevice : IPcapDevice
    {
        /// <summary>
        /// Retrieves the next packet from a device
        /// </summary>
        /// <param name="e"></param>
        /// <returns>Status of the operation</returns>
        GetPacketStatus GetNextPacket(out ReadOnlySpan<byte> e);
    }
}

