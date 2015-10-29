﻿using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace MediaBrowser.Common.Implementations.Networking
{
    public abstract class BaseNetworkManager
    {
        protected ILogger Logger { get; private set; }
        private Timer _clearCacheTimer;

        protected BaseNetworkManager(ILogger logger)
        {
            Logger = logger;

            // Can't use network change events due to a crash in Linux
            _clearCacheTimer = new Timer(ClearCacheTimerCallback, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        private void ClearCacheTimerCallback(object state)
        {
            lock (_localIpAddressSyncLock)
            {
                _localIpAddresses = null;
            }
        }

        private volatile List<string> _localIpAddresses;
        private readonly object _localIpAddressSyncLock = new object();

        /// <summary>
        /// Gets the machine's local ip address
        /// </summary>
        /// <returns>IPAddress.</returns>
        public IEnumerable<string> GetLocalIpAddresses()
        {
            if (_localIpAddresses == null)
            {
                lock (_localIpAddressSyncLock)
                {
                    if (_localIpAddresses == null)
                    {
                        var addresses = GetLocalIpAddressesInternal().ToList();

                        _localIpAddresses = addresses;

                        return addresses;
                    }
                }
            }

            return _localIpAddresses;
        }

        private IEnumerable<string> GetLocalIpAddressesInternal()
        {
            var list = GetIPsDefault()
                .Where(i => !IPAddress.IsLoopback(i))
                .Select(i => i.ToString())
                .Where(FilterIpAddress)
                .ToList();

            if (list.Count > 0)
            {
                return list;
            }

            return GetLocalIpAddressesFallback().Where(FilterIpAddress);
        }

        private bool FilterIpAddress(string address)
        {
            if (address.StartsWith("169.", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private bool IsInPrivateAddressSpace(string endpoint)
        {
            // Private address space:
            // http://en.wikipedia.org/wiki/Private_network

            if (endpoint.StartsWith("172.", StringComparison.OrdinalIgnoreCase))
            {
                return Is172AddressPrivate(endpoint);
            }

            return

                // If url was requested with computer name, we may see this
                endpoint.IndexOf("::", StringComparison.OrdinalIgnoreCase) != -1 ||

                endpoint.StartsWith("localhost", StringComparison.OrdinalIgnoreCase) ||
                endpoint.StartsWith("127.", StringComparison.OrdinalIgnoreCase) ||
                endpoint.StartsWith("10.", StringComparison.OrdinalIgnoreCase) ||
                endpoint.StartsWith("192.168", StringComparison.OrdinalIgnoreCase) ||
                endpoint.StartsWith("169.", StringComparison.OrdinalIgnoreCase);
        }

        private bool Is172AddressPrivate(string endpoint)
        {
            for (var i = 16; i <= 31; i++)
            {
                if (endpoint.StartsWith("172." + i.ToString(CultureInfo.InvariantCulture) + ".", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsInLocalNetwork(string endpoint)
        {
            return IsInLocalNetworkInternal(endpoint, true);
        }

        public bool IsInLocalNetworkInternal(string endpoint, bool resolveHost)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentNullException("endpoint");
            }

            if (IsInPrivateAddressSpace(endpoint))
            {
                return true;
            }

            const int lengthMatch = 4;

            if (endpoint.Length >= lengthMatch)
            {
                var prefix = endpoint.Substring(0, lengthMatch);

                if (GetLocalIpAddresses()
                    .Any(i => i.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            IPAddress address;
            if (resolveHost && !IPAddress.TryParse(endpoint, out address))
            {
                Uri uri;
                if (Uri.TryCreate(endpoint, UriKind.RelativeOrAbsolute, out uri))
                {
                    try
                    {
                        var host = uri.DnsSafeHost;
                        Logger.Debug("Resolving host {0}", host);

                        address = GetIpAddresses(host).FirstOrDefault();

                        if (address != null)
                        {
                            Logger.Debug("{0} resolved to {1}", host, address);

                            return IsInLocalNetworkInternal(address.ToString(), false);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Can happen with reverse proxy or IIS url rewriting
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorException("Error resovling hostname", ex);
                    }
                }
            }

            return false;
        }

        public IEnumerable<IPAddress> GetIpAddresses(string hostName)
        {
            return Dns.GetHostAddresses(hostName);
        }

        private IEnumerable<IPAddress> GetIPsDefault()
        {
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                var props = adapter.GetIPProperties();
                var gateways = from ga in props.GatewayAddresses
                               where !ga.Address.Equals(IPAddress.Any)
                               select true;

                if (!gateways.Any())
                {
                    continue;
                }

                foreach (var uni in props.UnicastAddresses)
                {
                    var address = uni.Address;
                    if (address.AddressFamily != AddressFamily.InterNetwork)
                    {
                        continue;
                    }
                    yield return address;
                }
            }
        }

        private IEnumerable<string> GetLocalIpAddressesFallback()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());

            // Reverse them because the last one is usually the correct one
            // It's not fool-proof so ultimately the consumer will have to examine them and decide
            return host.AddressList
                .Where(i => i.AddressFamily == AddressFamily.InterNetwork)
                .Select(i => i.ToString())
                .Reverse();
        }

        /// <summary>
        /// Gets a random port number that is currently available
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Any, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        /// <summary>
        /// Returns MAC Address from first Network Card in Computer
        /// </summary>
        /// <returns>[string] MAC Address</returns>
        public string GetMacAddress()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(i => i.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(i => BitConverter.ToString(i.GetPhysicalAddress().GetAddressBytes()))
                .FirstOrDefault();
        }

        /// <summary>
        /// Parses the specified endpointstring.
        /// </summary>
        /// <param name="endpointstring">The endpointstring.</param>
        /// <returns>IPEndPoint.</returns>
        public IPEndPoint Parse(string endpointstring)
        {
            return Parse(endpointstring, -1);
        }

        /// <summary>
        /// Parses the specified endpointstring.
        /// </summary>
        /// <param name="endpointstring">The endpointstring.</param>
        /// <param name="defaultport">The defaultport.</param>
        /// <returns>IPEndPoint.</returns>
        /// <exception cref="System.ArgumentException">Endpoint descriptor may not be empty.</exception>
        /// <exception cref="System.FormatException"></exception>
        private static IPEndPoint Parse(string endpointstring, int defaultport)
        {
            if (String.IsNullOrEmpty(endpointstring)
                || endpointstring.Trim().Length == 0)
            {
                throw new ArgumentException("Endpoint descriptor may not be empty.");
            }

            if (defaultport != -1 &&
                (defaultport < IPEndPoint.MinPort
                || defaultport > IPEndPoint.MaxPort))
            {
                throw new ArgumentException(String.Format("Invalid default port '{0}'", defaultport));
            }

            string[] values = endpointstring.Split(new char[] { ':' });
            IPAddress ipaddy;
            int port = -1;

            //check if we have an IPv6 or ports
            if (values.Length <= 2) // ipv4 or hostname
            {
                port = values.Length == 1 ? defaultport : GetPort(values[1]);

                //try to use the address as IPv4, otherwise get hostname
                if (!IPAddress.TryParse(values[0], out ipaddy))
                    ipaddy = GetIPfromHost(values[0]);
            }
            else if (values.Length > 2) //ipv6
            {
                //could [a:b:c]:d
                if (values[0].StartsWith("[") && values[values.Length - 2].EndsWith("]"))
                {
                    string ipaddressstring = String.Join(":", values.Take(values.Length - 1).ToArray());
                    ipaddy = IPAddress.Parse(ipaddressstring);
                    port = GetPort(values[values.Length - 1]);
                }
                else //[a:b:c] or a:b:c
                {
                    ipaddy = IPAddress.Parse(endpointstring);
                    port = defaultport;
                }
            }
            else
            {
                throw new FormatException(String.Format("Invalid endpoint ipaddress '{0}'", endpointstring));
            }

            if (port == -1)
                throw new ArgumentException(String.Format("No port specified: '{0}'", endpointstring));

            return new IPEndPoint(ipaddy, port);
        }

        protected static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Gets the port.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns>System.Int32.</returns>
        /// <exception cref="System.FormatException"></exception>
        private static int GetPort(string p)
        {
            int port;

            if (!Int32.TryParse(p, out port)
             || port < IPEndPoint.MinPort
             || port > IPEndPoint.MaxPort)
            {
                throw new FormatException(String.Format("Invalid end point port '{0}'", p));
            }

            return port;
        }

        /// <summary>
        /// Gets the I pfrom host.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns>IPAddress.</returns>
        /// <exception cref="System.ArgumentException"></exception>
        private static IPAddress GetIPfromHost(string p)
        {
            var hosts = Dns.GetHostAddresses(p);

            if (hosts == null || hosts.Length == 0)
                throw new ArgumentException(String.Format("Host not found: {0}", p));

            return hosts[0];
        }
    }
}
