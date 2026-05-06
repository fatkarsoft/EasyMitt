using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Application.Localization;
using EasyMitt.Domain.Localization;
using FluentValidation.Results;

namespace EasyMitt.Application.Services.Localization;

public sealed class DictionaryAppLocalizer(ICurrentLanguage currentLanguage) : IAppLocalizer
{
    private static readonly Dictionary<string, Dictionary<string, string>> Messages = new(StringComparer.OrdinalIgnoreCase)
    {
        [SupportedLanguages.English] = new(StringComparer.OrdinalIgnoreCase)
        {
            [MessageKeys.AuthenticationInvalidCredentials] = "Email or password is incorrect.",
            [MessageKeys.AuthenticationRequired] = "Authentication is required.",
            [MessageKeys.AuthorizationForbidden] = "You do not have permission to perform this action.",
            [MessageKeys.ValidationInvalidJson] = "The document field must contain valid JSON.",
            [MessageKeys.ValidationDocumentRequired] = "The document field is required.",
            [MessageKeys.ValidationFailed] = "Validation failed.",
            [MessageKeys.Authenticated] = "Authenticated.",
            [MessageKeys.InvoiceValidationSucceeded] = "Invoice is valid.",
            [MessageKeys.InvoiceDraftCreated] = "Invoice draft created.",
            [MessageKeys.InvoiceDraftFound] = "Invoice draft found.",
            [MessageKeys.InvoiceDraftNotFound] = "Invoice draft was not found.",
            [MessageKeys.InvoiceRawIngested] = "Raw invoice data was mapped.",
            [MessageKeys.InvoicePeppolSubmitted] = "Invoice submitted to the dispatch channel.",
            [MessageKeys.SystemHealthy] = "Service is healthy.",
            [MessageKeys.ErrorUnexpected] = "An unexpected error occurred.",
            [MessageKeys.ErrorBadRequest] = "The request is invalid.",
            [MessageKeys.ErrorNotFound] = "The requested resource was not found.",
            [ValidationErrorCodes.Required] = "{0} is required.",
            [ValidationErrorCodes.MaxLength] = "{0} must be at most {1} characters.",
            [ValidationErrorCodes.ExactLength] = "{0} must be exactly {1} characters.",
            [ValidationErrorCodes.GreaterThanZero] = "{0} must be greater than zero.",
            [ValidationErrorCodes.GreaterThanOrEqualZero] = "{0} must be greater than or equal to zero.",
            [ValidationErrorCodes.IbanRequired] = "BT-34 payment IBAN is required.",
            [ValidationErrorCodes.IbanFormat] = "BT-34 must be a valid IBAN format.",
            [ValidationErrorCodes.SellerVatRequired] = "BT-22 seller VAT identifier is required.",
            [ValidationErrorCodes.LinesRequired] = "At least one invoice line is required.",
            [ValidationErrorCodes.GermanVatRate] = "Line VAT rate must be one of the common German rates: 0, 7, 19.",
        },
        [SupportedLanguages.Turkish] = new(StringComparer.OrdinalIgnoreCase)
        {
            [MessageKeys.AuthenticationInvalidCredentials] = "E-posta veya şifre hatalı.",
            [MessageKeys.AuthenticationRequired] = "Kimlik doğrulama gerekli.",
            [MessageKeys.AuthorizationForbidden] = "Bu işlemi yapma yetkiniz yok.",
            [MessageKeys.ValidationInvalidJson] = "document alanı geçerli JSON içermeli.",
            [MessageKeys.ValidationDocumentRequired] = "document alanı zorunludur.",
            [MessageKeys.ValidationFailed] = "Doğrulama başarısız.",
            [MessageKeys.Authenticated] = "Kimlik doğrulandı.",
            [MessageKeys.InvoiceValidationSucceeded] = "Fatura geçerli.",
            [MessageKeys.InvoiceDraftCreated] = "Fatura taslağı oluşturuldu.",
            [MessageKeys.InvoiceDraftFound] = "Fatura taslağı bulundu.",
            [MessageKeys.InvoiceDraftNotFound] = "Fatura taslağı bulunamadı.",
            [MessageKeys.InvoiceRawIngested] = "Ham fatura verisi eşlendi.",
            [MessageKeys.InvoicePeppolSubmitted] = "Fatura iletim kanalına gönderildi.",
            [MessageKeys.SystemHealthy] = "Servis sağlıklı.",
            [MessageKeys.ErrorUnexpected] = "Beklenmeyen bir hata oluştu.",
            [MessageKeys.ErrorBadRequest] = "İstek geçersiz.",
            [MessageKeys.ErrorNotFound] = "İstenen kaynak bulunamadı.",
            [ValidationErrorCodes.Required] = "{0} zorunludur.",
            [ValidationErrorCodes.MaxLength] = "{0} en fazla {1} karakter olabilir.",
            [ValidationErrorCodes.ExactLength] = "{0} tam olarak {1} karakter olmalıdır.",
            [ValidationErrorCodes.GreaterThanZero] = "{0} sıfırdan büyük olmalıdır.",
            [ValidationErrorCodes.GreaterThanOrEqualZero] = "{0} sıfır veya daha büyük olmalıdır.",
            [ValidationErrorCodes.IbanRequired] = "BT-34 ödeme IBAN'ı zorunludur.",
            [ValidationErrorCodes.IbanFormat] = "BT-34 geçerli IBAN formatında olmalıdır.",
            [ValidationErrorCodes.SellerVatRequired] = "BT-22 satıcı KDV kimliği zorunludur.",
            [ValidationErrorCodes.LinesRequired] = "En az bir fatura satırı gerekli.",
            [ValidationErrorCodes.GermanVatRate] = "Satır KDV oranı yaygın Almanya oranlarından biri olmalıdır: 0, 7, 19.",
        },
        [SupportedLanguages.German] = new(StringComparer.OrdinalIgnoreCase)
        {
            [MessageKeys.AuthenticationInvalidCredentials] = "E-Mail oder Passwort ist falsch.",
            [MessageKeys.AuthenticationRequired] = "Authentifizierung ist erforderlich.",
            [MessageKeys.AuthorizationForbidden] = "Sie haben keine Berechtigung für diese Aktion.",
            [MessageKeys.ValidationInvalidJson] = "Das Feld document muss gültiges JSON enthalten.",
            [MessageKeys.ValidationDocumentRequired] = "Das Feld document ist erforderlich.",
            [MessageKeys.ValidationFailed] = "Validierung fehlgeschlagen.",
            [MessageKeys.Authenticated] = "Authentifiziert.",
            [MessageKeys.InvoiceValidationSucceeded] = "Die Rechnung ist gültig.",
            [MessageKeys.InvoiceDraftCreated] = "Rechnungsentwurf wurde erstellt.",
            [MessageKeys.InvoiceDraftFound] = "Rechnungsentwurf gefunden.",
            [MessageKeys.InvoiceDraftNotFound] = "Rechnungsentwurf wurde nicht gefunden.",
            [MessageKeys.InvoiceRawIngested] = "Rohe Rechnungsdaten wurden zugeordnet.",
            [MessageKeys.InvoicePeppolSubmitted] = "Rechnung wurde an den Versandkanal übergeben.",
            [MessageKeys.SystemHealthy] = "Dienst ist fehlerfrei.",
            [MessageKeys.ErrorUnexpected] = "Ein unerwarteter Fehler ist aufgetreten.",
            [MessageKeys.ErrorBadRequest] = "Die Anfrage ist ungültig.",
            [MessageKeys.ErrorNotFound] = "Die angeforderte Ressource wurde nicht gefunden.",
            [ValidationErrorCodes.Required] = "{0} ist erforderlich.",
            [ValidationErrorCodes.MaxLength] = "{0} darf höchstens {1} Zeichen lang sein.",
            [ValidationErrorCodes.ExactLength] = "{0} muss genau {1} Zeichen lang sein.",
            [ValidationErrorCodes.GreaterThanZero] = "{0} muss größer als null sein.",
            [ValidationErrorCodes.GreaterThanOrEqualZero] = "{0} muss größer oder gleich null sein.",
            [ValidationErrorCodes.IbanRequired] = "BT-34 Zahlungs-IBAN ist erforderlich.",
            [ValidationErrorCodes.IbanFormat] = "BT-34 muss ein gültiges IBAN-Format haben.",
            [ValidationErrorCodes.SellerVatRequired] = "BT-22 Umsatzsteuer-ID des Verkäufers ist erforderlich.",
            [ValidationErrorCodes.LinesRequired] = "Mindestens eine Rechnungsposition ist erforderlich.",
            [ValidationErrorCodes.GermanVatRate] = "Der Umsatzsteuersatz der Position muss einer der üblichen deutschen Sätze sein: 0, 7, 19.",
        },
    };

    public string Get(string key) => Get(key, currentLanguage.Language);

    public string Get(string key, string language)
    {
        var normalized = SupportedLanguages.NormalizeOrDefault(language);
        if (Messages.TryGetValue(normalized, out var localized) && localized.TryGetValue(key, out var message))
        {
            return message;
        }

        return Messages[SupportedLanguages.English].GetValueOrDefault(key, key);
    }

    public string Format(string key, params object[] args) => string.Format(Get(key), args);

    public string GetValidationMessage(ValidationFailure failure)
    {
        var code = string.IsNullOrWhiteSpace(failure.ErrorCode) ? ValidationErrorCodes.Required : failure.ErrorCode;
        var formatterValues = failure.FormattedMessagePlaceholderValues;
        return code switch
        {
            ValidationErrorCodes.MaxLength when formatterValues.TryGetValue("MaxLength", out var maxLength) =>
                Format(code, failure.PropertyName, maxLength),
            ValidationErrorCodes.ExactLength when formatterValues.TryGetValue("ExactLength", out var exactLength) =>
                Format(code, failure.PropertyName, exactLength),
            _ when Messages[SupportedLanguages.English].ContainsKey(code) => Format(code, failure.PropertyName),
            _ => failure.ErrorMessage,
        };
    }
}
