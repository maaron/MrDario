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

public class MultiplayerDialog : Dialog<MultiplayerMode?>
{
    [SerializeField] TMPro.TextMeshProUGUI serverStatusText;
    [SerializeField] TMPro.TextMeshProUGUI serverAddressText;
    [SerializeField] TMPro.TextMeshProUGUI clientStatusText;
    [SerializeField] TMPro.TMP_InputField hostnameInput, portInput;
    [SerializeField] Button cancelButton, acceptButton;

    bool hostnameValid = false;
    bool portValid = false;

    private void Awake()
    {
        hostnameInput.onValueChanged.AddListener(value =>
        {
            hostnameValid = !string.IsNullOrEmpty(value);
            acceptButton.interactable = hostnameValid && portValid;
        });

        portInput.onValueChanged.AddListener(value =>
        {
            portValid = ushort.TryParse(value, out var port);
            acceptButton.interactable = hostnameValid && portValid;
        });
    }

    protected override async Task<MultiplayerMode?> RunInternal(CancellationToken ct)
    {
        var settings = MultiplayerSettings.Load();
        hostnameInput.text = settings.Hostname;
        portInput.text = settings.Port.ToString();

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            await Net.Shutdown(ct);

            StartServer();
            
            var which = await Proc.Any(
                acceptButton.onClick.NextEvent().Select(_ => 0),
                cancelButton.onClick.NextEvent().Select(_ => 1),
                Net.NextClientConnected.Select(clientId => 2))(ct);

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
                settings.Hostname = hostnameInput.text;
                settings.Port = int.Parse(portInput.text);
                settings.Save();

                await Net.Shutdown(ct);

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
#if false
        var localAddress = System.Net.NetworkInformation.NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(iface => 
                iface.OperationalStatus == OperationalStatus.Up && 
                iface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(iface => iface.GetIPProperties().UnicastAddresses)
            .Where(addr => addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .Select(addr => $"{addr.Address}:{7777}");
#else
        var localAddress = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList.Where(ip =>
            ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .ToArray()
            .Select(addr => $"{addr}:{7777}");
#endif

        serverAddressText.text = string.Join("\n", localAddress);

        Debug.Log($"Starting server on port 7777");
        
        Net.StartServer(7777);
    }

    Proc<bool> ConnectToServer() => async ct =>
    {
        if (!ushort.TryParse(portInput.text, out var port))
        {
            Debug.LogError($"Invalid port: {port}");
            return false;
        }

        var hostname = hostnameInput.text;

        await Net.Shutdown(ct);

        Debug.Log($"Connecting to {hostname}:{port}");

        if (!Net.StartClient(hostname, port))
        {
            Debug.Log($"Connection failed synchronously");
            return false;
        }

        var result = await Proc.Any(
            Net.NextClientConnected.Select(clientId => true),
            Proc.Delay(2).Select(_ => false))(ct);

        if (!result)
            Debug.Log("Connection timed out");

        return result;
    };
}
