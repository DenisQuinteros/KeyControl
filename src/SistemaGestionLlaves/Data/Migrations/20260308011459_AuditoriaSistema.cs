using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGestionLlaves.Data.Migrations
{
    /// <inheritdoc />
    public partial class AuditoriaSistema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Auditoria_Usuario_id_usuario",
                table: "Auditoria");

            migrationBuilder.DropIndex(
                name: "IX_Auditoria_id_usuario",
                table: "Auditoria");

            migrationBuilder.DropColumn(
                name: "id_registro",
                table: "Auditoria");

            migrationBuilder.DropColumn(
                name: "id_usuario",
                table: "Auditoria");

            migrationBuilder.RenameColumn(
                name: "datos_nuevos",
                table: "Auditoria",
                newName: "datos_despues");

            migrationBuilder.RenameColumn(
                name: "datos_anteriores",
                table: "Auditoria",
                newName: "datos_antes");

            migrationBuilder.AlterColumn<string>(
                name: "tabla_afectada",
                table: "Auditoria",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "operacion",
                table: "Auditoria",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "usuario",
                table: "Auditoria",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "usuario",
                table: "Auditoria");

            migrationBuilder.RenameColumn(
                name: "datos_despues",
                table: "Auditoria",
                newName: "datos_nuevos");

            migrationBuilder.RenameColumn(
                name: "datos_antes",
                table: "Auditoria",
                newName: "datos_anteriores");

            migrationBuilder.AlterColumn<string>(
                name: "tabla_afectada",
                table: "Auditoria",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "operacion",
                table: "Auditoria",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<int>(
                name: "id_registro",
                table: "Auditoria",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_usuario",
                table: "Auditoria",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_id_usuario",
                table: "Auditoria",
                column: "id_usuario");

            migrationBuilder.AddForeignKey(
                name: "FK_Auditoria_Usuario_id_usuario",
                table: "Auditoria",
                column: "id_usuario",
                principalTable: "Usuario",
                principalColumn: "id_usuario",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
