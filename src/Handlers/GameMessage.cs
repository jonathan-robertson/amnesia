﻿using Amnesia.Data;
using Amnesia.Utilities;
using System;

namespace Amnesia.Handlers
{
    internal class GameMessage
    {
        private static readonly ModLog<GameMessage> _log = new ModLog<GameMessage>();

        public static bool Handle(ClientInfo clientInfo, EnumGameMessages messageType, string message, string mainName, bool localizeMain, string secondaryName, bool localizeSecondary)
        {
            if (!Config.Loaded) { return true; } // do not interrupt other mods from processing event
            try
            {
                if (EnumGameMessages.EntityWasKilled != messageType)
                {
                    return true; // only focus on entity killed messages
                }

                if (!GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out var player))
                {
                    return true; // player not present; skip
                }

                var clientIdentifier = $"{clientInfo.InternalId.CombinedString} ({player.GetDebugName()})";
                if (player.Buffs.HasBuff(Values.BuffBloodmoonLifeProtection) || player.Buffs.HasBuff(Values.BuffPostBloodmoonLifeProtection))
                {
                    _log.Trace($"{clientIdentifier} died but had bloodmoon memory protection.");
                    return true; // player had protection
                }
                else
                {
                    _log.Trace($"{clientIdentifier} died and did not have bloodmoon memory protection.");
                }

                if (Config.ProtectMemoryDuringPvp && mainName != secondaryName)
                {
                    var killerClient = ConnectionManager.Instance.Clients.GetForPlayerName(secondaryName);
                    if (killerClient != null)
                    {
                        _log.Trace($"{clientIdentifier} was killed by {killerClient.InternalId.CombinedString} ({secondaryName}) but this server has pvp deaths set to not harm memory.");
                        return true; // being killed in pvp doesn't count against player
                    }
                }

                if (player.Progression.Level < Config.LongTermMemoryLevel)
                {
                    _log.Trace($"{clientIdentifier} died but had not yet reached the configured LongTermMemoryLevel of {Config.LongTermMemoryLevel}");
                    return true;
                }

                if (!ModApi.Obituary.ContainsKey(clientInfo.entityId))
                {
                    ModApi.Obituary.Add(clientInfo.entityId, true);
                }
            }
            catch (Exception e)
            {
                _log.Error("Failed to handle GameMessage event.", e);
            }
            return true; // do not interrupt other mods from processing event
        }
    }
}
