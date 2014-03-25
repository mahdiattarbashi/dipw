using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DipW.Classes.Components
{
    // observable collection for dns spoofing class
    public class DNSspoofingList : ObservableCollection<DnsSpoofingEntry>
    {
        public bool ContainsIP(string ip)
        {
            foreach (DnsSpoofingEntry entry in this)
            {
                if (entry.IP == ip) return true;
            }

            return false;
        }

        public List<DnsSpoofingEntry> ToList()
        {
            return new List<DnsSpoofingEntry>(this);
        }
    }
}
