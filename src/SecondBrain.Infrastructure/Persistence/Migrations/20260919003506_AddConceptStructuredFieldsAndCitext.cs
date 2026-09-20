using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecondBrain.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConceptStructuredFieldsAndCitext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "tags",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "concepts",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AddColumn<string>(
                name: "CodeExample",
                table: "concepts",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentationUrl",
                table: "concepts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpectedOutput",
                table: "concepts",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Explanation",
                table: "concepts",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SimpleAnalogy",
                table: "concepts",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhereUsed",
                table: "concepts",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodeExample",
                table: "concepts");

            migrationBuilder.DropColumn(
                name: "DocumentationUrl",
                table: "concepts");

            migrationBuilder.DropColumn(
                name: "ExpectedOutput",
                table: "concepts");

            migrationBuilder.DropColumn(
                name: "Explanation",
                table: "concepts");

            migrationBuilder.DropColumn(
                name: "SimpleAnalogy",
                table: "concepts");

            migrationBuilder.DropColumn(
                name: "WhereUsed",
                table: "concepts");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "tags",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "concepts",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");
        }
    }
}
