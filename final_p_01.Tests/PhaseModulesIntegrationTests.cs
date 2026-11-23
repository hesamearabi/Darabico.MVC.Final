using final_p_01.Controllers;
using final_p_01.Domain.Entities;
using final_p_01.Application.Common.Interfaces;
using final_p_01.Infrastructure.Data;
using Darabico.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace final_p_01.Tests;

/// <summary>
/// Integration tests against a real relational provider (SQLite in-memory),
/// because the atomic booking reservation relies on ExecuteUpdate semantics.
/// </summary>
public class PhaseModulesIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;

    public PhaseModulesIntegrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    private final_p_01.Areas.Admin.Controllers.PhaseModulesController AdminController() =>
        new(_context, new CacheStore());

    private sealed class CacheStore : Microsoft.AspNetCore.OutputCaching.IOutputCacheStore
    {
        public ValueTask<byte[]?> GetAsync(string key, CancellationToken token) => ValueTask.FromResult<byte[]?>(null);
        public ValueTask SetAsync(string key, byte[] value, string[]? tags, TimeSpan validFor, CancellationToken token) => ValueTask.CompletedTask;
        public ValueTask EvictByTagAsync(string tag, CancellationToken token) => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task AdminSettings_ResolveExistingViews()
    {
        var controller = AdminController();
        Assert.Equal("SiteSettings", Assert.IsType<ViewResult>(await controller.SiteSettingsForm()).ViewName);
        Assert.Equal("EstimatorSettings", Assert.IsType<ViewResult>(await controller.EstimatorSettingsForm()).ViewName);
    }

    [Fact]
    public async Task EditPillarPage_UsesSharedEditorIncludingInvalidSubmission()
    {
        var page = new PillarPage { Slug = "regression", Title = "راهنمای پدل" };
        _context.PillarPages.Add(page);
        await _context.SaveChangesAsync();
        var controller = AdminController();
        Assert.Equal("CreatePillarPage", Assert.IsType<ViewResult>(await controller.EditPillarPage(page.Id)).ViewName);
        controller.ModelState.AddModelError("Title", "عنوان الزامی است.");
        Assert.Equal("CreatePillarPage", Assert.IsType<ViewResult>(await controller.EditPillarPage(page.Id, page)).ViewName);
    }

    [Fact]
    public async Task InvalidSettings_DoNotPersistAndReturnEditor()
    {
        var original = await _context.SiteSettings.AsNoTracking().Select(s => s.WorkshopVideoTitle).SingleAsync();
        var controller = AdminController();
        controller.ModelState.AddModelError("WorkshopVideoTitle", "عنوان معتبر نیست.");
        var result = Assert.IsType<ViewResult>(await controller.SiteSettingsForm(new SiteSettings { WorkshopVideoTitle = "نباید ذخیره شود" }));
        Assert.Equal("SiteSettings", result.ViewName);
        Assert.Equal(original, await _context.SiteSettings.AsNoTracking().Select(s => s.WorkshopVideoTitle).SingleAsync());
    }

    [Fact]
    public async Task InvalidLeadUpdate_PreservesInputAndReturnsDetails()
    {
        var lead = new InquiryLead { FullName = "آزمون", Mobile = "09123456789" };
        _context.InquiryLeads.Add(lead);
        await _context.SaveChangesAsync();
        var controller = new final_p_01.Areas.Admin.Controllers.LeadsController(_context, null!);
        controller.ModelState.AddModelError("Status", "وضعیت معتبر نیست.");
        var result = Assert.IsType<ViewResult>(await controller.Update(lead.Id, new final_p_01.ViewModels.LeadUpdateViewModel { AssignedTo = "پیگیری" }, null, default));
        Assert.Equal("Details", result.ViewName);
        Assert.Equal(final_p_01.Domain.Enums.LeadStatus.New, lead.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyLeadNote_ReturnsDetailsWithValidation(string text)
    {
        var lead = new InquiryLead { FullName = "آزمون", Mobile = "09123456789" };
        _context.InquiryLeads.Add(lead);
        await _context.SaveChangesAsync();
        var controller = new final_p_01.Areas.Admin.Controllers.LeadsController(_context, null!);
        var result = Assert.IsType<ViewResult>(await controller.AddNote(lead.Id, text, default));
        Assert.Equal("Details", result.ViewName);
        Assert.False(controller.ModelState.IsValid);
        Assert.False(await _context.InquiryLeadNotes.AnyAsync());
    }

    [Theory]
    [InlineData(400, "StatusCode")]
    [InlineData(429, "StatusCode")]
    [InlineData(404, "NotFound")]
    [InlineData(399, "NotFound")]
    [InlineData(600, "NotFound")]
    public void ErrorPages_ExplainSubmissionFailures(int status, string view)
    {
        var controller = new HomeController(null!, null!, null!, null!)
        {
            ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() }
        };
        Assert.Equal(view, Assert.IsType<ViewResult>(controller.PageNotFound(status)).ViewName);
        Assert.Equal(status is >= 400 and <= 599 ? status : 404, controller.Response.StatusCode);
    }

    [Theory]
    [InlineData("09:00", "09:00", 1)]
    [InlineData("10:00", "09:00", 1)]
    [InlineData("09:00", "10:00", 0)]
    [InlineData("invalid", "10:00", 1)]
    public async Task InvalidBookingSlot_DoesNotPersist(string start, string end, int capacity)
    {
        var controller = AdminController();
        var count = await _context.BookingSlots.CountAsync();
        var result = Assert.IsType<ViewResult>(await controller.CreateBookingSlot(new BookingSlot
        {
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), StartTime = start, EndTime = end, Capacity = capacity
        }));
        Assert.Equal("BookingSlots", result.ViewName);
        Assert.False(controller.ModelState.IsValid);
        Assert.Equal(count, await _context.BookingSlots.CountAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task InvalidEstimatorMultiplier_ReturnsFeedback(decimal multiplier)
    {
        var controller = AdminController();
        var result = Assert.IsType<ViewResult>(await controller.AddTier("استاندارد", multiplier, 1));
        Assert.Equal("EstimatorSettings", result.ViewName);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public void PersianValidation_TranslatesDefaultMessagesWithoutReplacingExplicitOnes()
    {
        var key = Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ModelMetadataIdentity.ForType(typeof(string));
        var context = new Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ValidationMetadataProviderContext(key,
            Microsoft.AspNetCore.Mvc.ModelBinding.ModelAttributes.GetAttributesForType(typeof(string)));
        var length = new System.ComponentModel.DataAnnotations.MaxLengthAttribute(12);
        var required = new System.ComponentModel.DataAnnotations.RequiredAttribute { ErrorMessage = "پیام اختصاصی" };
        context.ValidationMetadata.ValidatorMetadata.Add(length);
        context.ValidationMetadata.ValidatorMetadata.Add(required);
        new final_p_01.Models.PersianValidationMetadataProvider().CreateValidationMetadata(context);
        Assert.Equal("«نام» نباید بیش از 12 کاراکتر باشد.", length.FormatErrorMessage("نام"));
        Assert.Equal("پیام اختصاصی", required.ErrorMessage);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    // ---- Booking: atomic reservation (no overbooking) ----

    [Fact]
    public async Task Booking_ReserveLastFreeSlot_SecondReservationFails()
    {
        _context.BookingSlots.Add(new BookingSlot { Date = DateOnly.FromDateTime(DateTime.UtcNow), Capacity = 1, BookedCount = 0, IsActive = true });
        await _context.SaveChangesAsync();

        var first = await ReserveAsync(slotId: 1);
        var second = await ReserveAsync(slotId: 1);

        Assert.Equal(1, first);  // اولین رزرو موفق
        Assert.Equal(0, second); // دومین رزرو رد می‌شود
        Assert.Equal(1, (await _context.BookingSlots.AsNoTracking().SingleAsync()).BookedCount);
    }

    [Fact]
    public async Task Booking_ReserveUpToCapacity_BeyondCapacityFails()
    {
        _context.BookingSlots.Add(new BookingSlot { Date = DateOnly.FromDateTime(DateTime.UtcNow), Capacity = 2, IsActive = true });
        await _context.SaveChangesAsync();

        Assert.Equal(1, await ReserveAsync(1));
        Assert.Equal(1, await ReserveAsync(1));
        Assert.Equal(0, await ReserveAsync(1)); // ظرفیت تکمیل
    }

    [Fact]
    public async Task Booking_InactiveSlot_CannotBeReserved()
    {
        _context.BookingSlots.Add(new BookingSlot { Date = DateOnly.FromDateTime(DateTime.UtcNow), Capacity = 5, IsActive = false });
        await _context.SaveChangesAsync();

        Assert.Equal(0, await ReserveAsync(1));
    }

    private async Task<int> ReserveAsync(int slotId) =>
        await _context.BookingSlots
            .Where(s => s.Id == slotId && s.IsActive && s.BookedCount < s.Capacity)
            .ExecuteUpdateAsync(u => u.SetProperty(s => s.BookedCount, s => s.BookedCount + 1));

    // ---- City page: hard publish rule (>= 1 published project) ----

    [Fact]
    public async Task CityPage_WithPublishedProject_ReturnsView()
    {
        _context.CityPages.Add(new CityPage { CityName = "تهران", Slug = "tehran", Title = "t", ContentHtml = "<p>x</p>", IsPublished = true });
        _context.Projects.Add(new Project { Title = "پروژه", City = "تهران", IsPublished = true });
        await _context.SaveChangesAsync();

        var controller = new CityPagesController(_context);
        var result = await controller.Details("tehran");

        Assert.IsNotType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CityPage_WithoutPublishedProjects_ReturnsNotFound()
    {
        _context.CityPages.Add(new CityPage { CityName = "شیراز", Slug = "shiraz", Title = "t", ContentHtml = "<p>x</p>", IsPublished = true });
        await _context.SaveChangesAsync();

        var controller = new CityPagesController(_context);

        Assert.IsType<NotFoundResult>(await controller.Details("shiraz"));
    }

    [Fact]
    public async Task CityPage_WithOnlyUnpublishedProject_ReturnsNotFound()
    {
        _context.CityPages.Add(new CityPage { CityName = "اصفهان", Slug = "isfahan", Title = "t", ContentHtml = "<p>x</p>", IsPublished = true });
        _context.Projects.Add(new Project { Title = "پیش‌نویس", City = "اصفهان", IsPublished = false });
        await _context.SaveChangesAsync();

        var controller = new CityPagesController(_context);

        Assert.IsType<NotFoundResult>(await controller.Details("isfahan"));
    }

    [Fact]
    public async Task CityPage_UnpublishedPage_ReturnsNotFound()
    {
        _context.CityPages.Add(new CityPage { CityName = "تبریز", Slug = "tabriz", Title = "t", ContentHtml = "<p>x</p>", IsPublished = false });
        _context.Projects.Add(new Project { Title = "پروژه", City = "تبریز", IsPublished = true });
        await _context.SaveChangesAsync();

        var controller = new CityPagesController(_context);

        Assert.IsType<NotFoundResult>(await controller.Details("tabriz"));
    }

    [Fact]
    public async Task DeleteLead_RemovesLeadNotesAttachmentsAndFiles()
    {
        var lead = new InquiryLead { FullName = "آزمون حذف", Mobile = "09123456789" };
        lead.Attachments.Add(new InquiryLeadAttachment { StorageKey = "leads/test.txt", OriginalFileName = "test.txt", ContentType = "text/plain" });
        lead.Notes.Add(new InquiryLeadNote { Text = "یادداشت", Author = "مدیر", CreatedAtUtc = DateTime.UtcNow });
        _context.InquiryLeads.Add(lead);
        await _context.SaveChangesAsync();

        var deletedKeys = new List<string>();
        var storage = new DeletionTrackingStorage(keys => deletedKeys.AddRange(keys));
        var controller = WithTempData(new final_p_01.Areas.Admin.Controllers.LeadsController(_context, storage));

        var result = await controller.Delete(lead.Id, default);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        Assert.False(await _context.InquiryLeads.AsNoTracking().AnyAsync(l => l.Id == lead.Id));
        Assert.False(await _context.InquiryLeadAttachments.AsNoTracking().AnyAsync(a => a.InquiryLeadId == lead.Id));
        Assert.False(await _context.InquiryLeadNotes.AsNoTracking().AnyAsync(n => n.InquiryLeadId == lead.Id));
        Assert.Equal(new[] { "leads/test.txt" }, deletedKeys);
    }

    [Fact]
    public async Task DeleteLead_MissingLead_RedirectsWithoutThrowing()
    {
        var controller = WithTempData(new final_p_01.Areas.Admin.Controllers.LeadsController(_context, new DeletionTrackingStorage(_ => { })));
        var result = await controller.Delete(999, default);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task SaveSeo_PersistsTitlesAndUploadedFavicon()
    {
        var controller = WithTempData(new final_p_01.Areas.Admin.Controllers.SettingsController(_context));
        var model = new final_p_01.Areas.Admin.Models.SiteSeoViewModel
        {
            SeoHomeTitle = "ساخت زمین پدل | دارابیکو",
            SeoGuideDescription = "راهنمای جامع ساخت زمین پدل"
        };
        var faviconBytes = new byte[] { 0, 0, 1, 0, 1, 0, 16, 16, 32, 0, 1, 0, 32, 0, 168, 0, 0, 0, 0, 0 };
        var favicon = new Microsoft.AspNetCore.Http.FormFile(new MemoryStream(faviconBytes), 0, faviconBytes.Length, "favicon", "favicon.ico");

        var result = await controller.SaveSeo(model, favicon);

        Assert.IsType<RedirectToActionResult>(result);
        var settings = await _context.SiteSettings.AsNoTracking().SingleAsync();
        Assert.Equal("ساخت زمین پدل | دارابیکو", settings.SeoHomeTitle);
        Assert.Equal("راهنمای جامع ساخت زمین پدل", settings.SeoGuideDescription);
        Assert.Equal("/uploads/branding/favicon.ico", settings.FaviconPath);
    }

    [Fact]
    public async Task SaveSeo_RejectsDisallowedFaviconExtension()
    {
        var controller = WithTempData(new final_p_01.Areas.Admin.Controllers.SettingsController(_context));
        var bytes = new byte[] { 1, 2, 3 };
        var exe = new Microsoft.AspNetCore.Http.FormFile(new MemoryStream(bytes), 0, bytes.Length, "favicon", "favicon.exe");

        var result = await controller.SaveSeo(new final_p_01.Areas.Admin.Models.SiteSeoViewModel(), exe);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.True(controller.TempData.ContainsKey("SeoError"));
        Assert.Null(await _context.SiteSettings.AsNoTracking().Select(s => s.FaviconPath).SingleAsync());
    }

    private static T WithTempData<T>(T controller) where T : Microsoft.AspNetCore.Mvc.Controller
    {
        controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            new Microsoft.AspNetCore.Http.DefaultHttpContext(), new MemoryTempDataProvider());
        return controller;
    }

    private sealed class MemoryTempDataProvider : Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(Microsoft.AspNetCore.Http.HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(Microsoft.AspNetCore.Http.HttpContext context, IDictionary<string, object> values) { }
    }

    private sealed class DeletionTrackingStorage : IInquiryAttachmentStorage
    {
        private readonly Action<string[]> _onDelete;
        public DeletionTrackingStorage(Action<string[]> onDelete) => _onDelete = onDelete;
        public void Delete(string storageKey) => _onDelete(new[] { storageKey });
        public Task<InquiryLeadAttachment> SaveAsync(UploadFile file, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Stream Open(string storageKey) => throw new NotSupportedException();
    }
}
