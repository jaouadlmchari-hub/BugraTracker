

namespace BugTracker.Application.Interfaces.Persistence
{
    public interface ITransaction : IAsyncDisposable
    {
        Task CommitAsync();
        Task RollbackAsync();
    }
}
