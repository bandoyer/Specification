namespace Tests.Fixture;

// Makes a new context on each call, as AddDbContextFactory's factory does, and keeps each one so a test can check
// that it was disposed.
public sealed class RecordingContextFactory(DbContextOptions options) : IDbContextFactory<TestDbContext>
{
    public List<TrackedDbContext> Made { get; } = [];

    public TestDbContext CreateDbContext()
    {
        var context = new TrackedDbContext(options);
        Made.Add(context);
        return context;
    }
}

public sealed class TrackedDbContext(DbContextOptions options) : TestDbContext(options)
{
    public bool IsDisposed { get; private set; }

    public override void Dispose()
    {
        IsDisposed = true;
        base.Dispose();
    }

    public override ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return base.DisposeAsync();
    }
}
