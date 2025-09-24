using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YazilimYonetimSistemi.Web.Models;

namespace YazilimYonetimSistemi.Web.Data.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");

        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(profile => profile.NormalizedEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(profile => profile.NormalizedEmail)
            .IsUnique();

        builder.Property(profile => profile.DisplayName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(profile => profile.Role)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(profile => profile.PasswordHash)
            .HasMaxLength(512);

        builder.HasOne(profile => profile.Department)
            .WithMany(department => department.Users)
            .HasForeignKey(profile => profile.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(profile => profile.AuthenticationProvider)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(profile => profile.IsActive)
            .HasDefaultValue(true);

        builder.Property(profile => profile.LastLoginAt);
    }
}
