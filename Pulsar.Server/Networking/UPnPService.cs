using Open.Nat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Pulsar.Server.Networking
{
    public class UPnPService
    {
        /// <summary>
        /// Used to keep track of all created mappings.
        /// </summary>
        private readonly Dictionary<int, Mapping> _mappings = new Dictionary<int, Mapping>();

        /// <summary>
        /// The discovered UPnP device.
        /// </summary>
        private NatDevice _device;

        /// <summary>
        /// The NAT discoverer used to discover NAT-UPnP devices.
        /// </summary>
        private NatDiscoverer _discoverer;

        /// <summary>
        /// Initializes the discovery of new UPnP devices.
        /// </summary>
        public UPnPService()
        {
            _discoverer = new NatDiscoverer();
        }

        /// <summary>
        /// Creates a new port mapping on the UPnP device.
        /// </summary>
        /// <param name="port">The port to map.</param>
        public async Task CreatePortMapAsync(int port)
        {
            try
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                _device = await _discoverer.DiscoverDeviceAsync(PortMapper.Upnp, timeoutCts).ConfigureAwait(false);

                var mapping = new Mapping(Protocol.Tcp, port, port);

                await _device.CreatePortMapAsync(mapping).ConfigureAwait(false);

                _mappings[mapping.PrivatePort] = mapping;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"UPnP discovery timed out while creating port map for {port}.");
            }
            catch (Exception ex) when (ex is MappingException || ex is NatDeviceNotFoundException)
            {
                Debug.WriteLine($"UPnP mapping failed for port {port}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected UPnP error when creating port map for {port}: {ex}");
            }
        }

        /// <summary>
        /// Deletes an existing port mapping.
        /// </summary>
        /// <param name="port">The port mapping to delete.</param>
        public async Task DeletePortMapAsync(int port)
        {
            if (_mappings.TryGetValue(port, out var mapping))
            {
                try
                {
                    if (_device == null)
                        return;

                    await _device.DeletePortMapAsync(mapping).ConfigureAwait(false);
                    _mappings.Remove(mapping.PrivatePort);
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine($"UPnP delete timed out for port {port}.");
                }
                catch (Exception ex) when (ex is MappingException || ex is NatDeviceNotFoundException)
                {
                    Debug.WriteLine($"UPnP delete failed for port {port}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Unexpected UPnP error when deleting port map for {port}: {ex}");
                }
            }
        }
    }
}
