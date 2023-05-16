﻿using System.Net;
using System.Threading.Tasks;
using Impostor.Api.Net.Messages;

namespace Impostor.Api.Net
{
    /// <summary>
    ///     Represents the connection of the client.
    /// </summary>
    public interface IHazelConnection
    {
        /// <summary>
        ///     Gets the IP endpoint of the client.
        /// </summary>
        IPEndPoint EndPoint { get; }

        /// <summary>
        ///     Gets a value indicating whether the client is connected to the server.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        ///     Gets the client of the connection.
        /// </summary>
        IClient? Client { get; set; }

        /// <summary>
        ///     Sends a message writer to the connection.
        /// </summary>
        /// <param name="writer">The message.</param>
        /// <returns></returns>
        ValueTask SendAsync(IMessageWriter writer);

        /// <summary>
        ///     Disconnects the client and invokes the disconnect handler.
        /// </summary>
        /// <param name="reason">A reason.</param>
        /// <returns></returns>
        ValueTask DisconnectAsync(string? reason);
    }
}