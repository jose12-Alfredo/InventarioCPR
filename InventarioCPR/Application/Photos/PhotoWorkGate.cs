namespace InventarioCPR.Application.Photos;

// Bound decoded-pixel memory across requests, not just each browser's sequential queue.
public sealed class PhotoWorkGate
{
    private readonly SemaphoreSlim gate = new(1, 1);
    public async Task<IDisposable> EnterAsync(CancellationToken ct)
    {
        if (!await gate.WaitAsync(TimeSpan.FromSeconds(20), ct))
            throw new PhotoException("Hay otra fotografía procesándose. Reintente en unos segundos.", 429);
        return new Lease(gate);
    }
    private sealed class Lease(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose() => semaphore.Release();
    }
}
