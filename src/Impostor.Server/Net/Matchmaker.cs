﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Impostor.Hazel;
using Impostor.Hazel.Udp;
using Impostor.Server.Net.Hazel;
using Impostor.Server.Net.Manager;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

namespace Impostor.Server.Net
{
    internal class Matchmaker
    {
        private readonly ClientManager _clientManager;
        private readonly ObjectPool<MessageReader> _readerPool;
        private readonly ILogger<Matchmaker> _logger;
        private readonly ILogger<HazelConnection> _connectionLogger;
        private UdpConnectionListener _connection;

        public Matchmaker(
            ILogger<Matchmaker> logger,
            ClientManager clientManager,
            ObjectPool<MessageReader> readerPool,
            ILogger<HazelConnection> connectionLogger)
        {
            _logger = logger;
            _clientManager = clientManager;
            _readerPool = readerPool;
            _connectionLogger = connectionLogger;
        }

        public async ValueTask StartAsync(IPEndPoint ipEndPoint)
        {
            var mode = ipEndPoint.AddressFamily switch
            {
                AddressFamily.InterNetwork => IPMode.IPv4,
                AddressFamily.InterNetworkV6 => IPMode.IPv6,
                _ => throw new InvalidOperationException()
            };

            _connection = new UdpConnectionListener(ipEndPoint, _readerPool, mode);
            _connection.NewConnection = OnNewConnection;

            await _connection.StartAsync();
        }

        public async ValueTask StopAsync()
        {
            await _connection.DisposeAsync();
        }

        private async ValueTask OnNewConnection(NewConnectionEventArgs e)
        {
            // Handshake.
            var clientVersion = e.HandshakeData.ReadInt32();
            var name = e.HandshakeData.ReadString();

            var connection = new HazelConnection(e.Connection, _connectionLogger);

            // Register client
            await _clientManager.RegisterConnectionAsync(connection, name, clientVersion);
        }
    }
}
