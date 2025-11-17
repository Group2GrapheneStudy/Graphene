using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graphene.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    PatientId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.PatientId);
                });

            migrationBuilder.CreateTable(
                name: "DataFiles",
                columns: table => new
                {
                    FileId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FileHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirstTimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastTimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImportedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataFiles", x => x.FileId);
                    table.ForeignKey(
                        name: "FK_DataFiles_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PressureFrames",
                columns: table => new
                {
                    FrameId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    FileId = table.Column<long>(type: "bigint", nullable: false),
                    FrameIndex = table.Column<int>(type: "int", nullable: false),
                    CapturedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Width = table.Column<byte>(type: "tinyint", nullable: false),
                    Height = table.Column<byte>(type: "tinyint", nullable: false),
                    PeakPressure = table.Column<int>(type: "int", nullable: true),
                    PixelsAboveThr = table.Column<int>(type: "int", nullable: true),
                    ContactAreaPct = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PressureFrames", x => x.FrameId);
                    table.ForeignKey(
                        name: "FK_PressureFrames_DataFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "DataFiles",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PressureFrames_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    AlertId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    FrameId = table.Column<long>(type: "bigint", nullable: true),
                    TriggeredUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Severity = table.Column<byte>(type: "tinyint", nullable: false),
                    MaxPressure = table.Column<int>(type: "int", nullable: true),
                    PixelsAboveThr = table.Column<int>(type: "int", nullable: true),
                    RegionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.AlertId);
                    table.ForeignKey(
                        name: "FK_Alerts_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Alerts_PressureFrames_FrameId",
                        column: x => x.FrameId,
                        principalTable: "PressureFrames",
                        principalColumn: "FrameId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_FrameId",
                table: "Alerts",
                column: "FrameId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_PatientId_TriggeredUtc",
                table: "Alerts",
                columns: new[] { "PatientId", "TriggeredUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_Status_Severity",
                table: "Alerts",
                columns: new[] { "Status", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_DataFiles_PatientId_FilePath",
                table: "DataFiles",
                columns: new[] { "PatientId", "FilePath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_ExternalUserId",
                table: "Patients",
                column: "ExternalUserId",
                unique: true,
                filter: "[ExternalUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PressureFrames_FileId",
                table: "PressureFrames",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_PressureFrames_PatientId_CapturedUtc",
                table: "PressureFrames",
                columns: new[] { "PatientId", "CapturedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PressureFrames_PatientId_FileId_FrameIndex",
                table: "PressureFrames",
                columns: new[] { "PatientId", "FileId", "FrameIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "PressureFrames");

            migrationBuilder.DropTable(
                name: "DataFiles");

            migrationBuilder.DropTable(
                name: "Patients");
        }
    }
}
