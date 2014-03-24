using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
//using IPAddressTools;
using PcapDotNet.Core;
using PcapDotNet.Core.Extensions;

namespace DipW.Classes.Extensions
{
    public static class LivePacketDeviceExtensions
    {

        public static String DisplayName(this LivePacketDevice device)
        {
            return device.Name + " " + device.Description;
        }

        public static byte[] getIpAddressBytes(this LivePacketDevice device)
        {
            return device.GetNetworkInterface().GetIPProperties().UnicastAddresses.First((x)=> x.Address.GetAddressBytes().Length == 4).Address.GetAddressBytes();
        }

        public static UnicastIPAddressInformation getIpV4Address(this LivePacketDevice device)
        {
            return device.GetNetworkInterface().GetIPProperties().UnicastAddresses.First((x) => x.Address.GetAddressBytes().Length == 4);
        }

        public static GatewayIPAddressInformation getIpV4Gateway(this LivePacketDevice device) 
        {
            return device.GetNetworkInterface().GetIPProperties().GatewayAddresses.First((x) => x.Address.GetAddressBytes().Length == 4);
        }

        public static IEnumerable<string> getNetworkIpList(this LivePacketDevice device)
        {

            long[] range = NetworkHelper.MaskToStartEnd(device.getIpV4Address().Address.ToString(), device.getIpV4Address().IPv4Mask.ToString());
            long startIP = range[0];
            long endIP = range[1];
            long currentIP = startIP;
            List<string> ipList = new List<string>();
            while (currentIP <= endIP)
            {
                ipList.Add(NetworkHelper.LongToIP(currentIP).ToString());
                currentIP++;
            }
            return ipList;
        }

        // Print all the available information on the given interface
        public static void DevicePrint(this IPacketDevice device)
        {
            // Name
            Console.WriteLine(device.Name);

            // Description
            if (device.Description != null)
                Console.WriteLine("\tDescription: " + device.Description);

            // Loopback Address
            Console.WriteLine("\tLoopback: " +
                              (((device.Attributes & DeviceAttributes.Loopback) == DeviceAttributes.Loopback)
                                   ? "yes"
                                   : "no"));

            // IP addresses
            foreach (DeviceAddress address in device.Addresses)
            {
                Console.WriteLine("\tAddress Family: " + address.Address.Family);

                if (address.Address != null)
                    Console.WriteLine(("\tAddress: " + address.Address));
                if (address.Netmask != null)
                    Console.WriteLine(("\tNetmask: " + address.Netmask));
                if (address.Broadcast != null)
                    Console.WriteLine(("\tBroadcast Address: " + address.Broadcast));
                if (address.Destination != null)
                    Console.WriteLine(("\tDestination Address: " + address.Destination));
            }
            Console.WriteLine();
        }

    
    }
}
