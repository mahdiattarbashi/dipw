using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DipW.Classes.Extensions;

namespace DipW.Classes.Components
{
    // dns spoofing class
    public class DnsSpoofingEntry : INotifyPropertyChanged, IComparable
    {
        private string _domainName;
        private string _IP;

        public DnsSpoofingEntry()
        {
            _IP = "/";
        }

        public DnsSpoofingEntry(string DomainName, string ip)
        {
            _domainName = DomainName;
            _IP = ip;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string DomainName
        {
            get { return _domainName; }
            set
            {
                _domainName = value;
                OnPropertyChanged("DomainName");
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
