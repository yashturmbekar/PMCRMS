-- Update all users with duplicate placeholder phone number to NULL
-- This will fix the unique constraint violation issue

UPDATE "Users" 
SET "PhoneNumber" = NULL 
WHERE "PhoneNumber" = '0000000000' 
  AND "Email" IS NOT NULL 
  AND "Email" != '';

-- Verify the update
SELECT "Id", "Name", "Email", "PhoneNumber", "Role" 
FROM "Users" 
WHERE "PhoneNumber" IS NULL OR "PhoneNumber" = '0000000000';
