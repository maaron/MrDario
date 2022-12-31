using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Networking.Transport.Error;
using UnityEngine;
using UnityEngine.UI;

public class MrDario : TaskBehaviour
{
    [SerializeField] Game game;
    [SerializeField] Button settingsButton;
    [SerializeField] SettingsDialog settingsDialog;
    [SerializeField] Button multiplayerButton;
    [SerializeField] MultiplayerDialog multiplayerDialog;
    [SerializeField] Sprite singlePlayerImage, multiplayerImage;

    protected override async Task Run(CancellationToken ct)
    {
        game.StartOnePlayer(Settings.Load());

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            await Proc.AnyThen(
                settingsButton.onClick.NextEvent().Select(_ => RunSettings),
                multiplayerButton.onClick.NextEvent().Select(_ => RunMultiplayer))
                (ct);
        }
    }

    Proc<ValueTuple> RunSettings => async ct =>
    {
        game.Pause();

        var newSettings = await settingsDialog.Run(ct);

        if (newSettings.HasValue)
        {
            newSettings.Value.Save();
            
            await game.Stop(ct);

            game.StartOnePlayer(newSettings.Value);
        }

        game.Resume();

        return default;
    };

    Proc<ValueTuple> RunMultiplayer => async ct =>
    {
        game.Pause();

        var mode = await multiplayerDialog.Run(ct);

        if (mode.HasValue)
        {
            multiplayerButton.GetComponent<Image>().sprite = singlePlayerImage;

            try
            {
                if (mode.Value == MultiplayerMode.Client)
                {
                    await Proc.Any(ExitMultiplayer, Net.NextClientDisconnected.Ignore())(ct);
                }
                else
                {
                    var settings = await settingsDialog.Run(ct);

                    var peer = Net.SingleClient<NetworkPlayer>();

                    if (settings.HasValue)
                    {
                        game.StartTwoPlayer(settings.Value, peer);
                        peer.StartGame(settings.Value);

                        await Proc.Any(
                            ExitMultiplayer,
                            ChangeSettings.Forever(),
                            Net.NextClientDisconnected.Ignore())(ct);
                    }
                }
            }
            finally
            {
                multiplayerButton.GetComponent<Image>().sprite = multiplayerImage;
            }
        }
        else
        {
            game.Resume();
        }

        return default;
    };

    Proc<ValueTuple> ExitMultiplayer => async ct =>
    {
        await multiplayerButton.onClick.NextEvent()(ct);

        NetworkManager.Singleton.Shutdown();

        return default;
    };

    Proc<ValueTuple> ChangeSettings => async ct =>
    {
        await settingsButton.onClick.NextEvent()(ct);

        game.Pause();

        var newSettings = await settingsDialog.Run(ct);

        if (newSettings.HasValue)
        {
            newSettings.Value.Save();

            await game.Stop(ct);

            var peer = Net.SingleClient<NetworkPlayer>();

            game.StartTwoPlayer(newSettings.Value, peer);
            peer.StartGame(newSettings.Value);
        }

        game.Resume();

        return default;
    };
}
