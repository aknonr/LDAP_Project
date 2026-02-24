using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobTargetAndJobResourceUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobTargets_JobId",
                table: "JobTargets");

            migrationBuilder.DropIndex(
                name: "IX_JobResources_JobTargetId",
                table: "JobResources");

            migrationBuilder.CreateIndex(
                name: "IX_JobTargets_JobId_ServerName",
                table: "JobTargets",
                columns: new[] { "JobId", "ServerName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobResources_JobTargetId_ResourceType_ResourceName_ResourcePath",
                table: "JobResources",
                columns: new[] { "JobTargetId", "ResourceType", "ResourceName", "ResourcePath" },
                unique: true,
                filter: "[ResourcePath] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobTargets_JobId_ServerName",
                table: "JobTargets");

            migrationBuilder.DropIndex(
                name: "IX_JobResources_JobTargetId_ResourceType_ResourceName_ResourcePath",
                table: "JobResources");

            migrationBuilder.CreateIndex(
                name: "IX_JobTargets_JobId",
                table: "JobTargets",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobResources_JobTargetId",
                table: "JobResources",
                column: "JobTargetId");
        }
    }
}
