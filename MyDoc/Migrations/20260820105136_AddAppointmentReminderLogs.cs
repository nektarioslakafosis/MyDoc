using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyDoc.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentReminderLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppointmentReminderLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppointmentId = table.Column<int>(type: "int", nullable: false),
                    ReminderType = table.Column<int>(type: "int", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentReminderLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentReminderLogs_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentReminderLogs_AppointmentId_ReminderType",
                table: "AppointmentReminderLogs",
                columns: new[] { "AppointmentId", "ReminderType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentReminderLogs_SentAt",
                table: "AppointmentReminderLogs",
                column: "SentAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentReminderLogs");
        }
    }
}
