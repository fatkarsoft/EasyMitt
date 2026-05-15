using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyMitt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DatevSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "datev_settings",
                columns: table => new
                {
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChartOfAccounts = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    RevenueAccount = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    DefaultExpenseAccount = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CustomerContraAccount = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    VendorContraAccount = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ExpenseAccountMappingsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_datev_settings", x => x.CompanyId);
                    table.ForeignKey(
                        name: "FK_datev_settings_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "datev_settings");
        }
    }
}
