using System.Globalization;
using System.Security.Claims;
using EasyMitt.Api.Responses;
using EasyMitt.Api.Security;
using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.Payments;
using EasyMitt.Application.Localization;
using FluentValidation;

namespace EasyMitt.Api.Features;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/payments").WithTags("Payments");
        group.MapGet("/transactions", SearchTransactionsAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
        group.MapGet("/transactions/{id:guid}", GetTransactionAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
        group.MapPost("/transactions", CreateTransactionAsync).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPost("/transactions/import/csv", ImportCsvAsync).DisableAntiforgery().RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapGet("/transactions/{id:guid}/suggestions", SuggestAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
        group.MapPost("/allocations", AllocateAsync).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapGet("/invoices/{invoiceId:guid}/summary", InvoiceSummaryAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
    }

    private static async Task<IResult> SearchTransactionsAsync(string? q, string? status, ClaimsPrincipal user, IPaymentRepository repository, ApiResponseFactory responseFactory, IAppLocalizer localizer, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var data = await repository.SearchTransactionsAsync(user.CompanyId(), q, status, cancellationToken);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.PaymentsFound), data));
    }

    private static async Task<IResult> GetTransactionAsync(Guid id, ClaimsPrincipal user, IPaymentRepository repository, ApiResponseFactory responseFactory, IAppLocalizer localizer, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var data = await repository.GetTransactionAsync(user.CompanyId(), id, cancellationToken);
        return data is null
            ? Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.PaymentNotFound)))
            : Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.PaymentsFound), data));
    }

    private static async Task<IResult> CreateTransactionAsync(BankTransactionCreateDto body, ClaimsPrincipal user, IValidator<BankTransactionCreateDto> validator, IPaymentRepository repository, ApiResponseFactory responseFactory, IAppLocalizer localizer, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(body, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.BadRequest(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.ValidationFailed), EndpointHelpers.ToErrorDictionary(validation.Errors, localizer)));
        }

        var data = await repository.CreateTransactionAsync(user.CompanyId(), body, cancellationToken);
        return Results.Created($"/api/v1/payments/transactions/{data.Id}", responseFactory.Success(httpContext, localizer.Get(MessageKeys.PaymentSaved), data));
    }

    private static async Task<IResult> ImportCsvAsync(IFormFile? file, ClaimsPrincipal user, IPaymentRepository repository, ApiResponseFactory responseFactory, IAppLocalizer localizer, HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.ValidationFailed)));
        }

        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        var items = ParseCsv(await reader.ReadToEndAsync(cancellationToken));
        if (items.Count == 0)
        {
            return Results.BadRequest(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.ValidationFailed)));
        }

        var data = await repository.ImportTransactionsAsync(user.CompanyId(), items, cancellationToken);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.PaymentImportCompleted), data));
    }

    private static async Task<IResult> SuggestAsync(Guid id, ClaimsPrincipal user, IPaymentRepository repository, ApiResponseFactory responseFactory, IAppLocalizer localizer, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var data = await repository.SuggestInvoicesAsync(user.CompanyId(), id, cancellationToken);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.PaymentsFound), data));
    }

    private static async Task<IResult> AllocateAsync(PaymentAllocationCreateDto body, ClaimsPrincipal user, IValidator<PaymentAllocationCreateDto> validator, IPaymentRepository repository, ApiResponseFactory responseFactory, IAppLocalizer localizer, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(body, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.BadRequest(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.ValidationFailed), EndpointHelpers.ToErrorDictionary(validation.Errors, localizer)));
        }

        try
        {
            var data = await repository.AllocateAsync(user.CompanyId(), body, cancellationToken);
            return data is null
                ? Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.PaymentNotFound)))
                : Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.PaymentAllocated), data));
        }
        catch (InvalidOperationException)
        {
            return Results.BadRequest(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.ValidationFailed)));
        }
    }

    private static async Task<IResult> InvoiceSummaryAsync(Guid invoiceId, ClaimsPrincipal user, IPaymentRepository repository, ApiResponseFactory responseFactory, IAppLocalizer localizer, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var data = await repository.GetInvoiceSummaryAsync(user.CompanyId(), invoiceId, cancellationToken);
        return data is null
            ? Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.InvoiceDraftNotFound)))
            : Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.PaymentsFound), data));
    }

    private static IReadOnlyList<BankTransactionCreateDto> ParseCsv(string content)
    {
        var rows = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (rows.Length == 0)
        {
            return Array.Empty<BankTransactionCreateDto>();
        }

        var result = new List<BankTransactionCreateDto>();
        foreach (var row in rows.Skip(HasHeader(rows[0]) ? 1 : 0))
        {
            var delimiter = row.Count(x => x == ';') >= row.Count(x => x == ',') ? ';' : ',';
            var columns = row.Split(delimiter).Select(x => x.Trim().Trim('"')).ToArray();
            if (columns.Length < 3)
            {
                continue;
            }

            if (!DateOnly.TryParse(columns[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) &&
                !DateOnly.TryParse(columns[0], CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.None, out date))
            {
                continue;
            }

            var amountText = columns.Length >= 5 ? columns[4] : columns[2];
            amountText = amountText.Replace(".", "").Replace(',', '.');
            if (!decimal.TryParse(amountText, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var amount))
            {
                continue;
            }

            result.Add(new BankTransactionCreateDto
            {
                BookingDate = date,
                Description = columns.ElementAtOrDefault(1) ?? "Bank transaction",
                CounterpartyName = columns.ElementAtOrDefault(2),
                CounterpartyIban = columns.ElementAtOrDefault(3),
                Amount = amount,
                CurrencyCode = columns.ElementAtOrDefault(5) is { Length: 3 } currency ? currency : "EUR",
                Source = "CsvImport",
            });
        }

        return result;
    }

    private static bool HasHeader(string row) =>
        row.Contains("date", StringComparison.OrdinalIgnoreCase) ||
        row.Contains("datum", StringComparison.OrdinalIgnoreCase) ||
        row.Contains("booking", StringComparison.OrdinalIgnoreCase);
}
