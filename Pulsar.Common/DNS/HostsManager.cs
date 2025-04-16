using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Pulsar.Common.DNS
{
    public class HostsManager
    {
        public bool IsEmpty => _hosts.Count == 0;

        private readonly Queue<Host> _hosts = new Queue<Host>();
        private Host _lastResolvedHost;

        public HostsManager(List<Host> hosts)
        {
            foreach (var host in hosts)
                _hosts.Enqueue(host);
        }

        public Host GetNextHost()
        {
            var temp = _hosts.Dequeue();
            _hosts.Enqueue(temp); 
            temp.IpAddress = ResolveHostname(temp);
            if (temp.IpAddress == null && _lastResolvedHost != null)
            {
                temp = _lastResolvedHost;
            }
            else
            {
                _lastResolvedHost = temp;
            }

            return temp;
        }

        private static IPAddress ResolveHostname(Host host)
        {
            if (string.IsNullOrEmpty(host.Hostname)) return null;

            if (IPAddress.TryParse(host.Hostname, out IPAddress ip))
            {
                if (ip.AddressFamily == AddressFamily.InterNetworkV6 && !Socket.OSSupportsIPv6)
                    return null;

                return ip;
            }

            try
            {
                var ipAddresses = Dns.GetHostEntry(host.Hostname).AddressList;
                foreach (IPAddress ipAddress in ipAddresses)
                {
                    switch (ipAddress.AddressFamily)
                    {
                        case AddressFamily.InterNetwork:
                            return ipAddress;
                        case AddressFamily.InterNetworkV6:
                            if (ipAddresses.Length == 1)
                                return ipAddress;
                            break;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }

    }
}
