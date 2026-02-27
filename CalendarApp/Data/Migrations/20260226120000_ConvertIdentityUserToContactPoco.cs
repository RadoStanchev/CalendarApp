using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CalendarApp.Data.Migrations
{
    public partial class ConvertIdentityUserToContactPoco : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NOT NULL AND OBJECT_ID(N'[dbo].[Contacts]', N'U') IS NULL
BEGIN
    EXEC sp_rename N'[dbo].[AspNetUsers]', N'Contacts';
END");

            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Contacts', 'NormalizedUserName') IS NOT NULL ALTER TABLE [dbo].[Contacts] DROP COLUMN [NormalizedUserName];
IF COL_LENGTH('dbo.Contacts', 'NormalizedEmail') IS NOT NULL ALTER TABLE [dbo].[Contacts] DROP COLUMN [NormalizedEmail];
IF COL_LENGTH('dbo.Contacts', 'ConcurrencyStamp') IS NOT NULL ALTER TABLE [dbo].[Contacts] DROP COLUMN [ConcurrencyStamp];
IF COL_LENGTH('dbo.Contacts', 'PhoneNumber') IS NOT NULL ALTER TABLE [dbo].[Contacts] DROP COLUMN [PhoneNumber];
IF COL_LENGTH('dbo.Contacts', 'PhoneNumberConfirmed') IS NOT NULL ALTER TABLE [dbo].[Contacts] DROP COLUMN [PhoneNumberConfirmed];
IF COL_LENGTH('dbo.Contacts', 'TwoFactorEnabled') IS NOT NULL ALTER TABLE [dbo].[Contacts] DROP COLUMN [TwoFactorEnabled];
IF COL_LENGTH('dbo.Contacts', 'LockoutEnd') IS NOT NULL ALTER TABLE [dbo].[Contacts] DROP COLUMN [LockoutEnd];
IF COL_LENGTH('dbo.Contacts', 'LockoutEnabled') IS NOT NULL ALTER TABLE [dbo].[Contacts] DROP COLUMN [LockoutEnabled];
IF COL_LENGTH('dbo.Contacts', 'AccessFailedCount') IS NOT NULL ALTER TABLE [dbo].[Contacts] DROP COLUMN [AccessFailedCount];
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[AspNetUserClaims]', N'U') IS NOT NULL DROP TABLE [dbo].[AspNetUserClaims];
IF OBJECT_ID(N'[dbo].[AspNetUserLogins]', N'U') IS NOT NULL DROP TABLE [dbo].[AspNetUserLogins];
IF OBJECT_ID(N'[dbo].[AspNetUserRoles]', N'U') IS NOT NULL DROP TABLE [dbo].[AspNetUserRoles];
IF OBJECT_ID(N'[dbo].[AspNetUserTokens]', N'U') IS NOT NULL DROP TABLE [dbo].[AspNetUserTokens];
IF OBJECT_ID(N'[dbo].[AspNetRoleClaims]', N'U') IS NOT NULL DROP TABLE [dbo].[AspNetRoleClaims];
IF OBJECT_ID(N'[dbo].[AspNetRoles]', N'U') IS NOT NULL DROP TABLE [dbo].[AspNetRoles];
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Contacts]', N'U') IS NOT NULL AND OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NULL
BEGIN
    EXEC sp_rename N'[dbo].[Contacts]', N'AspNetUsers';
END");
        }
    }
}
