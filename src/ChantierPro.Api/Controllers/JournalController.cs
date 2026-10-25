using ChantierPro.Api.Dtos;
using ChantierPro.Api.Infrastructure;
using ChantierPro.Api.Services;
using ChantierPro.Core.Models;
using ChantierPro.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ChantierPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JournalController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly SqlChantierRepository _repo;
    private readonly IClock _clock;
    private readonly ICurrentUserContext _user;
    private readonly StorageOptions _storage;
    private readonly IWebHostEnvironment _env;

    public JournalController(
        AppDbContext db,
        SqlChantierRepository repo,
        IClock clock,
        ICurrentUserContext user,
        IOptions<StorageOptions> storage,
        IWebHostEnvironment env)
    {
        _db = db;
        _repo = repo;
        _clock = clock;
        _user = user;
        _storage = storage.Value;
        _env = env;
    }

    [HttpGet("entries/{chantierId:guid}")]
    public async Task<ActionResult<IEnumerable<JournalEntry>>> GetEntries(Guid chantierId)
    {
        var entries = await _db.JournalEntries
            .AsNoTracking()
            .Where(j => j.ChantierId == chantierId)
            .OrderByDescending(j => j.Date)
            .ToListAsync();

        return Ok(entries);
    }

    [HttpPost("entries/{chantierId:guid}")]
    [Authorize(Roles = Roles.Contributors)]
    public async Task<ActionResult<JournalEntry>> CreateEntry(Guid chantierId, [FromBody] CreateJournalEntryRequest request)
    {
        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            ChantierId = chantierId,
            Date = request.Date,
            Weather = request.Weather,
            Note = request.Note?.Trim() ?? string.Empty,
            Incident = request.Incident?.Trim() ?? string.Empty,
            Visitors = request.Visitors?.Trim() ?? string.Empty,
            Author = _user.Name
        };

        await _repo.AddJournalEntryAsync(entry);
        return Created($"/api/journal/entries/{entry.Id}", entry);
    }

    [HttpGet("photos/{chantierId:guid}")]
    public async Task<ActionResult<IEnumerable<SitePhoto>>> GetPhotos(Guid chantierId)
    {
        var photos = await _db.Photos
            .AsNoTracking()
            .Where(p => p.ChantierId == chantierId)
            .OrderByDescending(p => p.Date)
            .ToListAsync();

        return Ok(photos);
    }

    [HttpPost("photos/{chantierId:guid}")]
    [Authorize(Roles = Roles.Contributors)]
    public async Task<ActionResult<SitePhoto>> CreatePhoto(Guid chantierId, [FromBody] CreatePhotoRequest request)
    {
        var photo = new SitePhoto
        {
            Id = Guid.NewGuid(),
            ChantierId = chantierId,
            Path = request.Path,
            Stage = string.IsNullOrWhiteSpace(request.Stage) ? "Pendant" : request.Stage,
            Date = _clock.Now,
            Author = _user.Name
        };

        await _repo.AddPhotoAsync(photo);
        return Created($"/api/journal/photos/{photo.Id}", photo);
    }

    [HttpPost("photos/{chantierId:guid}/upload")]
    [Authorize(Roles = Roles.Contributors)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<SitePhoto>> UploadPhoto(Guid chantierId, [FromForm] IFormFile file, [FromForm] string stage = "Pendant")
    {
        if (file == null || file.Length == 0)
            throw Errors.Validation("Aucun fichier reçu.");

        if (file.Length > _storage.MaxUploadBytes)
            throw Errors.Validation($"Fichier trop volumineux. Taille max : {_storage.MaxUploadBytes / (1024 * 1024)} Mo.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowedExts.Contains(ext))
            throw Errors.Validation("Seules les images (jpg, png, webp) sont acceptées.");

        var uploadsFolder = Path.IsPathRooted(_storage.UploadsPath)
            ? _storage.UploadsPath
            : Path.Combine(_env.ContentRootPath, _storage.UploadsPath);

        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{chantierId}_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsFolder, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = $"/uploads/{fileName}";
        var photo = new SitePhoto
        {
            Id = Guid.NewGuid(),
            ChantierId = chantierId,
            Path = relativeUrl,
            Stage = stage,
            Date = _clock.Now,
            Author = _user.Name
        };

        await _repo.AddPhotoAsync(photo);
        return Created($"/api/journal/photos/{photo.Id}", photo);
    }
}
