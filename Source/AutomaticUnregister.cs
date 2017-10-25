using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;

namespace DnsSDNet
{
    /**
 * Automatically unregister services on JVM shutdown.
 * This class uses a shutdown hook to unregister services when
 * the application exits. Services may not be unregistered on a
 * JMV crash or other abnormal termination.
 * @author Daniel Nilsson
 */
    public class AutomaticUnregister
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(AutomaticUnregister));

        private DnsSDRegistrator registrator;
        private HashSet<ServiceName> serviceNames = new HashSet<ServiceName>();
        private Thread shutdownHook;

        /**
         * Create a new AutomaticUnregister object.
         * @param registrator the DnsSDRegistrator to use.
         */
        public AutomaticUnregister(DnsSDRegistrator registrator)
        {
            this.registrator = registrator;


            this.shutdownHook = new Thread(unregisterAll);

        }

        /**
         * Add a service for automatic unregistration.
         * @param serviceName the service name.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void addService(ServiceName serviceName)
        {
            if (serviceNames.Count == 0)
            {
                AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            }
            serviceNames.Add(serviceName);
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            shutdownHook.Start();
        }

        /**
         * Remove a service from automatic unregistration.
         * @param serviceName the service name.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void removeService(ServiceName serviceName)
        {
            serviceNames.Remove(serviceName);
            if (serviceNames.Count == 0)
            {
                AppDomain.CurrentDomain.ProcessExit -= CurrentDomain_ProcessExit;
            }
        }

        /**
         * Called from the shutdown hook to unregister the services.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void unregisterAll()
        {
            // because this code runs in a shutdown hook it doesn't use the logger
            foreach (ServiceName serviceName in serviceNames)
            {
                try
                {
                    registrator.unregisterService(serviceName);
                }
                catch (DnsSDException e)
                {

                    Log.Error(String.Format("WARNING: Failed to unregister service %s: %s\n", serviceName, e.Message), e);
                }
            }
        }

    }

}

