using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using UserManagement.Extensions;
using UserManagement.Infrastructure;
using UserManagement.Middleware;
using UserManagement.Services;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}


builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "User Management API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add<UserStatusFilter>();
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<PasswordHasherService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddHttpClient<EmailService>();

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_URL") ?? "localhost:6379";

    if (redisConnectionString.StartsWith("redis://") || redisConnectionString.StartsWith("rediss://"))
    {
        var uri = new Uri(redisConnectionString);
        var options = new ConfigurationOptions
        {
            EndPoints = { { uri.Host, uri.Port } },
            AbortOnConnectFail = false,
            Ssl = uri.Scheme == "rediss"
        };

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var parts = uri.UserInfo.Split(':');
            if (parts.Length == 2 && !string.IsNullOrEmpty(parts[1]))
            {
                options.Password = parts[1];
            }
        }

        return ConnectionMultiplexer.Connect(options);
    }

    return ConnectionMultiplexer.Connect(redisConnectionString);
});

var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(frontendUrl)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Management API v1"));


app.UseAuthentication();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.Run();