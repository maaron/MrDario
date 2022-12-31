using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public abstract class Dialog<TResult> : MonoBehaviour
{
    public bool IsOpen => gameObject.activeSelf;

    public async Task<TResult> Run(CancellationToken ct)
    {
        gameObject.SetActive(true);

        try
        {
            return await RunInternal(CancellationToken.None);
        }
        finally
        {
            gameObject.SetActive(false);
        }
    }

    protected abstract Task<TResult> RunInternal(CancellationToken ct);
}
