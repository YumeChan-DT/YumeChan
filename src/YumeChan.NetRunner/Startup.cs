using System.Reflection;
using AspNet.Security.OAuth.Discord;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using YumeChan.Core.Config;
using YumeChan.NetRunner.Infrastructure.Blazor;
using YumeChan.NetRunner.Plugins.Infrastructure;
using YumeChan.NetRunner.Plugins.Infrastructure.Api;
using YumeChan.NetRunner.Plugins.Infrastructure.Filesystem;
using YumeChan.NetRunner.Services.Authentication;
using YumeChan.PluginBase.Tools;

namespace YumeChan.NetRunner;

public static class Startup
{
	// This method gets called by the runtime. Use this method to add services to the container.
	// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
	public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
	{
		builder.Services.AddYumeCoreServices();
		
		builder.Services.AddSerilog(static (services, lc) => lc
			.ReadFrom.Configuration(services.GetRequiredService<IConfiguration>())
			.ReadFrom.Services(services)
		);
        
		string informationalVersion = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
		builder.Services.AddSingleton(new NetRunnerContext(RunnerType.Console, typeof(Program).Assembly.GetName().Name, informationalVersion));
		
		builder.Services.AddControllers(options =>
			{
				options.ConfigurePluginNameRoutingToken();
				options.Conventions.Add(new PluginApiRoutingConvention());
			}
		);
		
		builder.Services.AddApiPluginSupport();
		builder.Services.AddApiPluginsSwagger();
		builder.Services.AddPluginDocsSupport();

		builder.Services.AddRazorPages();
		builder.Services.AddServerSideBlazor();
		builder.Services.AddHttpContextAccessor();

		builder.Services.AddAuthentication(options =>
		{
			options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
			options.DefaultChallengeScheme = DiscordAuthenticationDefaults.AuthenticationScheme;
		})
		.AddCookie(options =>
		{

		})
		.AddDiscord(options =>
		{
			builder.Configuration.GetSection("DiscordAuth").Bind(options);

//			options.ClientId = Configuration["DiscordAuth:ClientId"];
//			options.ClientSecret = Configuration["DiscordAuth:ClientSecret"];
			options.CallbackPath = "/signin-oauth2";

			options.Scope.Add("identify");

			options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
			options.CorrelationCookie.SameSite = SameSiteMode.Lax;
		});

		builder.Services.AddSingleton<IComponentActivator, ComponentActivator>();
		builder.Services.AddScoped<IClaimsTransformation, WebAppClaims>();
		
		return builder;
	}

	// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
	public static void UseApplicationPipeline(this WebApplication app)
	{
		if (app.Environment.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
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
			FileProvider = new PluginWebAssetsProvider(app.Services.GetService<ICoreProperties>()?.Path_Plugins 
				?? throw new InvalidOperationException("Plugin path not found")),
			
			RequestPath = "/_content"
		});
		
		app.UseStaticFiles(options: new()
		{
			FileProvider = new PluginWebAssetsProvider(app.Services.GetService<ICoreProperties>()?.Path_Plugins 
				?? throw new InvalidOperationException("Plugin path not found")),
			
			RequestPath = "/p"
		});

		app.UseApiPluginsSwagger();

		app.UseHttpsRedirection();
		app.UseRouting();

		app.UseAuthentication();
		app.UseAuthorization();

		app.MapControllers();
		app.MapBlazorHub();
		
		app.MapFallback("/api/{*path}", context =>
		{
			context.Response.StatusCode = StatusCodes.Status404NotFound;
			return Task.CompletedTask;
		});
		
		app.MapFallbackToPage("/_Host");
	}
}
