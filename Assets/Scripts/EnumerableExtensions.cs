using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class EnumerableExtensions
{
    public static IEnumerable<T> NonNull<T>(this IEnumerable<T?> source) where T : struct =>
        source.Where(t => t.HasValue).Select(t => t.Value);
}
