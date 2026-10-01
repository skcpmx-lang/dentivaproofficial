using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using DentivaPro.Infrastructure.Persistence;

namespace DentivaPro.DbMigrations;

/// <summary>
/// Supplies EF tooling with an encrypted in-memory context. It never points design-time commands at clinic data.
/// </summary>
public sealed class DentivaDesignTimeDbContextFactory : IDesignTimeDbContextFactory<DentivaDbContext>
{
    public DentivaDbContext CreateDbContext(string[] args)
    {
        _ = args;
        SQLitePCL.Batteries_V2.Init();
        var key = RandomNumberGenerator.GetBytes(32);
        try
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = ":memory:",
                Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared,
                Pooling = false,
                ForeignKeys = true,
                Password = Convert.ToBase64String(key)
            }.ToString();

            var options = new DbContextOptionsBuilder<DentivaDbContext>()
                .UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly(typeof(DentivaDbContext).Assembly.GetName().Name))
                .Options;
            return new DentivaDbContext(options);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }
}
