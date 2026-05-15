using System.Security.Claims;
using EasyMitt.Api.Responses;
using EasyMitt.Application.Abstractions.Localization;
using FluentValidation.Results;

namespace EasyMitt.Api.Features;

internal static class EndpointHelpers
{
    public static Guid CompanyId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("company_id");
        return Guid.TryParse(value, out var companyId) ? companyId : Guid.Empty;
    }

    public static Guid UserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : Guid.Empty;
    }

    public static Dictionary<string, IReadOnlyList<ApiError>> ToErrorDictionary(
        IEnumerable<ValidationFailure> failures,
        IAppLocalizer localizer) =>
        failures
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ApiError>)g.Select(e => new ApiError
                {
                    Code = e.ErrorCode,
                    Message = localizer.GetValidationMessage(e),
                }).ToArray());
}
