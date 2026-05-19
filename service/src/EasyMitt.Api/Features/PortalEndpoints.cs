using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using EasyMitt.Api.Responses;
using EasyMitt.Api.Security;
using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Abstractions.Portal;
using EasyMitt.Application.Abstractions.Transformation;
using EasyMitt.Application.Dtos.En16931;
using EasyMitt.Application.Dtos.Portal;
using EasyMitt.Application.Localization;
using EasyMitt.Domain.Billing;
using EasyMitt.Domain.Quotes;
using EasyMitt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EasyMitt.Api.Features;

public static class PortalEndpoints
{
    public static void MapPortalEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/v1/customers").WithTags("CustomerPortalAccess");
        admin.MapGet("/{customerId:guid}/portal-access", ListAccessAsync)
            .RequireAuthorization(AuthorizationPolicies.InvoiceRead);
        admin.MapPost("/{customerId:guid}/portal-access", IssueAccessAsync)
            .RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        admin.MapPost("/portal-access/{tokenId:guid}/revoke", RevokeAccessAsync)
            .RequireAuthorization(AuthorizationPolicies.InvoiceWrite);

        var portal = app.MapGroup("/api/v1/portal").WithTags("CustomerPortal");
        portal.MapGet("/me", GetSessionAsync).AllowAnonymous();
        portal.MapGet("/invoices", ListInvoicesAsync).AllowAnonymous();
        portal.MapGet("/invoices/{invoiceId:guid}", GetInvoiceAsync).AllowAnonymous();
        portal.MapGet("/invoices/{invoiceId:guid}/zugferd.pdf", DownloadInvoicePdfAsync).AllowAnonymous();
        portal.MapGet("/invoices/{invoiceId:guid}/xrechnung.xml", DownloadInvoiceXmlAsync).AllowAnonymous();
        portal.MapGet("/quotes", ListQuotesAsync).AllowAnonymous();
        portal.MapGet("/quotes/{quoteId:guid}", GetQuoteAsync).AllowAnonymous();
        portal.MapPost("/quotes/{quoteId:guid}/accept", AcceptQuoteAsync).AllowAnonymous();
        portal.MapPost("/quotes/{quoteId:guid}/decline", DeclineQuoteAsync).AllowAnonymous();
    }

    // ---- Admin: manage portal tokens ----

    private static async Task<IResult> ListAccessAsync(
        Guid customerId,
        ClaimsPrincipal user,
        ICustomerPortalAccessRepository repository,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var records = await repository.ListForCustomerAsync(user.CompanyId(), customerId, cancellationToken);
        return Results.Ok(responseFactory.Success(
            httpContext,
            localizer.Get(MessageKeys.PortalTokensFound),
            records.Select(ToTokenDto).ToArray()));
    }

    private static async Task<IResult> IssueAccessAsync(
        Guid customerId,
        PortalAccessIssueRequestDto body,
        ClaimsPrincipal user,
        ICustomerRepository customerRepository,
        ICustomerPortalAccessRepository repository,
        IPortalTokenGenerator tokenGenerator,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetAsync(user.CompanyId(), customerId, cancellationToken);
        if (customer is null)
            return Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.CustomerNotFound)));

        var generated = tokenGenerator.Generate();
        DateTime? expiresAt = body.ValidityDays is > 0
            ? DateTime.UtcNow.AddDays(Math.Min(body.ValidityDays.Value, 3650))
            : null;
        var label = string.IsNullOrWhiteSpace(body.Label) ? customer.DisplayName : body.Label!.Trim();
        var record = await repository.CreateAsync(
            user.CompanyId(),
            customerId,
            label,
            generated.TokenHash,
            generated.TokenPrefix,
            expiresAt,
            user.FindFirstValue(ClaimTypes.Email) ?? "",
            cancellationToken);

        var portalUrl = BuildPortalUrl(httpContext, generated.Token);
        var data = new PortalAccessTokenIssuedDto
        {
            Access = ToTokenDto(record),
            Token = generated.Token,
            PortalUrl = portalUrl,
        };

        return Results.Ok(responseFactory.Success(
            httpContext,
            localizer.Get(MessageKeys.PortalTokenIssued),
            data));
    }

    private static async Task<IResult> RevokeAccessAsync(
        Guid tokenId,
        ClaimsPrincipal user,
        ICustomerPortalAccessRepository repository,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var revoked = await repository.RevokeAsync(user.CompanyId(), tokenId, cancellationToken);
        return revoked
            ? Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.PortalTokenRevoked), new { id = tokenId }))
            : Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.PortalTokenNotFound)));
    }

    // ---- Public portal endpoints ----

    private static async Task<IResult> GetSessionAsync(
        HttpContext httpContext,
        ICustomerPortalAccessRepository portalRepository,
        EasyMittDbContext db,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolvePortalAsync(httpContext, portalRepository, cancellationToken);
        if (resolved is null) return PortalUnauthorized(httpContext, responseFactory, localizer);

        var customer = await db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == resolved.CustomerId && x.CompanyId == resolved.CompanyId, cancellationToken);
        var company = await db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == resolved.CompanyId, cancellationToken);

        var data = new PortalSessionDto
        {
            CustomerId = resolved.CustomerId,
            CustomerDisplayName = customer?.DisplayName ?? "",
            CompanyName = company?.Name ?? "",
            TokenLabel = resolved.Label,
            ExpiresAtUtc = resolved.ExpiresAtUtc,
            LastUsedAtUtc = resolved.LastUsedAtUtc,
        };
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.PortalSessionFound), data));
    }

    private static async Task<IResult> ListInvoicesAsync(
        HttpContext httpContext,
        ICustomerPortalAccessRepository portalRepository,
        EasyMittDbContext db,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolvePortalAsync(httpContext, portalRepository, cancellationToken);
        if (resolved is null) return PortalUnauthorized(httpContext, responseFactory, localizer);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var invoices = await db.InvoiceDrafts.AsNoTracking()
            .Where(x => x.CompanyId == resolved.CompanyId && x.CustomerId == resolved.CustomerId
                && x.Status != InvoiceLifecycleStatus.Draft
                && x.Status != InvoiceLifecycleStatus.Cancelled)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(500)
            .ToListAsync(cancellationToken);

        var ids = invoices.Select(x => x.Id).ToArray();
        var paid = await db.PaymentAllocations.AsNoTracking()
            .Where(x => x.CompanyId == resolved.CompanyId && ids.Contains(x.InvoiceDraftId))
            .GroupBy(x => x.InvoiceDraftId)
            .Select(x => new { x.Key, Sum = x.Sum(y => y.Amount) })
            .ToDictionaryAsync(x => x.Key, x => x.Sum, cancellationToken);

        var items = invoices.Select(x => BuildInvoiceSummary(x, paid, today)).ToArray();
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.PortalInvoicesFound), items));
    }

    private static async Task<IResult> GetInvoiceAsync(
        Guid invoiceId,
        HttpContext httpContext,
        ICustomerPortalAccessRepository portalRepository,
        EasyMittDbContext db,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolvePortalAsync(httpContext, portalRepository, cancellationToken);
        if (resolved is null) return PortalUnauthorized(httpContext, responseFactory, localizer);

        var invoice = await db.InvoiceDrafts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == invoiceId && x.CompanyId == resolved.CompanyId && x.CustomerId == resolved.CustomerId, cancellationToken);
        if (invoice is null || invoice.Status == InvoiceLifecycleStatus.Draft || invoice.Status == InvoiceLifecycleStatus.Cancelled)
            return Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.PortalInvoiceNotFound)));

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var paidByInvoice = await db.PaymentAllocations.AsNoTracking()
            .Where(x => x.CompanyId == resolved.CompanyId && x.InvoiceDraftId == invoiceId)
            .GroupBy(x => x.InvoiceDraftId)
            .Select(x => new { x.Key, Sum = x.Sum(y => y.Amount) })
            .ToDictionaryAsync(x => x.Key, x => x.Sum, cancellationToken);

        var payments = await db.PaymentAllocations.AsNoTracking()
            .Where(x => x.CompanyId == resolved.CompanyId && x.InvoiceDraftId == invoiceId)
            .Join(db.BankTransactions, a => a.BankTransactionId, b => b.Id, (a, b) => new PortalPaymentDto
            {
                BookingDate = b.BookingDate,
                Amount = a.Amount,
                CurrencyCode = b.CurrencyCode,
                Description = b.Description,
            })
            .OrderBy(x => x.BookingDate)
            .ToListAsync(cancellationToken);

        var detail = new PortalInvoiceDetailDto
        {
            Summary = BuildInvoiceSummary(invoice, paidByInvoice, today),
            PayloadJson = invoice.PayloadJson,
            Payments = payments,
        };
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.PortalInvoiceFound), detail));
    }

    private static async Task<IResult> DownloadInvoicePdfAsync(
        Guid invoiceId,
        HttpContext httpContext,
        ICustomerPortalAccessRepository portalRepository,
        EasyMittDbContext db,
        IElectronicInvoiceGenerator generator,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolvePortalAsync(httpContext, portalRepository, cancellationToken);
        if (resolved is null) return PortalUnauthorized(httpContext, responseFactory, localizer);

        var invoice = await db.InvoiceDrafts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == invoiceId && x.CompanyId == resolved.CompanyId && x.CustomerId == resolved.CustomerId, cancellationToken);
        if (invoice is null || invoice.Status == InvoiceLifecycleStatus.Draft || invoice.Status == InvoiceLifecycleStatus.Cancelled)
            return Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.PortalInvoiceNotFound)));

        var document = DeserializeDocument(invoice.PayloadJson);
        if (document is null)
            return Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.PortalInvoiceNotFound)));

        var bytes = await generator.GenerateZugferdPdfAsync(document, null, cancellationToken);
        var name = $"Rechnung-{Sanitize(document.Core.InvoiceNumber)}.pdf";
        return Results.File(bytes, "application/pdf", name);
    }

    private static async Task<IResult> DownloadInvoiceXmlAsync(
        Guid invoiceId,
        HttpContext httpContext,
        ICustomerPortalAccessRepository portalRepository,
        EasyMittDbContext db,
        IElectronicInvoiceGenerator generator,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolvePortalAsync(httpContext, portalRepository, cancellationToken);
        if (resolved is null) return PortalUnauthorized(httpContext, responseFactory, localizer);

        var invoice = await db.InvoiceDrafts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == invoiceId && x.CompanyId == resolved.CompanyId && x.CustomerId == resolved.CustomerId, cancellationToken);
        if (invoice is null || invoice.Status == InvoiceLifecycleStatus.Draft || invoice.Status == InvoiceLifecycleStatus.Cancelled)
            return Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.PortalInvoiceNotFound)));

        var document = DeserializeDocument(invoice.PayloadJson);
        if (document is null)
            return Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.PortalInvoiceNotFound)));

        var bytes = await generator.GenerateXRechnungXmlAsync(document, cancellationToken);
        var name = $"Rechnung-{Sanitize(document.Core.InvoiceNumber)}.xml";
        return Results.File(bytes, "application/xml", name);
    }

    private static async Task<IResult> ListQuotesAsync(
        HttpContext httpContext,
        ICustomerPortalAccessRepository portalRepository,
        EasyMittDbContext db,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolvePortalAsync(httpContext, portalRepository, cancellationToken);
        if (resolved is null) return PortalUnauthorized(httpContext, responseFactory, localizer);

        var quotes = await db.Quotes.AsNoTracking()
            .Where(x => x.CompanyId == resolved.CompanyId && x.CustomerId == resolved.CustomerId
                && x.Status != QuoteStatus.Draft)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(500)
            .ToListAsync(cancellationToken);

        var items = quotes.Select(ToQuoteSummary).ToArray();
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.PortalQuotesFound), items));
    }

    private static async Task<IResult> GetQuoteAsync(
        Guid quoteId,
        HttpContext httpContext,
        ICustomerPortalAccessRepository portalRepository,
        EasyMittDbContext db,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolvePortalAsync(httpContext, portalRepository, cancellationToken);
        if (resolved is null) return PortalUnauthorized(httpContext, responseFactory, localizer);

        var quote = await db.Quotes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == quoteId && x.CompanyId == resolved.CompanyId && x.CustomerId == resolved.CustomerId, cancellationToken);
        if (quote is null || quote.Status == QuoteStatus.Draft)
            return Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.PortalQuoteNotFound)));

        var detail = new PortalQuoteDetailDto
        {
            Summary = ToQuoteSummary(quote),
            PayloadJson = quote.PayloadJson,
        };
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.PortalQuoteFound), detail));
    }

    private static async Task<IResult> AcceptQuoteAsync(
        Guid quoteId,
        HttpContext httpContext,
        ICustomerPortalAccessRepository portalRepository,
        EasyMittDbContext db,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        CancellationToken cancellationToken) =>
        await SetQuoteStatusFromPortalAsync(quoteId, QuoteStatus.Accepted, httpContext, portalRepository, db, localizer, responseFactory, cancellationToken);

    private static async Task<IResult> DeclineQuoteAsync(
        Guid quoteId,
        HttpContext httpContext,
        ICustomerPortalAccessRepository portalRepository,
        EasyMittDbContext db,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        CancellationToken cancellationToken) =>
        await SetQuoteStatusFromPortalAsync(quoteId, QuoteStatus.Declined, httpContext, portalRepository, db, localizer, responseFactory, cancellationToken);

    private static async Task<IResult> SetQuoteStatusFromPortalAsync(
        Guid quoteId,
        string nextStatus,
        HttpContext httpContext,
        ICustomerPortalAccessRepository portalRepository,
        EasyMittDbContext db,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolvePortalAsync(httpContext, portalRepository, cancellationToken);
        if (resolved is null) return PortalUnauthorized(httpContext, responseFactory, localizer);

        var quote = await db.Quotes.FirstOrDefaultAsync(x => x.Id == quoteId && x.CompanyId == resolved.CompanyId && x.CustomerId == resolved.CustomerId, cancellationToken);
        if (quote is null || quote.Status == QuoteStatus.Draft)
            return Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.PortalQuoteNotFound)));

        if (quote.Status == QuoteStatus.Converted
            || quote.Status == QuoteStatus.Accepted
            || quote.Status == QuoteStatus.Declined)
            return Results.BadRequest(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.PortalQuoteNotResponsive)));

        var now = DateTime.UtcNow;
        quote.Status = nextStatus;
        quote.UpdatedAtUtc = now;
        if (nextStatus == QuoteStatus.Accepted) quote.AcceptedAtUtc ??= now;
        if (nextStatus == QuoteStatus.Declined) quote.DeclinedAtUtc ??= now;
        await db.SaveChangesAsync(cancellationToken);

        var messageKey = nextStatus == QuoteStatus.Accepted ? MessageKeys.PortalQuoteAccepted : MessageKeys.PortalQuoteDeclined;
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(messageKey), ToQuoteSummary(quote)));
    }

    // ---- helpers ----

    private static async Task<PortalAccessRecord?> ResolvePortalAsync(
        HttpContext httpContext,
        ICustomerPortalAccessRepository repository,
        CancellationToken cancellationToken)
    {
        var token = ReadPortalToken(httpContext);
        if (string.IsNullOrWhiteSpace(token)) return null;

        var generator = httpContext.RequestServices.GetRequiredService<IPortalTokenGenerator>();
        var hash = generator.HashToken(token);
        var record = await repository.FindActiveByTokenHashAsync(hash, DateTime.UtcNow, cancellationToken);
        if (record is null) return null;
        await repository.TouchUsageAsync(record.Id, DateTime.UtcNow, cancellationToken);
        return record;
    }

    private static string? ReadPortalToken(HttpContext httpContext)
    {
        var header = httpContext.Request.Headers["X-Portal-Token"].ToString();
        if (!string.IsNullOrWhiteSpace(header)) return header.Trim();
        var auth = httpContext.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(auth) && auth.StartsWith("Portal ", StringComparison.OrdinalIgnoreCase))
            return auth["Portal ".Length..].Trim();
        var query = httpContext.Request.Query["token"].ToString();
        if (!string.IsNullOrWhiteSpace(query)) return query.Trim();
        return null;
    }

    private static IResult PortalUnauthorized(HttpContext httpContext, ApiResponseFactory responseFactory, IAppLocalizer localizer) =>
        Results.Json(
            responseFactory.Failure(httpContext, localizer.Get(MessageKeys.PortalInvalidToken)),
            statusCode: StatusCodes.Status401Unauthorized);

    private static string BuildPortalUrl(HttpContext httpContext, string token)
    {
        var origin = httpContext.Request.Headers.Origin.ToString();
        if (string.IsNullOrWhiteSpace(origin))
        {
            var referer = httpContext.Request.Headers.Referer.ToString();
            if (!string.IsNullOrWhiteSpace(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var uri))
                origin = $"{uri.Scheme}://{uri.Authority}";
        }
        if (string.IsNullOrWhiteSpace(origin))
            origin = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
        return $"{origin.TrimEnd('/')}/portal?token={Uri.EscapeDataString(token)}";
    }

    private static PortalAccessTokenDto ToTokenDto(PortalAccessRecord record) => new()
    {
        Id = record.Id,
        CustomerId = record.CustomerId,
        Label = record.Label,
        TokenPrefix = record.TokenPrefix,
        Status = record.Status,
        ExpiresAtUtc = record.ExpiresAtUtc,
        CreatedAtUtc = record.CreatedAtUtc,
        CreatedByUserEmail = record.CreatedByUserEmail,
        LastUsedAtUtc = record.LastUsedAtUtc,
        RevokedAtUtc = record.RevokedAtUtc,
    };

    private static PortalInvoiceListItemDto BuildInvoiceSummary(
        EasyMitt.Infrastructure.Persistence.Entities.InvoiceDraftEntity invoice,
        IReadOnlyDictionary<Guid, decimal> paidByInvoice,
        DateOnly today)
    {
        var document = DeserializeDocument(invoice.PayloadJson);
        var total = document?.Core.InvoiceTotalVatIncluded ?? 0m;
        var paid = paidByInvoice.GetValueOrDefault(invoice.Id);
        var open = total - paid;
        if (open < 0) open = 0;
        var dueDate = ParseDueDate(invoice.PayloadJson) ?? DateOnly.FromDateTime(invoice.CreatedAtUtc).AddDays(14);
        var isOverdue = invoice.Status != InvoiceLifecycleStatus.Paid
            && invoice.Status != InvoiceLifecycleStatus.Cancelled
            && open > 0
            && dueDate < today;
        return new PortalInvoiceListItemDto
        {
            Id = invoice.Id,
            InvoiceNumber = document?.Core.InvoiceNumber ?? invoice.Id.ToString(),
            IssueDate = document?.Core.IssueDate,
            TotalAmount = total,
            AmountPaid = paid,
            AmountOpen = open,
            Status = invoice.Status,
            IsOverdue = isOverdue,
            IssuedAtUtc = invoice.IssuedAtUtc,
            PaidAtUtc = invoice.PaidAtUtc,
        };
    }

    private static PortalQuoteListItemDto ToQuoteSummary(EasyMitt.Infrastructure.Persistence.Entities.QuoteEntity quote) => new()
    {
        Id = quote.Id,
        QuoteNumber = quote.QuoteNumber,
        TotalAmount = quote.TotalAmount,
        Status = quote.Status,
        ValidUntilUtc = quote.ValidUntilUtc,
        CreatedAtUtc = quote.CreatedAtUtc,
        SentAtUtc = quote.SentAtUtc,
        AcceptedAtUtc = quote.AcceptedAtUtc,
        DeclinedAtUtc = quote.DeclinedAtUtc,
    };

    private static InvoiceDocumentDto? DeserializeDocument(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson)) return null;
        try
        {
            return JsonSerializer.Deserialize<InvoiceDocumentDto>(payloadJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static DateOnly? ParseDueDate(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            if (doc.RootElement.TryGetProperty("core", out var core) && core.TryGetProperty("BT-9", out var bt9))
            {
                var text = bt9.GetString();
                if (DateOnly.TryParse(text, CultureInfo.InvariantCulture, out var due))
                    return due;
            }
        }
        catch (JsonException) { }
        return null;
    }

    private static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "invoice";
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '-' && chars[i] != '_')
                chars[i] = '_';
        }
        return new string(chars);
    }
}
