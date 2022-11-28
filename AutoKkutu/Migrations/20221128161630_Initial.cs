using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoKkutu.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Word",
                columns: table => new
                {
                    seq = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    word = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    wordindex = table.Column<string>(name: "word_index", type: "TEXT", maxLength: 1, nullable: false),
                    reversewordindex = table.Column<string>(name: "reverse_word_index", type: "TEXT", maxLength: 1, nullable: false),
                    kkutuindex = table.Column<string>(name: "kkutu_index", type: "TEXT", maxLength: 2, nullable: false),
                    flags = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Word", x => x.seq);
                });

            migrationBuilder.CreateTable(
                name: "WordIndexModel",
                columns: table => new
                {
                    wordindex = table.Column<string>(name: "word_index", type: "TEXT", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordIndexModel", x => x.wordindex);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Word");

            migrationBuilder.DropTable(
                name: "WordIndexModel");
        }
    }
}
