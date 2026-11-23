using System.ComponentModel.DataAnnotations;
using final_p_01.Application.Services;
using final_p_01.Controllers;
using final_p_01.Domain.Entities;
using final_p_01.Domain.Enums;
using final_p_01.Infrastructure.Data;
using final_p_01.Models;
using final_p_01.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace final_p_01.Tests;

public class InquiryTests
{
    [Theory]
    [InlineData("۰۹۱۲۳۴۵۶۷۸۹", "09123456789")]
    [InlineData("٠٩١٢٣٤٥٦٧٨٩", "09123456789")]
    [InlineData("۲۰", "20")]
    public void DigitsNormalize(string input, string expected) => Assert.Equal(expected, DigitNormalizer.Normalize(input));

    [Theory]
    [InlineData("=SUM(A1:A2)")]
    [InlineData(" +CMD")]
    [InlineData("@formula")]
    [InlineData("\tunsafe")]
    public void CsvNeutralizesFormulas(string input) => Assert.StartsWith("\"'", final_p_01.Areas.Admin.Controllers.LeadsController.CsvCell(input));

    [Fact]
    public void ExactStatusesAndJalaliDates()
    {
        Assert.Equal(new[] { "New", "Contacted", "ProposalSent", "Contracted", "Rejected" }, Enum.GetNames<LeadStatus>());
        Assert.StartsWith("1405/", LeadPresentation.Date(new DateTime(2026, 11, 22, 12, 0, 0)));
        Assert.Equal(new DateTime(2026, 11, 21, 20, 30, 0), LeadPresentation.ParseDate("۱۴۰۵/۰۹/۰۱"));
    }

    private static InquiryFormViewModel Valid() => new() { City = "تهران", SiteType = "زمین خالی", CourtCount = "۲", CourtType = "روباز", FullName = "متقاضی تست", Mobile = "۰۹۱۲۳۴۵۶۷۸۹", EstimatedStartTime = "تا ۱ ماه" };

    [Fact]
    public async Task SubmissionNormalizesAndPersistsExactFields()
    {
        using var connection = new SqliteConnection("DataSource=:memory:"); connection.Open();
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        var controller = CreateController(db);
        var result = await controller.Index(Valid(), CancellationToken.None);
        Assert.IsType<RedirectToActionResult>(result);
        var lead = Assert.Single(db.InquiryLeads);
        Assert.Equal("09123456789", lead.Mobile); Assert.Equal(2, lead.CourtCount);
        Assert.Equal("زمین خالی", lead.SiteType); Assert.Equal("تا ۱ ماه", lead.EstimatedStartTime);
        Assert.Equal(LeadStatus.New, lead.Status); Assert.Null(lead.UtmSource);
    }

    [Theory]
    [InlineData("21", "09123456789", "زمین خالی")]
    [InlineData("2", "989123456789", "زمین خالی")]
    [InlineData("2", "09123456789", "نامعتبر")]
    public async Task InvalidFieldsDoNotPersist(string count, string mobile, string siteType)
    {
        using var connection = new SqliteConnection("DataSource=:memory:"); connection.Open();
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated(); var model = Valid(); model.CourtCount = count; model.Mobile = mobile; model.SiteType = siteType;
        var result = await CreateController(db).Index(model, CancellationToken.None);
        Assert.IsType<ViewResult>(result); Assert.Empty(db.InquiryLeads);
    }

