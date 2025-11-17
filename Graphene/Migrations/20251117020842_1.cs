using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graphene.Migrations
{
    /// <inheritdoc />
    public partial class _1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PressureFrames_Patients_PatientId",
                table: "PressureFrames");

            migrationBuilder.AddForeignKey(
                name: "FK_PressureFrames_Patients_PatientId",
                table: "PressureFrames",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PressureFrames_Patients_PatientId",
                table: "PressureFrames");

            migrationBuilder.AddForeignKey(
                name: "FK_PressureFrames_Patients_PatientId",
                table: "PressureFrames",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "PatientId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
