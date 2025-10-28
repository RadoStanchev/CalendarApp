using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CalendarApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixFriendshipFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Friendships_AspNetUsers_ContactId",
                table: "Friendships");

            migrationBuilder.DropIndex(
                name: "IX_Friendships_ContactId",
                table: "Friendships");

            migrationBuilder.DropColumn(
                name: "ContactId",
                table: "Friendships");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ContactId",
                table: "Friendships",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_ContactId",
                table: "Friendships",
                column: "ContactId");

            migrationBuilder.AddForeignKey(
                name: "FK_Friendships_AspNetUsers_ContactId",
                table: "Friendships",
                column: "ContactId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
