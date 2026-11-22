using final_p_01.Domain.Entities;
using final_p_01.Domain.Enums;
using final_p_01.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace final_p_01.Tests;

public class SqlServerChunk1Tests
{
    [Fact]
    public async Task MigrationAndLeadGraphPersistOnSqlServerWhenConfigured()
    {
        var connection = Environment.GetEnvironmentVariable("CHUNK1_SQL_CONNECTION");
        if (string.IsNullOrEmpty(connection)) return;
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);
        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var lead = new InquiryLead { LeadType = "inquiry", FullName = "SQL integration", Mobile = "09123456789", City = "تهران", SiteType = "زمین خالی", CourtCount = 20, CourtType = 0, EstimatedStartTime = "تا ۱ ماه", Status = LeadStatus.ProposalSent, AssignedTo = "پیگیری تست", Source = "inquiry", Notes = [new InquiryLeadNote { Text = "یادداشت تست", Author = "integration" }], Attachments = [new InquiryLeadAttachment { StorageKey = "test.pdf", OriginalFileName = "test.pdf", ContentType = "application/pdf", ByteLength = 100 }] };
        db.InquiryLeads.Add(lead); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var loaded = await db.InquiryLeads.Include(l => l.Attachments).Include(l => l.Notes).AsSplitQuery().SingleAsync(l => l.Id == lead.Id);
        Assert.Equal(20, loaded.CourtCount); Assert.Equal(LeadStatus.ProposalSent, loaded.Status);
        Assert.Single(loaded.Attachments); Assert.Single(loaded.Notes); Assert.Null(loaded.UtmCampaign);
        await transaction.RollbackAsync();
    }
}
