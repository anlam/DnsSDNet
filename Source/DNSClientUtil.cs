using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using System.Net;

namespace DnsSDNet
{
    class DNSClientUtil
    {
        private static DnsClient client;
        private static List<IPAddress> servers;


        public static DnsClient GetDefaultClient()
        {

            if (client == null)
            {
                if (servers == null)
                    servers = new List<IPAddress>();

                String prop = Environment.GetEnvironmentVariable("dnssdServer");
                if (prop != null && !prop.Equals(""))
                {
                    String[] st = prop.Trim().Split(',');
                    foreach (string str in st)
                    {
                        if (str != null && !str.Equals(""))
                            servers.Add(IPAddress.Parse(str));
                    }

                }

                if (servers.Count <= 0)
                    client = DnsClient.Default;
                else
                    client = new DnsClient(servers, 10000);
            }

            return client;
        }

        //public static void AddServer(IPAddress server)
        //{
        //    if (servers == null)
        //        servers = new List<IPAddress>();
        //    servers.Add(server);
        //    client = null;
        //}
    }
}
