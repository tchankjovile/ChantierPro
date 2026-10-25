using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ChantierPro.Data;

public static class DataServiceCollectionExtensions
{
    /// <summary>
    /// Enregistre <see cref="AppDbContext"/>. Le fournisseur (Sqlite, PostgreSql, SqlServer) et la chaîne de
    /// connexion sont lus de façon différée via <see cref="DatabaseOptions"/> (section « Database »).
    /// </summary>
    public static IServiceCollection AddChantierProData(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var database = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            ConfigureProvider(options, database);
        });

        return services;
    }

    public static void ConfigureProvider(DbContextOptionsBuilder options, DatabaseOptions database)
    {
        var provider = (database.Provider ?? string.Empty).Trim().ToLowerInvariant();
        var connectionString = database.ConnectionString;

        switch (provider)
        {
            case "sqlite":
                options.UseSqlite(connectionString);
                break;

            case "postgresql":
            case "postgres":
            case "npgsql":
                // Les dates métier sont stockées sans fuseau (heure locale du chantier) : on garde le
                // comportement « timestamp without time zone » historique de Npgsql.
                AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
                options.UseNpgsql(connectionString);
                break;

            case "sqlserver":
                options.UseSqlServer(connectionString);
                break;

            default:
                throw new InvalidOperationException(
                    $"Fournisseur de base de données inconnu : « {database.Provider} ». Valeurs acceptées : Sqlite, PostgreSql, SqlServer.");
        }
    }
}
