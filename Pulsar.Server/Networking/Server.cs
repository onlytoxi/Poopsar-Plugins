using Pulsar.Common.Extensions;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Other;
using Pulsar.Server.Forms;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pulsar.Server.Networking
{
    public class Server
    {
        /// <summary>
        /// Occurs when the state of the server changes.
        /// </summary>
        public event ServerStateEventHandler ServerState;

        /// <summary>
        /// Represents a method that will handle a change in the server's state.
        /// </summary>
        /// <param name="s">The server which changed its state.</param>
        /// <param name="listening">The new listening state of the server.</param>
        /// <param name="port">The port the server is listening on, if listening is True.</param>
        public delegate void ServerStateEventHandler(Server s, bool listening, ushort port);

        /// <summary>
        /// Fires an event that informs subscribers that the server has changed it's state.
        /// </summary>
        /// <param name="listening">The new listening state of the server.</param>
        private void OnServerState(bool listening)
        {
            if (Listening == listening) return;

            Listening = listening;

            var handler = ServerState;
            handler?.Invoke(this, listening, Port);
        }

        /// <summary>
        /// Occurs when the state of a client changes.
        /// </summary>
        public event ClientStateEventHandler ClientState;

        /// <summary>
        /// Represents a method that will handle a change in a client's state.
        /// </summary>
        /// <param name="s">The server, the client is connected to.</param>
        /// <param name="c">The client which changed its state.</param>
        /// <param name="connected">The new connection state of the client.</param>
        public delegate void ClientStateEventHandler(Server s, Client c, bool connected);

        /// <summary>
        /// Fires an event that informs subscribers that a client has changed its state.
        /// </summary>
        /// <param name="c">The client which changed its state.</param>
        /// <param name="connected">The new connection state of the client.</param>
        private void OnClientState(Client c, bool connected)
        {
            if (!connected)
                RemoveClient(c);

            var handler = ClientState;
            handler?.Invoke(this, c, connected);
        }

        /// <summary>
        /// Occurs when a message is received by a client.
        /// </summary>
        public event ClientReadEventHandler ClientRead;

        /// <summary>
        /// Represents a method that will handle a message received from a client.
        /// </summary>
        /// <param name="s">The server, the client is connected to.</param>
        /// <param name="c">The client that has received the message.</param>
        /// <param name="message">The message that received by the client.</param>
        public delegate void ClientReadEventHandler(Server s, Client c, IMessage message);

        /// <summary>
        /// Fires an event that informs subscribers that a message has been
        /// received from the client.
        /// </summary>
        /// <param name="c">The client that has received the message.</param>
        /// <param name="message">The message that received by the client.</param>
        /// <param name="messageLength">The length of the message.</param>
        private void OnClientRead(Client c, IMessage message, int messageLength)
        {
            BytesReceived += messageLength;
            var handler = ClientRead;
            handler?.Invoke(this, c, message);
        }

        /// <summary>
        /// Occurs when a message is sent by a client.
        /// </summary>
        public event ClientWriteEventHandler ClientWrite;

        /// <summary>
        /// Represents the method that will handle the sent message by a client.
        /// </summary>
        /// <param name="s">The server, the client is connected to.</param>
        /// <param name="c">The client that has sent the message.</param>
        /// <param name="message">The message that has been sent by the client.</param>
        public delegate void ClientWriteEventHandler(Server s, Client c, IMessage message);

        /// <summary>
        /// Fires an event that informs subscribers that the client has sent a message.
        /// </summary>
        /// <param name="c">The client that has sent the message.</param>
        /// <param name="message">The message that has been sent by the client.</param>
        /// <param name="messageLength">The length of the message.</param>
        private void OnClientWrite(Client c, IMessage message, int messageLength)
        {
            BytesSent += messageLength;
            var handler = ClientWrite;
            handler?.Invoke(this, c, message);
        }

        /// <summary>
        /// The port on which the server is listening.
        /// </summary>
        public ushort Port { get; private set; }

        /// <summary>
        /// The total amount of received bytes.
        /// </summary>
        public long BytesReceived { get; set; }

        /// <summary>
        /// The total amount of sent bytes.
        /// </summary>
        public long BytesSent { get; set; }

        /// <summary>
        /// The buffer size for receiving data in bytes.
        /// </summary>
        private const int BufferSize = 1024 * 16; // 16 KB

        /// <summary>
        /// The keep-alive time in ms.
        /// </summary>
        private const uint KeepAliveTime = 25000; // 25 s

        /// <summary>
        /// The keep-alive interval in ms.
        /// </summary>
        private const uint KeepAliveInterval = 25000; // 25 s

        /// <summary>
        /// The buffer pool to hold the receive-buffers for the clients.
        /// </summary>
        private readonly BufferPool _bufferPool = new BufferPool(BufferSize, 1) { ClearOnReturn = false };

        /// <summary>
        /// The listening state of the server. True if listening, else False.
        /// </summary>
        public bool Listening { get; private set; }

        /// <summary>
        /// Gets the clients currently connected to the server.
        /// </summary>
        protected Client[] Clients
        {
            get
            {
                return _clients.Keys.ToArray();
            }
        }

        /// <summary>
        /// Handle of the Server Socket.
        /// </summary>
        private Socket _handle;

        /// <summary>
        /// The server certificate.
        /// </summary>
        protected readonly X509Certificate2 ServerCertificate;

        /// <summary>
        /// Concurrent dictionary of the clients connected to the server.
        /// </summary>
        private readonly ConcurrentDictionary<Client, byte> _clients = new ConcurrentDictionary<Client, byte>();

        /// <summary>
        /// The UPnP service used to discover, create and delete port mappings.
        /// </summary>
        private UPnPService _UPnPService;

        /// <summary>
        /// Determines if the server is currently processing Disconnect method. 
        /// </summary>
        protected bool ProcessingDisconnect { get; set; }

        /// <summary>
        /// Constructor of the server, initializes serializer types.
        /// </summary>
        /// <param name="serverCertificate">The server certificate.</param>
        protected Server(X509Certificate2 serverCertificate)
        {
            ServerCertificate = serverCertificate;
            TypeRegistry.AddTypesToSerializer(typeof(IMessage), TypeRegistry.GetPacketTypes(typeof(IMessage)).ToArray());
        }

        /// <summary>
        /// Begins listening for clients.
        /// </summary>
        /// <param name="port">Port to listen for clients on.</param>
        /// <param name="ipv6">If set to true, use a dual-stack socket to allow IPv4/6 connections. Otherwise use IPv4-only socket.</param>
        /// <param name="enableUPnP">Enables the automatic UPnP port forwarding.</param>
        public void ListenAsync(ushort port, bool ipv6, bool enableUPnP)
        {
            if (Listening) return;
            this.Port = port;

            if (enableUPnP)
            {
                _UPnPService = new UPnPService();
                _UPnPService.CreatePortMapAsync(port);
            }

            if (Socket.OSSupportsIPv6 && ipv6)
            {
                _handle = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                _handle.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, 0);
                _handle.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
            }
            else
            {
                _handle = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _handle.Bind(new IPEndPoint(IPAddress.Any, port));
            }
            _handle.Listen(1000);

            OnServerState(true);

            _ = AcceptClientsLoopAsync();

            FrmMain mainForm = Application.OpenForms.OfType<FrmMain>().FirstOrDefault();
            if (mainForm != null)
            {
                try
                {
                    mainForm.EventLog("Started listening for connections on port: " + port.ToString(), "info");
                    mainForm.statusStrip.Items["listenToolStripStatusLabel"].Image = Pulsar.Server.Properties.Resources.bullet_green;
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// Synchronous method to begin listening for clients.
        /// </summary>
        /// <param name="port">Port to listen for clients on.</param>
        /// <param name="ipv6">If set to true, use a dual-stack socket to allow IPv4/6 connections. Otherwise use IPv4-only socket.</param>
        /// <param name="enableUPnP">Enables the automatic UPnP port forwarding.</param>
        public void Listen(ushort port, bool ipv6, bool enableUPnP)
        {
            ListenAsync(port, ipv6, enableUPnP);
        }

        /// <summary>
        /// Accepts and begins authenticating incoming clients asynchronously.
        /// </summary>
        private async Task AcceptClientsLoopAsync()
        {
            while (Listening && _handle != null)
            {
                Socket clientSocket = null;
                try
                {
                    clientSocket = await _handle.AcceptAsync();
                    clientSocket.SetKeepAliveEx(KeepAliveInterval, KeepAliveTime);
                    var networkStream = new NetworkStream(clientSocket, true);
                    var sslStream = new SslStream(networkStream, false);
                    try
                    {
                        await sslStream.AuthenticateAsServerAsync(ServerCertificate, false, SslProtocols.Tls12, false);
                        var remoteEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;
                        Client client = new Client(_bufferPool, sslStream, remoteEndPoint);
                        AddClient(client);
                        OnClientState(client, true);
                    }
                    catch (Exception)
                    {
                        sslStream.Close();
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception)
                {
                    clientSocket?.Close();
                    Disconnect();
                    break;
                }
            }
        }

        /// <summary>
        /// Adds a connected client to the list of clients,
        /// subscribes to the client's events.
        /// </summary>
        /// <param name="client">The client to add.</param>
        private void AddClient(Client client)
        {
            client.ClientState += OnClientState;
            client.ClientRead += OnClientRead;
            client.ClientWrite += OnClientWrite;
            _clients.TryAdd(client, 0);
        }

        /// <summary>
        /// Removes a disconnected client from the list of clients,
        /// unsubscribes from the client's events.
        /// </summary>
        /// <param name="client">The client to remove.</param>
        private void RemoveClient(Client client)
        {
            if (ProcessingDisconnect) return;
            client.ClientState -= OnClientState;
            client.ClientRead -= OnClientRead;
            client.ClientWrite -= OnClientWrite;
            _clients.TryRemove(client, out _);
        }

        /// <summary>
        /// Disconnect the server from all of the clients and discontinue
        /// listening (placing the server in an "off" state).
        /// </summary>
        public void Disconnect()
        {
            if (ProcessingDisconnect) return;
            ProcessingDisconnect = true;

            if (_handle != null)
            {
                _handle.Close();
                _handle = null;
            }

            if (_UPnPService != null)
            {
                _UPnPService.DeletePortMapAsync(Port);
                _UPnPService = null;
            }

            foreach (var client in _clients.Keys.ToList())
            {
                try
                {
                    client.Disconnect();
                    client.ClientState -= OnClientState;
                    client.ClientRead -= OnClientRead;
                    client.ClientWrite -= OnClientWrite;
                    _clients.TryRemove(client, out _);
                }
                catch { }
            }

            ProcessingDisconnect = false;
            OnServerState(false);
            FrmMain mainForm = Application.OpenForms.OfType<FrmMain>().FirstOrDefault();
            if (mainForm != null)
            {
                if (mainForm.statusStrip.Items.ContainsKey("listenToolStripStatusLabel"))
                {
                    try
                    {
                        mainForm.statusStrip.Items["listenToolStripStatusLabel"].Image = Pulsar.Server.Properties.Resources.bullet_red;
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                    }
                }
            }
        }
    }
}

