using EasyMitt.Application.Dtos.En16931;

namespace EasyMitt.Application.Abstractions.Compliance;

/// <summary>
/// KoSIT XRechnung Schematron alt kümesini değerlendiren kural motoru.
/// </summary>
public interface IInvoiceSchematronValidator
{
    SchematronValidationResult Validate(InvoiceDocumentDto document);
}

public sealed record SchematronValidationResult(
    bool IsValid,
    IReadOnlyList<SchematronFailure> Failures);

public sealed record SchematronFailure(
    string RuleId,
    string Severity,
    string Description,
    string? Field);
