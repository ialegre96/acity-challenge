using MediatR;
using AuthService.Application.Interfaces;
namespace AuthService.Application.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenGenerator _tokenGenerator;

    public LoginCommandHandler(IUserRepository userRepository, ITokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        if (user == null || user.PasswordHash != request.Password)
            throw new UnauthorizedAccessException("Credenciales inválidas");

        var token = _tokenGenerator.GenerateToken(user);
        return new LoginResponse { Token = token };
    }
}
