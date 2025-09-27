using Microsoft.EntityFrameworkCore;
using YourApp.Domain.Entities;

namespace YourApp.Infrastructure.Data;

public class ApplicationDbContext :DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    
    //* DbSets for your entities
    public DbSet<User> User { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        //! Additional configuration can go here
        
        //* Set primary key for each entity
        modelBuilder.Entity<User>()
            .HasKey(x => x.UserId);
        
        //* Configure properties of each entity
        modelBuilder.Entity<User>()
            .HasIndex(x => x.Email)
            .IsUnique(); //* Set unique index on Email
        
        modelBuilder.Entity<User>()
            .HasIndex(x => x.UserName)
            .IsUnique(); //* Set unique index on UserName
        
        //* Configure relationships between entities if any here
    }
}