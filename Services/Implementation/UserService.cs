using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResumeGenerator.API.Data;
using ResumeGenerator.API.Models.DTOs;
using ResumeGenerator.API.Models.Entities;
using ResumeGenerator.API.Services.Interfaces;

namespace ResumeGenerator.API.Services.Implementation;

public class UserService : IUserService
{
    private readonly ResumeGeneratorContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(ResumeGeneratorContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User> CreateUserAsync(RegisterRequestDto request)
    {
        // Check if email already exists
        if (await EmailExistsAsync(request.Email))
        {
            throw new ArgumentException("Email already exists");
        }

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailVerified = false // Could implement email verification later
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User created successfully: {Email}", user.Email);
        return user;
    }

    public async Task<User?> ValidateUserAsync(string email, string password)
    {
        var user = await GetUserByEmailAsync(email);
        if (user == null || !user.IsActive)
        {
            return null;
        }

        var isValidPassword = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        return isValidPassword ? user : null;
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant() && u.IsActive);
    }

    public async Task UpdateLastLoginAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == email.ToLowerInvariant());
    }
}