using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyMitt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DatevExportArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArchiveObjectKey",
                table: "datev_export_logs",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchiveObjectKey",
                table: "datev_export_logs");
        }
    }
}
