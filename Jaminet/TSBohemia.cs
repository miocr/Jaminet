using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Collections.Generic;
using System.Text;

namespace Jaminet
{
    public class TSBohemia
    {
        public XElement ReadFeed(string fileName)
        {
            XElement feed = null;
            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    feed = XElement.Load(fs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex.Message);
                return null;
            }
            return feed;
        }
    }
}
