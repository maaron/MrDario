using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public abstract class TaskBehaviour : MonoBehaviour
{
    CancellationTokenSource cts;

    protected async void Start()
    {
        cts = new CancellationTokenSource();

        var nameCopy = name;

        try
        {
            await Run(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Debug.Log($"TaskBehaviour {nameCopy} cancelled");
        }
        catch (Exception e)
        {
            Debug.LogException(e, this);
        }
    }

    protected abstract Task Run(CancellationToken ct);

    protected void OnDestroy()
    {
        cts?.Cancel();
    }
}
