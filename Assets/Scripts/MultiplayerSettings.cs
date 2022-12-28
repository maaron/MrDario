using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class MultiplayerSettings
{
    public static string Hostname
    {
        get => PlayerPrefs.GetString("Multiplayer/Hostname", null);
        set => PlayerPrefs.SetString("Multiplayer/Port", value);
    }

    public static int Port
    {
        get => PlayerPrefs.GetInt("Multiplayer/Port", 7777);
        set => PlayerPrefs.SetInt("Multiplayer/Port", value);
    }
}
