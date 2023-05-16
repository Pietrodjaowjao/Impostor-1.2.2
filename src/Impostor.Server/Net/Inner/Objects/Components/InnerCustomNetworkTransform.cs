﻿using System.Numerics;
using System.Threading.Tasks;
using Impostor.Api;
using Impostor.Api.Innersloth;
using Impostor.Api.Net;
using Impostor.Api.Net.Messages;
using Impostor.Server.Net.State;
using Microsoft.Extensions.Logging;

namespace Impostor.Server.Net.Inner.Objects.Components
{
    internal partial class InnerCustomNetworkTransform : InnerNetObject
    {
        private static readonly FloatRange XRange = new FloatRange(-40f, 40f);
        private static readonly FloatRange YRange = new FloatRange(-40f, 40f);

        private readonly ILogger<InnerCustomNetworkTransform> _logger;
        private readonly InnerPlayerControl _playerControl;
        private readonly Game _game;

        private ushort _lastSequenceId;
        private Vector2 _targetSyncPosition;
        private Vector2 _targetSyncVelocity;

        public InnerCustomNetworkTransform(ILogger<InnerCustomNetworkTransform> logger, InnerPlayerControl playerControl, Game game)
        {
            _logger = logger;
            _playerControl = playerControl;
            _game = game;
        }

        private static bool SidGreaterThan(ushort newSid, ushort prevSid)
        {
            var num = (ushort)(prevSid + (uint) short.MaxValue);

            return (int) prevSid < (int) num
                ? newSid > prevSid && newSid <= num
                : newSid > prevSid || newSid <= num;
        }

        private static void WriteVector2(IMessageWriter writer, Vector2 vec)
        {
            writer.Write((ushort)(XRange.ReverseLerp(vec.X) * (double) ushort.MaxValue));
            writer.Write((ushort)(YRange.ReverseLerp(vec.Y) * (double) ushort.MaxValue));
        }

        private static Vector2 ReadVector2(IMessageReader reader)
        {
            var v1 = reader.ReadUInt16() / (float) ushort.MaxValue;
            var v2 = reader.ReadUInt16() / (float) ushort.MaxValue;

            return new Vector2(XRange.Lerp(v1), YRange.Lerp(v2));
        }

        public override ValueTask HandleRpc(ClientPlayer sender, ClientPlayer? target, RpcCalls call, IMessageReader reader)
        {
            if (call == RpcCalls.SnapTo)
            {
                if (!sender.IsOwner(this))
                {
                    throw new ImpostorCheatException($"Client sent {nameof(RpcCalls.SnapTo)} to an unowned {nameof(InnerPlayerControl)}");
                }

                if (target != null)
                {
                    throw new ImpostorCheatException($"Client sent {nameof(RpcCalls.SnapTo)} to a specific player instead of broadcast");
                }

                if (!sender.Character.PlayerInfo.IsImpostor)
                {
                    throw new ImpostorCheatException($"Client sent {nameof(RpcCalls.SnapTo)} as crewmate");
                }

                SnapTo(ReadVector2(reader), reader.ReadUInt16());
            }
            else
            {
                _logger.LogWarning("{0}: Unknown rpc call {1}", nameof(InnerCustomNetworkTransform), call);
            }

            return default;
        }

        public override bool Serialize(IMessageWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.Write(_lastSequenceId);
                WriteVector2(writer, _targetSyncPosition);
                WriteVector2(writer, _targetSyncVelocity);
                return true;
            }

            // TODO: DirtyBits == 0 return false.
            _lastSequenceId++;

            writer.Write(_lastSequenceId);
            WriteVector2(writer, _targetSyncPosition);
            WriteVector2(writer, _targetSyncVelocity);
            return true;
        }

        public override void Deserialize(IClientPlayer sender, IClientPlayer? target, IMessageReader reader, bool initialState)
        {
            var sequenceId = reader.ReadUInt16();

            if (initialState)
            {
                _lastSequenceId = sequenceId;
                _targetSyncPosition = ReadVector2(reader);
                _targetSyncVelocity = ReadVector2(reader);
            }
            else
            {
                if (!sender.IsOwner(this))
                {
                    throw new ImpostorCheatException($"Client attempted to send unowned {nameof(InnerCustomNetworkTransform)} data");
                }

                if (target != null)
                {
                    throw new ImpostorCheatException($"Client attempted to send {nameof(InnerCustomNetworkTransform)} data to a specific player, must be broadcast");
                }

                if (!SidGreaterThan(sequenceId, _lastSequenceId))
                {
                    return;
                }

                _lastSequenceId = sequenceId;
                _targetSyncPosition = ReadVector2(reader);
                _targetSyncVelocity = ReadVector2(reader);
            }
        }

        private void SnapTo(Vector2 position, ushort minSid)
        {
            if (!SidGreaterThan(minSid, _lastSequenceId))
            {
                return;
            }

            _lastSequenceId = minSid;
            _targetSyncPosition = position;
            _targetSyncVelocity = Vector2.Zero;
        }
    }
}
