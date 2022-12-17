using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditorInternal;
using UnityEngine;

public class Board<T>
{
    T[,] array = new T[16, 8];

    public Board()
    {

    }

    public int Width => array.GetLength(1);

    public int Height => array.GetLength(0);

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

    public T this[int row, int column]
    {
        get => array[row, column];
        set => array[row, column] = value;
    }

    public void GenerateRowMajor(Func<int, int, T> generator)
    {
        for (int i = 0; i < array.GetLength(0); i++)
            for (int j = 0; j < array.GetLength(1); j++)
                array[i, j] = generator(i, j);
    }

    public IEnumerable<T> Neighbors(int row, int column)
    {
        if (column + 1 < Width) yield return this[row, column + 1];
        if (row > 0) yield return this[row - 1, column];
        if (column > 0) yield return this[row, column - 1];
        if (row + 1 < Height) yield return this[row + 1, column];
    }

    public IEnumerable<T> ToEnumerable()
    {
        for (int i = 0; i < array.GetLength(0); i++)
            for (int j = 0; j < array.GetLength(1); j++)
                yield return array[i, j];
    }
}

public class Game : MonoBehaviour
{
    Board<Virus> viruses = new();
    Board<VirusType?> brokenPills = new();
    Board<VirusType?> halfPills = new();
    System.Random r = new System.Random(0);

    [SerializeField] Virus virusPrefab;
    [SerializeField] GameObject virusParent;

    // Start is called before the first frame update
    void Start()
    {
        Generate();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Generate()
    {
        int count = 0;

        foreach (var v in viruses.ToEnumerable()) GameObject.Destroy(v);
        viruses.Fill(null);

        viruses.GenerateRowMajor((i, j) =>
        {
            var choices = (VirusType[])Enum.GetValues(typeof(VirusType));

            var type = r.NextNullable(
                r.NextChoice(
                    choices.Except(
                        viruses.Neighbors(i, j)
                        .Where(v => v != null)
                        .Select(v => v.VirusType))
                    .ToArray()));

            Virus virus = null;
            if (type.HasValue)
            {
                count++;

                virus = GameObject.Instantiate<Virus>(virusPrefab, BoardPosition(i, j), Quaternion.identity, virusParent.transform);
                virus.VirusType = type.Value;
            }

            return virus;
        });
    }

    Vector3 BoardPosition(int row, int column)
    {
        return new Vector3(
            column - viruses.Width / 2 + 0.5f,
            row - viruses.Height / 2 + 0.5f,
            0);
    }
}
