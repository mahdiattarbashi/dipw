using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PcapDotNet.Core;
using PcapDotNet.Packets;

namespace DipW.Classes.Manipulators.Listener
{

    public class Listener
    {

        private LivePacketDevice _device;
        private BackgroundWorker _bgWorker;
        private String _filter;
        private PcapDotNet.Core.HandlePacket _packetHandler;

        public LivePacketDevice Device
        {
            get { return _device; }
            set { _device = value; }
        }

        public BackgroundWorker BgWorker
        {
            get { return _bgWorker; }
        }

        public String Filter
        {
            get { return _filter; }
            set { _filter = value; }
        }

        public PcapDotNet.Core.HandlePacket PacketHandler
        {
            get { return _packetHandler; }
            set { _packetHandler = value; }
        }

        //constructors
        public Listener()
        {
            _bgWorker = new BackgroundWorker();
        }

        public Listener(LivePacketDevice device)
            : this()
        {
            _device = device;
        }

        public Listener(LivePacketDevice device, String filter)
            : this(device)
        {
            _filter = filter;
        }

        //functions
        public void StartCapture(PcapDotNet.Core.HandlePacket PacketHandler)
        {
            _packetHandler = PacketHandler;
            _bgWorker.DoWork += new DoWorkEventHandler(DoWork);
            _bgWorker.RunWorkerAsync();
        }

        public void StartCapture(PcapDotNet.Core.HandlePacket PacketHandler, LivePacketDevice device)
        {
            _device = device;
            StartCapture(PacketHandler);
        }

        private void DoWork(object sender, DoWorkEventArgs e)
        {
            if (_device == null)
                throw new Exception("The capturing device must be set before starting the capture");

            using (PacketCommunicator communicator = _device.Open())
            {
                if (_filter != null)
                    communicator.SetFilter(_filter);
                communicator.ReceivePackets(0, _packetHandler);
            }
        }
    }

}
