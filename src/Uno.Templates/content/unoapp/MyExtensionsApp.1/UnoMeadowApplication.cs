using Meadow;
using Microsoft.UI.Dispatching;

namespace UnoApp12;

public class UnoMeadowDesktopApplication: UnoMeadowApplication<Desktop>
{
    
}


public class UnoMeadowApplication<T> : Application, IApp
    where T : class, IMeadowDevice
{
    public CancellationToken CancellationToken => throw new NotImplementedException();

    public static T Device => Resolver.Services.Get<IMeadowDevice>() as T;
    
    /// <inheritdoc/>
    public Dictionary<string, string> Settings { get; } = new();

    /// <inheritdoc/>
    public void InvokeOnMainThread(Action<object?> action, object? state = null)
    {
        var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        dispatcherQueue.TryEnqueue(() => action(state));
    }

    /// <inheritdoc/>
    public virtual Task OnError(Exception e)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public virtual Task OnShutdown()
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public virtual void OnUpdate(Version newVersion, out bool approveUpdate)
    {
        approveUpdate = true;
    }

    /// <inheritdoc/>
    public virtual void OnUpdateComplete(Version oldVersion, out bool rollbackUpdate)
    {
        rollbackUpdate = false;
    }
    
    public virtual Task MeadowRun()
    {
        return Task.CompletedTask;
    }
    
    public virtual Task MeadowInitialize()
    {
        return Task.CompletedTask;
    }

    Task IApp.Run()
    {
        return MeadowRun();
    }

    Task IApp.Initialize()
    {
        return MeadowInitialize();
    }

    public void LoadMeadowOS()
    {
        new Thread((o) =>
            {
                _ = MeadowOS.Start(this);
            })
            {
                IsBackground = true
            }
            .Start();
    }
}
