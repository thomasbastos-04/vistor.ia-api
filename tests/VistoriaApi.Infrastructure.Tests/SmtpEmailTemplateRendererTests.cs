using System.Threading;
using System.Threading.Tasks;
using VistoriaApi.Infrastructure.Email;
using Xunit;

namespace VistoriaApi.Infrastructure.Tests;

public class SmtpEmailTemplateRendererTests
{
    [Fact]
    public async Task RenderInspectionInviteAsync_ReturnsContent()
    {
        var renderer = new SmtpEmailTemplateRenderer();

        var content = await renderer.RenderInspectionInviteAsync(
            "João Silva",
            "https://example.com/inspect/abc",
            System.DateTime.UtcNow.AddDays(2),
            logoContentId: null,
            primaryColor: "#123456",
            secondaryColor: "#654321",
            CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(content.Subject));
        Assert.False(string.IsNullOrWhiteSpace(content.HtmlBody));
        Assert.False(string.IsNullOrWhiteSpace(content.TextBody));
    }

    [Fact]
    public async Task RenderVerificationCodeAsync_ReturnsContent()
    {
        var renderer = new SmtpEmailTemplateRenderer();

        var content = await renderer.RenderVerificationCodeAsync(
            "João Silva",
            "123456",
            logoContentId: null,
            primaryColor: "#123456",
            secondaryColor: "#654321",
            CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(content.Subject));
        Assert.False(string.IsNullOrWhiteSpace(content.HtmlBody));
        Assert.False(string.IsNullOrWhiteSpace(content.TextBody));
    }

    [Fact]
    public async Task RenderWelcomeAsync_ReturnsContent()
    {
        var renderer = new SmtpEmailTemplateRenderer();

        var content = await renderer.RenderWelcomeAsync(
            "João Silva",
            logoContentId: null,
            primaryColor: "#123456",
            secondaryColor: "#654321",
            CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(content.Subject));
        Assert.False(string.IsNullOrWhiteSpace(content.HtmlBody));
        Assert.False(string.IsNullOrWhiteSpace(content.TextBody));
    }
}
