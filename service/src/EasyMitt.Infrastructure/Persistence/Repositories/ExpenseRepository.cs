using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.Expenses;
using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyMitt.Infrastructure.Persistence.Repositories;

public sealed class ExpenseRepository(EasyMittDbContext db) : IExpenseRepository
{
    public async Task<IReadOnlyList<ExpenseDto>> SearchAsync(Guid companyId, string? query, string? status, CancellationToken cancellationToken)
    {
        var normalized = query?.Trim().ToLowerInvariant();
        var rows = db.Expenses.AsNoTracking().Where(x => x.CompanyId == companyId);
        if (!string.IsNullOrWhiteSpace(status))
        {
            rows = rows.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(normalized))
        {
            rows = rows.Where(x =>
                x.VendorName.ToLower().Contains(normalized) ||
                (x.DocumentNumber != null && x.DocumentNumber.ToLower().Contains(normalized)) ||
                x.Category.ToLower().Contains(normalized));
        }

        return await rows.OrderByDescending(x => x.CreatedAtUtc).Take(200).Select(x => ToDto(x)).ToArrayAsync(cancellationToken);
    }

    public async Task<ExpenseDto?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken) =>
        await db.Expenses.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.Id == id)
            .Select(x => ToDto(x))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ExpenseDto> CreateAsync(Guid companyId, ExpenseUpsertDto request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var entity = new ExpenseEntity { Id = Guid.NewGuid(), CompanyId = companyId, CreatedAtUtc = now, UpdatedAtUtc = now };
        Apply(entity, request);
        db.Expenses.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<ExpenseDto?> UpdateAsync(Guid companyId, Guid id, ExpenseUpsertDto request, CancellationToken cancellationToken)
    {
        var entity = await db.Expenses.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
        if (entity is null) return null;
        Apply(entity, request);
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<ExpenseDto?> UpdateStatusAsync(Guid companyId, Guid id, string status, CancellationToken cancellationToken)
    {
        var entity = await db.Expenses.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
        if (entity is null) return null;
        entity.Status = status is "Booked" or "Archived" ? status : "Inbox";
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private static void Apply(ExpenseEntity entity, ExpenseUpsertDto request)
    {
        entity.VendorName = request.VendorName.Trim();
        entity.DocumentNumber = TrimOrNull(request.DocumentNumber);
        entity.IssueDate = request.IssueDate;
        entity.Category = string.IsNullOrWhiteSpace(request.Category) ? "General" : request.Category.Trim();
        entity.NetAmount = Math.Max(0, request.NetAmount);
        entity.TaxAmount = Math.Max(0, request.TaxAmount);
        entity.TotalAmount = Math.Max(0, request.TotalAmount);
        entity.CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "EUR" : request.CurrencyCode.Trim().ToUpperInvariant();
        entity.DatevCreditorAccount = TrimOrNull(request.DatevCreditorAccount);
        entity.Notes = TrimOrNull(request.Notes);
    }

    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ExpenseDto ToDto(ExpenseEntity x) => new()
    {
        Id = x.Id,
        VendorName = x.VendorName,
        DocumentNumber = x.DocumentNumber,
        IssueDate = x.IssueDate,
        Category = x.Category,
        NetAmount = x.NetAmount,
        TaxAmount = x.TaxAmount,
        TotalAmount = x.TotalAmount,
        CurrencyCode = x.CurrencyCode,
        DatevCreditorAccount = x.DatevCreditorAccount,
        Status = x.Status,
        Notes = x.Notes,
        CreatedAtUtc = x.CreatedAtUtc,
        UpdatedAtUtc = x.UpdatedAtUtc,
    };
}
