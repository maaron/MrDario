using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MultiplayerDialog : MonoBehaviour
{
    [SerializeField] TMPro.TextMeshProUGUI serverStatusText;
    [SerializeField] TMPro.TextMeshProUGUI serverAddressText;
    [SerializeField] TMPro.TextMeshProUGUI clientStatusText;
    [SerializeField] TMPro.TMP_InputField hostnameInput, portInput;
    [SerializeField] Button cancelButton, acceptButton;

    NetworkManager nm;
    UnityTransport tp;

    public async void RunDialog(Action<MultiplayerMode?> onComplete)
    {
        nm = NetworkManager.Singleton;
        tp = nm.GetComponent<UnityTransport>();

        gameObject.SetActive(true);

        try
        {
            var result = await Run(CancellationToken.None);
            onComplete(result);
        }
        finally
        {
            gameObject.SetActive(false);
        }
    }

    public async Task<MultiplayerMode?> Run(CancellationToken ct)
    {
        var nm = NetworkManager.Singleton;
        var tp = nm.GetComponent<UnityTransport>();

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            await Shutdown()(ct);

            StartServer();
            
            var which = await Proc.Any(
                acceptButton.onClick.NextEvent().Select(_ => 0),
                cancelButton.onClick.NextEvent().Select(_ => 1),
                NextClientConnected().Select(clientId => 2))(ct);

            if (which == 1)
            {
                Debug.Log("Multiplayer dialog canceled");
                return null;
            }
            else if (which == 2)
            {
                // We are the server
                return MultiplayerMode.Server;
            }
            else // which == 0
            {
                await Shutdown()(ct);

                var connected = await ConnectToServer()(ct);

                if (connected)
                    // We are the client
                    return MultiplayerMode.Client;
            }

            await this.NextUpdate(ct);
        }
    }

    void StartServer()
    {
        var localAddress = System.Net.NetworkInformation.NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(iface => 
                iface.OperationalStatus == OperationalStatus.Up && 
                iface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(iface => iface.GetIPProperties().UnicastAddresses)
            .Where(addr => addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .Select(addr => $"{addr.Address}:{7777}");

        serverAddressText.text = string.Join("\n", localAddress);

        tp.SetConnectionData(hostnameInput.text, 7777, "0.0.0.0");
        Debug.Log($"Starting server on {tp.ConnectionData.ServerListenAddress ?? "0.0.0.0"}:{tp.ConnectionData.Port}");
        nm.StartServer();
    }

    Proc<bool> ConnectToServer() => async ct =>
    {
        if (!ushort.TryParse(portInput.text, out var port))
        {
            Debug.LogError($"Invalid port: {port}");
            return false;
        }

        var hostname = hostnameInput.text;

        await Shutdown()(ct);

        tp.SetConnectionData(hostname, port);

        Debug.Log($"Connecting to {hostname}:{port}");

        if (!nm.StartClient())
        {
            Debug.Log($"Connection failed synchronously");
            return false;
        }

        var result = await Proc.Any(
            NextClientConnected().Select(clientId => true),
            Proc.Delay(2).Select(_ => false))(ct);

        if (!result)
            Debug.Log("Connection timed out");

        return result;
    };

    Proc<ValueTuple> Shutdown() => async ct =>
    {
        Debug.Log("Shutting down network");
        nm.Shutdown();

        while (nm.ShutdownInProgress)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(100, ct);
        }

        return default;
    };

    Proc<ulong> NextClientConnected() => async ct =>
    {
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
}
