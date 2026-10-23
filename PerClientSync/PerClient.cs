using RoR2;
using UnityEngine.Networking;

namespace PerClientSync
{
    // Sends single clients what Unity's networking sends everyone, with vanilla messages only. Use it on the host.
    public static class PerClient
    {
        // The connection of a player's client, the host's own included
        public static NetworkConnection GetConnection(PlayerCharacterMasterController player)
        {
            return player && player.networkUser ? player.networkUser.connectionToClient : null;
        }

        public static NetworkConnection GetConnection(NetworkUser user)
        {
            return user ? user.connectionToClient : null;
        }

        // The player of a client, null before one is linked to it. With several players on one client (splitscreen), the
        // first one; they all see the same.
        public static PlayerCharacterMasterController GetPlayer(NetworkConnection client)
        {
            if (client == null)
            {
                return null;
            }
            foreach (var player in PlayerCharacterMasterController.instances)
            {
                if (player && player.networkUser && player.networkUser.connectionToClient == client)
                {
                    return player;
                }
            }
            return null;
        }

        // The host's own connection. The host shares the server's objects, so nothing per client reaches it; what only
        // the host's player should see is up to the host's own code, e.g. turning renderers off.
        public static bool IsLocal(NetworkConnection client)
        {
            return client != null && Util.ConnectionIsLocal(client);
        }

        // An update of only these fields of the component, as the dirty bits it uses for them, for this client with its
        // view's values, as Unity's networking writes it. The other components of the object on the same channel are
        // written without changes, and nothing counts as sent. For updates that must reach the client in a particular
        // order; send it on the component's channel.
        public static byte[] BuildUpdate(NetworkConnection client, NetworkBehaviour component, uint dirtyBits)
        {
            return Replication.BuildUpdate(client, component, dirtyBits);
        }

        // Sends BuildUpdate's update to this client right away
        public static bool SendUpdate(NetworkConnection client, NetworkBehaviour component, uint dirtyBits)
        {
            if (client == null || !client.isReady || IsLocal(client))
            {
                return false;
            }
            var update = BuildUpdate(client, component, dirtyBits);
            return client.SendBytes(update, update.Length, component.GetNetworkChannel());
        }
    }
}
