using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sensr.CX;
using Sensr.Utils;

namespace snsrpi.Models
{
    /// <summary>
    /// This is just a legacy of the original API example provided by SNSR.
    /// Used mainly as an example 
    /// </summary>
    public class APIWrapper
    {
        
        static void ShowUsage()
        {
            Console.WriteLine("Usage: cxapidemo command");
            Console.WriteLine("Where command is one of the following:");
            Console.WriteLine("List <search>                      : Display a list of devices, filter by search string.");
            Console.WriteLine("Name <name> <search>               : Set the name of the device. May use a search string");
            Console.WriteLine("                                     to find device.");
            Console.WriteLine("Stream <search>                    : Streams samples from a device and displays them. May");
            Console.WriteLine("                                     use a search string to find device.");
            Console.WriteLine("Configuration <ipaddress> <search> : Changes the ip address of the device. Enter DHCP for");
            Console.WriteLine("                                     the ipaddress to enable DHCP. May use a search string");
            Console.WriteLine("Password <password> <search>       : Changes the password of the user account. May use a");
            Console.WriteLine("                                     search string to find device.");
            Console.WriteLine("Info <search>                      : Displays various information about the device. May use");
            Console.WriteLine("                                     a search string to find device.");
            Console.WriteLine("Scratch <search>                   : Shows how to use the scratch pad. May use a search");
            Console.WriteLine("                                     string to find device.");
            Console.WriteLine("Connect                            : Shows how to connect to a device using a CXDevice object.");
        }

