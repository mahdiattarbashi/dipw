using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.IpV4;
using DipW.Classes;
using DipW.Classes.Components;
using System.Net.NetworkInformation;
using System.Windows.Threading;
using System.Windows;
using DipW.Classes.Extensions;

namespace DipW.Classes.Manipulators.Listener
{
    class ARPListener : Listener
    {
        private TargetList _targets;
        private TargetList _gateways;
        private bool _isOwnIpIncluded = false;

        //caching
        string _ownIp;
        string _gatewayIp;

        public TargetList Targets
        {
            get { return _targets; }
            set { _targets = value; }
        }

        public TargetList Gateways
        {
            get { return _gateways; }
            set { _gateways = value; }
        }

        //constructors
        public ARPListener(LivePacketDevice device)
            : base(device)
        {
            base.Filter = "arp";
            _targets = new TargetList();
            _gateways = new TargetList();
        }

        //methods
        public void StartCapture(bool isOwnIpIncluded = false)
        {
            _isOwnIpIncluded = isOwnIpIncluded;
            _ownIp = base.Device.getIpV4Address().Address.ToString();
            _gatewayIp = base.Device.getIpV4Gateway().Address.ToString();
            base.StartCapture(PacketHandler);
        }

        new private void PacketHandler(Packet packet)
        {
            var arpPacket = packet.Ethernet.Arp;
            switch (arpPacket.Operation)
            {
                case PcapDotNet.Packets.Arp.ArpOperation.Reply:
                    var senderIp = arpPacket.SenderProtocolIpV4Address.ToString();
                    if (_isOwnIpIncluded || _ownIp != senderIp)
                    {
                        if (senderIp == _gatewayIp )
                        {
                            if(!_gateways.ContainsIP(senderIp)){
                                var target = new Target();
                                target.IP = senderIp;
                                //target.MAC = arpPacket.SenderHardwareAddress.ToString();
                                target.PMAC = new PhysicalAddress(arpPacket.SenderHardwareAddress.ToArray());
                                target.MAC = Helper.AddSeperatorToNakedMac(target.PMAC.ToString(), ":");
                                target.Vendor = VendorCodeResolver.instance.Resolve(target.MAC);
                                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    _gateways.Add(target);
                                }));
                            }
                        } else if (!_targets.ContainsIP(senderIp))
                        {
                            var target = new Target();
                            target.IP = senderIp;
                            //target.MAC = arpPacket.SenderHardwareAddress.ToString();
                            target.PMAC = new PhysicalAddress(arpPacket.SenderHardwareAddress.ToArray());
                            target.MAC = Helper.AddSeperatorToNakedMac(target.PMAC.ToString(), ":");
                            target.Vendor = VendorCodeResolver.instance.Resolve(target.MAC);
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                _targets.Add(target);
                            }));
                            
                        }
                    }
                    break;
            }
        }

    }
}
