using GateKeeper.Application.Users.Services;
using GateKeeper.Application.Clients.Services;
using GateKeeper.Application.Users.Validators;
using GateKeeper.Domain.Interfaces;
using GateKeeper.Infrastructure;
using GateKeeper.Infrastructure.Persistence;
using GateKeeper.Server.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;

namespace GateKeeper.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add Infrastructure layer (includes DbContext, Repositories, OpenIddict)
            builder.Services.AddInfrastructure(builder.Configuration);

            // Add Application layer services
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<ClientService>();

            // Add controllers with FluentValidation
            builder.Services.AddControllers();
            
            // Register FluentValidation automatic validation
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddValidatorsFromAssemblyContaining<RegisterUserDtoValidator>();

            // Configure CORS for React frontend
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(
                              "http://localhost:5173", 
                              "https://localhost:5173",
                              "http://localhost:63461",
                              "https://localhost:63461",
                              "https://localhost:63462",
                              "http://localhost:8080") // Demo OAuth client
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            // JWT Authentication + Cookie Authentication
            builder.Services.AddAuthentication(options =>
            {
                // Default to JWT for API endpoints
                options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
                    ClockSkew = TimeSpan.Zero
                };
            })
            .AddCookie(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.LoginPath = "/api/auth/login-page";
                options.ExpireTimeSpan = TimeSpan.FromHours(1);
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            });

            // Add authorization
            builder.Services.AddAuthorization();

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Apply migrations automatically and seed initial data
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await dbContext.Database.MigrateAsync();
                
                // Seed initial data for development (only runs if DB is empty)
                var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                await ApplicationDbContextSeed.SeedAsync(dbContext, passwordHasher);
            }

            app.UseDefaultFiles();
            app.MapStaticAssets();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            // Global exception handling
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseHttpsRedirection();

            app.UseCors(); // Enable CORS

            app.UseStaticFiles(); // For React build files

            app.UseRouting();

            app.UseAuthentication(); // Must come before Authorization
            app.UseAuthorization();

            app.MapControllers();

            // Fallback to React app for client-side routing
            // This will serve index.html for any route that doesn't match an API endpoint
            app.MapFallbackToFile("/index.html");

            await app.RunAsync();
        }
    }
}
