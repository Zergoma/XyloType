using Microsoft.EntityFrameworkCore;

using XyloType.Domain.Entities;

namespace XyloType.Infrastructure.DbContexts;

public class DactyloDbContext : DbContext
{
    public DbSet<Word> Words => Set<Word>();

    public DbSet<WordAnalysis> WordAnalyses => Set<WordAnalysis>();

    public DbSet<ImportedSource> ImportedSources => Set<ImportedSource>();

    public DbSet<UserProfile> Users => Set<UserProfile>();

    public DbSet<ExerciseAttempt> ExerciseAttempts => Set<ExerciseAttempt>();

    public DactyloDbContext(
        DbContextOptions<DactyloDbContext> options)
        : base(options)
    {
        
    }


    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        
    }

    private static void ConfigureWord(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Word>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Text)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.LanguageCode)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(x => x.OccurrenceCount)
                .HasDefaultValue(1);

            // INDEX
            entity.HasIndex(x => new
            {
                x.Text,
                x.LanguageCode 
            })
            .IsUnique();

            entity.HasIndex(x => x.LanguageCode);
            entity.HasIndex(x => x.Length);
            entity.HasIndex(x => x.OccurrenceCount);
            entity.HasIndex(x => x.IsExcluded);
        });
    }

    private static void ConfigureWordAnalysis(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WordAnalysis>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Layout)
                .HasConversion<int>();

            entity.Property(x => x.RowMask)
                .HasConversion<int>();

            entity.Property(x => x.FingerMask)
                .HasConversion<int>();

            // RELATION
            entity.HasOne(x => x.Word)
                .WithMany(w => w.Analyses)
                .HasForeignKey(x => x.WordId)
                .OnDelete(DeleteBehavior.Cascade);

            // INDEXES
            entity.HasIndex(x => x.Layout);

            entity.HasIndex(x => x.WordId);

            entity.HasIndex(x => new
            {
                x.Layout,
                x.RowMask
            });

            entity.HasIndex(x => new
            {
                x.Layout,
                x.FingerMask
            });

            entity.HasIndex(x => new
            {
                x.Layout,
                x.UsesLeftHand,
                x.UsesRightHand
            });

            entity.HasIndex(x => new
            {
                x.WordId,
                x.Layout
            })
            .IsUnique();

        });
    }

    private static void ConfigureImportedSource(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImportedSource>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(300);

            entity.Property(x => x.FileName)
                .HasMaxLength(300);

            entity.Property(x => x.ContentHash)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(x => x.LanguageCode)
                .HasMaxLength(10);

            entity.HasIndex(x => x.ContentHash);
        });
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(UserProfile.NameMaxLength);

            // RELATION: the results go with their user
            entity.HasMany(x => x.Attempts)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureExerciseAttempt(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExerciseAttempt>(entity =>
        {
            entity.HasKey(x => x.Id);

            // INDEX: the results of a user, per exercise
            entity.HasIndex(x => new
            {
                x.UserId,
                x.ExerciseId
            });
        });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureWord(modelBuilder);
        ConfigureWordAnalysis(modelBuilder);
        ConfigureImportedSource(modelBuilder);
        ConfigureUser(modelBuilder);
        ConfigureExerciseAttempt(modelBuilder);
    }
}
