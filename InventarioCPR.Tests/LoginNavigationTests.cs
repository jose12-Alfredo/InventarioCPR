using InventarioCPR.Infrastructure.Security;
using Xunit;

namespace InventarioCPR.Tests;

public sealed class LoginNavigationTests
{
    [Theory]
    [InlineData("/", "/")]
    [InlineData("/inventario", "/inventario")]
    [InlineData("/inventario/expedientes/123?seccion=tecnica", "/inventario/expedientes/123?seccion=tecnica")]
    public void LocalReturnUrlIsPreserved(string returnUrl, string expected)
    {
        Assert.Equal(expected, SecurityEndpoints.NormalizeReturnUrl(returnUrl));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://example.com")]
    [InlineData("//example.com")]
    [InlineData("/\\example.com")]
    [InlineData("inventario")]
    [InlineData("/inventario\r\nX-Test: injected")]
    public void MissingOrUnsafeReturnUrlFallsBackToHome(string? returnUrl)
    {
        Assert.Equal("/", SecurityEndpoints.NormalizeReturnUrl(returnUrl));
    }
}
