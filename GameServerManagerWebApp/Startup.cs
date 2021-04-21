using System;
using System.IO;
using System.Linq;
using GameServerManagerWebApp.Entites;
using GameServerManagerWebApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GameServerManagerWebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddHttpClient();
            services.AddDbContext<GameServerManagerContext>(options =>
                options.UseSqlite(Configuration.GetConnectionString("GameServerManagerContext")));

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(options =>
                {
                    options.LoginPath = "/Authentication/SignIn";
                    options.LogoutPath = "/Authentication/SignOut";
                    options.AccessDeniedPath = "/Authentication/Denied";
                })
                .AddSteam(s => s.ApplicationKey = Configuration.GetValue<string>("Steam:Key"));

            services.AddAuthorization(options =>
            {
                var admins = Configuration.GetSection("Admins").Get<string[]>();
                options.AddPolicy("Admin", policy => policy.RequireClaim(
                    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
                    admins.Select(steamid => "https://steamcommunity.com/openid/id/" + steamid).ToArray())
                );
            });

            services.AddHostedService<ServerStateWorker>();

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                services.AddDataProtection()
                    .PersistKeysToFileSystem(new DirectoryInfo("/var/aspnet-keys"))
                    .SetApplicationName("qrfhq");
            }

            services.AddScoped<ServerStateService>();
            services.AddScoped<GameServerService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedProto
                });
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseCookiePolicy(new CookiePolicyOptions()
            {
                HttpOnly = HttpOnlyPolicy.Always,
                Secure = CookieSecurePolicy.SameAsRequest,
                MinimumSameSitePolicy = SameSiteMode.Lax
            });

            app.UseRequestLocalization("fr-FR");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
