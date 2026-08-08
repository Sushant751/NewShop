using System.Data;
using Billing.Domain.Entities;
using Billing.Persistence.ConnectionFactory;
using Billing.Shared.Enums;
using Dapper;

namespace Billing.Persistence.Repositories;

public sealed class TenantRepository : IGlobalRepository<Tenant>, ITenantRepository
{
    private readonly IDbConnectionFactory _factory;

    public TenantRepository(IDbConnectionFactory factory) => _factory = factory;

    private static async Task<IDbConnection> GetConnectionAsync(IDbConnectionFactory factory, IDbTransaction? transaction, CancellationToken cancellationToken)
    {
        if (transaction is not null) return transaction.Connection!;
        var connection = factory.CreateConnection();
        if (connection is System.Data.Common.DbConnection dbConn)
            await dbConn.OpenAsync(cancellationToken);
        else
            connection.Open();
        return connection;
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM Tenants WHERE Id = @Id AND IsDeleted = 0";
        var connection = await GetConnectionAsync(_factory, transaction, cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<Tenant>(
            new CommandDefinition(sql, new { Id = id }, transaction, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Tenant>> GetAllAsync(IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM Tenants WHERE IsDeleted = 0 ORDER BY CreatedDate DESC";
        var connection = await GetConnectionAsync(_factory, transaction, cancellationToken);
        var result = await connection.QueryAsync<Tenant>(new CommandDefinition(sql, null, transaction, cancellationToken: cancellationToken));
        return result.AsList();
    }

    public async Task<Guid> InsertAsync(Tenant entity, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        if (entity.Id == Guid.Empty) entity.Id = Guid.NewGuid();
        entity.CreatedDate = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO Tenants
                (Id, Name, Slug, Description, ContactEmail, ContactPhone, Address, Country,
                 CurrencyCode, TimeZone, TaxIdentificationNumber, Status, TrialEndsOn,
                 SubscriptionEndsOn, PlanId, MaxUsers, MaxProducts, CreatedDate, CreatedBy, IsDeleted)
            VALUES
                (@Id, @Name, @Slug, @Description, @ContactEmail, @ContactPhone, @Address, @Country,
                 @CurrencyCode, @TimeZone, @TaxIdentificationNumber, @Status, @TrialEndsOn,
                 @SubscriptionEndsOn, @PlanId, @MaxUsers, @MaxProducts, @CreatedDate, @CreatedBy, 0);
            SELECT @Id;";
        var connection = await GetConnectionAsync(_factory, transaction, cancellationToken);
        await connection.ExecuteScalarAsync<Guid>(new CommandDefinition(sql, entity, transaction, cancellationToken: cancellationToken));
        return entity.Id;
    }

    public async Task<int> UpdateAsync(Tenant entity, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        entity.UpdatedDate = DateTime.UtcNow;
        const string sql = @"
            UPDATE Tenants SET
                Name = @Name, Slug = @Slug, Description = @Description,
                ContactEmail = @ContactEmail, ContactPhone = @ContactPhone, Address = @Address,
                Country = @Country, CurrencyCode = @CurrencyCode, TimeZone = @TimeZone,
                TaxIdentificationNumber = @TaxIdentificationNumber, Status = @Status,
                TrialEndsOn = @TrialEndsOn, SubscriptionEndsOn = @SubscriptionEndsOn,
                PlanId = @PlanId, MaxUsers = @MaxUsers, MaxProducts = @MaxProducts,
                UpdatedBy = @UpdatedBy, UpdatedDate = @UpdatedDate
            WHERE Id = @Id AND IsDeleted = 0";
        var connection = await GetConnectionAsync(_factory, transaction, cancellationToken);
        return await connection.ExecuteAsync(new CommandDefinition(sql, entity, transaction, cancellationToken: cancellationToken));
    }

    public async Task<int> SoftDeleteAsync(Guid id, Guid deletedBy, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE Tenants SET IsDeleted = 1, DeletedBy = @DeletedBy, DeletedDate = @DeletedDate WHERE Id = @Id AND IsDeleted = 0";
        var connection = await GetConnectionAsync(_factory, transaction, cancellationToken);
        return await connection.ExecuteAsync(new CommandDefinition(sql,
            new { Id = id, DeletedBy = deletedBy, DeletedDate = DateTime.UtcNow }, transaction, cancellationToken: cancellationToken));
    }

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM Tenants WHERE Slug = @Slug AND IsDeleted = 0";
        using var connection = _factory.CreateConnection();
        if (connection is System.Data.Common.DbConnection dbConn)
            await dbConn.OpenAsync(cancellationToken);
        else
            connection.Open();
        return await connection.QueryFirstOrDefaultAsync<Tenant>(new CommandDefinition(sql, new { Slug = slug }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Tenant>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM Tenants WHERE IsDeleted = 0 AND Status = 1 ORDER BY Name";
        using var connection = _factory.CreateConnection();
        if (connection is System.Data.Common.DbConnection dbConn)
            await dbConn.OpenAsync(cancellationToken);
        else
            connection.Open();
        var result = await connection.QueryAsync<Tenant>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return result.AsList();
    }

    public async Task<int> UpdateStatusAsync(Guid tenantId, TenantStatus status, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE Tenants SET Status = @Status, UpdatedBy = @UpdatedBy, UpdatedDate = @UpdatedDate WHERE Id = @Id AND IsDeleted = 0";
        using var connection = _factory.CreateConnection();
        if (connection is System.Data.Common.DbConnection dbConn)
            await dbConn.OpenAsync(cancellationToken);
        else
            connection.Open();
        return await connection.ExecuteAsync(new CommandDefinition(sql,
            new { Id = tenantId, Status = (int)status, UpdatedBy = updatedBy, UpdatedDate = DateTime.UtcNow }, cancellationToken: cancellationToken));
    }
}
