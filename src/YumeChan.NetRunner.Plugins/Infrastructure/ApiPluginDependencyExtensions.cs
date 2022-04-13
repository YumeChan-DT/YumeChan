using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using YumeChan.NetRunner.Plugins.Infrastructure.Api;
using YumeChan.NetRunner.Plugins.Infrastructure.Swagger;
using YumeChan.NetRunner.Plugins.Services;

namespace YumeChan.NetRunner.Plugins.Infrastructure;

public static class ApiPluginDependencyExtensions
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
		SwaggerDocumentEnumerator swaggerDocsEnumerator = new() { Documents = { { "YumeChan.NetRunner", new() { Title = "YumeChan.NetRunner" } } } };
		
		services.AddSingleton(new SwaggerEndpointEnumerator { Endpoints = { new() { Name = "YumeChan.NetRunner", Url = "/swagger/YumeChan.NetRunner/swagger.json" } } });
		services.AddSingleton(swaggerDocsEnumerator);

		return services.AddSwaggerGen(options =>
			{
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
}