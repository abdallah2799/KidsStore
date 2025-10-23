using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
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

        var httpContext = _httpContextAccessor.HttpContext!;
        var session = httpContext.Session;

        // Save data in session
        session.SetInt32("UserId", user.Id);
        session.SetString("UserName", user.UserName);
        session.SetString("UserRole", user.Role.ToString());

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
            Role = role
        };

        user.SetPassword(password);

        await userRepo.AddAsync(user);
        await _uow.SaveChangesAsync();

        return user;
    }

    public Task LogoutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext!;
        httpContext.Session.Clear();

        // Remove cookies if exist
        httpContext.Response.Cookies.Delete("UserId");
        httpContext.Response.Cookies.Delete("UserName");
        httpContext.Response.Cookies.Delete("UserRole");

        return Task.CompletedTask;
    }
}
