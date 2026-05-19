namespace EasyMitt.Infrastructure.Persistence.Entities;

public sealed class AiSuggestionEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public CompanyEntity? Company { get; set; }
    public string SuggestionType { get; set; } = "";
    public string TargetType { get; set; } = "";
    public Guid? TargetId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public string? DecidedByUserEmail { get; set; }
    public Guid? SupersededById { get; set; }
}
