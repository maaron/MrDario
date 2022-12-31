using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class MultiplayerSettings
{
    public string Hostname;
    public int Port;

    public MultiplayerSettings(string hostname, int port)
    {
        this.Hostname = hostname;
        this.Port = port;
    }

    public static MultiplayerSettings Load() =>
        new MultiplayerSettings(
            hostname: PlayerPrefs.GetString("Multiplayer/Hostname", null),
            port: PlayerPrefs.GetInt("Multiplayer/Port", 7777));

    public void Save()
    {
        PlayerPrefs.SetString("Multiplayer/Hostname", Hostname);
        PlayerPrefs.SetInt("Multiplayer/Port", Port);
    }
}
