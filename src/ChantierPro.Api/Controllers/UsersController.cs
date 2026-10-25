using ChantierPro.Api.Dtos;
using ChantierPro.Api.Infrastructure;
using ChantierPro.Data;
using ChantierPro.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChantierPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Admin)]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;

    public UsersController(
        AppDbContext db,
        IPasswordHasher hasher,
        IClock clock)
    {
        _db = db;
        _hasher = hasher;
        _clock = clock;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
    {
        var users = await _db.Users.AsNoTracking().OrderBy(u => u.Name).ToListAsync();
        var list = users.Select(u => new
        {
            u.Id,
            u.Name,
            u.Email,
            Role = u.Role.ToString(),
            u.IsActive,
            u.CreatedAt
        });
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<object>> GetById(Guid id)
    {
        var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (u == null) throw Errors.NotFound("Utilisateur");

        return Ok(new
        {
            u.Id,
            u.Name,
            u.Email,
            Role = u.Role.ToString(),
            u.IsActive,
            u.CreatedAt
        });
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] RegisterUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw Errors.Validation("L'email et le mot de passe sont obligatoires.");
        if (request.Password.Length < 6)
            throw Errors.Validation("Le mot de passe doit comporter au moins 6 caractères.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var exists = await _db.Users.AnyAsync(u => u.Email == normalizedEmail);
        if (exists)
            throw Errors.Conflict($"Un compte avec l'adresse « {request.Email} » existe déjà.");

        var user = new UserAccount
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = normalizedEmail,
            PasswordHash = _hasher.HashPassword(request.Password),
            Role = request.Role,
            IsActive = true,
            CreatedAt = _clock.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new
        {
            user.Id,
            user.Name,
            user.Email,
            Role = user.Role.ToString(),
            user.IsActive,
            user.CreatedAt
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<object>> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) throw Errors.NotFound("Utilisateur");

        user.Name = request.Name.Trim();
        user.Role = request.Role;
        user.IsActive = request.IsActive;

        await _db.SaveChangesAsync();
        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            Role = user.Role.ToString(),
            user.IsActive,
            user.CreatedAt
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) throw Errors.NotFound("Utilisateur");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
