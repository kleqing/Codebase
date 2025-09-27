using Microsoft.EntityFrameworkCore;
using YourApp.Domain.Entities;
using YourApp.Domain.Enums;
using YourApp.Domain.Interfaces;
using YourApp.Infrastructure.Data;
using YourApp.Shared.Exceptions;

namespace YourApp.Infrastructure.Repository;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<User?> FindByEmailAsync(string email)
    {
        return await _context.User
            .FirstOrDefaultAsync(u => u.Email == email);
    }
    
    public async Task<User?> FindByIdAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var guid))
        {
            throw new GlobalException("Invalid user id");
        }

        return await _context.User
            .FirstOrDefaultAsync(u => u.UserId == guid);
    }


    public async Task<User?> FindByNameAsync(string userName)
    {
        return await _context.User
            .FirstOrDefaultAsync(u => u.UserName == userName);
    }
    
    public async Task CreateAsync(User user)
    {
        await _context.User.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task CreateAsync(User user, string password)
    {
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        await _context.User.AddAsync(user);
        await _context.SaveChangesAsync();
    }
    
    public async Task UpdateAsync(User user)
    {
        _context.User.Update(user);
        await _context.SaveChangesAsync();
    }
    
    public async Task DeleteAsync(User user)
    {
        _context.User.Remove(user);
        await _context.SaveChangesAsync();
    }

    public async Task<User?> AddToRoleAsync(User user, UserRole roleName)
    {
        var userInDb = await _context.User.FirstOrDefaultAsync(u => u.UserId == user.UserId);
        if (userInDb == null)
        {
            return null;
            
        }
        userInDb.UserRole = roleName;
        await _context.SaveChangesAsync();
        return userInDb;
    }

    public async Task<User?> ConfirmEmailAsync(User user)
    {
        var userInDb = await _context.User.FirstOrDefaultAsync(u => u.UserId == user.UserId);
        if (userInDb == null)
        {
            return null;
        }
        userInDb.EmailConfirmed = true;
        await _context.SaveChangesAsync();
        return userInDb;
    }
    
    public async Task<bool> CheckPasswordAsync(User user, string password)
    {
        var userInDb = await _context.User.FirstOrDefaultAsync(u => u.UserId == user.UserId);
        return userInDb != null && BCrypt.Net.BCrypt.Verify(password, userInDb.PasswordHash);
    }
    

    public async Task<bool> IsEmailConfirmedAsync(User user)
    {
        var userInDb = await _context.User.FirstOrDefaultAsync(u => u.UserId == user.UserId);
        return userInDb?.EmailConfirmed ?? false;
    }
    
    public async Task<User?> ResetPasswordAsync(User user, string newPassword)
    {
        var userInDb = await _context.User.FirstOrDefaultAsync(u => u.UserId == user.UserId);
        if (userInDb == null) return null;

        userInDb.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        userInDb.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return userInDb;
    }
}

