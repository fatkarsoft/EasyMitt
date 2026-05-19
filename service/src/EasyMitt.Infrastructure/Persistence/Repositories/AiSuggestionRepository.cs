using System.Text.Json;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.Ai;
using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyMitt.Infrastructure.Persistence.Repositories;

public sealed class AiSuggestionRepository(EasyMittDbContext db) : IAiSuggestionRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AiSuggestionDto> RecordAsync(
        Guid companyId,
        AiSuggestionCreateRequest request,
        string status,
        string? decidedByUserEmail,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var entity = new AiSuggestionEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            SuggestionType = request.SuggestionType,
            TargetType = request.TargetType,
            TargetId = request.TargetId,
            PayloadJson = request.Payload.ValueKind == JsonValueKind.Undefined ? "{}" : request.Payload.GetRawText(),
            Status = status,
            CreatedAtUtc = now,
            DecidedAtUtc = status == AiSuggestionStatuses.Pending ? null : now,
            DecidedByUserEmail = status == AiSuggestionStatuses.Pending ? null : decidedByUserEmail,
        };
        db.AiSuggestions.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<AiSuggestionDto?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.AiSuggestions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<IReadOnlyList<AiSuggestionDto>> SearchAsync(
        Guid companyId,
        string? suggestionType,
        string? status,
        string? targetType,
        Guid? targetId,
        int take,
        CancellationToken cancellationToken)
    {
        var query = db.AiSuggestions.AsNoTracking().Where(x => x.CompanyId == companyId);
        if (!string.IsNullOrEmpty(suggestionType))
            query = query.Where(x => x.SuggestionType == suggestionType);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(x => x.Status == status);
        if (!string.IsNullOrEmpty(targetType))
            query = query.Where(x => x.TargetType == targetType);
        if (targetId.HasValue)
            query = query.Where(x => x.TargetId == targetId);

        var entities = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .ToListAsync(cancellationToken);

        return entities.Select(ToDto).ToArray();
    }

    public async Task<AiSuggestionDto?> DecideAsync(
        Guid companyId,
        Guid id,
        string newStatus,
        string decidedByUserEmail,
        CancellationToken cancellationToken)
    {
        var entity = await db.AiSuggestions
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
        if (entity is null) return null;
        entity.Status = newStatus;
        entity.DecidedAtUtc = DateTime.UtcNow;
        entity.DecidedByUserEmail = decidedByUserEmail;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<AiSuggestionDto?> SupersedeAsync(
        Guid companyId,
        Guid id,
        Guid newSuggestionId,
        string decidedByUserEmail,
        CancellationToken cancellationToken)
    {
        var entity = await db.AiSuggestions
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
        if (entity is null) return null;
        entity.Status = AiSuggestionStatuses.Superseded;
        entity.SupersededById = newSuggestionId;
        entity.DecidedAtUtc = DateTime.UtcNow;
        entity.DecidedByUserEmail = decidedByUserEmail;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<AiSuggestionDto?> FindLatestPendingAsync(
        Guid companyId,
        string suggestionType,
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken)
    {
        var entity = await db.AiSuggestions.AsNoTracking()
            .Where(x => x.CompanyId == companyId
                && x.SuggestionType == suggestionType
                && x.TargetType == targetType
                && x.TargetId == targetId
                && x.Status == AiSuggestionStatuses.Pending)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    private static AiSuggestionDto ToDto(AiSuggestionEntity entity)
    {
        JsonElement payload;
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(entity.PayloadJson) ? "{}" : entity.PayloadJson);
            payload = doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            using var doc = JsonDocument.Parse("{}");
            payload = doc.RootElement.Clone();
        }

        return new AiSuggestionDto
        {
            Id = entity.Id,
            SuggestionType = entity.SuggestionType,
            TargetType = entity.TargetType,
            TargetId = entity.TargetId,
            Payload = payload,
            Status = entity.Status,
            CreatedAtUtc = entity.CreatedAtUtc,
            DecidedAtUtc = entity.DecidedAtUtc,
            DecidedByUserEmail = entity.DecidedByUserEmail,
            SupersededById = entity.SupersededById,
        };
    }
}
