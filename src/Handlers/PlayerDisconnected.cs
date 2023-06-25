using Amnesia.Data;
using Amnesia.Utilities;
using System;

namespace Amnesia.Handlers
{
    internal class PlayerDisconnected
    {
        private static readonly ModLog<PlayerDisconnected> _log = new ModLog<PlayerDisconnected>();

        public static void Handle(ClientInfo clientInfo, bool forShutdown)
        {
            try
            {
                if (!forShutdown)
                {
                    PlayerRecord.Unload(clientInfo);
                    DialogShop.ClearMoneyReferences(clientInfo);
                }
            }
            catch (Exception e)
            {
                _log.Error($"Failed to handle PlayerDisconnected event.", e);
            }
        }
    }
}
