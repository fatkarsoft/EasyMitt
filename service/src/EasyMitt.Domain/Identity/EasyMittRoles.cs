namespace EasyMitt.Domain.Identity;

public static class EasyMittRoles
{
    public const string Admin = "Admin";
    public const string Accountant = "Accountant";
    public const string Auditor = "Auditor";

    public static readonly string[] All = [Admin, Accountant, Auditor];
}
