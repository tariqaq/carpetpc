namespace CarpetPC.Core.Safety;

public sealed class PauseState
{
    public event EventHandler<bool>? Changed;

    public bool IsPaused { get; private set; }

    public void Pause()
    {
        if (IsPaused)
        {
            return;
        }

        IsPaused = true;
        Changed?.Invoke(this, IsPaused);
    }

    public void Resume()
    {
        if (!IsPaused)
        {
            return;
        }

        IsPaused = false;
        Changed?.Invoke(this, IsPaused);
    }
}

