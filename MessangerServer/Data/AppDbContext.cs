using MessengerServer.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;



namespace MessengerServer.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Chat> Chats { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<ChatMember> ChatMembers { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<Contact> Contacts { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Replace with your actual connection string from appsettings.json
            optionsBuilder.UseSqlServer("workstation id=MessengerBD.mssql.somee.com;packet size=4096;user id=Durhol;pwd=Dur11221122!;data source=MessengerBD.mssql.somee.com;persist security info=False;initial catalog=MessengerBD;TrustServerCertificate=True");
        }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // ChatMember composite key
        modelBuilder.Entity<ChatMember>()
            .HasKey(cm => new { cm.ChatId, cm.UserId });

        modelBuilder.Entity<ChatMember>()
            .HasOne(cm => cm.Chat)
            .WithMany(c => c.Members)
            .HasForeignKey(cm => cm.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChatMember>()
            .HasOne(cm => cm.User)
            .WithMany(u => u.ChatMemberships)
            .HasForeignKey(cm => cm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Message configuration
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Chat)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Group configuration
        modelBuilder.Entity<Group>()
            .HasOne(g => g.Chat)
            .WithOne(c => c.Group)
            .HasForeignKey<Group>(g => g.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Group>()
            .HasOne(g => g.Owner)
            .WithMany(u => u.OwnedGroups)
            .HasForeignKey(g => g.OwnerId)
            .OnDelete(DeleteBehavior.NoAction);

        // Contact configuration
        modelBuilder.Entity<Contact>()
            .HasOne(c => c.User)
            .WithMany(u => u.Contacts)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Contact>()
            .HasOne(c => c.ContactUser)
            .WithMany(u => u.ContactedBy)
            .HasForeignKey(c => c.ContactUserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Contact>()
            .HasIndex(c => new { c.UserId, c.ContactUserId })
            .IsUnique();
    }
}
