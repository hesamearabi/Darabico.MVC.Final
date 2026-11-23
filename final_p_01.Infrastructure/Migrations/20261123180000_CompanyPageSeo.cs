using final_p_01.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace final_p_01.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261123180000_CompanyPageSeo")]
public partial class CompanyPageSeo : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "SeoAboutTitle", table: "SiteSettings", type: "nvarchar(200)", maxLength: 200, nullable: true);
        migrationBuilder.AddColumn<string>(name: "SeoAboutDescription", table: "SiteSettings", type: "nvarchar(320)", maxLength: 320, nullable: true);
        migrationBuilder.AddColumn<string>(name: "SeoContactTitle", table: "SiteSettings", type: "nvarchar(200)", maxLength: 200, nullable: true);
        migrationBuilder.AddColumn<string>(name: "SeoContactDescription", table: "SiteSettings", type: "nvarchar(320)", maxLength: 320, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "SeoAboutTitle", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "SeoAboutDescription", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "SeoContactTitle", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "SeoContactDescription", table: "SiteSettings");
    }
}
