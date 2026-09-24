using System.Net;
using VistoriaApi.Application.Abstractions;

namespace VistoriaApi.Infrastructure.Email;

public sealed class SmtpEmailTemplateRenderer : IEmailTemplateRenderer
{
    private const string DefaultBackgroundColor = "#F5F7F6";

    public Task<EmailContent> RenderInspectionInviteAsync(
        string recipientName,
        string publicUrl,
        DateTime expiresAtUtc,
        string? logoContentId,
        string primaryColor,
        string secondaryColor,
        CancellationToken cancellationToken)
    {
        var safeName = WebUtility.HtmlEncode(recipientName.Trim());
        var safeUrl = WebUtility.HtmlEncode(publicUrl.Trim());
        var expiration = FormatExpiration(expiresAtUtc);
        var safeExpiration = WebUtility.HtmlEncode(expiration);

        var logoHtml = logoContentId is not null
            ? $"""
               <img
                   src="cid:{logoContentId}"
                   width="150"
                   alt="Vistor.ia"
                   style="display:block;width:150px;max-width:100%;height:auto;border:0;"
               />
               """
            : """
              <span style="font-family:Arial,Helvetica,sans-serif;font-size:26px;line-height:32px;font-weight:700;color:#FFFFFF;">
                  Vistor.ia
              </span>
              """;

        var html = $"""
            <!doctype html>
            <html lang="pt-BR">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <meta name="color-scheme" content="light">
                <meta name="supported-color-schemes" content="light">
                <title>Solicitação de vistoria</title>
            </head>

            <body style="margin:0;padding:0;background-color:{DefaultBackgroundColor};font-family:Arial,Helvetica,sans-serif;color:#17211F;">
                <div style="display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;">
                    Você recebeu uma solicitação para realizar uma vistoria.
                </div>

                <table
                    role="presentation"
                    width="100%"
                    cellspacing="0"
                    cellpadding="0"
                    border="0"
                    style="width:100%;background-color:{DefaultBackgroundColor};"
                >
                    <tr>
                        <td align="center" style="padding:32px 16px;">
                            <table role="presentation" width="600" cellspacing="0" cellpadding="0" border="0" style="width:100%;max-width:600px;background-color:#FFFFFF;border-radius:8px;overflow:hidden;">
                                <tr>
                                    <td style="padding:24px;background-color:{primaryColor};">
                                        <div style="display:flex;align-items:center;gap:16px;">
                                            {logoHtml}
                                            <div style="flex:1;color:#FFFFFF;">
                                                <div style="font-size:18px;font-weight:700;">Vistor.ia</div>
                                            </div>
                                        </div>
                                    </td>
                                </tr>

                                <tr>
                                    <td style="padding:24px;">
                                        <h1 style="font-size:20px;margin:0 0 8px 0;">Olá, {safeName}</h1>
                                        <p style="margin:0 0 16px 0;color:#44524A;">Você recebeu uma solicitação para realizar uma vistoria. Clique no botão abaixo para acessar:</p>
                                        <p style="text-align:center;margin:24px 0;">
                                            <a href="{safeUrl}" style="background-color:{secondaryColor};color:#FFFFFF;padding:12px 20px;border-radius:6px;text-decoration:none;display:inline-block;">Acessar vistoria</a>
                                        </p>
                                        <p style="margin:0 0 8px 0;color:#44524A;">O link expira em: {safeExpiration}</p>
                                    </td>
                                </tr>

                                <tr>
                                    <td style="padding:16px;background-color:#F0F2F1;color:#6B796E;font-size:12px;">
                                        <p style="margin:0;">Se você não solicitou essa vistoria, ignore este e-mail.</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;

        var text = $"Você recebeu uma solicitação para realizar uma vistoria. Acesse: {safeUrl} (expira em {safeExpiration})";

        var content = new EmailContent(
            subject: "Você recebeu uma solicitação de vistoria",
            htmlBody: html,
            textBody: text,
            inlineResources: logoContentId is not null
                ? new[] { new InlineResource(logoContentId, string.Empty, "image/png") }
                : null);

        return Task.FromResult(content);
    }

    public Task<EmailContent> RenderVerificationCodeAsync(
        string recipientName,
        string code,
        string? logoContentId,
        string primaryColor,
        string secondaryColor,
        CancellationToken cancellationToken)
    {
        var safeName = WebUtility.HtmlEncode(recipientName.Trim());
        var safeCode = WebUtility.HtmlEncode(code.Trim());

        var logoHtml = logoContentId is not null
            ? $"""
               <img
                   src="cid:{logoContentId}"
                   width="150"
                   alt="Vistor.ia"
                   style="display:block;width:150px;max-width:100%;height:auto;border:0;"
               />
               """
            : """
              <span style="font-family:Arial,Helvetica,sans-serif;font-size:26px;line-height:32px;font-weight:700;color:#FFFFFF;">
                  Vistor.ia
              </span>
              """;

        var html = $"""
            <!doctype html>
            <html lang="pt-BR">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <meta name="color-scheme" content="light">
                <meta name="supported-color-schemes" content="light">
                <title>Verificação de e-mail</title>
            </head>

            <body style="margin:0;padding:0;background-color:{DefaultBackgroundColor};font-family:Arial,Helvetica,sans-serif;color:#17211F;">
                <div style="display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;">
                    Seu código de verificação de e-mail.
                </div>

                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="width:100%;background-color:{DefaultBackgroundColor};">
                    <tr>
                        <td align="center" style="padding:32px 16px;">
                            <table role="presentation" width="600" cellspacing="0" cellpadding="0" border="0" style="width:100%;max-width:600px;background-color:#FFFFFF;border-radius:8px;overflow:hidden;">
                                <tr>
                                    <td style="padding:24px;background-color:{primaryColor};">
                                        <div style="display:flex;align-items:center;gap:16px;">
                                            {logoHtml}
                                            <div style="flex:1;color:#FFFFFF;">
                                                <div style="font-size:18px;font-weight:700;">Vistor.ia</div>
                                            </div>
                                        </div>
                                    </td>
                                </tr>

                                <tr>
                                    <td style="padding:24px;text-align:center;">
                                        <h1 style="font-size:20px;margin:0 0 8px 0;">Olá, {safeName}</h1>
                                        <p style="margin:0 0 16px 0;color:#44524A;">Use o código abaixo para verificar seu e-mail:</p>
                                        <p style="font-size:28px;font-weight:700;margin:16px 0;color:{secondaryColor};">{safeCode}</p>
                                        <p style="margin:0 0 8px 0;color:#44524A;">Caso não tenha solicitado, ignore este e-mail.</p>
                                    </td>
                                </tr>

                                <tr>
                                    <td style="padding:16px;background-color:#F0F2F1;color:#6B796E;font-size:12px;">
                                        <p style="margin:0;">Este código expira em alguns minutos.</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;

        var text = $"Seu código de verificação é: {safeCode}";

        var content = new EmailContent(
            subject: "Verifique seu e-mail",
            htmlBody: html,
            textBody: text,
            inlineResources: logoContentId is not null
                ? new[] { new InlineResource(logoContentId, string.Empty, "image/png") }
                : null);

        return Task.FromResult(content);
    }

