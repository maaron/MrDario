using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum VirusType { Red, Blue, Yellow }

[Serializable]
public struct PerVirusType<T>
{
    public T Red, Blue, Yellow;

    public PerVirusType(T red, T blue, T yellow)
    {
        Red = red;
        Blue = blue;
        Yellow = yellow;
    }

    public T this[VirusType t]
    {
        get 
        {
            switch (t)
            {
                case VirusType.Red: return Red;
                case VirusType.Blue: return Blue;
                case VirusType.Yellow: return Yellow;
                default: throw new Exception($"Unknown virus type {t}");
            }
        }
        set
        {
            switch (t)
            {
                case VirusType.Red: Red = value; break;
                case VirusType.Blue: Blue = value; break;
                case VirusType.Yellow: Yellow = value; break;
                default: throw new Exception($"Unknown virus type {t}");
            }
        }
    }

    public IEnumerable<T> ToEnumerable()
    {
        yield return Red;
        yield return Blue;
        yield return Yellow;
    }

    public void Visit(Action<VirusType, T> visitor)
    {
        visitor(VirusType.Red, Red);
        visitor(VirusType.Blue, Blue);
        visitor(VirusType.Yellow, Yellow);
    }
}
