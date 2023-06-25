namespace Amnesia.Utilities
{
    internal class ClientInfoHelper
    {
        public static PlatformUserIdentifierAbs GetUserIdentifier(ClientInfo clientInfo)
        {
            return clientInfo != null
                ? GameManager.Instance.persistentPlayers.GetPlayerDataFromEntityID(clientInfo.entityId)?.UserIdentifier
                : GameManager.Instance.persistentLocalPlayer.UserIdentifier;
        }

        public static int SafelyGetEntityIdFor(ClientInfo clientInfo)
        {
            return clientInfo != null
                ? clientInfo.entityId
                : GameManager.Instance.persistentLocalPlayer.EntityId;
        }
    }
}
