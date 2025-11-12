using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CalendarApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFriendshipPairKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Friendships_PairKey",
                table: "Friendships");

            migrationBuilder.DropColumn(
                name: "PairKey",
                table: "Friendships");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PairKey",
                table: "Friendships",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.Sql(@"UPDATE Friendships SET PairKey = CASE
WHEN RequesterId < ReceiverId THEN CONCAT(CONVERT(nvarchar(36), RequesterId), '_', CONVERT(nvarchar(36), ReceiverId))
ELSE CONCAT(CONVERT(nvarchar(36), ReceiverId), '_', CONVERT(nvarchar(36), RequesterId))
END");

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_PairKey",
                table: "Friendships",
                column: "PairKey",
                unique: true);
        }
    }
}
