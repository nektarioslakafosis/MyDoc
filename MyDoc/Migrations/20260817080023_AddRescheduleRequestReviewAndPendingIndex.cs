using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyDoc.Migrations
{
    /// <inheritdoc />
    public partial class AddRescheduleRequestReviewAndPendingIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReviewComment",
                table: "AppointmentRescheduleRequests",
                type: "nvarchar(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRescheduleRequests_OnePendingPerAppointment",
                table: "AppointmentRescheduleRequests",
                columns: new[] { "AppointmentId", "Status" },
                unique: true,
                filter: "[Status] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppointmentRescheduleRequests_OnePendingPerAppointment",
                table: "AppointmentRescheduleRequests");

            migrationBuilder.DropColumn(
                name: "ReviewComment",
                table: "AppointmentRescheduleRequests");
        }
    }
}
