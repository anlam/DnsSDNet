using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using System.Net;
using System.Net.NetworkInformation;
using System.Net;
using System.Text.RegularExpressions;
using ARSoft.Tools.Net.Dns.DynamicUpdate;

namespace DnsSDNet
{
    /**
     * Unicast {@link DnsSDRegistrator} implementation backed by dnsjava.
     * @author Daniel Nilsson
     */
    class UnicastDnsSDRegistrator : DnsSDRegistrator
    {

        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(UnicastDnsSDRegistrator));


        private static DomainName DNSUPDATE_UDP = DomainName.Parse("_dns-update._udp");
        private static DomainName SERVICES_DNSSD_UDP = DomainName.Parse("_services._dns-sd._udp");

        private DomainName registrationDomain;
        private DnsClient resolver;
        private DomainName servicesName;


        //private TSigRecord tsigRecord;
        private DomainName tsigName;
        private byte[] tsigKey;
        private TSigAlgorithm tsigAlg;

        private int timeToLive = 60;
        private String localHostname;

        /**
         * Create a UnicastDnsSDRegistrator.
         * @param registrationDomain the registration domain.
         * @throws UnknownHostException if the DNS server name for the domain failed to resolve.
         */
        public UnicastDnsSDRegistrator(DomainName registrationDomain)
        {

            this.registrationDomain = registrationDomain;
            this.resolver = findUpdateResolver(registrationDomain);
            this.servicesName = SERVICES_DNSSD_UDP + registrationDomain;
            Log.Info(String.Format("Created DNS-SD Registrator for domain {0}", registrationDomain));
        }

        /**
         * Create a DNS {@link Resolver} to handle updates to the given domain.
         * @param domain the domain for which updates will be generated.
         * @return a Resolver configured with the DNS server that handles zone for that domain.
         * @throws UnknownHostException if the DNS server name for the domain failed to resolve.
         */
        private DnsClient findUpdateResolver(DomainName domain)
        {
            DnsClient dnsClient = null;

            DnsMessage dnsMessage = DNSClientUtil.GetDefaultClient().Resolve(DNSUPDATE_UDP + domain, RecordType.Srv);



            if ((dnsMessage == null) || dnsMessage.AnswerRecords.Count == 0 ||  ((dnsMessage.ReturnCode != ReturnCode.NoError) && (dnsMessage.ReturnCode != ReturnCode.NxDomain)))
                throw new Exception("DNS request failed");
            else

            {
                foreach (DnsRecordBase record in dnsMessage.AnswerRecords)
                {

                    SrvRecord srv = record as SrvRecord;
                    IPAddress[] address = Dns.GetHostAddresses(srv.Target.ToString());
                    dnsClient = new DnsClient(address, 10000);

                    Log.Info(String.Format("Using DNS server {0} to perform updates.", address));

                    break;

                }
            }

            if (dnsClient == null)
                dnsClient = DNSClientUtil.GetDefaultClient();
            return dnsClient;
        }


        public override ServiceName makeServiceName(String name, ServiceType type)
        {
            return new ServiceName(name, type, registrationDomain.ToString());
        }


        public override String getLocalHostName()
        {
            if (localHostname == null)
            {
                List<String> names = new List<String>();
                names.AddRange(DomainUtil.getComputerHostNames());


                names.Add(Dns.GetHostEntry("").HostName);
                names.Add(Dns.GetHostName());
                foreach (String name in names)
                {
                    if (!name.StartsWith("localhost") && !Regex.IsMatch(name, "^([0-9]{1,3}\\.){3}[0-9]{1,3}$"))
                    {
                        localHostname = name.EndsWith(".") ? name : (name + ".");
                        break;
                    }
                }
            }
            if (localHostname == null)
            {
                throw new Exception("UnknownHostException");
            }
            return localHostname;
        }


        public override int getTimeToLive()
        {
            return timeToLive;
        }


        public override void setTimeToLive(int ttl)
        {
            timeToLive = ttl;
        }


        public override void setTSIGKey(String name, TSigAlgorithm algorithm, String key)
        {
            if (name != null && key != null)
            {
                this.tsigName = DomainName.Parse(name);
                this.tsigAlg = algorithm;
                 this.tsigKey = Convert.FromBase64String(key);

               // TSigRecord

               // this.tsigKey = System.Text.UTF8Encoding.ASCII.GetBytes(key); 
            }

        }

        private TSigRecord generateTSigRecord(ushort transactionID)
        {

            TSigRecord record = null;
            if (tsigName != null && tsigKey != null)
            {
                record = new TSigRecord(tsigName, tsigAlg, DateTime.Now, new TimeSpan(0, 5, 0), transactionID, ReturnCode.NoError, null, tsigKey);
            }
            return record;
        }

        public override bool registerService(ServiceData serviceData)
        {

            ServiceName serviceName = serviceData.getName();
            DomainName dnsName = serviceName.toDnsName();
            DomainName typeName = DomainName.Parse(serviceName.getType().toDnsString()) + registrationDomain;
            List<DomainName> subtypes = new List<DomainName>(serviceName.getType().getSubtypes().Count);
            foreach (String subtype in serviceName.getType().toDnsStringsWithSubtype())
            {
                subtypes.Add(DomainName.Parse(subtype) + registrationDomain);
            }
            DomainName target = DomainName.Parse(serviceData.getHost());

            List<String> strings = new List<String>();
            foreach (var entry in serviceData.getProperties())
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(entry.Key);
                if (entry.Key != null)
                {
                    sb.Append('=').Append(entry.Value);
                }
                strings.Add(sb.ToString());
            }
            if (strings.Count <= 0)
            {
                // Must not be empty
                strings.Add("");
            }

