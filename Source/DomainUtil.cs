using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Net;
using ARSoft.Tools.Net;

namespace DnsSDNet
{
    /**
  * Internal helper class for figuring out domain names.
  * @author Daniel Nilsson
  */
    class DomainUtil
    {


        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(DomainUtil));
        /**
         * Try to figure out the domain name for the computer.
         * @return a list of potential domain names.
         */
        public static List<String> getComputerDomains(String domain = null)
        {
            //String domain = System.getProperty("dnssd.domain");
            if (domain != null && !domain.Equals(""))
            {
                List<String> lst = new List<string>();
                lst.Add(domain);
                return lst;
            }
            List<String> results = new List<String>();
            try
            {

                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var i in interfaces)
                {
                    if (i.OperationalStatus.Equals(OperationalStatus.Up) && !i.NetworkInterfaceType.Equals(NetworkInterfaceType.Loopback))
                    {


                        foreach (var ifaddr in i.GetIPProperties().UnicastAddresses)
                        {
                            IPAddress inetAddr = ifaddr.Address;
                            try
                            {
                                //Try to figure out the domain by taking the host name...
                                String hostname = Dns.GetHostEntry(inetAddr).HostName;
                                //  ...and remove the leftmost part.
                                results.Add(DomainName.Parse(hostname).GetParentName().ToString());
                            }
                            catch (System.Net.Sockets.SocketException ex)
                            {
                                Log.Error(String.Format("No hostname for address: {0}", inetAddr), ex);
                            }

                            //Use the reverse lookup name for the network

                            IPAddress network = calculateNetworkAddress(ifaddr);
                            results.Add(network.GetReverseLookupAddress());

                        }
                    }
                }
            }
            catch (NetworkInformationException ex)
            {
                Log.Error("Failed to enumerate network interfaces", ex);
            }


            return results;
        }

        /**
         * Try to figure out the host name for the computer.
         * @return a list of potential host names.
         */
        public static List<String> getComputerHostNames(String hostname = null)
        {
           
            if (hostname != null)
            {
                List<String> lst = new List<string>();
                lst.Add(hostname);

                return lst;
            }

            List<String> results = new List<String>();
            try
            {
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var i in interfaces)
                {

                    if (i.OperationalStatus.Equals(OperationalStatus.Up) && !i.NetworkInterfaceType.Equals(NetworkInterfaceType.Loopback))
                    {
                        foreach (var ifaddr in i.GetIPProperties().UnicastAddresses)
                        {
                            IPAddress inetAddr = ifaddr.Address;
                            try
                            {
                                hostname = Dns.GetHostEntry(inetAddr).HostName;
                                results.Add(hostname);
                            }
                            catch (System.Net.Sockets.SocketException ex)
                            {
                                Log.Error(String.Format("No hostname for address: {0}", inetAddr), ex);
                            }
                        }
                    }
                }
            }
            catch (NetworkInformationException ex)
            {
                Log.Error("Failed to enumerate network interfaces", ex);
            }
            return results;
        }

        /**
         * Calculate the network address by taking the bitwise AND
         * between the IP-address and the netmask.
         * @param ifaddr the interface address to calculate the network address of. 
         * @return the network address (host part is all zero).
         * @throws UnknownHostException if something went terribly wrong.
         */
        public static IPAddress calculateNetworkAddress(UnicastIPAddressInformation ifaddr)
        {

            byte[] addr = ifaddr.Address.GetAddressBytes();
            int n = ifaddr.PrefixLength;
            int i = n / 8;
            int j = n % 8;
            if (i < addr.Length)
            {
                byte mask = (byte)(0xFF00 >> j);
                addr[i] &= mask;
                //Arrays.fill(addr, i + 1, addr.Length, (byte)0);

                for (int index = i + 1; index < addr.Length; index++)
                    addr[index] = (byte)0;
            }

            return new IPAddress(addr);


        }

    }

}
