using System.Security.Claims;
using EasyMitt.Api.Responses;
using EasyMitt.Api.Security;
using EasyMitt.Application.Abstractions.Ai;
using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Abstractions.Transformation;
using EasyMitt.Application.Dtos.Expenses;
using EasyMitt.Application.Dtos.Ingestion;
using EasyMitt.Application.Exceptions;
using EasyMitt.Application.Localization;
using FluentValidation;

namespace EasyMitt.Api.Features;

public static class ExpenseEndpoints
{
    public static void MapExpenseEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/expenses").WithTags("Expenses");
        group.MapGet("/", SearchAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
        group.MapGet("/{id:guid}", GetAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
        group.MapPost("/", CreateAsync).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPut("/{id:guid}", UpdateAsync).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPost("/{id:guid}/book", (Guid id, ClaimsPrincipal user, IExpenseRepository repository, ApiResponseFactory responseFactory, IAppLocalizer localizer, HttpContext httpContext, CancellationToken cancellationToken) =>
            UpdateStatusAsync(id, "Booked", user, repository, responseFactory, localizer, httpContext, cancellationToken)).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPost("/{id:guid}/archive", (Guid id, ClaimsPrincipal user, IExpenseRepository repository, ApiResponseFactory responseFactory, IAppLocalizer localizer, HttpContext httpContext, CancellationToken cancellationToken) =>
            UpdateStatusAsync(id, "Archived", user, repository, responseFactory, localizer, httpContext, cancellationToken)).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPost("/scan", ScanAsync).DisableAntiforgery().RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
    }

    private static async Task<IResult> SearchAsync(string? q, string? status, ClaimsPrincipal user, IExpenseRepository repository, ApiResponseFactory responseFactory, IAppLocalizer localizer, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var data = await repository.SearchAsync(user.CompanyId(), q, status, cancellationToken);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.ExpensesFound), data));
    }

    private static async Task<IResult> GetAsync(Guid id, ClaimsPrincipal user, IExpenseRepository repository, ApiResponseFactory responseFactory, IAppLocalizer localizer, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var data = await repository.GetAsync(user.CompanyId(), id, cancellationToken);
        return data is null
            ? Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.ExpenseNotFound)))
            : Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.ExpensesFound), data));
    }

    private static async Task<IResult> CreateAsync(ExpenseUpsertDto body, ClaimsPrincipal user, IValidator<ExpenseUpsertDto> validator, IExpenseRepository repository, ApiResponseFactory responseFactory, IAppLocalizer localizer, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(body, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.BadRequest(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.ValidationFailed), EndpointHelpers.ToErrorDictionary(validation.Errors, localizer)));
        }

        var data = await repository.CreateAsync(user.CompanyId(), body, cancellationToken);
        return Results.Created($"/api/v1/expenses/{data.Id}", responseFactory.Success(httpContext, localizer.Get(MessageKeys.ExpenseSaved), data));
    }

    private static async Task<IResult> UpdateAsync(Guid id, ExpenseUpsertDto body, ClaimsPrincipal user, IValidator<ExpenseUpsertDto> validator, IExpenseRepository repository, ApiResponseFactory responseFactory, IAppLocalizer localizer, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(body, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.BadRequest(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.ValidationFailed), EndpointHelpers.ToErrorDictionary(validation.Errors, localizer)));
        }

        var data = await repository.UpdateAsync(user.CompanyId(), id, body, cancellationToken);
        return data is null
            ? Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.ExpenseNotFound)))
            : Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.ExpenseSaved), data));
    }

    private static async Task<IResult> UpdateStatusAsync(Guid id, string status, ClaimsPrincipal user, IExpenseRepository repository, ApiResponseFactory responseFactory, IAppLocalizer localizer, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var data = await repository.UpdateStatusAsync(user.CompanyId(), id, status, cancellationToken);
        return data is null
            ? Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.ExpenseNotFound)))
            : Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.ExpenseStatusUpdated), data));
    }

    private static async Task<IResult> ScanAsync(IFormFile? file, IScannedInvoiceImportAnalyzer analyzer, IExpenseCategorySuggester categorySuggester, ApiResponseFactory responseFactory, IAppLocalizer localizer, HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.InvoiceScanFileRequired)));
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var raw = await analyzer.AnalyzeAsync(stream, file.FileName, file.ContentType, cancellationToken);
            var suggestion = categorySuggester.SuggestFromScan(raw);
            var expense = MapExpense(raw);
            var enriched = new ExpenseUpsertDto
            {
                VendorName = expense.VendorName,
                DocumentNumber = expense.DocumentNumber,
                IssueDate = expense.IssueDate,
                Category = suggestion.Confidence >= 0.6m ? suggestion.Category : expense.Category,
                NetAmount = expense.NetAmount,
                TaxAmount = expense.TaxAmount,
                TotalAmount = expense.TotalAmount,
                CurrencyCode = expense.CurrencyCode,
                DatevCreditorAccount = expense.DatevCreditorAccount,
                Notes = expense.Notes,
            };
            return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.ExpenseScanMapped), new { raw, expense = enriched, suggestion }));
        }
        catch (NotSupportedException)
        {
            return Results.BadRequest(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.InvoiceScanUnsupportedFile)));
        }
        catch (InvoiceScanAnalysisException)
        {
            return Results.BadRequest(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.InvoiceScanFailed)));
        }
        catch (InvalidOperationException)
        {
            return Results.BadRequest(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.InvoiceScanFailed)));
        }
    }

    private static ExpenseUpsertDto MapExpense(RawInvoiceImportDto raw)
    {
        var total = raw.TotalAmount ?? raw.LineHints.Sum(x => x.Amount ?? 0);
        var tax = raw.LineHints.Sum(line => ((line.Amount ?? 0) * (line.VatRatePercent ?? 0)) / (100 + (line.VatRatePercent ?? 0)));
        var net = Math.Max(0, total - tax);

        return new ExpenseUpsertDto
        {
            VendorName = raw.MerchantOrSellerHint ?? "",
            DocumentNumber = raw.BuyerReferenceHint,
            IssueDate = raw.IssueDateHint ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Category = "General",
            NetAmount = decimal.Round(net, 2),
            TaxAmount = decimal.Round(tax, 2),
            TotalAmount = decimal.Round(total, 2),
            CurrencyCode = raw.CurrencyHint ?? "EUR",
            Notes = raw.LineHints.Count > 0 ? string.Join("; ", raw.LineHints.Select(x => x.Description).Where(x => !string.IsNullOrWhiteSpace(x))) : null,
        };
    }
}
