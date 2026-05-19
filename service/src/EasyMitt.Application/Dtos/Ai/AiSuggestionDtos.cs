using System.Text.Json;

namespace EasyMitt.Application.Dtos.Ai;

public static class AiSuggestionTypes
{
    public const string ExpenseCategory = "ExpenseCategory";
    public const string DatevAccount = "DatevAccount";
    public const string PaymentMatch = "PaymentMatch";
    public const string InvoiceField = "InvoiceField";
}

public static class AiSuggestionStatuses
{
    public const string Pending = "Pending";
    public const string Accepted = "Accepted";
    public const string Rejected = "Rejected";
    public const string Superseded = "Superseded";
}

public sealed class AiSuggestionDto
{
    public Guid Id { get; init; }
    public string SuggestionType { get; init; } = "";
    public string TargetType { get; init; } = "";
    public Guid? TargetId { get; init; }
    public JsonElement Payload { get; init; }
    public string Status { get; init; } = AiSuggestionStatuses.Pending;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? DecidedAtUtc { get; init; }
    public string? DecidedByUserEmail { get; init; }
    public Guid? SupersededById { get; init; }
}

public sealed class AiSuggestionCreateRequest
{
    public string SuggestionType { get; init; } = "";
    public string TargetType { get; init; } = "";
    public Guid? TargetId { get; init; }
    public JsonElement Payload { get; init; }
}

public sealed class ExpenseCategorySuggestionDto
{
    public string Category { get; init; } = "";
    public decimal Confidence { get; init; }
    public string Rationale { get; init; } = "";
    public IReadOnlyList<string> Signals { get; init; } = Array.Empty<string>();
}

public sealed class DatevAccountSuggestionDto
{
    public string DocumentType { get; init; } = "";
    public Guid DocumentId { get; init; }
    public string Account { get; init; } = "";
    public string TaxKey { get; init; } = "";
    public decimal VatRate { get; init; }
    public decimal Confidence { get; init; }
    public string Rationale { get; init; } = "";
    public string MatchedRule { get; init; } = "";
}

public sealed class PaymentMatchSuggestionDto
{
    public Guid InvoiceDraftId { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public string BuyerName { get; init; } = "";
    public DateOnly IssueDate { get; init; }
    public decimal InvoiceTotal { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal OpenAmount { get; init; }
    public decimal Confidence { get; init; }
    public int Score { get; init; }
    public IReadOnlyList<string> Reasons { get; init; } = Array.Empty<string>();
    public bool AutoPreselect { get; init; }
}

public sealed class InvoiceFieldSuggestionDto
{
    public Guid InvoiceDraftId { get; init; }
    public string FieldCode { get; init; } = "";
    public string SuggestedValue { get; init; } = "";
    public string Rationale { get; init; } = "";
    public decimal Confidence { get; init; }
}
