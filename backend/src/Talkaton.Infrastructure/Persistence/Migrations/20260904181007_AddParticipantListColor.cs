using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Talkaton.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipantListColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "participant_lists",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "blue");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "participant_lists");
        }
    }
}
