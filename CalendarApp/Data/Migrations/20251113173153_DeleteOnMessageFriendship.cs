using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CalendarApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class DeleteOnMessageFriendship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Friendships_FriendshipId",
                table: "Messages");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Friendships_FriendshipId",
                table: "Messages",
                column: "FriendshipId",
                principalTable: "Friendships",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Friendships_FriendshipId",
                table: "Messages");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Friendships_FriendshipId",
                table: "Messages",
                column: "FriendshipId",
                principalTable: "Friendships",
                principalColumn: "Id");
        }
    }
}
