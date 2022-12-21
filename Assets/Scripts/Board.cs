using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board<T>
{
    T[,] array;

    public Board(int width, int height)
    {
        array = new T[width, height];
    }

    public int Width => array.GetLength(0);

    public int Height => array.GetLength(1);

    public void Fill(T value)
    {
        for (int i = 0; i < array.GetLength(0); i++)
            for (int j = 0; j < array.GetLength(1); j++)
                array[i, j] = value;
    }

    public void Generate(Func<T> value)
    {
        for (int i = 0; i < array.GetLength(0); i++)
            for (int j = 0; j < array.GetLength(1); j++)
                array[i, j] = value();
    }

    public T this[int x, int y]
    {
        get => array[x, y];
        set => array[x, y] = value;
    }

    public T this[Vector2Int location]
    {
        get => this[location.x, location.y];
        set => this[location.x, location.y] = value;
    }

    public T[] QueryRect(RectInt rect) => EnumerateRegion(rect).ToArray();

    public void FillRect(RectInt rect, T value)
    {
        for (int j = rect.min.y; j < rect.min.y + rect.height; j++)
            for (int i = rect.min.x; i < rect.min.x + rect.width; i++)
                array[i, j] = value;
    }

    public void GenerateRowMajor(Func<int, int, T> generator)
    {
        for (int i = 0; i < array.GetLength(0); i++)
            for (int j = 0; j < array.GetLength(1); j++)
                array[i, j] = generator(i, j);
    }

    public IEnumerable<T> ToEnumerable()
    {
        for (int i = 0; i < array.GetLength(0); i++)
            for (int j = 0; j < array.GetLength(1); j++)
                yield return array[i, j];
    }

    public IEnumerable<T> EnumerateRegion(RectInt region)
    {
        for (int j = region.min.y; j < region.min.y + region.height; j++)
            for (int i = region.min.x; i < region.min.x + region.width; i++)
                yield return array[i, j];
    }

    public RectInt Region() => new RectInt(new Vector2Int(0, 0), new Vector2Int(Width, Height));
}
