using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyMitt.Infrastructure.Persistence;

public sealed class EasyMittDbContext(DbContextOptions<EasyMittDbContext> options) : DbContext(options)
{
    public DbSet<CompanyEntity> Companies => Set<CompanyEntity>();

    public DbSet<UserEntity> Users => Set<UserEntity>();

    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();

    public DbSet<ProductEntity> Products => Set<ProductEntity>();

    public DbSet<InventoryMovementEntity> InventoryMovements => Set<InventoryMovementEntity>();

    public DbSet<QuoteEntity> Quotes => Set<QuoteEntity>();

    public DbSet<ExpenseEntity> Expenses => Set<ExpenseEntity>();

    public DbSet<DatevSettingsEntity> DatevSettings => Set<DatevSettingsEntity>();

    public DbSet<DatevExportLogEntity> DatevExportLogs => Set<DatevExportLogEntity>();

    public DbSet<BankTransactionEntity> BankTransactions => Set<BankTransactionEntity>();

    public DbSet<PaymentAllocationEntity> PaymentAllocations => Set<PaymentAllocationEntity>();

    public DbSet<DunningReminderEntity> DunningReminders => Set<DunningReminderEntity>();

    public DbSet<InvoiceDraftEntity> InvoiceDrafts => Set<InvoiceDraftEntity>();

    public DbSet<EmailDeliveryLogEntity> EmailDeliveryLogs => Set<EmailDeliveryLogEntity>();

    public DbSet<CustomerPortalAccessEntity> CustomerPortalAccesses => Set<CustomerPortalAccessEntity>();

    public DbSet<AiSuggestionEntity> AiSuggestions => Set<AiSuggestionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EasyMittDbContext).Assembly);
    }
}
