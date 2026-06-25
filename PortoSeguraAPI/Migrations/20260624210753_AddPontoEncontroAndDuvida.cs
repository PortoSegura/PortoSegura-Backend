using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortoSeguraAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPontoEncontroAndDuvida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DuvidaInicial",
                table: "SessoesChat",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PontoEncontro",
                table: "SessoesChat",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DuvidaInicial",
                table: "SessoesChat");

            migrationBuilder.DropColumn(
                name: "PontoEncontro",
                table: "SessoesChat");
        }
    }
}
