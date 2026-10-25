using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChantierPro.Api.Infrastructure;

/// <summary>Erreur métier ou de validation convertie en réponse HTTP « problem+json » par <see cref="ErrorHandlingMiddleware"/>.</summary>
public class ApiException : Exception
{
    public ApiException(int statusCode, string title, string message) : base(message)
    {
        StatusCode = statusCode;
        Title = title;
    }

    public int StatusCode { get; }
    public string Title { get; }
}

/// <summary>Fabrique d'erreurs standardisées.</summary>
public static class Errors
{
    public static ApiException NotFound(string what) =>
        new ApiException(StatusCodes.Status404NotFound, "Ressource introuvable", $"{what} introuvable.");

    public static ApiException Validation(string message) =>
        new ApiException(StatusCodes.Status400BadRequest, "Données invalides", message);

    public static ApiException Conflict(string message) =>
        new ApiException(StatusCodes.Status409Conflict, "Règle de gestion non respectée", message);

    public static ApiException Forbidden(string message) =>
        new ApiException(StatusCodes.Status403Forbidden, "Accès refusé", message);

    public static ApiException Unauthorized(string message) =>
        new ApiException(StatusCodes.Status401Unauthorized, "Authentification requise", message);

    /// <summary>RG-07 : un chantier clôturé est en lecture seule.</summary>
    public static ApiException Locked(string chantierName) =>
        Conflict($"Le chantier « {chantierName} » est clôturé : il est en lecture seule (RG-07).");
}

/// <summary>Transforme toute exception non gérée en réponse JSON normalisée (RFC 7807) sans jamais divulguer de détail interne.</summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Le client a interrompu la requête : rien à renvoyer.
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            var (status, title, detail) = Map(ex);
            if (status >= 500)
            {
                _logger.LogError(ex, "Erreur non gérée sur {Method} {Path}", context.Request.Method, context.Request.Path);
            }
            else
            {
                _logger.LogInformation("Requête rejetée ({Status}) sur {Method} {Path} : {Detail}", status,
                    context.Request.Method, context.Request.Path, detail);
            }

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };
            problem.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }

    private static (int Status, string Title, string Detail) Map(Exception ex)
    {
        if (ex is ApiException api)
        {
            return (api.StatusCode, api.Title, api.Message);
        }

        if (ex is DbUpdateConcurrencyException)
        {
            return (StatusCodes.Status409Conflict, "Conflit de modification",
                "Les données ont été modifiées par un autre utilisateur. Rechargez puis réessayez.");
        }

        if (ex is DbUpdateException)
        {
            return (StatusCodes.Status409Conflict, "Conflit de données",
                "La base de données a refusé l'opération (doublon ou référence invalide).");
        }

        return (StatusCodes.Status500InternalServerError, "Erreur interne",
            "Une erreur interne est survenue. Contactez l'administrateur en indiquant l'identifiant de trace.");
    }
}
