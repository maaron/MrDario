using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SettingsDialog : MonoBehaviour
{
    [SerializeField] Button acceptButton, cancelButton;
    [SerializeField] Slider fallSpeedSlider, virusDepthSlider;

    public void RunDialog(Settings settings, Action<Settings?> onComplete)
    {
        fallSpeedSlider.value = settings.DropSpeedMultiplier;
        virusDepthSlider.value = settings.VirusDepth;

        UnityAction onAccept = null;
        UnityAction onCancel = null;
        
        onAccept = new UnityAction(() =>
        {
            acceptButton.onClick.RemoveListener(onAccept);
            cancelButton.onClick.RemoveListener(onCancel);
            gameObject.SetActive(false);

            onComplete(new Settings
            {
                DropSpeedMultiplier = fallSpeedSlider.value,
                VirusDepth = (int)virusDepthSlider.value
            });
        });

        onCancel = new UnityAction(() =>
        {
            acceptButton.onClick.RemoveListener(onAccept);
            cancelButton.onClick.RemoveListener(onCancel);
            gameObject.SetActive(false);

            onComplete(null);
        });

        acceptButton.onClick.AddListener(onAccept);
        cancelButton.onClick.AddListener(onCancel);

        gameObject.SetActive(true);
    }
}
