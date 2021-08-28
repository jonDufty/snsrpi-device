using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sensr.CX;
using Sensr.Utils;

namespace snsrpi
{
    public class CXUtils
    {
        public static List<CXDevice> List()
        {
            CXCom cx = new CXCom();
            string search = "";

         

            //Placeholder list
            List<CXDevice> devs = new List<CXDevice>();

            try
            {
                // Search for and return a list of device that were found based
                // on the provided search terms
                devs = cx.ListDevices(search);

                if (devs.Count == 0)
                {
                    Console.WriteLine("No devices found");
                }

                int d = 1;
                foreach (CXDevice dev in devs)
                {
                    Console.WriteLine("Device({0}):\n{1}", d++, dev.ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}", e);
            }
            finally
            {
                cx.Disconnect();
            }

            return devs;
        }

    }
}
