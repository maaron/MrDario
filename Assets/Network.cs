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

        private void OnGUI()
        {
            GUI.skin.button.fontSize = 40;
            GUILayout.BeginArea(new Rect(10, 10, 600, 300));

            var networkManager = NetworkManager.Singleton;
            if (!networkManager.IsClient && !networkManager.IsServer)
            {
                if (GUILayout.Button("Host"))
                {
                    networkManager.StartHost();
                }

                if (GUILayout.Button("Client"))
                {
                    networkManager.StartClient();
                }

                if (GUILayout.Button("Server"))
                {
                    networkManager.StartServer();
                }
            }
            else
            {
                GUILayout.Label($"Mode: {(networkManager.IsHost ? "Host" : networkManager.IsServer ? "Server" : "Client")}");

                if (GUILayout.Button("Send Drop"))
                {
                    SendDrop(new Drop(new[] { 1, 3 }, new[] { VirusType.Blue, VirusType.Yellow }));
                }
            }

            GUILayout.EndArea();
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
