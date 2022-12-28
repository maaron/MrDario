using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;

public struct Settings : INetworkSerializable
{
    public float DropSpeedMultiplier;
    public int VirusDepth;

    public Settings(float dropSpeedMultiplier, int virusDepth)
    {
        DropSpeedMultiplier = dropSpeedMultiplier;
        VirusDepth = virusDepth;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref DropSpeedMultiplier);
        serializer.SerializeValue(ref VirusDepth);
    }
}
