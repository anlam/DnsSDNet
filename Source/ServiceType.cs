using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnsSDNet
{
    /**
  * Identifiers for service types.
  * A DNS-SD service type consists of the application protocol name
  * prepended with an underscore and the transport protocol (TCP or UDP).
  * @author Daniel Nilsson
  */
    public class ServiceType
    {

        /**
         * The transport protocol.
         */
        public class Transport
        {

            public static String TCP = "_tcp";

            public static String UDP = "_udp";

            private String label;

            private Transport(String label)
            {
                this.label = label;
            }

            /**
		     * Get the DNS label.
		     * @return the DNS label.
		     */
            public String getLabel()
            {
                return label;
            }

            public override String ToString()
            {
                return label;
            }


            public override bool Equals(object obj)
            {

                if (obj == null || GetType() != obj.GetType())
                {
                    return false;
                }

                Transport transport = obj as Transport;
                return this.getLabel().Equals(transport.getLabel());
            }





            /**
		     * Get the Transport corresponding to a DNS label.
		     * @param label the DNS label.
		     * @return the corresponding Transport constant.
		     */
            public static Transport fromLabel(String label)
            {

                if (label.ToLower().Trim().Equals(TCP))
                    return new Transport(TCP);
                else if (label.ToLower().Trim().Equals(UDP))
                    return new Transport(UDP);
                else
                    throw new ArgumentException("Not a valid transport label: " + label);
            }
        }

        private String type;
        private Transport transport;
        private List<String> subtypes;

        /**
         * Create a new ServiceType.
         * @param type the service type (eg. "_http").
         * @param transport the transport protocol.
         */
        public ServiceType(String type, Transport transport)
        {
            this.type = type;
            this.transport = transport;
            this.subtypes = new List<string>();
        }

        /**
         * Create a new ServiceType.
         * For internal use only.
         * @param type the service type.
         * @param transport the transport DNS label.
         */
        public ServiceType(String type, String transport) : this(type, Transport.fromLabel(transport))
        {
        }

        /**
         * Create a new ServiceType with subtypes.
         * For internal use only.
         * @param baseType the base service type.
         * @param subtypes the subtypes of the service type, if any.
         */
        private ServiceType(ServiceType baseType, String[] subtypes)
        {
            this.type = baseType.type;
            this.transport = baseType.transport;
            this.subtypes = subtypes.ToList();
        }

        /**
         * Create a subtype variant of this ServiceType.
         * Any existing subtypes in this ServiceType is not passed on to the new ServiceType.
         * A subtype of a ServiceType only provides additional filtering when browsing, it is still
         * the same service type. In particular the subtype variant still {@link #equals(Object)}
         * the base ServiceType and has the same {@link #hashCode()}.
         * @param subtype the subtype.
         * @return a new ServiceType based on this ServiceType but with the given subtype.
         */
        public ServiceType withSubtype(String subtype)
        {
            List<String> st = new List<string>();
            st.Add(subtype);
            return new ServiceType(this, st.ToArray());
        }

        /**
         * Create a variant of this ServiceType with multiple subtypes.
         * Any existing subtypes in this ServiceType is not passed on to the new ServiceType.
         * A subtype of a ServiceType only provides additional filtering when browsing, it is still
         * the same service type. In particular the subtype variant still {@link #equals(Object)}
         * the base ServiceType and has the same {@link #hashCode()}.
         * @param subtypes the subtypes.
         * @return a new ServiceType based on this ServiceType but with the given subtypes.
         */
        public ServiceType withSubtypes(String[] subtypes)
        {
            return new ServiceType(this, subtypes);
        }

        /**
         * Returns a ServiceType representing the base type of this ServiceType.
         * If there are no subtypes then this ServiceType is returned
         * else a new ServiceType is returned based on this but without the subtypes.
         * @return the base ServiceType without subtypes.
         */
        public ServiceType baseType()
        {
            if (subtypes.Count == 0)
            {
                return this;
            }
            else
            {
                return new ServiceType(type, transport);
            }
        }

        /**
         * Get the service type.
         * @return the service type.
         */
        public String getType()
        {
            return type;
        }

        /**
         * Get the transport protocol.
         * @return the transport protocol.
         */
        public Transport getTransport()
        {
            return transport;
        }

        /**
         * Get the list of subtypes.
         * @return the list of subtypes.
         */
        public List<String> getSubtypes()
        {
            return subtypes;
        }

        /**
         * Returns a string representation of the ServiceType.
         * Examples: "_http._tcp", "_ftp._tcp,_anon".
         * @return a string of the format "{type}.{transport}[,{subtype}][,{subtype}][...]".
         */
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(type).Append('.').Append(transport);
            foreach (String subtype in subtypes)
            {
                sb.Append(',').Append(subtype);
            }
            return sb.ToString();
        }

        /**
         * Returns a ServiceType object representing the type specified in the String.
         * The argument is expected to be in the format returned by {@link #toString()}.
         * @param s the string to be parsed.
         * @return a ServiceType representing the type specified by the argument.
         * @throws IllegalArgumentException if the string cannot be parsed as a ServiceType.
         */
        public static ServiceType valueOf(String s)
        {
            int i = s.IndexOf(',');
            String domain = (i < 0) ? s : s.Substring(0, i);
            String sublist = (i < 0) ? null : s.Substring(i + 1);
            i = domain.IndexOf('.');
            if (i < 0)
            {
                throw new ArgumentException("No '.' in service type: " + s);
            }
            String type = domain.Substring(0, i);
            String transport = domain.Substring(i + 1);
            ServiceType res = new ServiceType(type, transport);
            if (sublist != null)
            {
                String[] subs = sublist.Split(',');
                foreach (String sub in subs)
                {
                    if (sub.Trim().Equals(""))
                    {
                        throw new ArgumentException("Zero length subtype is not allowed: " + s);
                    }
                }
                res = res.withSubtypes(subs);
            }
            return res;
        }

        /**
         * Get the DNS-SD subdomain that represents this type (excluding any subtypes).
         * For internal use only.
         * @return A string of the form "{type}.{transport}".
         */
        public String toDnsString()
        {
            return type + "." + transport;
        }

        /**
         * Get the DNS-SD subdomains that represent this type, one for each subtype.
         * For internal use only.
         * @return A list of strings of the form "{subtype}._sub.{type}.{transport}".
         */
       public List<String> toDnsStringsWithSubtype()
        {
            List<String> list = new List<String>(subtypes.Count);
            foreach (String subtype in subtypes)
            {
                list.Add(subtype + "._sub." + type + "." + transport);
            }
            return list;
        }


        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            ServiceType other = (ServiceType)obj;
            if ((this.type == null) ? (other.type != null) : !this.type.Equals(other.type))
            {
                return false;
            }
            if (this.transport != other.transport)
            {
                return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            int hash = 7;
            hash = 29 * hash + (this.type != null ? this.type.GetHashCode() : 0);
            hash = 29 * hash + (this.transport != null ? this.transport.GetHashCode() : 0);
            return hash;
        }

    }
}
