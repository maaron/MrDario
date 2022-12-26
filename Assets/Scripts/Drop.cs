using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;

public struct Drop : INetworkSerializable
{
    public int[] XOffsets;
    public VirusType[] Colors;

    public Drop(int[] xOffsets, VirusType[] colors)
    {
        XOffsets = xOffsets;
        Colors = colors;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref XOffsets);
        serializer.SerializeValue(ref Colors);
    }
}
