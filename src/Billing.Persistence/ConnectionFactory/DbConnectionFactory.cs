using System.Data;
using Microsoft.Data.SqlClient;

namespace Billing.Persistence.ConnectionFactory;

/// <summary>
/// Abstraction over creating SQL Server connections. Allows swapping the
/// connection strategy (e.g. for multi-database tenancy) without touching
/// repository code.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates a new, unopened <see cref="SqlConnection"/>. The caller is
    /// responsible for opening and disposing it (typically via a using block).
    /// </summary>
    IDbConnection CreateConnection();
}

/// <summary>
/// Default SQL Server connection factory backed by configuration.
/// </summary>
public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection()
    {
        var connection = new SqlConnection(_connectionString);
        return connection;
    }
}
