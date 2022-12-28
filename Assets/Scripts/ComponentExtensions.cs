using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class ComponentExtensions
{
    public static Task NextUpdate(this MonoBehaviour mb, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var tcs = new TaskCompletionSource<int>();
        mb.StartCoroutine(Routine(tcs));
        return tcs.Task;

        IEnumerator Routine(TaskCompletionSource<int> tcs)
        {
            yield return null;
            tcs.TrySetResult(0);
        }
    }
}
