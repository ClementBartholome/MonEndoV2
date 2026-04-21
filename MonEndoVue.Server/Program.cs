using System.Text;
using System.Text.Json.Serialization;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Hubs;
using MonEndoVue.Server.Jobs;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
using Quartz;
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

            // TypeGen generate --project-folder "C:\\Users\\Clementoss\\source\\repos\\MonEndoVue\\MonEndoVue.Server" --output-folder "C:\\Users\\Clementoss\\source\\repos\\MonEndoVue\\monendovue.client\\src\\interfaces"

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("Logs/MonEndoVue-.log", rollingInterval: RollingInterval.Month)
                .CreateLogger();

            builder.Host.UseSerilog();

            // Add services to the container.
            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            }
            else
            {
                builder.Services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            }

            builder.Services.AddScoped<CarnetSanteService>();
            builder.Services.AddScoped<TokenService>();
            builder.Services.AddScoped<DeviceTokenService>();
            builder.Services.AddScoped<NotificationService>();
            // builder.Services.AddHostedService<NotificationService>(serviceProvider =>
            // {
            //     var logger = serviceProvider.GetRequiredService<ILogger<NotificationService>>();
            //     var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            //     return new NotificationService(serviceProvider, logger, httpClientFactory);
            // });
            // builder.Services.AddHttpClient("PingClient", client =>
            // {
            //     client.Timeout = TimeSpan.FromMinutes(2);
            // });


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
                .AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
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

            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 15 * 1024 * 1024; // 15 MB
            });




            builder.Services.AddControllers().AddJsonOptions(options =>
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


            FirebaseApp.Create(new AppOptions()
            {
                Credential =
                    GoogleCredential.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        "serviceAccountKey.json")),
            });

            builder.Services.AddSingleton(FirebaseMessaging.DefaultInstance);

            builder.Services.AddQuartz(q =>
            {
                // Job des notifications (21h)
                var notificationJobKey = JobKey.Create("SendPushNotifications");
                q.AddJob<NotificationJob>(opts => opts.WithIdentity(notificationJobKey));
                q.AddTrigger(opts => opts
                    .ForJob(notificationJobKey)
                    .WithIdentity("SendPushNotifications-trigger")
                    .WithCronSchedule("0 00 21 * * ?"));

                // // Job du ping (toutes les 30 minutes)
                // var pingJobKey = JobKey.Create("PingApplication");
                // q.AddJob<PingJob>(opts => opts.WithIdentity(pingJobKey));
                // q.AddTrigger(opts => opts
                //     .ForJob(pingJobKey)
                //     .WithIdentity("PingApplication-trigger")
                //     .WithSimpleSchedule(s => s
                //         .WithIntervalInMinutes(30)
                //         .RepeatForever()));
            });

            builder.Services.AddQuartzHostedService(opts => { opts.WaitForJobsToComplete = true; });

            // builder.Services.AddHttpClient("PingClient", client =>
            // {
            //     client.Timeout = TimeSpan.FromMinutes(2);
            // });

            builder.Services.AddHttpClient("OneSignalClient", client =>
            {
                client.BaseAddress = new Uri("https://onesignal.com");
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {builder.Configuration["OneSignal:ApiKey"]}");
                client.DefaultRequestHeaders.Add("Content-Type", "application/json");
            });

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

            builder.Services.AddSignalR();

            builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                using var scope = app.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // await dbContext.Database.MigrateAsync();

                await RootUserSeeder.Seed(scope, builder.Configuration, dbContext);
            }

            if (app.Environment.IsProduction())
            {
                using var scope = app.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await RootUserSeeder.Seed(scope, builder.Configuration, dbContext);

                await dbContext.Database.MigrateAsync();

                app.UseHsts();
            }

            var identityApi = app.MapIdentityApi<ApplicationUser>();
            identityApi.RequireRateLimiting("auth");

            var notificationHub = app.MapHub<NotificationHub>("/notificationHub");
            notificationHub.RequireRateLimiting("api");

            app.UseCors("CorsPolicy");

            app.Use(async (context, next) =>
            {
                context.Response.Headers["X-Frame-Options"] = "DENY";
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                context.Response.Headers["Permissions-Policy"] = "camera=(self), microphone=(), geolocation=()";
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
            app.MapControllers().RequireRateLimiting("api");
            app.MapFallbackToFile("/index.html");
            await app.RunAsync();
        }
    }
}