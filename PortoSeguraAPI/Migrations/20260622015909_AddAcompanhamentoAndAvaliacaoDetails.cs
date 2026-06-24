using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortoSeguraAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAcompanhamentoAndAvaliacaoDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AcompanhamentoDataFim",
                table: "SessoesChat",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AcompanhamentoDataInicio",
                table: "SessoesChat",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcompanhamentoHoraFim",
                table: "SessoesChat",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcompanhamentoHoraInicio",
                table: "SessoesChat",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Avaliada",
                table: "SessoesChat",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "SolicitacaoId",
                table: "Avaliacoes",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "ServicoTipo",
                table: "Avaliacoes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessaoChatId",
                table: "Avaliacoes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Avaliacoes_SessaoChatId",
                table: "Avaliacoes",
                column: "SessaoChatId");

            migrationBuilder.AddForeignKey(
                name: "FK_Avaliacoes_SessoesChat_SessaoChatId",
                table: "Avaliacoes",
                column: "SessaoChatId",
                principalTable: "SessoesChat",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Avaliacoes_SessoesChat_SessaoChatId",
                table: "Avaliacoes");

            migrationBuilder.DropIndex(
                name: "IX_Avaliacoes_SessaoChatId",
                table: "Avaliacoes");

            migrationBuilder.DropColumn(
                name: "AcompanhamentoDataFim",
                table: "SessoesChat");

            migrationBuilder.DropColumn(
                name: "AcompanhamentoDataInicio",
                table: "SessoesChat");

            migrationBuilder.DropColumn(
                name: "AcompanhamentoHoraFim",
                table: "SessoesChat");

            migrationBuilder.DropColumn(
                name: "AcompanhamentoHoraInicio",
                table: "SessoesChat");

            migrationBuilder.DropColumn(
                name: "Avaliada",
                table: "SessoesChat");

            migrationBuilder.DropColumn(
                name: "ServicoTipo",
                table: "Avaliacoes");

            migrationBuilder.DropColumn(
                name: "SessaoChatId",
                table: "Avaliacoes");

            migrationBuilder.AlterColumn<int>(
                name: "SolicitacaoId",
                table: "Avaliacoes",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
