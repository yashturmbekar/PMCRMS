using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMCRMS.API.Migrations
{
    /// <inheritdoc />
    public partial class CapturePendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "FormConfigurations",
                keyColumn: "Id",
                keyValue: 2,
                column: "BaseFee",
                value: 3000m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "FormConfigurations",
                keyColumn: "Id",
                keyValue: 2,
                column: "BaseFee",
                value: 2500m);
        }
    }
}
