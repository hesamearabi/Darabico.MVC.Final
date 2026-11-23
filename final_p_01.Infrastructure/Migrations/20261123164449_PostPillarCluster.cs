using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace final_p_01.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PostPillarCluster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FaviconPath",
                table: "SiteSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoBlogDescription",
                table: "SiteSettings",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoBlogTitle",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoEstimatorDescription",
                table: "SiteSettings",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoEstimatorTitle",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoGuideDescription",
                table: "SiteSettings",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoGuideTitle",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoHomeDescription",
                table: "SiteSettings",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoHomeTitle",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoInquiryDescription",
                table: "SiteSettings",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoInquiryTitle",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoProjectsDescription",
                table: "SiteSettings",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoProjectsTitle",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentPostId",
                table: "Posts",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FaviconPath", "SeoBlogDescription", "SeoBlogTitle", "SeoEstimatorDescription", "SeoEstimatorTitle", "SeoGuideDescription", "SeoGuideTitle", "SeoHomeDescription", "SeoHomeTitle", "SeoInquiryDescription", "SeoInquiryTitle", "SeoProjectsDescription", "SeoProjectsTitle" },
                values: new object[] { null, null, null, null, null, null, null, null, null, null, null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ParentPostId",
                table: "Posts",
                column: "ParentPostId");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Posts_ParentPostId",
                table: "Posts",
                column: "ParentPostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Posts_ParentPostId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_ParentPostId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "FaviconPath",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SeoBlogDescription",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SeoBlogTitle",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SeoEstimatorDescription",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SeoEstimatorTitle",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SeoGuideDescription",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SeoGuideTitle",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SeoHomeDescription",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SeoHomeTitle",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SeoInquiryDescription",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SeoInquiryTitle",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SeoProjectsDescription",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SeoProjectsTitle",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ParentPostId",
                table: "Posts");
        }
    }
}
