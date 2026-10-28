using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using App.Domain;
namespace App.Data;
public static class SeedData
{
    public static async Task Initialize(IServiceProvider services,IConfiguration configuration)
    {
        var db=services.GetRequiredService<AppDbContext>();
        // Development bootstrap only. Production deployments require reviewed migrations.
        await db.Database.EnsureCreatedAsync();
        var users=services.GetRequiredService<UserManager<AppUser>>();
        var roles=services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach(var role in new[]{"Admin","Staff","Citizen"})
            if(!await roles.RoleExistsAsync(role))await roles.CreateAsync(new IdentityRole(role));
        var password=configuration["Seed:Password"];
        if(string.IsNullOrWhiteSpace(password))
        {
            if(!await users.Users.AnyAsync())throw new InvalidOperationException("Set Seed:Password with dotnet user-secrets or Seed__Password before first run.");
            return;
        }
        foreach(var (email,role,name) in new[]{("admin@example.test","Admin","Local Administrator"),("staff@example.test","Staff","Local Staff"),("citizen@example.test","Citizen","Sample Citizen"),("other@example.test","Citizen","Other Citizen")})
        {
            var user=await users.FindByEmailAsync(email);
            if(user is null)
            {
                user=new AppUser {UserName=email,Email=email,EmailConfirmed=true,DisplayName=name};
                var result=await users.CreateAsync(user,password);
                if(!result.Succeeded)throw new InvalidOperationException(string.Join("; ",result.Errors.Select(e=>e.Description)));
            }
            if(!await users.IsInRoleAsync(user,role))await users.AddToRoleAsync(user,role);
        }
        if (!await db.Providers.AnyAsync())
        {
            var a = new Provider { Name="Jordan Taylor",Specialty="General services" };
            var b = new Provider { Name="Morgan Lee",Specialty="Accessibility services" };
            db.Providers.AddRange(a,b);
            for(int day=1;day<=21;day++)
            {
                var date=DateTime.UtcNow.Date.AddDays(day);
                if(date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)continue;
                foreach(var provider in new[]{a,b})foreach(var hour in new[]{9,10,11,13,14,15})
                    db.Slots.Add(new ScheduleSlot {Provider=provider,StartUtc=date.AddHours(hour),EndUtc=date.AddHours(hour).AddMinutes(30)});
            }
            await db.SaveChangesAsync();
        }
    }
}
