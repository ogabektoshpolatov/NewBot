using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bot.Migrations
{
    /// <inheritdoc />
    public partial class AddColumn_ToTaskuser_ColumName_ : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UserQueueTime",
                table: "TaskUsers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserQueueTime",
                table: "TaskUsers");
        }
    }
}
