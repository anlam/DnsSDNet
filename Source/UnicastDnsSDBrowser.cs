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
 * Unicast {@link DnsSDBrowser} implementation backed by dnsjava.
 * @author Daniel Nilsson
 */
    public class UnicastDnsSDBrowser : DnsSDBrowser
    {

        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(UnicastDnsSDBrowser));


        private static DomainName SERVICES_DNSSD_UDP = DomainName.Parse("_services._dns-sd._udp");




        private List<DomainName> browserDomains;

        /**
         * Create a new UnicastDnsSDBrowser.
         * @param browserDomains list of domain names to browse.
         */
        public UnicastDnsSDBrowser(List<DomainName> browserDomains)
        {
            this.browserDomains = browserDomains;
            Log.Info(String.Format("Created DNS-SD Browser for domains: {0}", String.Join(", ", browserDomains)));
        }


        public ICollection<ServiceType> getServiceTypes()
        {
            HashSet<ServiceType> results = new HashSet<ServiceType>();
            foreach (DomainName domain in browserDomains)
            {
                results.UnionWith(getServiceTypes(domain));
            }
            return results;
        }

        public ICollection<ServiceName> getServiceInstances(ServiceType type)
        {
            List<ServiceName> results = new List<ServiceName>();
            foreach (DomainName domain in browserDomains)
            {
                results.AddRange(getServiceInstances(type, domain));
            }
            return results;
        }

        public ServiceData getServiceData(ServiceName service)
        {
            DomainName serviceName = service.toDnsName();

            DnsMessage dnsMessage = DNSClientUtil.GetDefaultClient().Resolve(serviceName, RecordType.Srv);
            if ((dnsMessage == null) || dnsMessage.AnswerRecords.Count == 0 || ((dnsMessage.ReturnCode != ReturnCode.NoError) && (dnsMessage.ReturnCode != ReturnCode.NxDomain)))
                return null;
            
            ServiceData data = new ServiceData();
            data.setName(service);
            foreach (DnsRecordBase record in dnsMessage.AnswerRecords)
            {
               
                // TODO Handle priority and weight correctly in case of multiple SRV record.
                SrvRecord srv = record as SrvRecord;
                data.setHost(srv.Target.ToString());
                data.setPort(srv.Port);
                break;
            
             }

            dnsMessage = DNSClientUtil.GetDefaultClient().Resolve(serviceName, RecordType.Txt);


            if ((dnsMessage == null) || dnsMessage.AnswerRecords.Count == 0 || ((dnsMessage.ReturnCode != ReturnCode.NoError) && (dnsMessage.ReturnCode != ReturnCode.NxDomain)))
            {
			    return data;
		    }
            foreach (DnsRecordBase record in dnsMessage.AnswerRecords)
            {
			   
				// TODO Handle multiple TXT records as different variants of same service
				TxtRecord txt = record as TxtRecord;
				foreach (String str in txt.TextParts)
                {
					     // Safe cast
                        int i = str.IndexOf('=');
                        String key;
                        String value;
					if (i == 0 || str.Equals(""))
                    {
						continue;   // Invalid empty key, should be ignored
					}
                    else if (i > 0)
                    {
						key = str.Substring(0, i).ToLower();
                        value = str.Substring(i + 1);
					}
                    else
                    {
						key = str;
						value = null;
					}

					if (!data.getProperties().ContainsKey(key)) {   // Ignore all but the first
						data.getProperties()[key] = value;
					}
				}
				break;
			
		}
		return data;
	}


        /**
         * Get the service types from a single domain.
         * @param domainName the domain to browse.
         * @return a list of service types.
         */
        private List<ServiceType> getServiceTypes(DomainName domainName)
        {

            List<ServiceType> results = new List<ServiceType>();

            DnsMessage dnsMessage = DNSClientUtil.GetDefaultClient().Resolve(SERVICES_DNSSD_UDP + domainName, RecordType.Ptr);

            if ((dnsMessage == null) || dnsMessage.AnswerRecords.Count == 0 || ((dnsMessage.ReturnCode != ReturnCode.NoError) && (dnsMessage.ReturnCode != ReturnCode.NxDomain)))
                throw new Exception("DNS request failed");
            else
            {
                foreach (DnsRecordBase record in dnsMessage.AnswerRecords)
                {

                    PtrRecord ptr = record as PtrRecord;
                    DomainName name = ptr.PointerDomainName;

                    String type = name.Labels[0];
                    String transport = name.Labels[1];
                    results.Add(new ServiceType(type, transport));
                }
            }

            return results;
        }

        /**
         * Get all service names of a service type in a single domain.
         * If the specified type has subtypes then only instances registered under any of those are returned.
         * @param type the service type.
         * @param domainName the domain to browse.
         * @return a list of service names.
         */
        private List<ServiceName> getServiceInstances(ServiceType type, DomainName domainName)
        {
            if (type.getSubtypes().Count <= 0)
            {
                List<ServiceName> results = new List<ServiceName>();
                getServiceInstances(type.toDnsString(), domainName, results);
                return results;
            }
            else
            {
                HashSet<ServiceName> results = new HashSet<ServiceName>();
                foreach (String subtype in type.toDnsStringsWithSubtype())
                {
                    getServiceInstances(subtype, domainName, results);
                }
                return new List<ServiceName>(results);
            }
        }

        /**
         * Get all service names of a specific type in a single domain.
         * @param type the service type as a string, including transport and subtype (if any).
         * @param domainName the domain to browse.
         * @param results a collection to put found service names into.
         */
        private void getServiceInstances(String type, DomainName domainName, ICollection<ServiceName> results)
        {

            DomainName typeDN = DomainName.Parse(type);
            DomainName typeDomainName = typeDN + domainName;

            DnsMessage dnsMessage = DNSClientUtil.GetDefaultClient().Resolve(typeDomainName, RecordType.Ptr);

            if ((dnsMessage == null) || dnsMessage.AnswerRecords.Count == 0 || ((dnsMessage.ReturnCode != ReturnCode.NoError) && (dnsMessage.ReturnCode != ReturnCode.NxDomain)))
                throw new Exception("DNS request failed");


            else
            {
                foreach (DnsRecordBase record in dnsMessage.AnswerRecords)
                {

                    PtrRecord ptr = record as PtrRecord;
                    DomainName name = ptr.PointerDomainName;
                    try
                    {
                        results.Add(ServiceName.fromDnsName(name));
                    }
                    catch (ArgumentException e)
                    {
                        Log.Error("Invalid service instance " + name + ": " + e.Message, e);
                    }

                }
            }


        }
    }
}

