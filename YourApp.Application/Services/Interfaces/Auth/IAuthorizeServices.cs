using System.Security.Claims;
using YourApp.Application.Contracts.Requests;
using YourApp.Application.Contracts.Responses;
using YourApp.Domain.Entities;

namespace YourApp.Application.Services.Interfaces.Auth;

public interface IAuthorizeServices
{
    Task<User> LoginWithGoogle(ClaimsPrincipal claimsPrincipal);
    Task<User?> CreateAccount(RegisterRequest request);
    Task<LoginResponse?> Login(LoginRequest request);
    Task InitiatePasswordReset(string email);
    Task<bool> VerifyPasswordResetToken(string token);
    Task ResetPasswordAsync(ResetPasswordRequest request);
    Task ResendEmailConfirmationAsync(User user);
    Task Logout(User user);
}