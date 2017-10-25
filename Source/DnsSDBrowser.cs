using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnsSDNet
{
    /**
    * A DnsSDBrowser object provides methods for discovering services.
    * @author Daniel Nilsson
    */
    public interface DnsSDBrowser
    {

        /**
         * Get the service details for a service.
         * @param service the name of the service.
         * @return the service data.
         */
        ServiceData getServiceData(ServiceName service);

        /**
         * Get the names of all services of a certain type.
         * If the type has one or more subtypes specified then the result
         * is the union of services registered under those subtypes.
         * @param type the service type to look up.
         * @return a collection of service names.
         */
        ICollection<ServiceName> getServiceInstances(ServiceType type);

        /**
         * Get the available service types.
         * This only lists the base types without any subtypes.
         * The DNS-SD RFC provides no way to enumerate subtypes.
         * @return a collection of service types.
         */
        ICollection<ServiceType> getServiceTypes();

    }
}