    public Task<EmailContent> RenderWelcomeAsync(
        string recipientName,
        string? logoContentId,
        string primaryColor,
        string secondaryColor,
        CancellationToken cancellationToken)
    {
        var safeName = WebUtility.HtmlEncode(recipientName.Trim());

        var logoHtml = logoContentId is not null
            ? $"""
               <img
                   src="cid:{logoContentId}"
                   width="150"
                   alt="Vistor.ia"
                   style="display:block;width:150px;max-width:100%;height:auto;border:0;"
               />
               """
            : """
              <span style="font-family:Arial,Helvetica,sans-serif;font-size:26px;line-height:32px;font-weight:700;color:#FFFFFF;">
                  Vistor.ia
              </span>
              """;

        var html = $"""
            <!doctype html>
            <html lang="pt-BR">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <meta name="color-scheme" content="light">
                <meta name="supported-color-schemes" content="light">
                <title>Bem-vindo</title>
            </head>

            <body style="margin:0;padding:0;background-color:{DefaultBackgroundColor};font-family:Arial,Helvetica,sans-serif;color:#17211F;">
                <div style="display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;">
                    Bem-vindo à Vistor.ia.
                </div>

                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="width:100%;background-color:{DefaultBackgroundColor};">
                    <tr>
                        <td align="center" style="padding:32px 16px;">
                            <table role="presentation" width="600" cellspacing="0" cellpadding="0" border="0" style="width:100%;max-width:600px;background-color:#FFFFFF;border-radius:8px;overflow:hidden;">
                                <tr>
                                    <td style="padding:24px;background-color:{primaryColor};">
                                        <div style="display:flex;align-items:center;gap:16px;">
                                            {logoHtml}
                                            <div style="flex:1;color:#FFFFFF;">
                                                <div style="font-size:18px;font-weight:700;">Vistor.ia</div>
                                            </div>
                                        </div>
                                    </td>
                                </tr>

                                <tr>
                                    <td style="padding:24px;">
                                        <h1 style="font-size:20px;margin:0 0 8px 0;">Olá, {safeName}</h1>
                                        <p style="margin:0 0 16px 0;color:#44524A;">Bem-vindo à Vistor.ia! Estamos felizes em tê-lo conosco.</p>
                                        <p style="margin:0 0 16px 0;color:#44524A;">Acesse sua conta e comece a criar vistorias agora mesmo.</p>
                                        <p style="text-align:center;margin:24px 0;">
                                            <a href="#" style="background-color:{secondaryColor};color:#FFFFFF;padding:12px 20px;border-radius:6px;text-decoration:none;display:inline-block;">Acessar minha conta</a>
                                        </p>
                                    </td>
                                </tr>

                                <tr>
                                    <td style="padding:16px;background-color:#F0F2F1;color:#6B796E;font-size:12px;">
                                        <p style="margin:0;">Se você não criou essa conta, ignore este e-mail.</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;

        var text = $"Bem-vindo à Vistor.ia, {safeName}!";

        var content = new EmailContent(
            subject: "Bem-vindo à Vistor.ia",
            htmlBody: html,
            textBody: text,
            inlineResources: logoContentId is not null
                ? new[] { new InlineResource(logoContentId, string.Empty, "image/png") }
                : null);

        return Task.FromResult(content);
    }

    private static string FormatExpiration(DateTime expiresAtUtc)
    {
        var utcExpiration = expiresAtUtc.Kind switch
        {
            DateTimeKind.Utc => expiresAtUtc,
            DateTimeKind.Local => expiresAtUtc.ToUniversalTime(),
            _ => DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc)
        };

        return $"{utcExpiration:dd/MM/yyyy 'às' HH:mm} UTC";
    }
}
