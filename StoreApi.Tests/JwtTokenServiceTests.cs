using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StoreApi.Models;
using StoreApi.Services;

namespace StoreApi.Tests;

public class JwtTokenServiceTests
{
    [Fact]
    public async Task CreateTokenAsync_IncludesTheUsersRole()
    {
        var user = new AppUser
        {
            Id = "user-123",
            Email = "admin@example.com",
            FullName = "Store Admin"
        };
        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.GetRolesAsync(user)).ReturnsAsync(["Admin"]);
        var service = new JwtTokenService(
            Options.Create(new JwtOptions
            {
                Key = "test-signing-key-that-is-at-least-32-bytes-long",
                Issuer = "StoreApi.Tests",
                Audience = "StoreApi.Tests",
                ExpirationMinutes = 15
            }),
            userManager.Object);

        var (token, expiresAtUtc) = await service.CreateTokenAsync(user);
        var claims = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims;

        Assert.Contains(claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "Admin");
        Assert.True(expiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public void ValidateOptions_RejectsShortSigningKeys()
    {
        var options = new JwtOptions
        {
            Key = "too-short",
            Issuer = "StoreApi.Tests",
            Audience = "StoreApi.Tests"
        };

        Assert.Throws<InvalidOperationException>(() => JwtTokenService.ValidateOptions(options));
    }

    private static Mock<UserManager<AppUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(
            store.Object,
            Options.Create(new IdentityOptions()),
            Mock.Of<IPasswordHasher<AppUser>>(),
            Array.Empty<IUserValidator<AppUser>>(),
            Array.Empty<IPasswordValidator<AppUser>>(),
            Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<AppUser>>>());
    }
}
