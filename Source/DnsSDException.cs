using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnsSDNet
{
    /**
 * Thrown when a DNS-SD operation fails.
 * @author Daniel Nilsson
 */
    public class DnsSDException : Exception
    {


    private static long serialVersionUID = 1L;

    public DnsSDException()
    {
    }

    public DnsSDException(String message) : base(message)
    {
        
    }

    public DnsSDException(String message, Exception cause) : base(message, cause)
    {
        
    }

}
}
