using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyMitt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EmailDeliveryLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_delivery_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Subject = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    AttachmentType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    SenderUserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SenderUserEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_delivery_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_email_delivery_logs_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_delivery_logs_CompanyId_CreatedAtUtc",
                table: "email_delivery_logs",
                columns: new[] { "CompanyId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_email_delivery_logs_CompanyId_DocumentType_DocumentId",
                table: "email_delivery_logs",
                columns: new[] { "CompanyId", "DocumentType", "DocumentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_delivery_logs");
        }
    }
}
