using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace DipW.Classes.Extensions
{
    /* taken from nighthawk https://code.google.com/p/nighthawk/ and added modifications */

    public static class StaticARP
    {
        private enum StaticARPOperation
        {
            Add,
            Remove
        }

        public static bool AddEntry(string IP, PhysicalAddress mac, string InterfaceName)
        {
            return ModifyStaticARP(IP, mac, InterfaceName, StaticARPOperation.Add);
        }

        public static bool RemoveEntry(string IP, PhysicalAddress mac, string InterfaceName)
        {
            return ModifyStaticARP(IP, mac, InterfaceName, StaticARPOperation.Remove);
        }

        // static ARP entry manipulation (IP, MAC, friendly interface name, add/remove)
        private static bool ModifyStaticARP(string IP, PhysicalAddress mac, string InterfaceName, StaticARPOperation operation)
        {
            OperatingSystem system = Environment.OSVersion;

            // format MAC address
            var macString = NetworkHelper.FriendlyPhysicalAddress(mac).Replace(":", "-").ToLower();

            // prepare process
            Process p = new Process();

            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            p.StartInfo.FileName = "cmd";

            // Vista, Windows 7/8 - use "netsh"
            if (system.Version.Major > 5)
            {
                if (operation == StaticARPOperation.Add)
                    p.StartInfo.Arguments = "/k netsh interface ip delete neighbors \"" + InterfaceName + "\" " + IP + " && netsh interface ip add neighbors \"" + InterfaceName + "\" " + IP + " " + macString + " && exit";
                else
                    p.StartInfo.Arguments = "/k netsh interface ip delete neighbors \"" + InterfaceName + "\" " + IP + " && exit";

                p.Start();
                p.WaitForExit();

                p.Dispose();

                return true;
            }

            p.Dispose();

            return false;
        }
    }
}
