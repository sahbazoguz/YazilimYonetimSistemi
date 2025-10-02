using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UludagSoftwareTracking.Data;
using UludagSoftwareTracking.Services.Implementations;
using UludagSoftwareTracking.Services.Interfaces;
using UludagSoftwareTracking.Services.Security;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=UludagSoftwareTracking;Trusted_Connection=True;MultipleActiveResultSets=true";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = NegotiateDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Giris";
        options.AccessDeniedPath = "/Home/Hata";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    })
    .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(RoleConstants.Policies.RequireBirimKullanicisi,
        policy => policy.RequireRole(RoleConstants.Roles.BirimKullanicisi, RoleConstants.Roles.BirimYetkilisi, RoleConstants.Roles.Admin));
    options.AddPolicy(RoleConstants.Policies.RequireBirimYetkilisi,
        policy => policy.RequireRole(RoleConstants.Roles.BirimYetkilisi, RoleConstants.Roles.Admin));
    options.AddPolicy(RoleConstants.Policies.RequireDegerlendirici,
        policy => policy.RequireRole(
            RoleConstants.Roles.DegerlendiriciBir,
            RoleConstants.Roles.DegerlendiriciIki,
            RoleConstants.Roles.DegerlendiriciUc,
            RoleConstants.Roles.Admin));
    options.AddPolicy(RoleConstants.Policies.RequireBaskan,
        policy => policy.RequireRole(RoleConstants.Roles.DegerlendirmeBaskani, RoleConstants.Roles.Admin));
    options.AddPolicy(RoleConstants.Policies.RequireYazilimci,
        policy => policy.RequireRole(RoleConstants.Roles.Yazilimci, RoleConstants.Roles.Admin));
    options.AddPolicy(RoleConstants.Policies.RequireAdmin,
        policy => policy.RequireRole(RoleConstants.Roles.Admin));
    options.AddPolicy(RoleConstants.Policies.RequireWorkflowEditor,
        policy => policy.RequireRole(RoleConstants.Roles.BirimKullanicisi, RoleConstants.Roles.Yazilimci, RoleConstants.Roles.Admin));
});

builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<ISoftwareCatalogService, SoftwareCatalogService>();
builder.Services.AddScoped<IRequestWorkflowService, RequestWorkflowService>();
builder.Services.AddScoped<IManualService, ManualService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<ISoftwareManagementService, SoftwareManagementService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IClaimsTransformation, UserClaimsTransformation>();
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
})
.AddViewLocalization()
.AddDataAnnotationsLocalization();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var culture = new CultureInfo("tr-TR");
    options.DefaultRequestCulture = new RequestCulture(culture);
    options.SupportedCultures = new List<CultureInfo> { culture };
    options.SupportedUICultures = new List<CultureInfo> { culture };
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SeedData.EnsureSeedDataAsync(dbContext);
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Hata");
    app.UseHsts();
}

var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(localizationOptions);

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
