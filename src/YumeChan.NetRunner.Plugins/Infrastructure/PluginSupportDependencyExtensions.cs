using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Nodsoft.MoltenObsidian.Blazor;
using Nodsoft.MoltenObsidian.Blazor.Services;
using Nodsoft.MoltenObsidian.Vault;
using Swashbuckle.AspNetCore.SwaggerGen;
using YumeChan.NetRunner.Plugins.Infrastructure.Api;
using YumeChan.NetRunner.Plugins.Infrastructure.Swagger;
using YumeChan.NetRunner.Plugins.Services;
using YumeChan.NetRunner.Plugins.Services.Docs;

namespace YumeChan.NetRunner.Plugins.Infrastructure;

public static class PluginSupportDependencyExtensions
{
	public static IServiceCollection AddApiPluginSupport(this IServiceCollection services)
	{
		services.AddSingleton<IActionDescriptorChangeProvider>(PluginActionDescriptorChangeProvider.Instance);
		services.AddSingleton(PluginActionDescriptorChangeProvider.Instance);

		services.AddEndpointsApiExplorer();
		
		services.AddHostedService<ApiPluginLoader>();

		return services;
	}

	public static IServiceCollection AddApiPluginsSwagger(this IServiceCollection services, Action<SwaggerGenOptions>? swaggerOptions = null)
	{
		SwaggerGeneratorOptions swaggerGeneratorOptions = new();
		services.AddSingleton(swaggerGeneratorOptions);
		
		SwaggerDocumentEnumerator swaggerDocsEnumerator = new() { Documents = { { "YumeChan.NetRunner", new() { Title = "YumeChan.NetRunner" } } } };
		
		services.AddSingleton(new SwaggerEndpointEnumerator { Endpoints = { new() { Name = "YumeChan.NetRunner", Url = "/swagger/YumeChan.NetRunner/swagger.json" } } });
		services.AddSingleton(swaggerDocsEnumerator);

		return services.AddSwaggerGen(options =>
			{
				options.SwaggerGeneratorOptions = swaggerGeneratorOptions;
				
				options.SwaggerGeneratorOptions.SwaggerDocs = swaggerDocsEnumerator.Documents;

				// Discord Authentication
				options.AddSecurityDefinition("oauth2", new()
					{
						Type = SecuritySchemeType.OAuth2,
						Flows = new()
						{
							AuthorizationCode = new()
							{
								AuthorizationUrl = new("https://discord.com/api/oauth2/authorize"),
								TokenUrl = new("https://discord.com/api/oauth2/token"),
								Scopes = new Dictionary<string, string>
								{
									{ "identify", "Access to your Discord account" }
								}
							}
						}
					}
				);

				// Make sure Swagger UI requires Discord authentication
				OpenApiSecurityScheme securityScheme = new()
				{
					Reference = new()
					{
						Id = "oauth2",
						Type = ReferenceType.SecurityScheme
					}
				};

				options.AddSecurityRequirement(new()
					{
						{ securityScheme, new[] { "identify" } }
					}
				);

				// Finally use the provided swagger options.
				swaggerOptions?.Invoke(options);

				options.DocumentFilter<PluginNamespaceDocumentFilter>();

				options.TryIncludeXmlCommentsFromAssembly(Assembly.GetEntryAssembly()!);
			}
		);
	}

	public static IApplicationBuilder UseApiPluginsSwagger(this IApplicationBuilder app)
	{
		app.UseSwagger(options => options.RouteTemplate = "swagger/{documentName}/swagger.json");
		
		app.UseSwaggerUI(options =>
			{
				options.ConfigObject.Urls = app.ApplicationServices.GetRequiredService<SwaggerEndpointEnumerator>();
				options.RoutePrefix = "swagger";
			}
		);

		return app;
	}


	public static void ConfigurePluginNameRoutingToken(this MvcOptions options, string tokenName = "plugin")
		=> options.Conventions.Add(new CustomRouteToken(tokenName, c => c.ControllerType.Namespace));


	public static bool TryIncludeXmlCommentsFromAssembly(this SwaggerGenOptions options, Assembly assembly)
	{
		string filePath = assembly.Location.Replace(".dll", ".xml", StringComparison.OrdinalIgnoreCase);
		
		if (File.Exists(filePath))
		{
			options.IncludeXmlComments(filePath, true);
			return true;
		}

		return false;
	}
	
	/// <summary>
	/// Adds support for MoltenObsidian-flavoured plugin documentation, via the use of <see creef="PluginDocsLoader" />.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
	/// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
	public static IServiceCollection AddPluginDocsSupport(this IServiceCollection services)
	{
		services.AddMoltenObsidianBlazorIntegration();
		services.AddSingleton<PluginDocsLoader>();
		services.AddSingleton<VaultRouter>();
		
		return services;
	}
}