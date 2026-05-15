using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyMitt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DatevExportPeriods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "PeriodFrom",
                table: "datev_export_logs",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PeriodTo",
                table: "datev_export_logs",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_datev_export_logs_CompanyId_ExportType_StatusFilter_PeriodF~",
                table: "datev_export_logs",
                columns: new[] { "CompanyId", "ExportType", "StatusFilter", "PeriodFrom", "PeriodTo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_datev_export_logs_CompanyId_ExportType_StatusFilter_PeriodF~",
                table: "datev_export_logs");

            migrationBuilder.DropColumn(
                name: "PeriodFrom",
                table: "datev_export_logs");

            migrationBuilder.DropColumn(
                name: "PeriodTo",
                table: "datev_export_logs");
        }
    }
}
