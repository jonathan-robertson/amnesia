﻿using Amnesia.Data;
using Amnesia.Utilities;
using System;

namespace Amnesia.Handlers
{
    internal class GameUpdate
    {
        private static readonly ModLog<GameUpdate> log = new ModLog<GameUpdate>();
        private static readonly uint _ceiling = 100;
        private static uint _counter = 0;
        private static bool isBloodmoon = false;

        internal static void Handle()
        {
            if (!Config.Loaded || !Config.ProtectMemoryDuringBloodmoon) { return; }
            try
            {
                _counter++;
                if (_counter > _ceiling)
                {
                    _counter = 0;
                    HandleBloodMoon();
                }
            }
            catch (Exception e)
            {
                log.Error("Failed to handle GameUpdate event.", e);
            }
        }

        private static void HandleBloodMoon()
        {
            // TODO: this should probably be injected into triggers when bloodmoon starts and ends, honestly
            try
            {
                if (isBloodmoon == GameManager.Instance.World.aiDirector.BloodMoonComponent.BloodMoonActive)
                {
                    return;
                }
                isBloodmoon = !isBloodmoon;

                var players = GameManager.Instance.World.Players.list;
                if (isBloodmoon)
                {
                    for (var i = 0; i < players.Count; i++)
                    {
                        _ = players[i].Buffs.AddBuff(Values.BuffBloodmoonLifeProtection);
                    }
                }
                else
                {
                    for (var i = 0; i < players.Count; i++)
                    {
                        _ = players[i].Buffs.AddBuff(Values.BuffPostBloodmoonLifeProtection);
                        players[i].Buffs.RemoveBuff(Values.BuffBloodmoonLifeProtection);
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("Failed to handle bloodmoon.", e);
            }
        }
    }
}
