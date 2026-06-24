using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortoSeguraAPI.Migrations
{
    /// <inheritdoc />
    public partial class AjustarPareamentoSobDemanda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "MadrinhaId",
                table: "Solicitacoes",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "MadrinhaId",
                table: "SessoesChat",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "Aeroporto",
                table: "SessoesChat",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HorarioDesembarque",
                table: "SessoesChat",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocaisVisitados",
                table: "SessoesChat",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantidadeHoras",
                table: "SessoesChat",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeLocalId",
                table: "SessoesChat",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessoesChat_TimeLocalId",
                table: "SessoesChat",
                column: "TimeLocalId");

            migrationBuilder.AddForeignKey(
                name: "FK_SessoesChat_TimesLocais_TimeLocalId",
                table: "SessoesChat",
                column: "TimeLocalId",
                principalTable: "TimesLocais",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SessoesChat_TimesLocais_TimeLocalId",
                table: "SessoesChat");

            migrationBuilder.DropIndex(
                name: "IX_SessoesChat_TimeLocalId",
                table: "SessoesChat");

            migrationBuilder.DropColumn(
                name: "Aeroporto",
                table: "SessoesChat");

            migrationBuilder.DropColumn(
                name: "HorarioDesembarque",
                table: "SessoesChat");

            migrationBuilder.DropColumn(
                name: "LocaisVisitados",
                table: "SessoesChat");

            migrationBuilder.DropColumn(
                name: "QuantidadeHoras",
                table: "SessoesChat");

            migrationBuilder.DropColumn(
                name: "TimeLocalId",
                table: "SessoesChat");

            migrationBuilder.AlterColumn<int>(
                name: "MadrinhaId",
                table: "Solicitacoes",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MadrinhaId",
                table: "SessoesChat",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