        // *****************************************************************
        // * Search terms:
        // * Used with the ListDevices and Connect functions to
        // * narrow down the list of possible devices to connect to or list.
        // * Terms consist of a key/value pair. Multiple terms may be specified
        // * in a search string. Separate multiple terms with a colon. Multiple
        // * values may be specified per key, just separate with a semicolon.
        // *
        // * Keys:
        // * interface=usb;ethernet
        // * This key specifies which interface to look for devices on.
        // * Default is both interfaces are used.
        // *
        // * ethernet.ipaddress=auto;xxx.xxx.xxx.xxx;...
        // * This key specified which ip addresses to probe while looking for
        // * devices. Use auto to automatically find devices using a UDP broadcast.
        // * This key only applies if the ethernet interface is used for searching.
        // * Default is auto.
        // *
        // * name=23rd Floor East Corner;...
        // * Restrict the search to only the devices with the specified names.
        // * Default is no restriction on device name
        // * NOTE: this search key is not yet implemented, coming soon...
        // *
        // * model=cx1;...
        // * Restrict the search to only the device models listed.
        // * Default is no restriction on device model
        // *
        // * serial=1234;...
        // * Restrict the search to only the devices with the specified serial number.
        // * Default is no restriction on device model
        // *
        // * proc=R001-186-V2,123;...
        // * Restrict the search to only the device with the specified process number.
        // * Default is no restriction on process number
        // *
        // * Examples:
        // * example: Get a list of all ethernet devices
        // *          "interface=ethernet"
        // * example: Get a list of all usb devices
        // *          "interface=usb"
        // * example: find a cx1 at ip 1.2.3.4 with a serial # of 4321
        // *          "interface=ethernet : ethernet.ipaddress=1.2.3.4 : model=cx1 : serial=4321"
        // * example: find any CX1 on any interface
        // *          "model=cx1"
        // * example: Get a list of ethernet devices and probe ip 1.2.3.4
        // *          "interface=ethernet : ethernet.ipaddress=auto,1.2.3.4"
        // * example: find device cx1 with sn 1234 on any interface
        // *          "model=cx1 : serial=1234"
        // * example: find a device with the name "Basement East" on any interface
        // *          "name=Basement East"
        // *****************************************************************
        static void List(List<string> args)
        {
            CXCom cx = new CXCom();
            string search = "";

            if (args.Count > 0) search = args[0];

            try
            {
                // Search for and return a list of device that were found based
                // on the provided search terms
                List<CXDevice> devs = cx.ListDevices(search);

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
        }

        // This routine demonstrates how to set and get the device name. 
        // The device name is a user specifiable descriptive name for a
        // device, example "North tower first floor". Also shown is using
        // a search string to connect to a device, note that if multiple
        // devices match the search criteria, the first one that was found
        // will be used.
        static void Name(List<string> args)
        {
            CXCom cx = new CXCom();
            string name = "";
            string search = "";

            if (args.Count > 0) name = args[0];
            if (args.Count > 1) search = args[1];

            try
            {
                // Search for and connect to a device
                cx.Connect(search);

                // Login to device as the administrative user. "admin" rights are
                // required for changing the device name. The normal "user" account
                // can get the device name but does not have permission to change it.
                CXCom.LoginStatus login = cx.Login(CXCom.LoginUserID.Admin, "admin");
                if (login != CXCom.LoginStatus.Ok)
                {
                    Console.WriteLine("Could not log into device: {0}", login);
                    return;
                }

                // Set the device name
                if (!cx.SetDeviceName(name))
                {
                    Console.WriteLine("SetDeviceName failed");
                    return;
                }

                // Get the device name
                Console.WriteLine("Device Name = {0}", cx.GetDeviceName());
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}", e);
            }
            finally
            {
                cx.Disconnect();
            }
        }
        // This routine demonstrates how to stream samples from a device.
        public static void Stream(List<string> args)
        {
            CXCom cx = new CXCom();
            string search = "";

            if (args.Count > 0) search = args[0];

            try
            {
                // Search for and connect to a device
                cx.Connect(search);

                // Login to device as a normal user
                CXCom.LoginStatus login = cx.Login(CXCom.LoginUserID.User, "user");
                if (login != CXCom.LoginStatus.Ok)
                {
                    Console.WriteLine("Could not log into device: {0}", login);
                    return;
                }

                // Enable streaming
                if (!cx.StreamEnable())
                {
                    Console.WriteLine("Could not enable streaming");
                    return;
                }

                // Display header
                Console.WriteLine("Press any key to stop");
                Console.WriteLine("SampleNumber,TimeStamp,Reconstructed,ReconstructedCount,Acceleration_X,Acceleration_Y,Acceleration_Z,Tilt_X,Tilt_Y,Temperature");

                // Acquire and display samples
                StringBuilder sb = new StringBuilder();
                int ReconstructedCount = 0;
                while (!Console.KeyAvailable)
                {
                    List<CXCom.Sample> samples = cx.GetSamples();

                    foreach (CXCom.Sample s in samples)
                    {
                        // Sample.Reconstructed is used to indicate that the sample was originally missing
                        // or corrupt and was reconstructed by the api. You will never have to insert or compensate
                        // for missing samples, because the api handles it for you, and gives you this notice that it did.
                        if (s.Reconstructed) ReconstructedCount++;

                        sb.Length = 0;

                        // Every sample is assigned a sequential 64 bit number. Starts at 0 every time streaming is enabled.
                        sb.AppendFormat("{0,6},", s.SampleNumber);

                        // The TimeStamp for the sample. Timestamps are computed and maintained by the api.
                        sb.AppendFormat("{0,26},", s.TimeStamp.ToString("MM/dd/yyyy HH:mm:ss.ffffff"));

                        // See the above discussion
                        sb.AppendFormat("{0,6},", s.Reconstructed);
                        sb.AppendFormat("{0,4},", ReconstructedCount);

                        // Acceleration data available
                        if (s.AccelerationValid)
                        {
                            sb.AppendFormat("{0,10:0.0000000},", s.Acceleration_X);
                            sb.AppendFormat("{0,10:0.0000000},", s.Acceleration_Y);
                            sb.AppendFormat("{0,10:0.0000000},", s.Acceleration_Z);
                        }

                        // Tilt data available
                        if (s.TiltValid)
                        {
                            sb.AppendFormat("{0,8:0.000},", s.Tilt_X);
                            sb.AppendFormat("{0,8:0.000},", s.Tilt_Y);
                        }

                        // Temperature data available
                        if (s.TemperatureValid)
                        {
                            sb.AppendFormat("        ,        ,{0,6:0.0},", s.Temperature);
                        }

                        Console.WriteLine(sb.ToString());
                    }
                    Thread.Sleep(50);
                }

                // Disable streaming. Not absolutely needed as the device will stop streaming
                // automatically when we disconnect, just here to demonstrate.
                if (!cx.StreamDisable())
                {
                    Console.WriteLine("Could not disable streaming");
                    return;
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
        }

        // This routine demonstrates how to get and set the device configuration.
        // The device configuration contains settings like IP address, gateway, etc.
        static void Configuration(List<string> args)
        {
            CXCom cx = new CXCom();
            string ipaddress = "dhcp";
            string search = "";

            if (args.Count > 0) ipaddress = args[0];
            if (args.Count > 1) search = args[1];

            try
            {
                // Search for and connect to a device
                cx.Connect(search);

                // Login to device as the administrative user. "admin" rights are
                // required for changing the device configuration. The normal "user" account
                // can get the device configuration but does not have permission to change it.
                CXCom.LoginStatus login = cx.Login(CXCom.LoginUserID.Admin, "admin");
                if (login != CXCom.LoginStatus.Ok)
                {
                    Console.WriteLine("Could not log into device: {0}", login);
                    return;
                }

                // Get the device configuration
                // Note that GetDeviceConfiguration may return null if no configuration has been
                // set for the device. If no configuration has been set on the device it uses its default
                // settings.
                CXCom.DeviceConfiguration config = cx.GetDeviceConfiguration();
                if (config == null)
                {
                    Console.WriteLine("No device config was available, device running on defaults.");
                }
                else
                {
                    Console.WriteLine("Current Configuration:\n{0}", config.ToString());
                }

                // Prepare a new device configuration
                CXCom.DeviceConfiguration newconfig = new CXCom.DeviceConfiguration();
                if (ipaddress == "dhcp")
                {
                    // Specify the method of obtaining an ip address, DHCP or Manual.
                    // If using DHCP, you do not need to set the IPAddress, Gateway, or NetMask values
                    newconfig.IPAcquisition = CXCom.DeviceConfiguration.IPAcquireMethod.DHCP;

                    // The TCP & UDP port that the device will listen on for connection and probe requests.
                    // Don't change the port, this will be removed in a later version.
                    // newconfig.Port = 1234;
                }
                else
                {
                    newconfig.IPAcquisition = CXCom.DeviceConfiguration.IPAcquireMethod.Manual;
                    newconfig.IPAddress = ipaddress;
                    newconfig.Gateway = "192.168.0.1";
                    newconfig.NetMask = "255.255.255.0";
                }

                // Set the device configuration
                if (!cx.SetDeviceConfiguration(newconfig))
                {
                    Console.WriteLine("SetDeviceConfiguration failed");
                    return;
                }

                // Get the device configuration, just for kicks
                config = cx.GetDeviceConfiguration();
                if (config == null)
                {
                    Console.WriteLine("No device config was available, device running on defaults.");
                }
                else
                {
                    Console.WriteLine("New Configuration:\n{0}", config.ToString());
                }

                // Reboot the device so the new configuration takes effect.
                if (!cx.DeviceReboot())
                {
                    Console.WriteLine("DeviceReboot failed");
                    return;
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
        }

        // This routine demonstrates how to change the password on the
        // "user" account. The password of the "admin" account can also
        // be changed. Admin access is required to change any password.
        // Also shown is logging out of a device. You may log in and out of
        // a device multiple times without disconnecting from the device
        // in between. Note: Do not loose the admin password, it is not
        // field recoverable, the device will have to be sent back to the
        // factory to have the admin password reset.
        static void Password(List<string> args)
        {
            CXCom cx = new CXCom();
            string password = "";
            string search = "";

            if (args.Count > 0) password = args[0];
            if (args.Count > 1) search = args[1];

            try
            {
                // Search for and connect to a device
                cx.Connect(search);

                // Login to device as the administrative user. "admin" rights are
                // required for changing any password. The normal "user" account
                // can't change it's own password.
                CXCom.LoginStatus login = cx.Login(CXCom.LoginUserID.Admin, "admin");
                if (login != CXCom.LoginStatus.Ok)
                {
                    Console.WriteLine("Could not log into device: {0}", login);
                    return;
                }

                // Set the user account password
                if (!cx.SetPassword(CXCom.LoginUserID.User, password))
                {
                    Console.WriteLine("SetPassword failed");
                    return;
                }
                Console.WriteLine("SetPassword = {0}", password);

                // Log out of device, not absolutely required as the api will do a logout
                // on disconnect from device. Just here to demonstrate.
                CXCom.LoginStatus logout = cx.Logout();
                if (logout != CXCom.LoginStatus.Ok)
                {
                    Console.WriteLine("Could not log out of device: {0}", logout);
                    return;
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
        }

        // This routine demonstrates how to get various information from
        // the device. It also demonstrates the more obscure public members
        // of the CXCom object.
        static void Info(List<string> args)
        {
            CXCom cx = new CXCom();
            string search = "";

            if (args.Count > 0) search = args[0];

            try
            {
                // This enables/disables the command response cache.
                // The cache is used to speed up repeated access to 
                // frequently used data structures from the device.
                // Do not disable it, unless you have a good reason.
                // This will be removed later.
                //cx.CacheEnable = false;

                // Search for and connect to a device
                cx.Connect(search);

                // Returns the CXDevice object for the currently connected device,
                // or null if not connected.
                CXDevice condev = cx.ConnectedDevice;
                Console.WriteLine("ConnectedDevice:\n{0}", condev.ToString());

                // Are we currently connected to a device?
                Console.WriteLine("Are we connected = {0}", cx.IsConnected());

                // Ping the device. This is not an ICMP Ping request. This command
                // sends a command packet to the firmware on the CX1 and the CX1 echoes
                // the packet back. This command can be called without logging in to
                // the device, one of the few(Ping, Identify, Login).
                cx.Ping();

                // Get some device information. This command can also be called without
                // logging in.
                CXCom.DeviceIdentify id = cx.Identify();
                Console.WriteLine("Identify:\n{0}", id.ToString());

                // Login to device as a normal user. We are only going to be querying
                // information not changing anything, so user level access is enough.
                CXCom.LoginStatus login = cx.Login(CXCom.LoginUserID.User, "user");
                if (login != CXCom.LoginStatus.Ok)
                {
                    Console.WriteLine("Could not log into device: {0}", login);
                    return;
                }

                // Get the device status, this is for diagnostic purposes. It contains the
                // result of the internal power on self test of the CX1.
                CXCom.DeviceStatus status = cx.GetDeviceStatus();
                Console.WriteLine("Status:\n{0}", status.ToString());

                // Get some info about the part number and date of manufacture of the device.
                // Once again, this is for diagnostic purposes.
                CXCom.ManufactureInfo minfo = cx.GetManufactureInfo();
                Console.WriteLine("Manufacture Info:\n{0}", minfo.ToString());

                // Get some info about the sensor board in the device.
                // Once again, this is for diagnostic purposes.
                CXCom.SensorInformation sinfo = cx.GetSensorInformation();
                Console.WriteLine("Sensor Info:\n{0}", sinfo.ToString());

                // Perform a firmware update on the device.
                // NOTE: this command is not yet implemented, coming soon...
                //if (!cx.SystemUpdate("sensr.snf"))
                //{
                //  Console.WriteLine("Could not update device");
                //  return;
                //}

                // Get raw un-calibrated samples from the device.
                // Don't use, may be removed from the public API.
                //cx.GetRawSamples();
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}", e);
            }
            finally
            {
                cx.Disconnect();
            }
        }

        // This routine demonstrates how to use the scratch pad within the
        // CX1. The scratch pad is a 64K byte section of Flash memory that
        // is available for application use to store whatever context info
        // it may want. There is one important restrictions however on its
        // use, being a simple flash chip it can only be written to 100000
        // times before wearing out. It can be read as many time as desired,
        // there is no limit. The API limits the number of writes to 1 every
        // 2 seconds.
        static void Scratch(List<string> args)
        {
            CXCom cx = new CXCom();
            string search = "";

            if (args.Count > 0) search = args[0];

            try
            {
                // Search for and connect to a device
                cx.Connect(search);

                // Login to device as a normal user. The scratch pad may be read
                // and written by the normal user account. We may later add a
                // admin restriction to write.
                CXCom.LoginStatus login = cx.Login(CXCom.LoginUserID.User, "user");
                if (login != CXCom.LoginStatus.Ok)
                {
                    Console.WriteLine("Could not log into device: {0}", login);
                    return;
                }

                // Returns the size(in bytes) of the scratch pad
                UInt32 size = cx.ScratchPadSize();

                // Write some data to the scratch pad, the Address parameter is an
                // offset within the scratch pad where to start writing data. 
                // This example writes a full sized chuck of data to scratch pad.
                byte[] data = new byte[size];
                for (int i = 0; i < data.Length; i++) data[i] = (byte)i;
                Console.WriteLine("Starting scratch pad write, please wait.");
                if (!cx.SetScratchPad(0, data))
                {
                    Console.WriteLine("Could not write data to scratch pad.");
                    return;
                }
                Console.WriteLine("Scratch pad write Ok.");

                // Read some data from the scratch pad.
                // This example reads 20 bytes starting at address 10
                byte[] rdata = cx.GetScratchPad(10, 20);
                Console.WriteLine("Scratch pad read Ok.");
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}", e);
            }
            finally
            {
                cx.Disconnect();
            }
        }

        // This routine demonstrates how to connect to a device using
        // a CXDevice object as returned by ListDevices. This is the 
        // normal way of connecting. Application software would use
        // ListDevices and present the user with a choice of devices
        // then use the CXDevice to do the actual connect not a search
        // string
        static void Connect()
        {
            CXCom cx = new CXCom();

            try
            {
                // Find all available devices
                List<CXDevice> devs = cx.ListDevices();

                if (devs.Count == 0)
                {
                    Console.WriteLine("No devices found");
                    return;
                }

                // List the devices that were found
                int d = 0;
                foreach (CXDevice dev in devs)
                {
                    Console.WriteLine("Device({0}):\n{1}", d++, dev.ToString());
                }

                // Prompt the user for a device
                Console.Write("Please enter a device number \'Device(#)\' of the desired deivce...");
                string devnum = Console.ReadLine();
                int devidx = int.Parse(devnum);

                // Connect to the specified device
                cx.Connect(devs[devidx]);
                Console.WriteLine("Connect:\n{0}", cx.ToString());

                // Get some device information.
                CXCom.DeviceIdentify id = cx.Identify();
                Console.WriteLine("Identify:\n{0}", id.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}", e);
            }
            finally
            {
                cx.Disconnect();
            }
        }
    }
}
