using Domain.Entities;

public interface IAccountService
{
    Task<User?> LoginAsync(string username, string password);
    Task<User> CreateUserAsync(string username, string password, UserRole role);
    Task LogoutAsync();
}
