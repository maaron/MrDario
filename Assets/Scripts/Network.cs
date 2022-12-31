using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public static class Net
{
    public static Proc<ulong> NextClientConnected => async ct =>
    {
        var nm = NetworkManager.Singleton;

        var tcs = new TaskCompletionSource<ulong>();

        var callback = new Action<ulong>(clientId =>
        {
            Debug.Log($"Client connected, clientId = {clientId}");

            tcs.TrySetResult(clientId);
        });

        nm.OnClientConnectedCallback += callback;

        try
        {
            using (ct.Register(() => tcs.TrySetCanceled()))
            {
                return await tcs.Task;
            }
        }
        finally
        {
            nm.OnClientConnectedCallback -= callback;
        }
    };

    public static Proc<ulong> NextClientDisconnected => async ct =>
    {
        var nm = NetworkManager.Singleton;

        var tcs = new TaskCompletionSource<ulong>();

        var callback = new Action<ulong>(clientId =>
        {
            Debug.Log($"Client connected, clientId = {clientId}");

            tcs.TrySetResult(clientId);
        });

        nm.OnClientDisconnectCallback += callback;

        try
        {
            using (ct.Register(() => tcs.TrySetCanceled()))
            {
                return await tcs.Task;
            }
        }
        finally
        {
            nm.OnClientDisconnectCallback -= callback;
        }
    };

    public static Proc<ValueTuple> Shutdown => async ct =>
    {
        Debug.Log("Shutting down network");

        var nm = NetworkManager.Singleton;

        nm.Shutdown();

        while (nm.ShutdownInProgress)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(100, ct);
        }

        return default;
    };

    public static bool StartServer(ushort port)
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData("0.0.0.0", port, "0.0.0.0");
        return NetworkManager.Singleton.StartServer();
    }

    public static bool StartClient(string hostname, ushort port)
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(hostname, port);
        return NetworkManager.Singleton.StartClient();
    }

    public static T SingleClient<T>()
    {
        var client = NetworkManager.Singleton.ConnectedClients.Values.FirstOrDefault();

        if (client == null)
            throw new Exception("No clients connected");

        if (!client.PlayerObject.TryGetComponent<T>(out var peer))
            throw new Exception($"Peer {typeof(T).Name} not available?");

        return peer;
    }

    public static T LocalPlayer<T>()
    {
        if (!NetworkManager.Singleton.LocalClient.PlayerObject.TryGetComponent<T>(out var peer))
            throw new Exception($"Peer {typeof(T).Name} not available?");

        return peer;
    }
}
