using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using DipW.Classes;
using DipW.Classes.Extensions;

namespace DipW.Classes.Components
{
    // target class
    public class Target : INotifyPropertyChanged, IComparable
    {

        private string _IP;
        public string MAC { get; set; }
        public PhysicalAddress PMAC { get; set; }
        public string Vendor { get; set; }

        private bool _arpPoisoning;
        private bool _arpDos;

        public Target()
        {
            _IP = "/";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool ArpPoisoning
        {
            get { return _arpPoisoning; }
            set
            {
                _arpPoisoning = value;
                OnPropertyChanged("ArpPoisoning");
            }
        }

        public bool ArpDos
        {
            get { return _arpDos; }
            set
            {
                _arpDos = value;
                OnPropertyChanged("ArpDos");
            }
        }

        public string IP
        {
            get { return _IP; }
            set
            {
                _IP = value;
                OnPropertyChanged("IP");
            }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public int CompareTo(object o)
        {
            if (((Target)o).IP == "/") return -1;
            if (IP == "/") return 1;

            long num2 = NetworkHelper.IPToLong(((Target)o).IP);
            long num1 = NetworkHelper.IPToLong(IP);

            if (num1 > num2)
                return 1;

            if (num1 < num2)
                return -1;

            return 0;
        }
    }
}
