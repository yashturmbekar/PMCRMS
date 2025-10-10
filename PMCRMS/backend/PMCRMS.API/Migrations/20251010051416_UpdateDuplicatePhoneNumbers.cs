using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMCRMS.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDuplicatePhoneNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update all users with duplicate placeholder phone number '0000000000' to NULL
            // This fixes the unique constraint violation for email-only users
            migrationBuilder.Sql(@"
                UPDATE ""Users"" 
                SET ""PhoneNumber"" = NULL 
                WHERE ""PhoneNumber"" = '0000000000' 
                  AND ""Email"" IS NOT NULL 
                  AND ""Email"" != '';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No need to revert this data migration
            // If users didn't have phone numbers, they should remain null
        }
    }
}
