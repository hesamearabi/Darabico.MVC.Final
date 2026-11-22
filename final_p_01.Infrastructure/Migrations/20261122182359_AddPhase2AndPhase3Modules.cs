using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace final_p_01.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase2AndPhase3Modules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthorName",
                table: "Posts",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Canonical",
                table: "Posts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaTitle",
                table: "Posts",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAtUtc",
                table: "Posts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PostId",
                table: "Faqs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "PageKey",
                table: "Faqs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Faqs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "BookingSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    EndTime = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    BookedCount = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingSlots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CityPages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContentHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetaTitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    Canonical = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OgImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityPages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EstimatorSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BasePricePerCourtOutdoor = table.Column<long>(type: "bigint", nullable: false),
                    BasePricePerCourtIndoor = table.Column<long>(type: "bigint", nullable: false),
                    UpperBoundMultiplier = table.Column<decimal>(type: "decimal(5,3)", precision: 5, scale: 3, nullable: false),
                    Disclaimer = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstimatorSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PillarPages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    ContentHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetaTitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    Canonical = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OgImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PillarPages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessStages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstimatedDuration = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IconClass = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StepNumber = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessStages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CanonicalHost = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CanonicalUseWww = table.Column<bool>(type: "bit", nullable: false),
                    WorkshopVideoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WorkshopVideoPosterUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WorkshopVideoTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    WorkshopVideoDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FeasibilityPdfPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FeasibilityPageTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FeasibilityPageIntro = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FeasibilityMetaTitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    FeasibilityMetaDescription = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    PaymentTermsHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WarrantyHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterSalesIntro = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EnableMultiLanguage = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InquiryLeads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeadType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Mobile = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CourtCount = table.Column<int>(type: "int", nullable: true),
                    CourtType = table.Column<byte>(type: "tinyint", nullable: true),
                    MaterialTier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EstimatedMinCost = table.Column<long>(type: "bigint", nullable: true),
                    EstimatedMaxCost = table.Column<long>(type: "bigint", nullable: true),
                    BookingSlotId = table.Column<int>(type: "int", nullable: true),
                    ServiceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VisitType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccessToken = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsHandled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InquiryLeads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InquiryLeads_BookingSlots_BookingSlotId",
                        column: x => x.BookingSlotId,
                        principalTable: "BookingSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EstimatorCityMultiplier",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Multiplier = table.Column<decimal>(type: "decimal(5,3)", precision: 5, scale: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EstimatorSettingsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstimatorCityMultiplier", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstimatorCityMultiplier_EstimatorSettings_EstimatorSettingsId",
                        column: x => x.EstimatorSettingsId,
                        principalTable: "EstimatorSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EstimatorMaterialTier",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Multiplier = table.Column<decimal>(type: "decimal(5,3)", precision: 5, scale: 3, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EstimatorSettingsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstimatorMaterialTier", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstimatorMaterialTier_EstimatorSettings_EstimatorSettingsId",
                        column: x => x.EstimatorSettingsId,
                        principalTable: "EstimatorSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "EstimatorSettings",
                columns: new[] { "Id", "BasePricePerCourtIndoor", "BasePricePerCourtOutdoor", "Disclaimer", "UpdatedAtUtc", "UpperBoundMultiplier" },
                values: new object[] { 1, 0L, 0L, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1.35m });

            migrationBuilder.InsertData(
                table: "SiteSettings",
                columns: new[] { "Id", "AfterSalesIntro", "CanonicalHost", "CanonicalUseWww", "EnableMultiLanguage", "FeasibilityMetaDescription", "FeasibilityMetaTitle", "FeasibilityPageIntro", "FeasibilityPageTitle", "FeasibilityPdfPath", "PaymentTermsHtml", "UpdatedAtUtc", "WarrantyHtml", "WorkshopVideoDescription", "WorkshopVideoPosterUrl", "WorkshopVideoTitle", "WorkshopVideoUrl" },
                values: new object[] { 1, null, "darabico.com", false, false, null, null, null, "نمونه طرح توجیهی احداث زمین پدل", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Slug",
                table: "Posts",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Faqs_PageKey",
                table: "Faqs",
                column: "PageKey");

            migrationBuilder.CreateIndex(
                name: "IX_BookingSlots_Date_IsActive",
                table: "BookingSlots",
                columns: new[] { "Date", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CityPages_Slug",
                table: "CityPages",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentItems_IsPublished_SortOrder",
                table: "DocumentItems",
                columns: new[] { "IsPublished", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_EstimatorCityMultiplier_EstimatorSettingsId",
                table: "EstimatorCityMultiplier",
                column: "EstimatorSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimatorMaterialTier_EstimatorSettingsId",
                table: "EstimatorMaterialTier",
                column: "EstimatorSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_InquiryLeads_AccessToken",
                table: "InquiryLeads",
                column: "AccessToken");

            migrationBuilder.CreateIndex(
                name: "IX_InquiryLeads_BookingSlotId",
                table: "InquiryLeads",
                column: "BookingSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_InquiryLeads_IsHandled",
                table: "InquiryLeads",
                column: "IsHandled");

            migrationBuilder.CreateIndex(
                name: "IX_InquiryLeads_LeadType_CreatedAtUtc",
                table: "InquiryLeads",
                columns: new[] { "LeadType", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PillarPages_IsPublished_SortOrder",
                table: "PillarPages",
                columns: new[] { "IsPublished", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PillarPages_Slug",
                table: "PillarPages",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessStages_IsActive_SortOrder",
                table: "ProcessStages",
                columns: new[] { "IsActive", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CityPages");

            migrationBuilder.DropTable(
                name: "DocumentItems");

            migrationBuilder.DropTable(
                name: "EstimatorCityMultiplier");

            migrationBuilder.DropTable(
                name: "EstimatorMaterialTier");

            migrationBuilder.DropTable(
                name: "InquiryLeads");

            migrationBuilder.DropTable(
                name: "PillarPages");

            migrationBuilder.DropTable(
                name: "ProcessStages");

            migrationBuilder.DropTable(
                name: "SiteSettings");

            migrationBuilder.DropTable(
                name: "EstimatorSettings");

            migrationBuilder.DropTable(
                name: "BookingSlots");

            migrationBuilder.DropIndex(
                name: "IX_Posts_Slug",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Faqs_PageKey",
                table: "Faqs");

            migrationBuilder.DropColumn(
                name: "AuthorName",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Canonical",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "MetaTitle",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "PublishedAtUtc",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "PageKey",
                table: "Faqs");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Faqs");

            migrationBuilder.AlterColumn<int>(
                name: "PostId",
                table: "Faqs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
