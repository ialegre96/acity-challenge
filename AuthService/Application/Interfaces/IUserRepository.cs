using AuthService.Domain.Entities;
namespace AuthService.Application.Interfaces;

public interface IUserRepository { Task<User?> GetByUsernameAsync(string username); }
