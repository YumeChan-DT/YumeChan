using System.IO;
using AspNet.Security.OAuth.Discord;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YumeChan.Core.Config;
using YumeChan.NetRunner.Infrastructure.Blazor;
using YumeChan.NetRunner.Plugins.Infrastructure;
using YumeChan.NetRunner.Plugins.Infrastructure.Api;
using YumeChan.NetRunner.Plugins.Infrastructure.Filesystem;
using YumeChan.NetRunner.Services.Authentication;

namespace YumeChan.NetRunner;

public class Startup
{
	public IConfiguration Configuration { get; }

	public Startup(IConfiguration configuration)
	{
		Configuration = configuration;
	}

	// This method gets called by the runtime. Use this method to add services to the container.
	// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
	public void ConfigureServices(IServiceCollection services)
	{
		services.AddControllers(builder =>
			{
				builder.ConfigurePluginNameRoutingToken();
				builder.Conventions.Add(new PluginApiRoutingConvention());
			}
		);

		services.AddApiPluginSupport();
		services.AddApiPluginsSwagger();

		services.AddRazorPages();
		services.AddServerSideBlazor();
		services.AddHttpContextAccessor();

		services.AddAuthentication(options =>
		{
			options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
			options.DefaultChallengeScheme = DiscordAuthenticationDefaults.AuthenticationScheme;
		})
		.AddCookie(options =>
		{

		})
		.AddDiscord(options =>
		{
			options.ClientId = Configuration["DiscordAuth:ClientId"];
			options.ClientSecret = Configuration["DiscordAuth:ClientSecret"];
			options.CallbackPath = "/signin-oauth2";

			options.Scope.Add("identify");

			options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
			options.CorrelationCookie.SameSite = SameSiteMode.Lax;
		});

		services.AddLogging(x =>
		{
			x.ClearProviders();
		});

		

		services.AddSingleton<IComponentActivator, ComponentActivator>();
		services.AddSingleton(YumeCore.Instance);

		services.AddScoped<IClaimsTransformation, WebAppClaims>();
	}

	// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
	public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		if (env.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
			app.UseBrowserLink();
		}
		else
		{
			app.UseExceptionHandler("/Error");
			// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
			app.UseHsts();

			// Nginx support
			app.UseForwardedHeaders(new() { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto });
		}

		
		app.UseStaticFiles();
		app.UseStaticFiles(options: new()
		{
			FileProvider = new PluginWebAssetsProvider(app.ApplicationServices.GetService<ICoreProperties>()?.Path_Plugins 
				?? throw new InvalidOperationException("Plugin path not found")),
			
			RequestPath = "/_content"
		});
		
		app.UseStaticFiles(options: new()
		{
			FileProvider = new PluginWebAssetsProvider(app.ApplicationServices.GetService<ICoreProperties>()?.Path_Plugins 
				?? throw new InvalidOperationException("Plugin path not found")),
			
			RequestPath = "/p"
		});

		app.UseApiPluginsSwagger();

		app.UseHttpsRedirection();
		app.UseRouting();

		app.UseAuthentication();
		app.UseAuthorization();

		app.UseEndpoints(endpoints =>
		{
			endpoints.MapControllers();
			endpoints.MapBlazorHub();
			
			endpoints.MapFallback("/api/{*path}", context =>
			{
				context.Response.StatusCode = StatusCodes.Status404NotFound;
				return Task.CompletedTask;
			});
			
			endpoints.MapFallbackToPage("/_Host");
		});
	}
}
