using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

public class AccountService : IAccountService
{
    private readonly IUnitOfWork _uow;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AccountService(IUnitOfWork uow, IHttpContextAccessor httpContextAccessor)
    {
        _uow = uow;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        var userRepo = _uow.Repository<User>();

        var user = await userRepo.AsQueryable()
            .FirstOrDefaultAsync(u => u.UserName == username && u.IsActive);

        if (user is null || !user.VerifyPassword(password))
            return null;

        // مفيش تخزين في Session هنا — CookieAuth بيتكفل بكل حاجة
        return user;
    }

    public async Task<User> CreateUserAsync(string username, string password, UserRole role)
    {
        var userRepo = _uow.Repository<User>();

        var existing = await userRepo.AsQueryable()
            .FirstOrDefaultAsync(u => u.UserName == username);

        if (existing is not null)
            throw new InvalidOperationException("Username already exists.");

        var user = new User
        {
            UserName = username,
            Role = role,
            IsActive = true
        };

        user.SetPassword(password);

        await userRepo.AddAsync(user);
        await _uow.SaveChangesAsync();

        return user;
    }

    public Task LogoutAsync()
    {
        return Task.CompletedTask;
    }
}
