using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
namespace AuthService.Infrastructure.Repositories;

public class MockUserRepository : IUserRepository
{
    public Task<User?> GetByUsernameAsync(string username)
    {
        if (username == "admin")
            return Task.FromResult<User?>(new User { Id = Guid.NewGuid(), Username = "admin", PasswordHash = "admin123", Email = "admin@acitychallenge.com", Role = "Admin" });
        return Task.FromResult<User?>(null);
    }
}
