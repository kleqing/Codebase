using System.Reflection;
using System.Text;
using System.Text.Json;
using dotenv.net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using YourApp.Application.Services.Interfaces.Email;
using YourApp.Infrastructure.Data;
using YourApp.Infrastructure.Services.Email;
using YourApp.Shared.Jwt;
using YourApp.Shared.Utils;

namespace YourApp.WebApi;

public class Program
{
    public static void Main(string[] args)
    {
        DotEnv.Load();
        var builder = WebApplication.CreateBuilder(args);
        
        var corsPolicy = "AllowAll";

        //* Register database context
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                               Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING") ?? string.Empty;
        
        builder.Services.AddDbContext<ApplicationDbContext>(options
            => options.UseSqlServer(connectionString));
        //! NOTE: If using another SQL database (e.g. PostgreSQL, MySQL, etc.), change the above line accordingly.
        
        //* JWT Authentication
        builder.Services.Configure<Jwt>(options =>
        {
            options.Secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? builder.Configuration["Jwt:Secret"] ?? string.Empty;
            options.Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["Jwt:Issuer"] ?? string.Empty;
            options.Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["Jwt:Audience"] ?? string.Empty;
            options.ExpiryInMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES") ?? builder.Configuration["Jwt:ExpiryInMinutes"] ?? "60");
        });
        
        //* Lowercase URLs
        builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
        
        //* CORS Policy
        builder.Services.AddCors(options => 
        {
            options.AddPolicy(name: corsPolicy,
                policy =>
                {
                    policy.WithOrigins("https://example.com") //* For testing, change to AllowAnyOrigin() in production, also remove AllowCredentials()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
        });
        
        //* Add services to the container.
        var currentAssembly  = typeof(Program).Assembly;
        var referencedAssemblies = currentAssembly .GetReferencedAssemblies()
            .Where(x => x.Name!.StartsWith("YourApp") && x.Name != null); //! Replace "YourApp" with your actual project prefix
        
        var assemblies = referencedAssemblies
            .Select(Assembly.Load)
            .Append(currentAssembly);

        foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
        {
            if (type.IsClass && !type.IsAbstract)
            {
                foreach (var iface in type.GetInterfaces())
                {
                    if (iface.Name == $"I{type.Name}")
                    {
                        builder.Services.AddScoped(iface, type);
                    }
                }
            }
        }
        
        //* Some DI registrations can override for services lifetimes
        builder.Services.AddTransient<IEmailSender, EmailSender>();
        builder.Services.AddSingleton<CloudinaryUploader>();

        //* Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "YourApp API Name", Version = "v1", Description = "YourApp API Description" });
            
            //* Add JWT Authentication to Swagger
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "bearer"
            });
            
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                    new string[]{}
                }
            });
        });
        
        //* Additional for controllers with json
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase; //* Use original property names
                options.JsonSerializerOptions.PropertyNameCaseInsensitive =
                    true; //* Enable case-insensitive property names
            });

        //* Add Authentication and Authorization
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer("Bearer", _ => { })
        .AddGoogle(options =>
        {
            var clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? builder.Configuration["Authentication:Google:ClientId"] ?? string.Empty;
            var clientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
            options.ClientId = clientId;
            options.ClientSecret = clientSecret;
            options.SaveTokens = true;
            options.ClaimActions.MapJsonKey("picture", "picture");
            options.CallbackPath = "/signin-google";
        });
        
        builder.Services.PostConfigure<JwtBearerOptions>("Bearer", options =>
        {
            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["Jwt:Issuer"] ?? string.Empty;
            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["Jwt:Audience"] ?? string.Empty;
            var secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? builder.Configuration["Jwt:Secret"] ?? string.Empty;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ClockSkew = TimeSpan.Zero, //* Disable the default 5-minute clock skew
                RequireExpirationTime = true //* Require the token to have an expiration time
            };
        });
        
        builder.Services.AddAuthorization();
        builder.Services.AddHttpContextAccessor();
        
        //* Redis Cache
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ?? 
                                        builder.Configuration["Redis:ConnectionString"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(redisConnectionString))
            {
                throw new InvalidOperationException("Redis connection string is not set in environment variables.");
            }

            var configurationOptions = ConfigurationOptions.Parse(redisConnectionString, true);
            configurationOptions.AbortOnConnectFail = false;

            try
            {
                return ConnectionMultiplexer.Connect(configurationOptions);
            }
            catch (RedisConnectionException ex)
            {
                throw new InvalidOperationException("Failed to connect to Redis: " + ex.Message, ex);
            }
        });
        
        builder.Services.AddScoped<IDatabase>(sp =>
        {
            var connectionMultiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
            return connectionMultiplexer.GetDatabase();
        });
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("EnableSwagger"))
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            // Custom error handling endpoint
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Internal Server Error" }));
                });
            });
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseRouting();
        app.UseCors(corsPolicy);

        app.UseAuthentication();
        app.UseAuthorization();
        
        app.MapControllers();

        app.Run();
    }
}