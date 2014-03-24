using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Arp;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Core.Extensions;
using System.ComponentModel;
using PcapDotNet.Packets.IpV4;
using DipW.Classes;
using System.Windows;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using DipW.Classes.Extensions;

namespace DipW.Classes.Manipulators.Senders
{
    public class ARPSender
    {
        private LivePacketDevice _device;
        private BackgroundWorker _bgWorker;

        //cache information for packet generation
        private byte[] _ownMacAddrByte;
        private byte[] _ownIpAddrByte;
        private byte[] _targetMacAddr = new byte[] { 0, 0, 0, 0, 0, 0 };
        private MacAddress _macBroadcastAddr = new MacAddress("ff:ff:ff:ff:ff:ff");
        private MacAddress _ownMacAddr;

        public LivePacketDevice Device
        {
            get { return _device; }
            set { _device = value; }
        }

        public BackgroundWorker BgWorker
        {
            get { return _bgWorker; }
        }

        //constructors
        public ARPSender(LivePacketDevice device)
        {
            _device = device;
            _bgWorker = new BackgroundWorker();
            _bgWorker.DoWork += new DoWorkEventHandler(DoWork);
        }

        //methods
        public void ScanAddresses()
        {
            _ownMacAddrByte = _device.GetNetworkInterface().GetPhysicalAddress().GetAddressBytes();
            _ownMacAddr = _device.GetMacAddress();
            _ownIpAddrByte = _device.getIpAddressBytes();

            if (!_bgWorker.IsBusy)
                _bgWorker.RunWorkerAsync();
            else
                throw new Exception("Scanning is still ongoing, cannot start another scan!");
        }

        private void DoWork(object sender, DoWorkEventArgs e)
        {
            var scanlist = _device.getNetworkIpList();

            List<Packet> packetList = new List<Packet>();
            foreach (var packet in scanlist)
            {
                packetList.Add(BuildArpPacketRequest(new IpV4Address(packet)));
            }
            using (PacketCommunicator communicator = _device.Open())
            {
                foreach (var packet in packetList)
                {
                    communicator.SendPacket(packet);
                }
            }
        }


        private Packet BuildArpPacketRequest(IpV4Address targetIp)
        {
            EthernetLayer ethernetLayer =
            new EthernetLayer
            {
                Source = _ownMacAddr,
                Destination = _macBroadcastAddr,   //broadcast
                EtherType = EthernetType.None, // Will be filled automatically.
            };

            ArpLayer arpLayer =
                new ArpLayer
                {
                    ProtocolType = EthernetType.IpV4,
                    Operation = ArpOperation.Request,
                    SenderHardwareAddress = Array.AsReadOnly(_ownMacAddrByte), // self mac-address
                    SenderProtocolAddress = Array.AsReadOnly(_ownIpAddrByte), // self ip-address
                    TargetHardwareAddress = Array.AsReadOnly(_targetMacAddr), // Not Yet known
                    TargetProtocolAddress = Helper.IpAddressToBytes(targetIp) // ip we want to get the mac for
                };

            PacketBuilder builder = new PacketBuilder(ethernetLayer, arpLayer);

            return builder.Build(DateTime.Now);
        }


    }
}