            DnsUpdateMessage msg = new DnsUpdateMessage();
            msg.ZoneName = registrationDomain; // XXX Should really be the zone (SOA) for the RRs we are about to add

            msg.Prequisites.Add(new RecordNotExistsPrequisite(dnsName, RecordType.Any));
            msg.Updates.Add(new AddRecordUpdate(new PtrRecord(servicesName, timeToLive, typeName)));
            msg.Updates.Add(new AddRecordUpdate(new PtrRecord(typeName, timeToLive, dnsName)));


            foreach (DomainName subtype in subtypes)
            {
                msg.Updates.Add(new AddRecordUpdate(new PtrRecord(subtype, timeToLive, dnsName)));
            }

            msg.Updates.Add(new AddRecordUpdate(new SrvRecord(dnsName, timeToLive, 0, 0, Convert.ToUInt16(serviceData.getPort()), target)));

            msg.Updates.Add(new AddRecordUpdate(new TxtRecord(dnsName, timeToLive, strings)));

            TSigRecord tsigRecord = generateTSigRecord(msg.TransactionID);
            if (tsigRecord != null)
                msg.TSigOptions = tsigRecord;


            DnsUpdateMessage response = resolver.SendUpdate(msg);


            if (response != null)
                switch (response.ReturnCode)
                {
                    case ReturnCode.NoError:
                        //flushCache(update);
                        return true;
                    case ReturnCode.YXDomain:    // Prerequisite failed, the service already exists.
                        return false;
                    default:
                        throw new DnsSDException("Failed to send DNS update to server. Server returned error code: " + response.ReturnCode.ToString());
                }
            else
                throw new DnsSDException("Failed to send DNS update to server. Server time out");
        }

        public override bool unregisterService(ServiceName serviceName)
        {

            DomainName dnsName = serviceName.toDnsName();
            DomainName typeName = DomainName.Parse(serviceName.getType().toDnsString()) + registrationDomain;
            List<DomainName> subtypes = new List<DomainName>(serviceName.getType().getSubtypes().Count);
            foreach (String subtype in serviceName.getType().toDnsStringsWithSubtype())
            {
                subtypes.Add(DomainName.Parse(subtype) + registrationDomain);
            }

            DnsUpdateMessage msg = new DnsUpdateMessage();
            msg.ZoneName = registrationDomain; // XXX Should really be the zone (SOA) for the RRs we are about to add

            msg.Prequisites.Add(new RecordExistsPrequisite(dnsName, RecordType.Any));

            msg.Updates.Add(new DeleteRecordUpdate(new PtrRecord(typeName, timeToLive, dnsName)));


            foreach (DomainName subtype in subtypes)
            {
                msg.Updates.Add(new DeleteRecordUpdate(new PtrRecord(subtype, timeToLive, dnsName)));

            }

            msg.Updates.Add(new DeleteAllRecordsUpdate(dnsName));

            TSigRecord tsigRecord = generateTSigRecord(msg.TransactionID);
            if (tsigRecord != null)
                msg.TSigOptions = tsigRecord;


            DnsUpdateMessage response = resolver.SendUpdate(msg);

            if (response != null)
                switch (response.ReturnCode)
                {
                    case ReturnCode.NoError:
                        //flushCache(update);
                        break;
                    case ReturnCode.YXDomain:    // Prerequisite failed, the service already exists.
                        return false;
                    default:
                        throw new DnsSDException("Failed to send DNS update to server. Server returned error code: " + response.ReturnCode.ToString());
                }
            else
                throw new DnsSDException("Failed to send DNS update to server. Server time out");

            // Remove the service type if there are no instances left
            msg = new DnsUpdateMessage();
            msg.ZoneName = registrationDomain; // XXX Should really be the zone (SOA) for the RRs we are about to add

            msg.Prequisites.Add(new RecordNotExistsPrequisite(typeName, RecordType.Any));
            msg.Updates.Add(new DeleteRecordUpdate(new PtrRecord(servicesName, timeToLive, typeName)));

            tsigRecord = generateTSigRecord(msg.TransactionID);
            if (tsigRecord != null)
                msg.TSigOptions = tsigRecord;


            response = resolver.SendUpdate(msg);
            if (response != null)
                switch (response.ReturnCode)
                {
                    case ReturnCode.NoError:
                        //flushCache(update);
                        Log.Info(String.Format("Removed service type record {0}", typeName));
                        break;
                    case ReturnCode.YXDomain:    // Prerequisite failed, service instances exists
                        Log.Info(String.Format("Did not remove service type record {0}, instances left.", typeName));
                        break;
                    default:
                        Log.Error(String.Format("Failed to remove service type {0}, server returned status {1}", typeName, response.ReturnCode.ToString()));
                        break;
                }
            else
                throw new DnsSDException("Failed to send DNS update to server. Server time out");

           
            
            return true;


        }

        /**
         * Flush all records related to the update from the default cache.
         * @param update the update to flush.
         */
        //private static void flushCache(Update update)
        //{


        //    Cache cache = Lookup.getDefaultCache(DClass.IN);
        //    Record[] records = update.getSectionArray(Section.UPDATE);
        //    for (Record rec : records)
        //    {
        //        logger.log(Level.FINE, "Flush name {0} due to update: {1}", new Object[] { rec.getName(), rec });
        //        cache.flushName(rec.getName());
        //    }
        //}
    }

}
