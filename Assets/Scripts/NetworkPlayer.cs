using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    void AddDrop(Drop drop)
    {
        var game = GameObject.FindObjectOfType<Game>();
        if (game != null)
        {
            Debug.Log($"Adding drop: {string.Join(", ", drop.XOffsets.Zip(drop.Colors, (o, c) => $"({o}, {c})"))}");
            game.AddDrop(drop);
        }
    }

    [ServerRpc]
    public void AddDropServerRpc(Drop drop) => AddDrop(drop);

    [ClientRpc]
    public void AddDropClientRpc(Drop drop, ClientRpcParams clientRpcParams = new()) => AddDrop(drop);

    [ClientRpc]
    public void StartGameClientRpc(Settings settings, ClientRpcParams clientRpcParams = new())
    {
        var game = GameObject.FindObjectOfType<Game>();
        if (game != null)
        {
            Debug.Log($"Starting game with speed = {settings.DropSpeedMultiplier}, depth = {settings.VirusDepth}");
            game.StartGame(settings);
        }
    }
}
