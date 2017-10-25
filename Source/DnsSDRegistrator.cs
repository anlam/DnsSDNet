using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ARSoft.Tools.Net.Dns;

namespace DnsSDNet
{
    /**
   * A DnsSDRegistrator object provides methods for registering services.
   * @author Daniel Nilsson
   */
    public abstract class DnsSDRegistrator
    {

        /**
         * Make a service name for a service in this DnsSDRegistrators domain.
         * @param name the service name.
         * @param type the service type.
         * @return a service name.
         */
        public abstract ServiceName makeServiceName(String name, ServiceType type);

        /**
         * Convenience method for getting the fully qualified name of the local host.
         * The host name is often used for passing to {@link ServiceData#setHost(String)}.
         * This method seems to work better than using
         * <code>InetAddress.getLocalHost().getCanonicalHostName()</code>
         * @return the fully qualified host name.
         * @throws UnknownHostException if the host name cannot be found.
         */
        public abstract String getLocalHostName();

        /**
         * Get the time to live value that will be used for new DNS records.
         * @return the TTL value in seconds.
         */
        public abstract int getTimeToLive();

        /**
         * Set the time to live value that will be used for new DNS records.
         * @param ttl the new TTL value in seconds.
         */
        public abstract void setTimeToLive(int ttl);

        /**
         * Set the TSIG key used to authenticate updates sent to the DNS server.
         * Passing null for all values to disable TSIG authentication.
         * @param name the name of the key.
         * @param algorithm the signature algorithm, one of {@link #TSIG_ALGORITHM_HMAC_MD5},
         *        {@link #TSIG_ALGORITHM_HMAC_SHA1}, {@link #TSIG_ALGORITHM_HMAC_SHA256} 
         * @param key the base64 encoded key.
         */
        public abstract void setTSIGKey(String name, TSigAlgorithm algorithm, String key);

        /**
         * Add a new service to DNS-SD.
         * If the service name is already taken this method will not update the service data,
         * but return false to indicate the collision.
         * @param serviceData the service to register.
         * @return true if the service was registered, false if the service name was already registered.
         * @throws DnsSDException if the service couldn't be registered due to some error.
         */
        public abstract bool registerService(ServiceData serviceData);

        /**
         * Remove a service from DNS-SD.
         * @param serviceName the name of the service to remove.
         * @return true if the service was removed, false if no service was found.
         * @throws DnsSDException if the service couldn't be unregistered due to some error.
         */
        public abstract bool unregisterService(ServiceName serviceName);

        /**
         * Constant specifying the hmac-md5 TSIG algorithm.
         */
        public String TSIG_ALGORITHM_HMAC_MD5 = "hmac-md5";

        /**
         * Constant specifying the hmac-sha1 TSIG algorithm.
         */
        public String TSIG_ALGORITHM_HMAC_SHA1 = "hmac-sha1";

        /**
         * Constant specifying the hmac-sha256 TSIG algorithm.
         */
        public String TSIG_ALGORITHM_HMAC_SHA256 = "hmac-sha256";

    }
}
