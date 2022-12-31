using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SettingsDialog : Dialog<Settings?>
{
    [SerializeField] Button acceptButton, cancelButton;
    [SerializeField] Slider fallSpeedSlider, virusDepthSlider;
    [SerializeField] TMPro.TMP_InputField seedInput;

    private void Awake()
    {
        var acceptButtonBackground = acceptButton.GetComponent<Image>();

        seedInput.onValueChanged.AddListener(value =>
        {
            if (int.TryParse(value, out var val))
            {
                acceptButton.interactable = true;
                acceptButtonBackground.color = Color.white;
            }
            else
            {
                acceptButton.interactable = false;
                acceptButtonBackground.color = new Color(250, 131, 131);
            }
        });
    }

    protected override Task<Settings?> RunInternal(CancellationToken ct)
    {
        var currentSettings = Settings.Load();

        fallSpeedSlider.value = currentSettings.DropSpeedMultiplier;
        virusDepthSlider.value = currentSettings.VirusDepth;
        seedInput.text = currentSettings.Seed.ToString();

        return Proc.Any(
            acceptButton.onClick.NextEvent().Select(_ => (Settings?)new Settings(
                dropSpeedMultiplier: fallSpeedSlider.value,
                virusDepth: (int)virusDepthSlider.value,
                seed: int.Parse(seedInput.text))),
            cancelButton.onClick.NextEvent().Select(_ => (Settings?)null))
            (ct);
    }
}