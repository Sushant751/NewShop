using System.Data;
using Billing.Persistence.ConnectionFactory;

namespace Billing.Persistence.UnitOfWork;

/// <summary>
/// Unit of Work abstraction. Coordinates transactional writes across multiple
/// repositories and ensures a single shared connection for the duration of a
/// business transaction.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// The connection used by this unit of work. Repositories participating in
    /// the transaction should use this connection rather than creating their own.
    /// </summary>
    IDbConnection Connection { get; }

    /// <summary>
    /// The active transaction, or null if no transaction has been started.
    /// </summary>
    IDbTransaction? Transaction { get; }

    /// <summary>
    /// Begins a new transaction with the default isolation level (ReadCommitted).
    /// </summary>
    Task BeginTransactionAsync(IsolationLevel isolation = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the active transaction.
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the active transaction.
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Default Unit of Work implementation. Lazily opens a single connection and
/// manages a transaction across repository writes.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly IDbConnectionFactory _factory;
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;
    private bool _disposed;

    public UnitOfWork(IDbConnectionFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public IDbConnection Connection
    {
        get
        {
            EnsureNotDisposed();
            if (_connection is null)
            {
                _connection = _factory.CreateConnection();
                if (_connection.State != ConnectionState.Open)
                    _connection.Open();
            }
            return _connection;
        }
    }

    public IDbTransaction? Transaction
    {
        get
        {
            EnsureNotDisposed();
            return _transaction;
        }
    }

    public async Task BeginTransactionAsync(IsolationLevel isolation = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        if (_transaction is not null)
            return;

        // Ensure connection is open (accessing the property opens it lazily).
        _ = Connection;
        _transaction = await Task.Run(() => _connection!.BeginTransaction(isolation), cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        if (_transaction is null)
            throw new InvalidOperationException("Cannot commit: no active transaction.");

        await Task.Run(() => _transaction.Commit(), cancellationToken);
        _transaction.Dispose();
        _transaction = null;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        if (_transaction is null)
            return;

        await Task.Run(() => _transaction.Rollback(), cancellationToken);
        _transaction.Dispose();
        _transaction = null;
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UnitOfWork));
    }

    public void Dispose()
    {
        if (_disposed) return;
        try { _transaction?.Dispose(); } catch { /* ignore */ }
        try { _connection?.Dispose(); } catch { /* ignore */ }
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        try { _transaction?.Dispose(); } catch { /* ignore */ }
        if (_connection is IAsyncDisposable asyncConnection)
        {
            await asyncConnection.DisposeAsync();
        }
        else
        {
            _connection?.Dispose();
        }
        _disposed = true;
    }
}
