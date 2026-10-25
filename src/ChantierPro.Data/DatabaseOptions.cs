namespace ChantierPro.Data;

/// <summary>
/// Configuration du fournisseur de base de données (section « Database » de appsettings.json).
/// Valeurs de <see cref="Provider"/> acceptées : Sqlite, PostgreSql, SqlServer.
/// </summary>
public class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Provider { get; set; } = "Sqlite";
    public string ConnectionString { get; set; } = "Data Source=chantierpro.db";
}
