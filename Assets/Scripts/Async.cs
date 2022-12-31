using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Events;

public delegate Task<T> Proc<T>(CancellationToken ct);

public static class Proc
{
    public static Proc<T> Never<T>() => async ct =>
    {
        var tcs = new TaskCompletionSource<T>();

        using (ct.Register(() => tcs.TrySetCanceled()))
        {
            return await tcs.Task;
        }
    };

    public static Proc<ValueTuple> Delay(float seconds) => async ct =>
    {
        await Task.Delay(TimeSpan.FromSeconds(seconds), ct);
        return default;
    };

    public static Func<T, Proc<ValueTuple>> Action<T>(Action<T> action) => t => ct =>
    {
        action(t);
        return Task.FromResult(ValueTuple.Create());
    };

    public static Proc<R> Select<T, R>(this Proc<T> proc, Func<T, R> f) => async ct =>
    {
        return f(await proc(ct));
    };

    public static Proc<T> Any<T>(params Proc<T>[] procs) => async ct =>
    {
        if (procs.Length == 0)
            return await Proc.Never<T>()(ct);

        using (var cts = new CancellationTokenSource())
        using (ct.Register(() => cts.Cancel()))
        {
            var tasks = procs.Select(proc => proc(cts.Token)).ToArray();

            var winner = await Task.WhenAny(tasks);

            cts.Cancel();

            foreach (var task in tasks)
            {
                if (task != winner)
                {
                    try { await task; } catch (Exception) { }
                }
            }

            return await winner;
        }
    };

    public static Proc<ValueTuple> NextEvent(this UnityEvent ue) => async ct =>
    {
        var tcs = new TaskCompletionSource<ValueTuple>();

        var call = new UnityAction(() =>
        {
            tcs.TrySetResult(default);
        });

        ue.AddListener(call);

        try
        {
            using (ct.Register(() => tcs.TrySetCanceled()))
            {
                return await tcs.Task;
            }
        }
        finally
        {
            ue.RemoveListener(call);
        }
    };

    public static Proc<T> NextEvent<T>(this UnityEvent<T> ue) => async ct =>
    {
        var tcs = new TaskCompletionSource<T>();

        var call = new UnityAction<T>(t =>
        {
            tcs.TrySetResult(t);
        });

        ue.AddListener(call);

        try
        {
            using (ct.Register(() => tcs.TrySetCanceled()))
            {
                return await tcs.Task;
            }
        }
        finally
        {
            ue.RemoveListener(call);
        }
    };

    public static Proc<T> AnyThen<T>(params Proc<Proc<T>>[] procs) => async ct =>
    {
        var winner = await Any(procs)(ct);
        return await winner(ct);
    };

    public static Proc<ValueTuple> Forever<T>(this Proc<T> proc) => async ct =>
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            await proc(ct);
        }
    };

    public static Proc<ValueTuple> Ignore<T>(this Proc<T> proc) => proc.Select(_ => ValueTuple.Create());
}
