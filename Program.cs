using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ConcertBookingApp.Data;
using MudBlazor.Services;
using ConcertBookingApp.Helpers;
using Microsoft.AspNetCore.Components.Authorization;
namespace ConcertBookingApp
{
    internal class Program
    {


        private static string PromptText(string label, bool hidden)
        {
            Console.Write(label + ":");
            var input = string.Empty;
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Backspace && input.Length > 0)
                {
                    Console.Write("\b \b");
                    input = input[0..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    Console.Write(hidden ? "*" : keyInfo.KeyChar);
                    input += keyInfo.KeyChar;
                }
            } while (key != ConsoleKey.Enter);
            Console.WriteLine();
            return input;
        }
        static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

            Environment.SetEnvironmentVariable("server", PromptText("IP/Host", false));
            Environment.SetEnvironmentVariable("user", PromptText("Username", false));
            Environment.SetEnvironmentVariable("password", PromptText("Password", true));

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
            builder.Services.AddSingleton<DataService>();
            builder.Services.AddMudServices();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");
            app.Run();
        }
    }
}