using Microsoft.Extensions.Options;

namespace ChantierPro.Api.Infrastructure;

public class AppOptions
{
    public const string SectionName = "App";

    /// <summary>Identifiant du fuseau horaire métier (IANA ou Windows). Par défaut : Cameroun.</summary>
    public string TimeZone { get; set; } = "Africa/Douala";
}

public class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Dossier des fichiers téléversés (relatif au répertoire de l'application ou absolu).</summary>
    public string UploadsPath { get; set; } = "uploads";

    public long MaxUploadBytes { get; set; } = 10 * 1024 * 1024;
}

/// <summary>Source de temps unique : facilite les tests et garantit un « aujourd'hui » cohérent avec le fuseau du chantier.</summary>
public interface IClock
{
    DateTime UtcNow { get; }

    /// <summary>Date et heure locales du chantier (type non spécifié, comme les dates saisies par l'application mobile).</summary>
    DateTime Now { get; }

    DateTime Today { get; }
}

public sealed class SystemClock : IClock
{
    private readonly TimeZoneInfo _zone;

    public SystemClock(IOptions<AppOptions> options)
    {
        _zone = ResolveZone(options.Value.TimeZone);
    }

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _zone);

    public DateTime Today => Now.Date;

    private static TimeZoneInfo ResolveZone(string? id)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        // Repli sûr (conteneur sans base de fuseaux) : UTC+1, heure légale du Cameroun.
        return TimeZoneInfo.CreateCustomTimeZone("WAT", TimeSpan.FromHours(1), "Afrique de l'Ouest (UTC+1)", "WAT");
    }
}
