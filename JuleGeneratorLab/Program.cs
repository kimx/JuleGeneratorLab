using JuleGeneratorLab.Components;
using JuleGeneratorLab.Models;
using JuleGeneratorLab.Services;

namespace JuleGeneratorLab
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

builder.Services.AddScoped<DatabaseSchemaReader>();
builder.Services.AddScoped<CodeSnippetService>();
builder.Services.AddScoped<CodeGenerationService>();

            builder.Services.AddScoped<ProjectService>();
            builder.Services.AddScoped<DatabaseConnectionService>();
            builder.Services.AddScoped<SnippetSetService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
