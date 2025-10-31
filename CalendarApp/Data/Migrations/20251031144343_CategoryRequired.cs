using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CalendarApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class CategoryRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Meetings_Categories_CategoryId",
                table: "Meetings");

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "Meetings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("13E6BC48-E565-47EE-9336-039EDF9C0B0D"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Meetings_Categories_CategoryId",
                table: "Meetings",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Meetings_Categories_CategoryId",
                table: "Meetings");

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "Meetings",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_Meetings_Categories_CategoryId",
                table: "Meetings",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");
        }
    }
}
