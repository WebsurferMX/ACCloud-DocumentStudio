using System.Text;
using ACCloud.DocumentStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ACCloud.DocumentStudio.Persistence;

public sealed class DocumentStudioDbContext : DbContext
{
    public const string Schema = "document_studio";

    public DocumentStudioDbContext(DbContextOptions<DocumentStudioDbContext> options)
        : base(options)
    {
    }

    public DbSet<Template> Templates => Set<Template>();

    public DbSet<TemplateVersion> TemplateVersions => Set<TemplateVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Template>(entity =>
        {
            entity.ToTable("templates");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).ValueGeneratedNever();
            entity.Property(t => t.Name).HasMaxLength(Template.MaxNameLength).IsRequired();
            entity.Property(t => t.Description).HasMaxLength(Template.MaxDescriptionLength);
            entity.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            entity.Ignore(t => t.IsArchived);
            entity.HasIndex(t => t.Status);

            entity.HasMany(t => t.Versions)
                .WithOne()
                .HasForeignKey(v => v.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(t => t.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<TemplateVersion>(entity =>
        {
            entity.ToTable("template_versions");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.Id).ValueGeneratedNever();
            entity.Property(v => v.DefinitionJson).HasColumnType("jsonb").IsRequired();
            entity.Property(v => v.SampleDataJson).HasColumnType("jsonb");
            entity.Property(v => v.Comment).HasMaxLength(Template.MaxCommentLength);

            // Dos guardados simultáneos no pueden crear el mismo número de versión.
            entity.HasIndex(v => new { v.TemplateId, v.VersionNumber }).IsUnique();
        });

        ApplySnakeCase(modelBuilder);
    }

    /// <summary>Columnas, llaves e índices en snake_case (convención de PostgreSQL).</summary>
    private static void ApplySnakeCase(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }

            foreach (var key in entity.GetKeys())
            {
                key.SetName(ToSnakeCase(key.GetName()!));
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                foreignKey.SetConstraintName(ToSnakeCase(foreignKey.GetConstraintName()!));
            }

            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()!));
            }
        }
    }

    internal static string ToSnakeCase(string name)
    {
        var builder = new StringBuilder(name.Length + 8);

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (char.IsUpper(c))
            {
                if (i > 0 && name[i - 1] != '_' && (char.IsLower(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1]))))
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(c));
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}
