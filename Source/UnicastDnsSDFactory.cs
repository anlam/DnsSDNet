using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;

namespace DnsSDNet
{
    /**
   * Unicast {@link DnsSDFactory} implementation backed by dnsjava.
   * @author Daniel Nilsson
   */
    public class UnicastDnsSDFactory : DnsSDFactory
    {

        public UnicastDnsSDFactory()
        {
        }



        public override DnsSDDomainEnumerator createDomainEnumerator(ICollection<String> computerDomains)
        {
            List<DomainName> domains = new List<DomainName>(computerDomains.Count);
            foreach (String domain in computerDomains)
            {
                domains.Add(DomainName.Parse(domain));

            }
            return new UnicastDnsSDDomainEnumerator(domains);
        }


        public override DnsSDBrowser createBrowser(ICollection<String> browserDomains)
        {
            List<DomainName> domains = new List<DomainName>(browserDomains.Count);
            foreach (String domain in browserDomains)
            {

                domains.Add(DomainName.Parse(domain));

            }
            return new UnicastDnsSDBrowser(domains);
        }


        public override DnsSDRegistrator createRegistrator(String registeringDomain)
        {
            try
            {
                //return null;
                return new UnicastDnsSDRegistrator(DomainName.Parse(registeringDomain));
            }
            catch (Exception ex)
            {
                throw new DnsSDException("Failed to find DNS update server for domain: " + registeringDomain, ex);
            }
        }

    }

}
