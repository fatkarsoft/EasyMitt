using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyMitt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "invoice_drafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    CanonicalSha256Hex = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsImmutableSnapshot = table.Column<bool>(type: "boolean", nullable: false),
                    ArchiveObjectKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_drafts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_invoice_drafts_CanonicalSha256Hex",
                table: "invoice_drafts",
                column: "CanonicalSha256Hex");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_drafts_CreatedAtUtc",
                table: "invoice_drafts",
                column: "CreatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invoice_drafts");
        }
    }
}
