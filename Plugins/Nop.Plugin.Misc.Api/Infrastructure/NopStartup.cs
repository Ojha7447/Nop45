using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Nop.Core.Infrastructure;
using Nop.Plugin.Api.Authorization.Requirements;
using Nop.Plugin.Misc.Api.Authorization.Policies;
using Nop.Plugin.Misc.Api.Authorization.Requirements;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Nop.Plugin.Misc.Api.Infrastructure
{
    /// <summary>
    /// Represents object for configuring services on application startup
    /// </summary>
    public class NopStartup : INopStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var securityKey = "62499582-2692-4fce-8349-d55a2261debd-62499582-2692-4fce-8349-d55a2261debd-4fce-8349-d55a2261debd-4fce-8349-d55a2261debd";
            var key = Encoding.ASCII.GetBytes(securityKey);

            AddAuthorizationPipeline(services);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Ecom API", Version = "v4.50" });

                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "Enter 'Bearer' [space] and then your valid token",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT"
                };

                c.AddSecurityDefinition("Bearer", securityScheme);
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    new string[] {}
                }
            });
            });

            services.AddCors(options =>
            {
                options.AddPolicy("ApiCorsPolicy", builder =>
                {
                    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                });
            });
        }

        public void Configure(IApplicationBuilder application)
        {
            var env = application.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

            application.MapWhen(context => context.Request.Path.StartsWithSegments("/api"), apiApp =>
            {
                if (env.IsDevelopment())
                {
                    apiApp.UseDeveloperExceptionPage();
                }

                apiApp.Use(async (context, next) =>
                {
                    context.Request.EnableBuffering();
                    await next();
                });

                apiApp.UseExceptionHandler(errorApp =>
                {
                    errorApp.Run(async context =>
                    {
                        var exception = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(new { error = exception?.Message });
                    });
                });

                apiApp.UseRouting();
                apiApp.UseCors();
                apiApp.UseAuthentication();
                apiApp.UseAuthorization();

                apiApp.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

                apiApp.UseSwagger(c =>
                {
                    c.RouteTemplate = "api/swagger/{documentName}/swagger.json";
                });

                apiApp.UseSwaggerUI(c =>
                {
                    c.RoutePrefix = "api/swagger";
                    c.SwaggerEndpoint("/api/swagger/v1/swagger.json", "Ecom API v4.50");
                    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
                });
            });
        }

        private static void AddAuthorizationPipeline(IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy(JwtBearerDefaults.AuthenticationScheme, policy =>
                {
                    policy.Requirements.Add(new ActiveApiPluginRequirement());
                    policy.Requirements.Add(new AuthorizationSchemeRequirement());
                    policy.Requirements.Add(new CustomerRoleRequirement());
                    policy.RequireAuthenticatedUser();
                });
            });

            services.AddSingleton<IAuthorizationHandler, ActiveApiPluginAuthorizationPolicy>();
            services.AddSingleton<IAuthorizationHandler, ValidSchemeAuthorizationPolicy>();
            services.AddSingleton<IAuthorizationHandler, CustomerRoleAuthorizationPolicy>();
        }

        public int Order => 10;
    }
}
