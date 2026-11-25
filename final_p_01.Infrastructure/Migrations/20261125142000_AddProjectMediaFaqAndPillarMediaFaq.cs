using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace final_p_01.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectMediaFaqAndPillarMediaFaq : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Projects: attachment/audio/alt media fields
            migrationBuilder.AddColumn<string>(
                name: "AttachmentDescription",
                table: "Projects",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentFilePath",
                table: "Projects",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentTitle",
                table: "Projects",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AudioFilePath",
                table: "Projects",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeaturedImageAlt",
                table: "Projects",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // PillarPages: same media fields
            migrationBuilder.AddColumn<string>(
                name: "AttachmentDescription",
                table: "PillarPages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentFilePath",
                table: "PillarPages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentTitle",
                table: "PillarPages",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AudioFilePath",
                table: "PillarPages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverImageAlt",
                table: "PillarPages",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // Faqs: allow FAQ items attached to Projects or PillarPages,
            // and relax the PostId FK (NOT NULL -> NULL) for existing databases.
            migrationBuilder.DropForeignKey(
                name: "FK_Faqs_Posts_PostId",
                table: "Faqs");

            migrationBuilder.DropIndex(
                name: "IX_Faqs_PostId",
                table: "Faqs");

            migrationBuilder.AddColumn<int>(
                name: "PillarPageId",
                table: "Faqs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "Faqs",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PostId",
                table: "Faqs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Faqs_PostId",
                table: "Faqs",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_Faqs_PillarPageId",
                table: "Faqs",
                column: "PillarPageId");

            migrationBuilder.CreateIndex(
                name: "IX_Faqs_ProjectId",
                table: "Faqs",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Faqs_Posts_PostId",
                table: "Faqs",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Faqs_PillarPages_PillarPageId",
                table: "Faqs",
                column: "PillarPageId",
                principalTable: "PillarPages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Faqs_Projects_ProjectId",
                table: "Faqs",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Faqs_PillarPages_PillarPageId",
                table: "Faqs");

            migrationBuilder.DropForeignKey(
                name: "FK_Faqs_Projects_ProjectId",
                table: "Faqs");

            migrationBuilder.DropForeignKey(
                name: "FK_Faqs_Posts_PostId",
                table: "Faqs");

            migrationBuilder.DropIndex(
                name: "IX_Faqs_PostId",
                table: "Faqs");

            migrationBuilder.DropIndex(
                name: "IX_Faqs_PillarPageId",
                table: "Faqs");

            migrationBuilder.DropIndex(
                name: "IX_Faqs_ProjectId",
                table: "Faqs");

            migrationBuilder.DropColumn(
                name: "PillarPageId",
                table: "Faqs");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Faqs");

            migrationBuilder.AlterColumn<int>(
                name: "PostId",
                table: "Faqs",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Faqs_PostId",
                table: "Faqs",
                column: "PostId");

            migrationBuilder.AddForeignKey(
                name: "FK_Faqs_Posts_PostId",
                table: "Faqs",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropColumn(
                name: "AttachmentDescription",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "AttachmentFilePath",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "AttachmentTitle",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "AudioFilePath",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "FeaturedImageAlt",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "AttachmentDescription",
                table: "PillarPages");

            migrationBuilder.DropColumn(
                name: "AttachmentFilePath",
                table: "PillarPages");

            migrationBuilder.DropColumn(
                name: "AttachmentTitle",
                table: "PillarPages");

            migrationBuilder.DropColumn(
                name: "AudioFilePath",
                table: "PillarPages");

            migrationBuilder.DropColumn(
                name: "CoverImageAlt",
                table: "PillarPages");
        }
    }
}
