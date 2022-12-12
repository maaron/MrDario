using System;

public enum Halves { Left, Right}

[Serializable]
public struct PerHalf<T>
{
    public T Left, Right;

    public PerHalf(T left, T right)
    {
        Left = left;
        Right = right;
    }
}