    [Fact]
    public async Task StorageRejectsSpoofedAndOversizedFilesAndKeepsFilesPrivate()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new InquiryAttachmentStorage(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["InquiryStorage:Root"] = root }).Build(), new TestEnvironment());
            var fake = new FormFile(new MemoryStream("not an image"u8.ToArray()), 0, 12, "Files", "fake.jpg");
            await Assert.ThrowsAsync<InvalidDataException>(() => storage.SaveAsync(fake, CancellationToken.None));
            var oversized = new FormFile(Stream.Null, 0, InquiryAttachmentStorage.MaxBytes + 1, "Files", "large.pdf");
            await Assert.ThrowsAsync<InvalidDataException>(() => storage.SaveAsync(oversized, CancellationToken.None));
            var pdf = "%PDF-1.4\n1 0 obj << /Type /Catalog >> endobj\n%%EOF"u8.ToArray();
            var saved = await storage.SaveAsync(new FormFile(new MemoryStream(pdf), 0, pdf.Length, "Files", "test.pdf"), CancellationToken.None);
            Assert.Equal("application/pdf", saved.ContentType);
            using (var stream = storage.Open(saved.StorageKey)) Assert.Equal(pdf.Length, stream.Length);
            Assert.Throws<InvalidDataException>(() => storage.Open("../test.pdf"));
            storage.Delete(saved.StorageKey); Assert.Empty(Directory.GetFiles(root));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static InquiryController CreateController(ApplicationDbContext db)
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        Microsoft.Extensions.DependencyInjection.MvcServiceCollectionExtensions.AddControllersWithViews(services);
        var provider = Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(services);
        return new InquiryController(db, new UnusedStorage()) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = provider }, RouteData = new Microsoft.AspNetCore.Routing.RouteData(), ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor() } };
    }

    private class UnusedStorage : IInquiryAttachmentStorage
    {
        public Task<InquiryLeadAttachment> SaveAsync(IFormFile file, CancellationToken token) => throw new InvalidOperationException();
        public Stream Open(string key) => throw new InvalidOperationException();
        public void Delete(string key) => throw new InvalidOperationException();
    }
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MissingStorageRootCreatesDurablePrivateFallback(string? configuredRoot)
    {
        var parent = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var contentRoot = Path.Combine(parent, "site");
        var expectedRoot = Path.Combine(parent, "App_Data", "inquiry-attachments");
        var environment = new TestEnvironment { ContentRootPath = contentRoot, WebRootPath = Path.Combine(contentRoot, "wwwroot") };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["InquiryStorage:Root"] = configuredRoot }).Build();
        try
        {
            var storage = new InquiryAttachmentStorage(configuration, environment);
            Assert.True(Directory.Exists(expectedRoot));
            var pdf = "%PDF-1.4\n1 0 obj << /Type /Catalog >> endobj\n%%EOF"u8.ToArray();
            var file = new FormFile(new MemoryStream(pdf), 0, pdf.Length, "Files", "site.pdf");
            var attachment = await storage.SaveAsync(file, CancellationToken.None);
            Assert.True(File.Exists(Path.Combine(expectedRoot, attachment.StorageKey)));
            using (var stream = new InquiryAttachmentStorage(configuration, environment).Open(attachment.StorageKey))
            using (var copy = new MemoryStream())
            {
                await stream.CopyToAsync(copy);
                Assert.Equal(pdf, copy.ToArray());
            }
            storage.Delete(attachment.StorageKey);
            Assert.False(File.Exists(Path.Combine(expectedRoot, attachment.StorageKey)));
        }
        finally { if (Directory.Exists(parent)) Directory.Delete(parent, true); }
    }

    [Theory]
    [InlineData("relative")]
    [InlineData("wwwroot")]
    [InlineData("wwwroot/")]
    [InlineData("wwwroot/uploads")]
    [InlineData("private/../wwwroot/uploads")]
    public void ExplicitInvalidStorageRootIsRejected(string path)
    {
        var parent = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var environment = new TestEnvironment { ContentRootPath = parent, WebRootPath = Path.Combine(parent, "wwwroot") };
        var root = path == "relative" ? path : Path.Combine(parent, path);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["InquiryStorage:Root"] = root }).Build();
        Assert.Throws<InvalidOperationException>(() => new InquiryAttachmentStorage(configuration, environment));
        Assert.False(Directory.Exists(parent));
    }

    [Fact]
    public async Task JsonSubmissionPersistsWithoutRedirect()
    {
        using var connection = new SqliteConnection("DataSource=:memory:"); connection.Open();
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        var controller = CreateController(db);
        controller.Request.Headers.Accept = "application/json";
        var result = Assert.IsType<JsonResult>(await controller.Index(Valid(), CancellationToken.None));
        Assert.True(System.Text.Json.JsonSerializer.SerializeToElement(result.Value).GetProperty("success").GetBoolean());
        db.ChangeTracker.Clear();
        Assert.Equal("09123456789", Assert.Single(await db.InquiryLeads.ToListAsync()).Mobile);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("21")]
    public async Task JsonValidationReturnsFieldErrorsWithoutPersisting(string count)
    {
        using var connection = new SqliteConnection("DataSource=:memory:"); connection.Open();
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        var controller = CreateController(db);
        controller.Request.Headers.Accept = "application/json";
        var model = Valid(); model.CourtCount = count;
        var result = Assert.IsType<JsonResult>(await controller.Index(model, CancellationToken.None));
        Assert.Equal(422, result.StatusCode);
        Assert.True(System.Text.Json.JsonSerializer.SerializeToElement(result.Value).GetProperty("errors").TryGetProperty("CourtCount", out _));
        Assert.Empty(db.InquiryLeads);
    }

    [Theory]
    [InlineData("home", "Home")]
    [InlineData("inquiry", "Inquiry")]
    public async Task NativeSubmissionReturnsToSourceInsteadOfThanks(string source, string expectedController)
    {
        using var connection = new SqliteConnection("DataSource=:memory:"); connection.Open();
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        var controller = CreateController(db);
        controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(controller.HttpContext, new MemoryTempDataProvider());
        var model = Valid(); model.Source = source;
        var result = Assert.IsType<RedirectToActionResult>(await controller.Index(model, CancellationToken.None));
        Assert.Equal("Index", result.ActionName);
        Assert.Equal(expectedController, result.ControllerName);
        Assert.Equal(true, controller.TempData["InquirySubmitted"]);
        Assert.Single(db.InquiryLeads);
    }

    private class MemoryTempDataProvider : Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "test";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "wwwroot");
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
