using ERecruitment.Application.Abstractions;
using ERecruitment.Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace ERecruitment.Infrastructure.Seeding
{
    public static class SeedData
    {
        public static async Task SeedSuperAdminAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var hasher = new PasswordHasher<AppUser>();

            var exists = await db.Users.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == null && x.Role == "SuperAdmin");

            if (exists) return;

            var sa = new AppUser
            {
                Id = Guid.NewGuid(),
                TenantId = null,
                FullName = "Super Admin",
                Email = "superadmin@erecruitment.com",
                Role = "SuperAdmin",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            sa.PasswordHash = hasher.HashPassword(sa, "SuperAdmin@123");

            db.Users.Add(sa);
            await db.SaveChangesAsync(default);
        }
    }
}
