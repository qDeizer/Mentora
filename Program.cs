using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PsikologProje_Void.Data;
using PsikologProje_Void.Filters;
using PsikologProje_Void.Middleware;
using PsikologProje_Void.Models;
using PsikologProje_Void.Options;
using PsikologProje_Void.Services;
using PsikologProje_Void.Services.Email;
using PsikologProje_Void.Services.EmailVerification;
using PsikologProje_Void.Services.Upload;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Security.Claims;
using System.Threading.RateLimiting;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    {
        var logDirectory = Path.Combine(context.HostingEnvironment.ContentRootPath, "logs");
        Directory.CreateDirectory(logDirectory);

        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {SourceContext} {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Information)
            .WriteTo.File(
                Path.Combine(logDirectory, "mentora-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                shared: true,
                restrictedToMinimumLevel: LogEventLevel.Information,
                outputTemplate: "{Timestamp:o} [{Level:u3}] [{CorrelationId}] ({MachineName}/{EnvironmentName}/T{ThreadId}) {SourceContext} {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                new CompactJsonFormatter(),
                Path.Combine(logDirectory, "mentora-.ndjson"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                shared: true,
                restrictedToMinimumLevel: LogEventLevel.Debug);
    });

    builder.Services.Configure<DetailedLoggingOptions>(builder.Configuration.GetSection(DetailedLoggingOptions.SectionName));
    builder.Services.AddScoped<MvcAuditActionFilter>();

    builder.Services.AddControllersWithViews(options =>
    {
        options.Filters.AddService<MvcAuditActionFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")));

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddSession(options =>
    {
        options.Cookie.Name = ".Mentora.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.IdleTimeout = TimeSpan.FromHours(4);
    });

    var enableEfDetailedErrors = builder.Configuration.GetValue<bool?>("DetailedLogging:EnableEfDetailedErrors") ?? true;
    var enableEfSensitiveDataLogging = builder.Configuration.GetValue<bool?>("DetailedLogging:EnableEfSensitiveDataLogging") ?? false;

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions.UseNetTopologySuite());

        if (enableEfDetailedErrors)
        {
            options.EnableDetailedErrors();
        }

        if (enableEfSensitiveDataLogging)
        {
            options.EnableSensitiveDataLogging();
        }
    });

    builder.Services.AddIdentity<User, IdentityRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 3;

        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;
        options.SignIn.RequireConfirmedPhoneNumber = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    // Çerezlerin güvenliği ve veritabanı ile senkronizasyonunu anlık yapmak için:
    builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    {
        options.ValidationInterval = TimeSpan.Zero; // Her request'te kullanıcının veritabanında olup olmadığını kontrol et
    });

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        // SameAsRequest keeps local HTTP demo sessions working while still marking cookies secure on HTTPS requests.
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddFixedWindowLimiter("auth", limiterOptions =>
        {
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.PermitLimit = 8;
            limiterOptions.QueueLimit = 0;
            limiterOptions.AutoReplenishment = true;
        });

        options.AddFixedWindowLimiter("request-write", limiterOptions =>
        {
            limiterOptions.Window = TimeSpan.FromSeconds(30);
            limiterOptions.PermitLimit = 15;
            limiterOptions.QueueLimit = 0;
            limiterOptions.AutoReplenishment = true;
        });
    });

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>("db");

    builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
    builder.Services.Configure<UploadPolicyOptions>(builder.Configuration.GetSection(UploadPolicyOptions.SectionName));

    builder.Services.AddScoped<IAppointmentService, AppointmentService>();
    builder.Services.AddScoped<IAppointmentRequestService, AppointmentRequestService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IAppointmentAutomationService, AppointmentAutomationService>();
    builder.Services.AddScoped<IClinicalNoteService, ClinicalNoteService>();
    builder.Services.AddScoped<IPeopleService, PeopleService>();
    builder.Services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IGlobalLocationContextService, GlobalLocationContextService>();
    builder.Services.AddScoped<IEmailOutboxService, EmailOutboxService>();
    builder.Services.AddScoped<ISmtpConfigurationValidator, SmtpConfigurationValidator>();
    builder.Services.AddScoped<IFileValidationService, FileValidationService>();
    builder.Services.AddScoped<IImageProcessingService, ImageProcessingService>();
    builder.Services.AddScoped<IFileStorageService, FileStorageService>();
    builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
    // E-posta gönderici: SMTP portları bloklu ortamlarda (DigitalOcean vb) HTTPS API kullanılır
    var useHttpApi = builder.Configuration.GetValue<bool?>("Smtp:UseHttpApi") ?? false;
    if (useHttpApi)
    {
        builder.Services.AddSingleton<IEmailSender, HttpEmailSender>();
    }
    else
    {
        builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
    }

    builder.Services.AddHostedService<AppointmentStatusUpdaterService>();
    builder.Services.AddHostedService<AppointmentAutomationRunnerService>();
    builder.Services.AddHostedService<AppointmentReminderService>();
    builder.Services.AddHostedService<EmailQueueDispatcherService>();

    var app = builder.Build();

    app.UseForwardedHeaders();
    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
            diagnosticContext.Set("UserId", httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous");
            diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        };
        options.GetLevel = (httpContext, _, ex) =>
        {
            if (ex != null)
            {
                return LogEventLevel.Error;
            }

            return httpContext.Response.StatusCode >= 500
                ? LogEventLevel.Error
                : httpContext.Response.StatusCode >= 400
                    ? LogEventLevel.Warning
                    : LogEventLevel.Information;
        };
    });

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.Use(async (context, next) =>
    {
        context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
        context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
        context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.TryAdd("Permissions-Policy", "geolocation=(self), microphone=(), camera=()");
        await next();
    });

    app.UseMiddleware<DetailedRequestLoggingMiddleware>();

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();
    app.UseSession();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = _ => true
    });

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    using (var scope = app.Services.CreateScope())
    {
        Log.Information("Applying database migrations and seed data.");
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
        var seedDemoData = app.Configuration.GetValue<bool?>("Seed:DemoData") ?? true;
        await ApplicationDbSeeder.SeedAsync(scope.ServiceProvider, seedDemoData);
        Log.Information("Database migration and seeding completed.");
    }

    Log.Information("Mentora web host starting.");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Mentora terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
