using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PcapDotNet.Core;
using DipW.Classes;
using PcapDotNet.Packets;
using System.ComponentModel;
using DipW.Classes.Manipulators;
using DipW.Classes.Components;
using DipW.Classes.Extensions;
using DipW.Classes.Manipulators.Senders;
using DipW.Classes.Manipulators.Listener;

namespace DipW
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IList<LivePacketDevice> _allDevices;
        private int _currentDeviceIndex = 0;

        private ulong _udpPacketCount = 0;
        private ulong _tcpPacketCount = 0;
        private ulong _dnsPacketCount = 0;

        private Listener _tcpListener;
        private Listener _udpListener;
        private Listener _dnsListener;
        private ARPListener _arpListener;

        private ARPSender _arpSender;
        private ArpPoisoning _arpSpoofSender;
        private CAMOverflowSender _camOverflowSender;


        private Brush _defaultBrush;
        private Brush _activeBrush;
        private Brush _cancelingBrush;

        private LivePacketDevice getCurrentDevice()
        {
            return _allDevices[_currentDeviceIndex];
        }

        public MainWindow()
        {
            InitializeComponent();
            getDevices();
            cbxNetworkInterface.SelectionChanged += cbxNetworkInterface_SelectionChanged;
            cbxNetworkInterface_SelectionChanged(null, null);
            _defaultBrush = gbxCAMflood.BorderBrush;
            _activeBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            _cancelingBrush = new SolidColorBrush(Color.FromRgb(255, 255, 0));
        }

        private void cbxNetworkInterface_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentDeviceIndex = cbxNetworkInterface.SelectedIndex;
            StartPacketCounting();
            _arpSpoofSender = null;
        }

        private void getDevices()
        {
            _allDevices = LivePacketDevice.AllLocalMachine;
            if (_allDevices.Count == 0)
            {
                MessageBox.Show("No interfaces found! Make sure WinPcap is installed.");
                return;
            }

            List<String> deviceList = new List<string>();
            foreach (var element in _allDevices)
            {
                try
                {
                    var first = element.Addresses.First((x) => x.Address.Family == SocketAddressFamily.Internet);
                    var message = Helper.getIpString(first.Address.ToString());
                    message += "/" + NetworkHelper.MaskToCIDR(Helper.getIpString(first.Netmask.ToString())) + " " + element.DisplayName();
                    deviceList.Add(message);
                }
                catch (Exception Ex)
                {
                    MessageBox.Show("Device could not be added: " + Ex.Message);
                    _allDevices.Remove(element);
                }
            }

            cbxNetworkInterface.ItemsSource = deviceList;
            cbxNetworkInterface.SelectedIndex = 0;
            StartPacketCounting();
        }

        private void StartPacketCounting()
        {
            _udpPacketCount = 0;
            _tcpPacketCount = 0;
            _dnsPacketCount = 0;
            updateGUI();

            _tcpListener = new Listener(getCurrentDevice(), "ip and tcp");
            _tcpListener.StartCapture(PacketHandlerTCP);
            _udpListener = new Listener(getCurrentDevice(), "ip and udp");
            _udpListener.StartCapture(PacketHandlerUDP);
            _dnsListener = new Listener(getCurrentDevice(), "ip and udp and port 53");
            _dnsListener.StartCapture(PacketHandlerDNS);
        }

        private void PacketHandlerTCP(Packet packet)
        {
            _tcpPacketCount++;
            updateGUI(Classes.Components.Enums.PacketType.TCP);
        }
        private void PacketHandlerUDP(Packet packet)
        {
            _udpPacketCount++;
            updateGUI(Enums.PacketType.UDP);
        }

        private void PacketHandlerDNS(Packet packet)
        {
            _dnsPacketCount++;
            updateGUI(Enums.PacketType.DNS);
        }

        private void updateGUI(Enums.PacketType? type = null)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                switch (type)
                {
                    case Enums.PacketType.DNS:
                        tbxDnsPacketCount.Text = "" + _dnsPacketCount;
                        break;
                    case Enums.PacketType.TCP:
                        tbxTcpPacketCount.Text = "" + _tcpPacketCount;
                        break;
                    case Enums.PacketType.UDP:
                        tbxUdpPacketCount.Text = "" + _udpPacketCount;
                        break;
                    default:
                        tbxDnsPacketCount.Text = "" + _dnsPacketCount;
                        tbxTcpPacketCount.Text = "" + _tcpPacketCount;
                        tbxUdpPacketCount.Text = "" + _udpPacketCount;
                        break;
                }
            }));
        }

        private void btnStartScan_Click(object sender, RoutedEventArgs e)
        {
            _arpListener = new ARPListener(getCurrentDevice());
            _arpListener.StartCapture();
            lvwTargets.ItemsSource = _arpListener.Targets;
            lvwGatway.ItemsSource = _arpListener.Gateways;
            _arpSender = new ARPSender(getCurrentDevice());
            _arpSender.ScanAddresses();
        }

        private void btnCAMflood_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_camOverflowSender == null)
                {
                    _camOverflowSender = new CAMOverflowSender(getCurrentDevice(), int.Parse(tbxCAMentries.Text));
                    _camOverflowSender.BgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(delegate
                    {
                        btnCAMflood.IsEnabled = true;
                        gbxCAMflood.BorderBrush = _defaultBrush;
                    });
                }
                if (_camOverflowSender.BgWorker.IsBusy)
                {
                    gbxCAMflood.BorderBrush = _cancelingBrush;
                    btnCAMflood.IsEnabled = false;
                    _camOverflowSender.BgWorker.CancelAsync();
                }
                else
                {
                    gbxCAMflood.BorderBrush = _activeBrush;
                    _camOverflowSender.FloodSwitch();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            //_camOverflowSender.FloodSwitch(500000);
            //_arpSpoofSender = new ArpSpoofSender(getCurrentDevice(), _arpListener.Gateways[0]);
            //_arpSpoofSender.addTarget((Target)lvwTargets.SelectedItem);
            //_forwarder = new Forwarder(getCurrentDevice(), new MacAddress("CC:35:40:2C:CC:6F"));
        }

        private void lvwTargets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvwTargets.SelectedItems.Count > 0)
            {
                btnAddARPTarget.IsEnabled = true;
                btnRemoveARPTarget.IsEnabled = true;
            }
            else
            {
                btnAddARPTarget.IsEnabled = false;
                btnRemoveARPTarget.IsEnabled = false;
            }
        }

        #region ARP_Poisoning

        private void CheckInitArpPoison()
        {
            if (_arpSpoofSender == null)
            {
                _arpSpoofSender = new ArpPoisoning(getCurrentDevice(), (Target)lvwGatway.Items[0]);
                _arpSpoofSender.BgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(delegate
                {
                    btnARPpoison.IsEnabled = true;
                    gbxARPPoison.BorderBrush = _defaultBrush;
                });
            }
        }

        private void btnAddARPTarget_Click(object sender, RoutedEventArgs e)
        {
            CheckInitArpPoison();
            foreach (Target element in lvwTargets.SelectedItems)
            {
                _arpSpoofSender.AddTarget(element);
                element.ArpPoisoning = true;
            }
        }

        private void btnRemoveARPTarget_Click(object sender, RoutedEventArgs e)
        {
            CheckInitArpPoison();
            foreach (Target element in lvwTargets.SelectedItems)
            {
                _arpSpoofSender.RemoveTarget(element);
                element.ArpPoisoning = false;
            }
        }

        private void btnARPpoison_Click(object sender, RoutedEventArgs e)
        {
            CheckInitArpPoison();

            try
            {
                if (_arpSpoofSender.BgWorker.IsBusy)
                {
                    gbxARPPoison.BorderBrush = _cancelingBrush;
                    btnARPpoison.IsEnabled = false;
                    _arpSpoofSender.BgWorker.CancelAsync();
                }
                else
                {
                    gbxARPPoison.BorderBrush = _activeBrush;
                    _arpSpoofSender.StartSpoofing();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

    }
}
