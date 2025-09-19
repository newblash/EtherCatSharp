// Copyright 2005 Tamir Gal <tamir@tamirgal.com>
// Copyright 2008-2009 Chris Morgan <chmorgan@gmail.com>
//
// SPDX-License-Identifier: MIT

using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SharpPcap
{
    /// <summary>
    /// List of available Pcap Interfaces.
    /// </summary>
    public class LibPcapLiveDeviceList : ReadOnlyCollection<LibPcapLiveDevice>
    {
        private static LibPcapLiveDeviceList? instance;

        /// <summary>
        /// Method to retrieve this classes singleton instance
        /// </summary>
        public static LibPcapLiveDeviceList Instance
        {
            get
            {
                instance ??= new LibPcapLiveDeviceList();

                return instance;
            }
        }

        /// <summary>
        /// Caution: Use the singlton instance unless you know why you need to call this.
        /// One use is for multiple filters on the same physical device. To apply multiple
        /// filters open the same physical device multiple times, one for each
        /// filter by calling this routine and picking the same device out of each list.
        /// </summary>
        /// <returns>
        /// A <see cref="LibPcapLiveDeviceList"/>
        /// </returns>
        public static LibPcapLiveDeviceList New()
        {
            return new LibPcapLiveDeviceList();
        }

        /// <summary>
        /// Represents a strongly typed, read-only list of PcapDevices.
        /// </summary>
        private LibPcapLiveDeviceList() : base(new List<LibPcapLiveDevice>())
        {
            Refresh();
        }

        /// <summary>
        /// Retrieve a list of the current PcapDevices
        /// </summary>
        /// <returns>
        /// A <see cref="List&lt;LibPcapLiveDevice&gt;"/>
        /// </returns>
        private static List<LibPcapLiveDevice> GetDevices()
        {
            var deviceList = new List<LibPcapLiveDevice>();
            var pcapInterfaces = PcapInterface.GetAllPcapInterfaces();
            foreach (var pcap_if in pcapInterfaces)
            {
                deviceList.Add(new LibPcapLiveDevice(pcap_if));
            }
            return deviceList;
        }

        /// <summary>
        /// Refresh the device list
        /// </summary>
        public void Refresh()
        {
            lock (this)
            {
                // retrieve the current device list
                var newDeviceList = GetDevices();

                // find items the current list is missing
                foreach (var newItem in newDeviceList)
                {
                    bool found = false;
                    foreach (var existingItem in base.Items)
                    {
                        if (existingItem.Name == newItem.Name)
                        {
                            found = true;
                            break;
                        }
                    }

                    // add items that we were missing
                    if (!found)
                    {
                        base.Items.Add(newItem);
                    }
                }

                // find items that we have that the current list is missing
                var itemsToRemove = new List<LibPcapLiveDevice>();
                foreach (var existingItem in base.Items)
                {
                    bool found = false;

                    foreach (var newItem in newDeviceList)
                    {
                        if (existingItem.Name == newItem.Name)
                        {
                            found = true;
                            break;
                        }
                    }

                    // add the PcapDevice we didn't see in the new list
                    if (!found)
                    {
                        itemsToRemove.Add(existingItem);
                    }
                }

                // remove the items outside of the foreach() to avoid
                // enumeration errors
                foreach (var itemToRemove in itemsToRemove)
                {
                    base.Items.Remove(itemToRemove);
                }
            }
        }
    }
}
