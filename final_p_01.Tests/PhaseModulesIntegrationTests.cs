using final_p_01.Controllers;
using final_p_01.Domain.Entities;
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
}
