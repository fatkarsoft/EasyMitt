using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyMitt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InvoiceLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAtUtc",
                table: "invoice_drafts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IssuedAtUtc",
                table: "invoice_drafts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAtUtc",
                table: "invoice_drafts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentAtUtc",
                table: "invoice_drafts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "invoice_drafts",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_drafts_CompanyId_Status",
                table: "invoice_drafts",
                columns: new[] { "CompanyId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_invoice_drafts_CompanyId_Status",
                table: "invoice_drafts");

            migrationBuilder.DropColumn(
                name: "CancelledAtUtc",
                table: "invoice_drafts");

            migrationBuilder.DropColumn(
                name: "IssuedAtUtc",
                table: "invoice_drafts");

            migrationBuilder.DropColumn(
                name: "PaidAtUtc",
                table: "invoice_drafts");

            migrationBuilder.DropColumn(
                name: "SentAtUtc",
                table: "invoice_drafts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "invoice_drafts");
        }
    }
}
