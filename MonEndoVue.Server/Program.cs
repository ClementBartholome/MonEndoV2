using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Jobs;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Agenda;
using MonEndoVue.Server.Services.WebPush;
using MonEndoVue.Server.Services.WebPush.Rappels;
using Quartz;
using MonEndoVue.Server.Services.Accueil;
using MonEndoVue.Server.Services.Consentement;
using MonEndoVue.Server.Services.Export;
using MonEndoVue.Server.Services.Photos;
using MonEndoVue.Server.Services.SuppressionCompte;
using MonEndoVue.Server.Services.Activite;
using MonEndoVue.Server.Services.Cycle;
using MonEndoVue.Server.Services.Traitements;
using Serilog;
using Serilog.Events;
using System.Threading.RateLimiting;


namespace MonEndoVue.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: false,
                    reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddUserSecrets<Program>();

            // Hors dev : Information pour l'application, Warning pour le framework (EF Core, ASP.NET Core).
            // Un fichier par jour, supprimé automatiquement au bout de 30 jours.
            var estDev = builder.Environment.IsDevelopment();
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(estDev ? LogEventLevel.Debug : LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft", estDev ? LogEventLevel.Information : LogEventLevel.Warning)
                .MinimumLevel.Override("System", estDev ? LogEventLevel.Information : LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("Logs/MonEndoVue-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileTimeLimit: TimeSpan.FromDays(30))
                .CreateLogger();

            builder.Host.UseSerilog();

            // Add services to the container.
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<CarnetSanteService>();
            builder.Services.AddScoped<TokenService>();
            builder.Services.AddScoped<HistoriqueBilansService>();
            // Notifications Web Push (clés VAPID dans la section WebPush ; sans elles, envoi désactivé)
            builder.Services.Configure<WebPushOptions>(builder.Configuration.GetSection(WebPushOptions.Section));
            builder.Services.AddHttpClient<IEnvoiPush, WebPushService>();
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddScoped<NotificationsPushService>();
            builder.Services.AddScoped<NotificationsService>();
            // Un rappel = une règle (ajouter un type : nouvelle implémentation de IRegleRappel)
            builder.Services.AddScoped<IRegleRappel, RappelBilanQuotidien>();
            builder.Services.AddScoped<IRegleRappel, RappelSuiviAcne>();

            // Agenda Google en lecture : clé API côté serveur, calendrier associé à une utilisatrice par la configuration
            builder.Services.Configure<AgendaOptions>(builder.Configuration.GetSection(AgendaOptions.Section));
            builder.Services.AddHttpClient<AgendaService>(client => client.Timeout = TimeSpan.FromSeconds(10));

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", policyBuilder =>
                {
                    policyBuilder.WithOrigins("https://localhost:7206/", "https://localhost:5173",
                            "http://localhost:5173",
                            "https://monendoapp.fr",
                            "https://localhost:5175")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .WithExposedHeaders("Access-Control-Allow-Origin")
                        .AllowCredentials();
                });
            });

            builder.Services
                .AddDefaultIdentity<ApplicationUser>(OptionsIdentite.Appliquer)
                .AddEntityFrameworkStores<AppDbContext>();

            var azureBlobOptions = new AzureBlobStorageOptions
            {
                ConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING") 
                                   ?? builder.Configuration["AzureBlobStorage:ConnectionString"],
                ContainerName = Environment.GetEnvironmentVariable("AZURE_CONTAINER_NAME") 
                                ?? builder.Configuration["AzureBlobStorage:ContainerName"]
            };

            builder.Services.Configure<AzureBlobStorageOptions>(options =>
            {
                options.ConnectionString = azureBlobOptions.ConnectionString;
                options.ContainerName = azureBlobOptions.ContainerName;
            });

            builder.Services.AddScoped<AzureBlobStorageService>();
            builder.Services.AddScoped<IStockagePhotos>(sp => sp.GetRequiredService<AzureBlobStorageService>());
            builder.Services.AddScoped<ExportDonneesService>();
            builder.Services.AddScoped<SuppressionCompteService>();
            builder.Services.AddScoped<ComptesInactifsService>();
            builder.Services.AddScoped<AccueilService>();
            builder.Services.AddScoped<TraitementsService>();
            builder.Services.AddScoped<CycleService>();
            builder.Services.AddScoped<AcneService>();
            builder.Services.AddScoped<ActiviteService>();

            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 15 * 1024 * 1024; // 15 MB
            });




            builder.Services.AddControllers(options => options.Filters.Add<ExigeConsentementFilter>()).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
            });

            builder.Services.AddEndpointsApiExplorer();

            var keysDirectory = Path.Combine(Directory.GetCurrentDirectory(), "keys");
            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory))
                .SetApplicationName("MonEndoVue");

            builder.Services.AddAuthorization();

            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddFixedWindowLimiter("api", limiterOptions =>
                {
                    limiterOptions.PermitLimit = 120;
                    limiterOptions.Window = TimeSpan.FromMinutes(1);
                    limiterOptions.QueueLimit = 0;
                    limiterOptions.AutoReplenishment = true;
                });

                options.AddFixedWindowLimiter("auth", limiterOptions =>
                {
                    limiterOptions.PermitLimit = 20;
                    limiterOptions.Window = TimeSpan.FromMinutes(1);
                    limiterOptions.QueueLimit = 0;
                    limiterOptions.AutoReplenishment = true;
                });
            });

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    var validIssuer = builder.Configuration["Authentication:Schemes:Bearer:ValidIssuer"];
                    var validAudiences = builder.Configuration
                        .GetSection("Authentication:Schemes:Bearer:ValidAudiences").Get<string[]>();
                    var secret = builder.Configuration["Authentication:Schemes:Bearer:Secret"];

                    var hasIssuer = !string.IsNullOrWhiteSpace(validIssuer);
                    var hasAudiences = validAudiences is { Length: > 0 };

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = hasIssuer,
                        ValidateAudience = hasAudiences,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ClockSkew = TimeSpan.FromMinutes(1),
                        ValidIssuer = validIssuer,
                        ValidAudiences = validAudiences,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret))
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                            logger.LogError(context.Exception, "Authentication failed.");
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                            logger.LogInformation("Token validated.");
                            return Task.CompletedTask;
                        }
                    };
                });


            builder.Services.AddQuartz(q =>
            {
                // Rappel quotidien du bilan, à l'heure choisie par chaque utilisatrice
                var rappelBilanJobKey = JobKey.Create("RappelBilan");
                q.AddJob<RappelBilanJob>(opts => opts.WithIdentity(rappelBilanJobKey));
                q.AddTrigger(opts => opts
                    .ForJob(rappelBilanJobKey)
                    .WithIdentity("RappelBilan-trigger")
                    .WithCronSchedule(RappelBilanJob.Cron));

                // Durée de conservation : suppression des comptes inactifs depuis 2 ans
                var comptesInactifsJobKey = JobKey.Create("SuppressionComptesInactifs");
                q.AddJob<SuppressionComptesInactifsJob>(opts => opts.WithIdentity(comptesInactifsJobKey));
                q.AddTrigger(opts => opts
                    .ForJob(comptesInactifsJobKey)
                    .WithIdentity("SuppressionComptesInactifs-trigger")
                    .WithCronSchedule(SuppressionComptesInactifsJob.Cron));
            });

            builder.Services.AddQuartzHostedService(opts => { opts.WaitForJobsToComplete = true; });

            builder.Services.AddSwaggerGen(option =>
            {
                option.SwaggerDoc("v1", new OpenApiInfo { Title = "Demo API", Version = "v1" });
                option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter a valid token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });
                option.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        []
                    }
                });
            });


            builder.Services.AddHealthChecks();

            builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

            var app = builder.Build();

            var erreurWebPush = app.Services.GetRequiredService<IOptions<WebPushOptions>>().Value.Erreur();
            if (erreurWebPush is not null)
            {
                app.Logger.LogWarning("Web Push notifications disabled: {Raison}", erreurWebPush);
            }

            if (app.Environment.IsDevelopment())
            {
                using var scope = app.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await RootUserSeeder.Seed(scope, builder.Configuration, dbContext);
            }

            // Pas de compte créé automatiquement en production : les comptes passent par l'inscription (Account/register).
            if (app.Environment.IsProduction())
            {
                using var scope = app.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await dbContext.Database.MigrateAsync();

                app.UseHsts();
            }


            app.UseCors("CorsPolicy");

            app.Use(async (context, next) =>
            {
                EntetesSecurite.Appliquer(context.Response.Headers);
                await next();
            });

            // Middleware to add the Authorization header from the cookie to the request headers
            app.Use(async (context, next) =>
            {
                var token = context.Request.Cookies["accessToken"];
                if (!string.IsNullOrEmpty(token) && !context.Request.Headers.ContainsKey("Authorization"))
                {
                    context.Request.Headers.Append("Authorization", $"Bearer {token}");
                }

                await next();
            });

            app.UseHttpsRedirection();
            app.UseDefaultFiles();
            app.UseStaticFiles();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }


            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRateLimiter();
            app.MapControllers().Add(endpoint => PolitiquesDebit.AppliquerParDefaut(endpoint, PolitiquesDebit.Api));
            // Sonde de disponibilité utilisée par le pipeline de déploiement (anonyme, hors rate limit).
            app.MapHealthChecks("/health").AllowAnonymous();
            app.MapFallbackToFile("/index.html");
            await app.RunAsync();
        }
    }
}