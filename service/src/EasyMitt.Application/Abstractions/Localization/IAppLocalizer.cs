using FluentValidation.Results;

namespace EasyMitt.Application.Abstractions.Localization;

public interface IAppLocalizer
{
    string Get(string key);

    string Get(string key, string language);

    string Format(string key, params object[] args);

    string GetValidationMessage(ValidationFailure failure);
}
