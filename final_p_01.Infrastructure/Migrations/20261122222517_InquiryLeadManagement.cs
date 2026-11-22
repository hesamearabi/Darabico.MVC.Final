using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace final_p_01.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InquiryLeadManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InquiryBudgetRanges",
                table: "SiteSettings",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InquiryCities",
                table: "SiteSettings",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InquiryThanksBody",
                table: "SiteSettings",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InquiryThanksTitle",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LeadNotificationEmail",
                table: "SiteSettings",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeadNotificationPhone",
                table: "SiteSettings",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApproximateBudget",
                table: "InquiryLeads",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedTo",
                table: "InquiryLeads",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstimatedStartTime",
                table: "InquiryLeads",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LandingPage",
                table: "InquiryLeads",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "InquiryLeads",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Referrer",
                table: "InquiryLeads",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SiteType",
                table: "InquiryLeads",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "InquiryLeads",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "InquiryLeads",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UtmCampaign",
                table: "InquiryLeads",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmMedium",
                table: "InquiryLeads",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmSource",
                table: "InquiryLeads",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InquiryLeadAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InquiryLeadId = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ByteLength = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InquiryLeadAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InquiryLeadAttachments_InquiryLeads_InquiryLeadId",
                        column: x => x.InquiryLeadId,
                        principalTable: "InquiryLeads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InquiryLeadNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InquiryLeadId = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Author = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InquiryLeadNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InquiryLeadNotes_InquiryLeads_InquiryLeadId",
                        column: x => x.InquiryLeadId,
                        principalTable: "InquiryLeads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "InquiryBudgetRanges", "InquiryCities", "InquiryThanksBody", "InquiryThanksTitle", "LeadNotificationEmail", "LeadNotificationPhone" },
                values: new object[] { "", "", "کارشناسان دارابیکو برای بررسی پروژه با شما تماس می‌گیرند.", "درخواست شما ثبت شد", null, null });

            migrationBuilder.CreateIndex(
                name: "IX_InquiryLeads_ProjectId",
                table: "InquiryLeads",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_InquiryLeads_Source_CreatedAtUtc",
                table: "InquiryLeads",
                columns: new[] { "Source", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InquiryLeads_Status_CreatedAtUtc_Id",
                table: "InquiryLeads",
                columns: new[] { "Status", "CreatedAtUtc", "Id" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_InquiryLeads_Status",
                table: "InquiryLeads",
                sql: "[Status] IN (0,1,2,3,4)");

            migrationBuilder.CreateIndex(
                name: "IX_InquiryLeadAttachments_InquiryLeadId",
                table: "InquiryLeadAttachments",
                column: "InquiryLeadId");

            migrationBuilder.CreateIndex(
                name: "IX_InquiryLeadNotes_InquiryLeadId",
                table: "InquiryLeadNotes",
                column: "InquiryLeadId");

            migrationBuilder.Sql("UPDATE InquiryLeads SET Status = CASE WHEN IsHandled = 1 THEN 1 ELSE 0 END, Source = LeadType;");
            migrationBuilder.Sql("UPDATE SiteSettings SET InquiryThanksTitle = N'درخواست شما ثبت شد', InquiryThanksBody = N'کارشناسان دارابیکو برای بررسی پروژه با شما تماس می‌گیرند.';");

            migrationBuilder.AddForeignKey(
                name: "FK_InquiryLeads_Projects_ProjectId",
                table: "InquiryLeads",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InquiryLeads_Projects_ProjectId",
                table: "InquiryLeads");

            migrationBuilder.DropTable(
                name: "InquiryLeadAttachments");

            migrationBuilder.DropTable(
                name: "InquiryLeadNotes");

            migrationBuilder.DropIndex(
                name: "IX_InquiryLeads_ProjectId",
                table: "InquiryLeads");

            migrationBuilder.DropIndex(
                name: "IX_InquiryLeads_Source_CreatedAtUtc",
                table: "InquiryLeads");

            migrationBuilder.DropIndex(
                name: "IX_InquiryLeads_Status_CreatedAtUtc_Id",
                table: "InquiryLeads");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InquiryLeads_Status",
                table: "InquiryLeads");

            migrationBuilder.DropColumn(
                name: "InquiryBudgetRanges",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "InquiryCities",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "InquiryThanksBody",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "InquiryThanksTitle",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "LeadNotificationEmail",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "LeadNotificationPhone",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ApproximateBudget",
                table: "InquiryLeads");

            migrationBuilder.DropColumn(
                name: "AssignedTo",
                table: "InquiryLeads");

            migrationBuilder.DropColumn(
                name: "EstimatedStartTime",
                table: "InquiryLeads");

            migrationBuilder.DropColumn(
                name: "LandingPage",
                table: "InquiryLeads");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "InquiryLeads");

            migrationBuilder.DropColumn(
                name: "Referrer",
                table: "InquiryLeads");

            migrationBuilder.DropColumn(
                name: "SiteType",
                table: "InquiryLeads");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "InquiryLeads");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "InquiryLeads");

            migrationBuilder.DropColumn(
                name: "UtmCampaign",
                table: "InquiryLeads");

            migrationBuilder.DropColumn(
                name: "UtmMedium",
                table: "InquiryLeads");

            migrationBuilder.DropColumn(
                name: "UtmSource",
                table: "InquiryLeads");
        }
    }
}
