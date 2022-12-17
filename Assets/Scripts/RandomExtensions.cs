using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class RandomExtensions
{
    public static T NextChoice<T>(this Random r, params T[] choices) =>
        choices[r.Next(choices.Length)];
    
    public static T NextEnum<T>(this Random r) =>
        r.NextChoice((T[])Enum.GetValues(typeof(T)));

    public static T? NextNullable<T>(this Random r, T value) where T : struct =>
        r.Next(2) == 1 ? value : null;

    public static VirusType? NextVirusType(this Random r) =>
        r.NextNullable(r.NextEnum<VirusType>());

}
