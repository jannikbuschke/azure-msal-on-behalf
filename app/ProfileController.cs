using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;

namespace App
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly HttpClient httpClient;

        public ProfileController(IConfiguration configuration, IHttpClientFactory clientFactory)
        {
            this.configuration = configuration;
            this.httpClient = clientFactory.CreateClient();
        }

        //[Authorize("create:messages")]
        [Authorize]
        [HttpGet]
        public string Get()
        {
            return this.User.Identity.Name;
            //return "Hello World";
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<ActionResult<string>> GetProfile()
        {
            var auth = this.HttpContext.Request.Headers["Authorization"].First();

            AzureADOptions config = new AzureADOptions();
            configuration.GetSection("Aad").Bind(config);

            var authority = $"{config.Instance}/{config.TenantId}";

            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                    .WithClientSecret(config.ClientSecret)
                    .WithAuthority(new Uri(authority))
                    .Build();

            string[] scopes = new string[] { "User.Read", "Calendars.Read", "Files.Read.All" };

            AuthenticationResult result = await app.AcquireTokenOnBehalfOf(scopes, new UserAssertion(auth.Split(" ")[1])).ExecuteAsync();

            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + result.AccessToken);
            var profileResult = await httpClient.GetStringAsync("https://graph.microsoft.com/v1.0/groups");
            return profileResult;
        }
    }
}
