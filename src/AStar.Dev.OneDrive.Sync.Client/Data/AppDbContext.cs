using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AccountEntity>    Accounts    => Set<AccountEntity>();
    public DbSet<SyncFolderEntity> SyncFolders => Set<SyncFolderEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountEntity>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasMany(a => a.SyncFolders)
             .WithOne(f => f.Account)
             .HasForeignKey(f => f.AccountId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SyncFolderEntity>(e =>
        {
            e.HasKey(f => f.Id);
            e.HasIndex(f => new { f.AccountId, f.FolderId }).IsUnique();
        });
    }
}
