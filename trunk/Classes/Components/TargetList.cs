using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DipW.Classes.Components
{
    // observable collection for targets
    public class TargetList : ObservableCollection<Target>
    {
        public bool ContainsIP(string ip)
        {
            foreach (Target target in this)
            {
                if (target.IP == ip) return true;
            }

            return false;
        }

        public List<Target> ToList()
        {
            return new List<Target>(this);
        }
    }
}
