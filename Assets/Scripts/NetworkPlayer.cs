using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    public void StartGame(Settings settings)
    {
        if (IsServer) StartGameClientRpc(settings);
        else throw new Exception("Client cannot start a server game");
    }

    [ClientRpc]
    void StartGameClientRpc(Settings settings, ClientRpcParams clientRpcParams = new())
    {
        var game = GameObject.FindObjectOfType<Game>();
        if (game != null)
        {
            Debug.Log($"Starting game with speed = {settings.DropSpeedMultiplier}, depth = {settings.VirusDepth}");
            game.StartTwoPlayer(settings, Net.LocalPlayer<NetworkPlayer>());
        }
    }

    public void AddDrop(Drop drop)
    {
        if (IsServer) AddDropClientRpc(drop);
        else AddDropServerRpc(drop);
    }

    [ServerRpc]
    void AddDropServerRpc(Drop drop) => AddDropLocal(drop);

    [ClientRpc]
    void AddDropClientRpc(Drop drop, ClientRpcParams clientRpcParams = new()) => AddDropLocal(drop);

    void AddDropLocal(Drop drop)
    {
        var game = GameObject.FindObjectOfType<Game>();
        if (game != null)
        {
            Debug.Log($"Adding drop: {string.Join(", ", drop.XOffsets.Zip(drop.Colors, (o, c) => $"({o}, {c})"))}");
            game.AddDrop(drop);
        }
    }

    public void End(bool weWon)
    {
        if (!IsServer) EndServerRpc(weWon);
        else EndClientRpc(weWon);
    }

    [ServerRpc]
    void EndServerRpc(bool theyWon) => EndLocal(theyWon);

    [ClientRpc]
    void EndClientRpc(bool theyWon) => EndLocal(theyWon);

    void EndLocal(bool theyWon)
    {
        var game = GameObject.FindObjectOfType<Game>();
        if (game != null)
        {
            Debug.Log($"Received end: they won is {theyWon}");
            game.End(theyWon);
        }
    }

    public void Pause()
    {
        if (IsClient) PauseServerRpc();
        else PauseClientRpc();
    }

    [ServerRpc]
    void PauseServerRpc() => PauseLocal();

    [ClientRpc]
    void PauseClientRpc() => PauseLocal();

    void PauseLocal()
    {
        var game = GameObject.FindObjectOfType<Game>();
        if (game != null)
        {
            Debug.Log($"Received Pause");
            game.PeerPaused();
        }
    }

    public void Resume()
    {
        if (IsClient) ResumeServerRpc();
        else ResumeClientRpc();
    }

    [ServerRpc]
    void ResumeServerRpc() => ResumeLocal();

    [ClientRpc]
    void ResumeClientRpc() => ResumeLocal();

    void ResumeLocal()
    {
        var game = GameObject.FindObjectOfType<Game>();
        if (game != null)
        {
            Debug.Log($"Received Resume");
            game.PeerResumed();
        }
    }
}
