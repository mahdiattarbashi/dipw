using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.IpV4;

namespace DipW.Classes.Extensions
{
    public class Helper
    {
        public static ReadOnlyCollection<byte> IpAddressToBytes(IpV4Address ipAddress)
        {
            return Array.AsReadOnly(BitConverter.GetBytes(ipAddress.ToValue()).Reverse().ToArray());
        }

        public static ReadOnlyCollection<byte> MacAddressToBytes(MacAddress macAddress)
        {
            return Array.AsReadOnly(BitConverter.GetBytes(macAddress.ToValue()).Reverse().Skip(2).ToArray());
        }

        public static string AddSeperatorToNakedMac(string MacAddress, string SeperatorString){

            var temp = "";
            for (int i = 0; i < MacAddress.Length; i+=2)
            {
                temp += "" + MacAddress[i] + MacAddress[i + 1] + SeperatorString;
            }
            return temp.Remove(temp.Length - 1);
        }

        public static IPAddress CalculateNetwork(UnicastIPAddressInformation addr)
        {
            // The mask will be null in some scenarios, like a dhcp address 169.254.x.x
            if (addr.IPv4Mask == null)
                return null;

            var ip = addr.Address.GetAddressBytes();
            var mask = addr.IPv4Mask.GetAddressBytes();
            var result = new Byte[4];
            for (int i = 0; i < 4; ++i)
            {
                result[i] = (Byte)(ip[i] & mask[i]);
            }

            return new IPAddress(result);
        }

        public static IPAddress CalculateBroadcast(UnicastIPAddressInformation addr)
        {
            // The mask will be null in some scenarios, like a dhcp address 169.254.x.x
            if (addr.IPv4Mask == null)
                return null;

            var ip = addr.Address.GetAddressBytes();
            var mask = addr.IPv4Mask.GetAddressBytes();
            var result = new Byte[4];
            for (int i = 0; i < 4; ++i)
            {
                result[i] = (Byte)(ip[i] | ~mask[i]);
            }

            return new IPAddress(result);
        }

        static public string getIpString(string address)
        {
            var allowedChars =
                Enumerable.Range('0', 10).Concat(
                Enumerable.Range('.', 1));

            var goodChars = address.Where(c => allowedChars.Contains(c));
            return new string(goodChars.ToArray());
        }
    }
}
