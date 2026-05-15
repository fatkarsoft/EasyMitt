using System.Text.Json;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.En16931;
using EasyMitt.Application.Dtos.Quotes;
using EasyMitt.Domain.Quotes;
using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyMitt.Infrastructure.Persistence.Repositories;

public sealed class QuoteRepository(EasyMittDbContext db) : IQuoteRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public async Task<IReadOnlyList<QuoteDto>> SearchAsync(Guid companyId, string? query, string? status, CancellationToken cancellationToken)
    {
        var normalized = query?.Trim().ToLowerInvariant();
        var rows = db.Quotes.AsNoTracking().Where(x => x.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(status) && QuoteStatus.IsKnown(status))
        {
            rows = rows.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(normalized))
        {
            rows = rows.Where(x =>
                x.QuoteNumber.ToLower().Contains(normalized) ||
                x.PayloadJson.ToLower().Contains(normalized));
        }

        var entities = await rows.OrderByDescending(x => x.CreatedAtUtc).Take(200).ToListAsync(cancellationToken);
        return entities.Select(ToDto).ToArray();
    }

    public async Task<QuoteDto?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Quotes.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<QuoteDto> CreateAsync(Guid companyId, QuoteUpsertDto request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var entity = new QuoteEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Status = QuoteStatus.Draft,
        };

        Apply(entity, request, now);
        db.Quotes.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<QuoteDto?> UpdateAsync(Guid companyId, Guid id, QuoteUpsertDto request, CancellationToken cancellationToken)
    {
        var entity = await db.Quotes.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (entity.Status is QuoteStatus.Converted)
        {
            throw new InvalidOperationException("quote_converted_cannot_update");
        }

        Apply(entity, request, DateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<QuoteDto?> UpdateStatusAsync(Guid companyId, Guid id, string status, DateTime nowUtc, Guid? convertedInvoiceDraftId, CancellationToken cancellationToken)
    {
        var entity = await db.Quotes.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (!QuoteStatus.IsKnown(status))
        {
            throw new InvalidOperationException("quote_status_unknown");
        }

        entity.Status = status;
        entity.UpdatedAtUtc = nowUtc;

        switch (status)
        {
            case QuoteStatus.Sent:
                entity.SentAtUtc ??= nowUtc;
                break;
            case QuoteStatus.Accepted:
                entity.AcceptedAtUtc ??= nowUtc;
                break;
            case QuoteStatus.Declined:
                entity.DeclinedAtUtc ??= nowUtc;
                break;
            case QuoteStatus.Converted:
                entity.ConvertedAtUtc ??= nowUtc;
                entity.ConvertedInvoiceDraftId = convertedInvoiceDraftId;
                break;
        }

        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private static void Apply(QuoteEntity entity, QuoteUpsertDto request, DateTime nowUtc)
    {
        entity.CustomerId = request.CustomerId;
        entity.LineProductIdsJson = JsonSerializer.Serialize(request.ProductIds);
        entity.PayloadJson = JsonSerializer.Serialize(request.Document, JsonOptions);
        entity.QuoteNumber = string.IsNullOrWhiteSpace(request.Document.Core.InvoiceNumber)
            ? $"ANG-{nowUtc:yyyyMMddHHmmss}"
            : request.Document.Core.InvoiceNumber.Trim();
        entity.TotalAmount = request.Document.Core.InvoiceTotalVatIncluded;
        entity.ValidUntilUtc = request.ValidUntilUtc ?? nowUtc.AddDays(14);
        entity.UpdatedAtUtc = nowUtc;
    }

    private static QuoteDto ToDto(QuoteEntity entity) => new()
    {
        Id = entity.Id,
        CustomerId = entity.CustomerId,
        ProductIds = DeserializeProductIds(entity.LineProductIdsJson),
        QuoteNumber = entity.QuoteNumber,
        Status = entity.Status,
        TotalAmount = entity.TotalAmount,
        ValidUntilUtc = entity.ValidUntilUtc,
        CreatedAtUtc = entity.CreatedAtUtc,
        UpdatedAtUtc = entity.UpdatedAtUtc,
        SentAtUtc = entity.SentAtUtc,
        AcceptedAtUtc = entity.AcceptedAtUtc,
        DeclinedAtUtc = entity.DeclinedAtUtc,
        ConvertedAtUtc = entity.ConvertedAtUtc,
        ConvertedInvoiceDraftId = entity.ConvertedInvoiceDraftId,
        Document = DeserializeDocument(entity.PayloadJson),
    };

    private static InvoiceDocumentDto DeserializeDocument(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<InvoiceDocumentDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new InvoiceDocumentDto();
        }
        catch (JsonException)
        {
            return new InvoiceDocumentDto();
        }
    }

    private static IReadOnlyList<Guid?> DeserializeProductIds(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Guid?[]>(json) ?? Array.Empty<Guid?>();
        }
        catch (JsonException)
        {
            return Array.Empty<Guid?>();
        }
    }
}
