namespace Colabs.Dtos;

public record class LoginResponse(
    string Token,
    string TokenType,
    DateTime ExpiresAt,
    int UserId,
    string Name,
    string Email,
    string Role
);