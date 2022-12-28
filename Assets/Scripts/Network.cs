using System.Linq;
using UnityEngine;

namespace Unity.Netcode.Samples
{
    /// <summary>
    /// Class to display helper buttons and status labels on the GUI, as well as buttons to start host/client/server.
    /// Once a connection has been established to the server, the local player can be teleported to random positions via a GUI button.
    /// </summary>
    public class Network : MonoBehaviour
    {
        [SerializeField] Game game;

        private void Awake()
        {
            game.DropGenerated += SendDrop;
        }

        public void SendDrop(Drop drop)
        {
            var networkManager = NetworkManager.Singleton;

            if (networkManager.IsClient && !networkManager.IsServer)
            {
                if (networkManager.LocalClient.PlayerObject.TryGetComponent<NetworkPlayer>(out var peer))
                {
                    peer.AddDropServerRpc(drop);
                }
            }
            else if (networkManager.IsServer)
            {
                var clientPair = networkManager.ConnectedClients.FirstOrDefault();
                if (clientPair.Value != null)
                {
                    if (clientPair.Value.PlayerObject.TryGetComponent<NetworkPlayer>(out var peer))
                    {
                        peer.AddDropClientRpc(drop);
                    }
                }
            }
        }
    }
}
