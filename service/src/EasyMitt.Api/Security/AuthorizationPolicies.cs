using EasyMitt.Domain.Identity;
using Microsoft.AspNetCore.Authorization;

namespace EasyMitt.Api.Security;

public static class AuthorizationPolicies
{
    public const string InvoiceRead = "invoice.read";
    public const string InvoiceWrite = "invoice.write";
    public const string InvoiceDispatch = "invoice.dispatch";

    public static void AddEasyMittPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(InvoiceRead, policy =>
            policy.RequireRole(EasyMittRoles.Admin, EasyMittRoles.Accountant, EasyMittRoles.Auditor));

        options.AddPolicy(InvoiceWrite, policy =>
            policy.RequireRole(EasyMittRoles.Admin, EasyMittRoles.Accountant));

        options.AddPolicy(InvoiceDispatch, policy =>
            policy.RequireRole(EasyMittRoles.Admin, EasyMittRoles.Accountant));
    }
}
