## DNS-SD for C# .NET

### Overview:

This library provides service discovery and registration using [DNS-SD](http://www.dns-sd.org/) to C# applications.

### Note:
- Use environment method to specify the dns server address: 
```cs
Environment.SetEnvironmentVariable("dnssdServer", "10.200.0.10");
```
Otherwise, the default DNS Server which is configured on the executing computer will be used. See _DNSClientUtil.cs_ for more details.
- [log4net](https://logging.apache.org/log4net/) is used for logging purpose.

### Acknowledgments:

- The implementation of this library is almost similar to the [DNS-SD for Java](https://github.com/DanielN/dnssdjava) provided by Daniel Nilsson.
- The low level DNS communication is provided by [ARSoft.Tools.Net](https://arsofttoolsnet.codeplex.com/).
