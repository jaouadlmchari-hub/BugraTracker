using BugTracker.Application.Interfaces.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

public class EfTransaction : ITransaction
{
    private readonly IDbContextTransaction _transaction;

    public EfTransaction(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

   public Task CommitAsync()
        => _transaction.CommitAsync();

    public Task RollbackAsync()
        => _transaction.RollbackAsync();

    public ValueTask DisposeAsync()
        => _transaction.DisposeAsync();
}