using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
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
                .WriteTo.File("Logs/MonEndoVue-.log", rollingInterval: RollingInterval.Day)
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
                            "https://monendoapp.fr")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .WithExposedHeaders("Access-Control-Allow-Origin")
                        .AllowCredentials();
                });
            });

            builder.Services
                .AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<AppDbContext>();

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

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    var validIssuer = builder.Configuration["Authentication:Schemes:Bearer:ValidIssuer"];
                    var validAudiences = builder.Configuration
                        .GetSection("Authentication:Schemes:Bearer:ValidAudiences").Get<string[]>();
                    var secret = builder.Configuration["Authentication:Schemes:Bearer:Secret"];

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = validIssuer,
                        ValidAudiences = validAudiences,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret))
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                            logger.LogError("Authentication failed.", context.Exception);
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
            
            builder.Services.AddQuartzHostedService(opts => 
            {
                opts.WaitForJobsToComplete = true;
            });
            
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
                        new string[] { }
                    }
                });
            });
            
            builder.Services.AddSignalR();

            builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
            
            var app = builder.Build();
            
            app.MapHub<NotificationHub>("/notificationHub");

            if (app.Environment.IsDevelopment())
            {
                using var scope = app.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await dbContext.Database.MigrateAsync();

                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                var rootUser = new ApplicationUser
                {
                    UserName = "coralie.owczaruk@yahoo.fr", Email = "coralie.owczaruk@yahoo.fr", EmailConfirmed = true
                };

                if (await userManager.FindByNameAsync(rootUser.UserName) == null)
                {
                    await userManager.CreateAsync(rootUser, "Password123$");
                }
            }

            if (app.Environment.IsProduction())
            {
                using var scope = app.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await dbContext.Database.MigrateAsync();
                
                app.UseHsts();
                app.Use(async (context, next) =>
                {
                    context.Response.Headers.Append("X-Frame-Options", "DENY");
                    await next();
                });
            }

            app.MapIdentityApi<ApplicationUser>();

            app.UseCors("CorsPolicy");
            
            // Middleware to add the Authorization header from the cookie to the request headers
            app.Use(async (context, next) =>
            {
                var token = context.Request.Cookies["accessToken"];
                if (!string.IsNullOrEmpty(token))
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
            app.MapControllers();
            app.MapFallbackToFile("/index.html");
            await app.RunAsync();
        }
    }
}