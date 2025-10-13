using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMCRMS.API.Migrations
{
    /// <inheritdoc />
    public partial class FixTransactionsForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTE: The FK change from Applications to PositionApplications was already applied manually.
            // This migration only removes the shadow property "ApplicationId1" from the model snapshot.
            // 
            // What was done manually:
            // - Dropped: FK_Transactions_Applications_ApplicationId
            // - Created: FK_Transactions_PositionApplications_ApplicationId
            //
            // The model snapshot is now synchronized with the actual database schema.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Downgrade not supported - this is a model snapshot sync only
        }
    }
}
