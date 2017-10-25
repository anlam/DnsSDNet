using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ARSoft.Tools.Net;

namespace DnsSDNet
{
    /**
  * A unique identifier for a service instance.
  * A service name consists of the triple domain, service type and name.
  * The domain is the fully qualified domain where the service is registered.
  * The service type specified the protocol to use when accessing the service.
  * The name identifies this particular instance of the service. Instance names
  * should be human readable and can contain spaces, punctuation and international
  * characters.
  * <p>
  * Instances of the class are immutable.
  * @author Daniel Nilsson
  */
    public class ServiceName
    {


        //private static Charset NET_UNICODE = Charset.forName("UTF-8");

        private String name;
        private ServiceType type;
        private String domain;

        /**
         * Create a new ServiceName.
         * @param name the name of the service.
         * @param type the type of service.
         * @param domain the fully qualified domain name.
         */
        public ServiceName(String name, ServiceType type, String domain)
        {
            this.name = name;
            this.type = type;
            this.domain = domain;
        }

        /**
         * Get the service name.
         * @return the service name.
         */
        public String getName()
        {
            return name;
        }

        /**
         * Get the service type.
         * @return the service type.
         */
        public ServiceType getType()
        {
            return type;
        }

        /**
         * Get the fully qualified domain name.
         * @return the domain name.
         */
        public String getDomain()
        {
            return domain;
        }

        /**
         * Returns a ServiceName object representing the service specified in the String.
         * The argument is expected to be in the format returned by {@link #toString()}.
         * @param s the string to be parsed.
         * @return a ServiceName representing the service specified by the argument.
         * @throws IllegalArgumentException if the string cannot be parsed as a ServiceName.
         */
        public static ServiceName valueOf(String s)
        {
            int i = indexOfNonEscaped(s, '.');
            if (i < 0)
            {
                throw new ArgumentException("No '.' in service name: " + s);
            }
            String name = unescape(s.Substring(0, i));
            int j = s.IndexOf('.', i + 1);
            if (j < 0)
            {
                throw new ArgumentException("No '.' in service type: " + s);
            }
            j = s.IndexOf('.', j + 1);
            if (j < 0)
            {
                throw new ArgumentException("No '.' after service type: " + s);
            }
            ServiceType type = ServiceType.valueOf(s.Substring(i + 1, j - i -  1));
            i = s.IndexOf(',', j + 1);
            String domain = (i < 0) ? s.Substring(j + 1) : s.Substring(j + 1, i - j - 1);
            if (i >= 0)
            {
                String sublist = s.Substring(i + 1);
                String[] subs = sublist.Split(',');
                foreach (String sub in subs)
                {
                    if (sub.Equals(""))
                    {
                        throw new ArgumentException("Zero length subtype is not allowed: " + s);
                    }
                }
                type = type.withSubtypes(subs);
            }
            return new ServiceName(name, type, domain);
        }

        public override string ToString()
        {
           
            StringBuilder sb = new StringBuilder();
            sb.Append(escape(name)).Append('.').Append(type.toDnsString()).Append('.').Append(domain);
            foreach (String subtype in type.getSubtypes())
            {
                sb.Append(',').Append(subtype);
            }
            return sb.ToString();
        }

