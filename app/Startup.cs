using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace App
{
    public class HasScopeRequirement : IAuthorizationRequirement
    {
        public string Issuer { get; }
        public string Scope { get; }

        public HasScopeRequirement(string scope, string issuer)
        {
            Scope = scope ?? throw new ArgumentNullException(nameof(scope));
            Issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        }
    }

    public class HasScopeHandler : AuthorizationHandler<HasScopeRequirement>
    {
        private const string ScopeID = "http://schemas.microsoft.com/identity/claims/scope";
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HasScopeRequirement requirement)
        {
            
            // If user does not have the scope claim, get out of here
            if (!context.User.HasClaim(c => c.Type == ScopeID && c.Issuer == requirement.Issuer))
                return Task.CompletedTask;

            // Split the scopes string into an array
            var scopes = context.User.FindFirst(c => c.Type == ScopeID && c.Issuer == requirement.Issuer).Value.Split(' ');

            // Succeed if the scope array contains the required scope
            if (scopes.Any(s => s == requirement.Scope))
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }

    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            AzureADOptions o = new AzureADOptions();
            Configuration.GetSection("Aad").Bind(o);
            
            services
                .AddAuthentication(AzureADDefaults.BearerAuthenticationScheme)
                .AddAzureADBearer(options =>
                {
                    options.ClientId = o.ClientId;
                    options.Domain = o.Domain;
                    options.Instance = o.Instance;
                    options.TenantId = o.TenantId;
                });

            services.Configure<JwtBearerOptions>(AzureADDefaults.JwtBearerAuthenticationScheme, options =>
            {
                // This is an Azure AD v2.0 Web API
                //options.Authority += "/v2.0";

                // The valid audiences are both the Client ID (options.Audience) and api://{ClientID}
                options.TokenValidationParameters.ValidAudiences = new string[] { options.Audience, $"api://{options.Audience}" };

                // Instead of using the default validation (validating against a single tenant, as we do in line of business apps),
                // we inject our own multitenant validation logic (which even accepts both V1 and V2 tokens)
                //options.TokenValidationParameters.IssuerValidator = AadIssuerValidator.ValidateAadIssuer;
            });

            services.AddAuthorization();

            services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();

            services.AddHttpClient();

            services.AddSpaStaticFiles(options => options.RootPath = "webapp/build");
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();

            app.UseMvc();

            //app.Map("/api/hello-world", builder =>
            //{
            //    builder.Run(async context =>
            //    {
            //        context.Response.StatusCode = (int) HttpStatusCode.OK;
            //        await context.Response.WriteAsync("Hello World");
            //    });
            //});

            app.Map("/api", builder =>
            {
                builder.Run(async context =>
                {
                    context.Response.StatusCode = (int) HttpStatusCode.NotFound;
                    await context.Response.WriteAsync("");
                });
            });

            app.UseStaticFiles();

            app.UseSpaStaticFiles();

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "webapp";

                if (env.IsDevelopment())
                {
                    spa.UseProxyToSpaDevelopmentServer("http://localhost:3000");
                }
            });
        }
    }
}
