using BLL.Interfaces;
using BLL.Services;
using Core.Configurations;
using Core.Helpers;
using Core.Models;
using DAL;
using DAL.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

namespace Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
                    options.JsonSerializerOptions.WriteIndented = true;
                });

            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60); // Час життя сесії
                options.Cookie.HttpOnly = true; // Сесійні cookie доступні тільки для серверів
                options.Cookie.IsEssential = true; // Cookie для роботи сесії обов'язкові
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                //фільтр для підтримки IFormFile
                c.OperationFilter<SwaggerFileOperationFilter>();
            });


            // Configure CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontendApp", policy =>
                {
                    policy.WithOrigins("https://localhost:5173")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            builder.Services.AddDbContext<ApplicationContext>(options =>
            {
                options.UseLazyLoadingProxies()
                    .UseSqlServer(builder.Configuration.GetConnectionString("DBConnectionString"));
            });

            builder.Services.AddScoped<IDeviceService, DeviceService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IElixirService, ElixirService>();
            builder.Services.AddScoped<IHistoryService, HistoryService>();
            builder.Services.AddScoped<INoteService, NoteService>();
            builder.Services.AddScoped<IPreferencesService, PreferencesService>();
            builder.Services.AddScoped<IFileRepository, FileRepository>();
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped(typeof(UnitOfWork));

            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(nameof(EmailSettings)));

            //For identity app User settings
            builder.Services.AddIdentity<AppUser, IdentityRole>().
                AddDefaultTokenProviders().AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationContext>().AddSignInManager<SignInManager<AppUser>>();
            builder.Services.AddScoped<UserManager<AppUser>>();

            QuestPDF.Settings.License = LicenseType.Community;

            var app = builder.Build();

            app.UseSession();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();


            app.UseAuthentication();

            app.UseCors("AllowFrontendApp");

            app.UseAuthorization();


            app.MapControllers();

            app.MapFallbackToFile("/index.html");

            app.Run();

            app.Run();

            Console.WriteLine("Application has been stopped.");

        }
    }
}