        public override bool Equals(object obj)
        {

            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            ServiceName other = (ServiceName)obj;
            if ((this.name == null) ? (other.name != null) : !this.name.Equals(other.name))
            {
                return false;
            }
            if (this.type != other.type && (this.type == null || !this.type.Equals(other.type)))
            {
                return false;
            }
            if ((this.domain == null) ? (other.domain != null) : !this.domain.Equals(other.domain))
            {
                return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            int hash = 7;
            hash = 89 * hash + (this.name != null ? this.name.GetHashCode() : 0);
            hash = 89 * hash + (this.type != null ? this.type.GetHashCode() : 0);
            hash = 89 * hash + (this.domain != null ? this.domain.GetHashCode() : 0);
            return hash;
        }


        /**
         * Convert to a dnsjava {@link Name}.
         * This is an internal helper method.
         * @return the ServiceName as a Name.
         */
        public DomainName toDnsName()
        {
            try
            {
                //List<String> labels = new List<string>();
                DomainName dnsname = DomainName.Parse(domain);

                DomainName transportDN = DomainName.Parse(type.getTransport().getLabel());
                DomainName typeDN = DomainName.Parse(type.getType());
                DomainName nameDN = DomainName.Parse(name);
                nameDN = relativize(nameDN, dnsname);

                dnsname = nameDN + typeDN + transportDN + dnsname;


                return dnsname;
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Invalid DNS name", ex);
            }
        }


        /**
            * If this name is a subdomain of origin, return a new name relative to
            * origin with the same value. Otherwise, return the existing name.
            * @param origin The origin to remove.
            * @return The possibly relativized name.
            */
        private DomainName relativize(DomainName dmName, DomainName origin)
        {
            if (origin == null || !dmName.IsSubDomainOf(origin))
                return dmName;

            int labelCount = dmName.LabelCount - origin.LabelCount;

            String[] newLabels = new String[labelCount];
            Array.Copy(dmName.Labels, newLabels, labelCount);

            DomainName newName = new DomainName(newLabels);

            return newName;

        }

        /**
         * Make a new ServiceName from a dnsjava {@link Name}.
         * @param dnsname the Name to convert.
         * @return the Name as a ServiceName.
         */
        public static ServiceName fromDnsName(DomainName dnsname)
        {
            if (dnsname.LabelCount < 4)
            {
                throw new ArgumentException("Too few labels in service name: " + dnsname);
            }
            String name = dnsname.Labels[0];
            String type = dnsname.Labels[1]; ;
            String transport = dnsname.Labels[2];
            String domain = dnsname.GetParentName(3).ToString();
            return new ServiceName(name, new ServiceType(type, transport), domain);
        }

        /**
         * Decode a raw DNS label into a string.
         * The methods in dnsjava don't understand UTF-8 and escapes some characters,
         * we don't want that here.
         * @param label the raw label data.
         * @return the decoded string.
         */
        private static String decodeName(byte[] label)
        {
            // First byte is length

            return UTF8Encoding.ASCII.GetString(label, 1, label.Length - 1);
        }

        /**
         * Encode a string into a raw DNS label.
         * The methods in dnsjava don't understand UTF-8 and escapes some characters,
         * we don't want that here.
         * @param s the string to encode.
         * @return the raw DNS label.
         */
        private byte[] encodeName(String s)
        {

            byte[] tmp = UTF8Encoding.ASCII.GetBytes(s);
            if (tmp.Length > 63)
            {
                throw new ArgumentException("Name too long: " + s);
            }
            byte[] bytes = new byte[tmp.Length + 2];
            bytes[0] = (byte)tmp.Length;
            Array.Copy(tmp, 0, bytes, 1, tmp.Length);
            bytes[tmp.Length + 1] = 0;
            return bytes;
        }

        /**
         * Escape a service name according to RFC6763 chapter 4.3.
         * @param name the name to escape.
         * @return the name with '.' and '\' escaped.
         */
        private static String escape(String name)
        {
            return name.Replace("\\\\|\\.", "\\\\$0");       // Replace "\" with "\\" and "." with "\."
        }

        /**
         * Undo escaping of a service name.
         * @see #escape(String)
         * @param name the escaped name.
         * @return the name with escapes removed.
         */
        private static String unescape(String name)
        {

            return name.Replace("\\\\(.)", "$1");        // Replace "\x" with "x" for any x
        }

        /**
         * Find the first non-escaped occurrence of a character in a string.
         * @see String#indexOf(int)
         * @param string the string to look through.
         * @param ch the character to find.
         * @return the index of the first occurrence, or -1 if it can't be found. 
         */
        private static int indexOfNonEscaped(String str, char ch)
        {
            for (int i = 0; i < str.Length; i++)
            {
                int c = str[i];
                if (c == '\\')
                {
                    i++;
                }
                else if (c == ch)
                {
                    return i;
                }
            }
            return -1;
        }

    }

}
