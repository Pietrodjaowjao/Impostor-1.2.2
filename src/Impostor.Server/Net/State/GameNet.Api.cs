﻿using Impostor.Api.Net.Inner;
using Impostor.Api.Net.Inner.Objects;

namespace Impostor.Server.Net.State
{
    /// <inheritdoc />
    internal partial class GameNet : IGameNet
    {
        IInnerLobbyBehaviour IGameNet.LobbyBehaviour => LobbyBehaviour;

        IInnerGameData IGameNet.GameData => GameData;

        IInnerVoteBanSystem IGameNet.VoteBan => VoteBan;

        IInnerShipStatus IGameNet.ShipStatus => ShipStatus;
    }
}