using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMCRMS.API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOfficerEmailUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Officers_Email",
                table: "Officers");

            migrationBuilder.CreateIndex(
                name: "IX_Officers_Email",
                table: "Officers",
                column: "Email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Officers_Email",
                table: "Officers");

            migrationBuilder.CreateIndex(
                name: "IX_Officers_Email",
                table: "Officers",
                column: "Email",
                unique: true);
        }
    }
}
