using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public delegate T Generator<T>(Random r);

public static class Generator
{
    public static Generator<int> Int => r => r.Next();

    public static Generator<int> IntRange(int low, int highExclusive) => r => r.Next(low, highExclusive);

    public static Generator<R> Select<T, R>(
        this Generator<T> g,
        Func<T, R> f) => r => f(g(r));

    public static Generator<PerHalf<T>> PerHalf<T>(this Generator<T> g) =>
        r => new PerHalf<T>(g(r), g(r));

    public static Generator<T> OneOf<T>(params T[] choices)
    {
        if (choices.Length == 0)
            throw new ArgumentException($"choices must contain at least one element");

        return IntRange(0, choices.Length).Select(i => choices[i]);
    }

    public static Generator<T> Enum<T>() where T : struct =>
        OneOf((T[])System.Enum.GetValues(typeof(T)));

    public static Generator<VirusType> VirusType => Enum<VirusType>();

    public static Generator<T?> Nullable<T>(this Generator<T> g)
        where T : struct => r => r.Next(2) == 1 ? g(r) : null;
}
