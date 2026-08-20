using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Linear.Web.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoadmaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RoadmapItemId",
                table: "Issues",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Roadmaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roadmaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roadmaps_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoadmapItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoadmapId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadmapItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoadmapItems_Roadmaps_RoadmapId",
                        column: x => x.RoadmapId,
                        principalTable: "Roadmaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Issues_RoadmapItemId_Status",
                table: "Issues",
                columns: new[] { "RoadmapItemId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapItems_RoadmapId_StartDate",
                table: "RoadmapItems",
                columns: new[] { "RoadmapId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Roadmaps_TeamId",
                table: "Roadmaps",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_RoadmapItems_RoadmapItemId",
                table: "Issues",
                column: "RoadmapItemId",
                principalTable: "RoadmapItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Issues_RoadmapItems_RoadmapItemId",
                table: "Issues");

            migrationBuilder.DropTable(
                name: "RoadmapItems");

            migrationBuilder.DropTable(
                name: "Roadmaps");

            migrationBuilder.DropIndex(
                name: "IX_Issues_RoadmapItemId_Status",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "RoadmapItemId",
                table: "Issues");
        }
    }
}
