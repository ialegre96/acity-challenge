using AuthService.Domain.Entities;
namespace AuthService.Application.Interfaces;

public interface ITokenGenerator { string GenerateToken(User user); }
