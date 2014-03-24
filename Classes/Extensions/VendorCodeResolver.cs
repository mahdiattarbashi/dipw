using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace DipW.Classes.Extensions
{
    public class VendorCodeResolver
    {
        public static VendorCodeResolver instance = new VendorCodeResolver();

        private Dictionary<string, string> _lookupDict;
        private VendorCodeResolver()
        {
            try
            {
                XDocument document = XDocument.Load("./Assets/vendorMacs.xml");
                _lookupDict = (from code in document.Root.Descendants()
                               select new
                               {
                                   Code = code.Attribute("mac_prefix").Value,
                                   Name = code.Attribute("vendor_name").Value
                               }).ToDictionary(x => x.Code, x => x.Name);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error accessing resources! " + ex.Message);
            }
        }

        public string Resolve(string MacAddress)
        {
            try
            {
                var vendorCode = MacAddress.Remove(8);
                if (_lookupDict.ContainsKey(vendorCode))
                    return _lookupDict[vendorCode];
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error resolving vendorcode " + ex.Message);
            }

            return "";
        }

    }

}
