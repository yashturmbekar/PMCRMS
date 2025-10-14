using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMCRMS.API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveApplicationsTableConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old foreign key constraint that references non-existent Applications table
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF EXISTS (
                        SELECT 1 FROM information_schema.table_constraints 
                        WHERE constraint_name = 'FK_Transactions_Applications_ApplicationId'
                        AND table_name = 'Transactions'
                    ) THEN
                        ALTER TABLE ""Transactions"" DROP CONSTRAINT ""FK_Transactions_Applications_ApplicationId"";
                    END IF;
                END $$;
            ");

            // Ensure the correct foreign key constraint to PositionApplications exists
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints 
                        WHERE constraint_name = 'FK_Transactions_PositionApplications_ApplicationId'
                        AND table_name = 'Transactions'
                    ) THEN
                        ALTER TABLE ""Transactions"" 
                        ADD CONSTRAINT ""FK_Transactions_PositionApplications_ApplicationId"" 
                        FOREIGN KEY (""ApplicationId"") 
                        REFERENCES ""PositionApplications""(""Id"") 
                        ON DELETE CASCADE;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not reversible - we don't want to recreate the wrong constraint
        }
    }
}
