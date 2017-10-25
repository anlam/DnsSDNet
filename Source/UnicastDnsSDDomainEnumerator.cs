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
    * Unicast {@link DnsSDDomainEnumerator} implementation backed by dnsjava.
    * @author Daniel Nilsson
    */
    class UnicastDnsSDDomainEnumerator : DnsSDDomainEnumerator
    {

        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(UnicastDnsSDDomainEnumerator));


        private static DomainName B_DNSSD_UDP = DomainName.Parse("b._dns-sd._udp");
        private static DomainName DB_DNSSD_UDP = DomainName.Parse("db._dns-sd._udp");
        private static DomainName R_DNSSD_UDP = DomainName.Parse("r._dns-sd._udp");
        private static DomainName DR_DNSSD_UDP = DomainName.Parse("dr._dns-sd._udp");
        private static DomainName LB_DNSSD_UDP = DomainName.Parse("lb._dns-sd._udp");

        private List<DomainName> computerDomains;

        /**
         * Create a UnicastDnsSDDomainEnumerator.
         * @param computerDomains the list of domains to query for browsing and registering domains.
         */
        public UnicastDnsSDDomainEnumerator(List<DomainName> computerDomains)
        {
            this.computerDomains = computerDomains;
            Log.Info(String.Format("Created DNS-SD DomainEnumerator for computer domains: {0}", String.Join(", ",  computerDomains)));
        }


        public ICollection<String> getBrowsingDomains()
        {
            return getDomains(B_DNSSD_UDP);
        }


        public String getDefaultBrowsingDomain()
        {
            return getDomain(DB_DNSSD_UDP);
        }


        public ICollection<String> getRegisteringDomains()
        {
            return getDomains(R_DNSSD_UDP);
        }

        public String getDefaultRegisteringDomain()
        {
            return getDomain(DR_DNSSD_UDP);
        }


        public ICollection<String> getLegacyBrowsingDomains()
        {
            return getDomains(LB_DNSSD_UDP);
        }

        /**
         * Get all domains pointed to by the given resource record name,
         * searching all computer domains.
         * @param rrName the DNS resource record name.
         * @return a collection of domain names.
         */
        private ICollection<String> getDomains(DomainName rrName)
        {
            List<String> results = new List<String>();
            foreach (DomainName domain in computerDomains)
            {
                results.AddRange(getDomains(rrName, domain));
            }
            return results;
        }

        /**
         * Get one domain pointed to by the given resource record name,
         * searching all computer domains.
         * @param rrName the DNS resource record name.
         * @return a domain name, the first one found.
         */
        private String getDomain(DomainName rrName)
        {
            foreach (DomainName domain in computerDomains)
            {
                List<String> domains = getDomains(rrName, domain);
                if (domains.Count > 0)
                {
                    return domains[0];
                }
            }
            return null;
        }

        /**
         * Get all domains pointed to by the given resource record name,
         * looking in a single computer domain.
         * @param rrName the DNS resource record name.
         * @return a collection of domain names.
         */
        private List<String> getDomains(DomainName rrName, DomainName domainName)
        {

            List<String> results = new List<String>();

            DnsMessage dnsMessage = DNSClientUtil.GetDefaultClient().Resolve(rrName + domainName, RecordType.Ptr);


            if ((dnsMessage == null) || dnsMessage.AnswerRecords.Count == 0 || ((dnsMessage.ReturnCode != ReturnCode.NoError) && (dnsMessage.ReturnCode != ReturnCode.NxDomain)))
                throw new Exception("DNS request failed");
            else

            {
                foreach (DnsRecordBase record in dnsMessage.AnswerRecords)
                {

                    PtrRecord ptr = record as PtrRecord;
                    DomainName name = ptr.PointerDomainName;
                    results.Add(name.ToString());

                }
            }

            return results;

        }

    }

}
