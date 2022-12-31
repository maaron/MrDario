using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public struct Settings : INetworkSerializable
{
    public float DropSpeedMultiplier;
    public int VirusDepth;
    public int Seed;

    public Settings(float dropSpeedMultiplier, int virusDepth, int seed)
    {
        DropSpeedMultiplier = dropSpeedMultiplier;
        VirusDepth = virusDepth;
        Seed = seed;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref DropSpeedMultiplier);
        serializer.SerializeValue(ref VirusDepth);
        serializer.SerializeValue(ref Seed);
    }

    public static Settings Load()
    {
        return new Settings(
            dropSpeedMultiplier: PlayerPrefs.GetFloat("DropSpeedMultipler", 1.0f),
            virusDepth: PlayerPrefs.GetInt("VirusDepth", 5),
            seed: PlayerPrefs.GetInt("Seed", 0));
    }

    public void Save()
    {
        PlayerPrefs.SetFloat("DropSpeedMultiplier", DropSpeedMultiplier);
        PlayerPrefs.SetInt("VirusDepth", VirusDepth);
        PlayerPrefs.SetInt("Seed", Seed);
    }
}
