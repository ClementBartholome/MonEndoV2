using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonEndoVue.Server.Migrations
{
    /// <inheritdoc />
    public partial class AjouteEmotionsBilan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Mood",
                table: "BilansQuotidiens",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "EmotionsBilan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Emotion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    BilanQuotidienId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmotionsBilan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmotionsBilan_BilansQuotidiens_BilanQuotidienId",
                        column: x => x.BilanQuotidienId,
                        principalTable: "BilansQuotidiens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmotionsBilan_BilanQuotidienId_Emotion",
                table: "EmotionsBilan",
                columns: new[] { "BilanQuotidienId", "Emotion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmotionsBilan");

            migrationBuilder.AlterColumn<string>(
                name: "Mood",
                table: "BilansQuotidiens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
