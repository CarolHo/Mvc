// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BasicApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Serialization;

namespace BasicApi
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json")
                .Build();
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var rsa = new RSACryptoServiceProvider(2048);
            var key = new RsaSecurityKey(rsa.ExportParameters(true));

            services.AddSingleton(new SigningCredentials(
                key,
                SecurityAlgorithms.RsaSha256Signature));

            services.AddAuthentication().AddJwtBearer(options =>
            {
                options.TokenValidationParameters.IssuerSigningKey = key;
                options.TokenValidationParameters.ValidAudience = "Myself";
                options.TokenValidationParameters.ValidIssuer = "BasicApi";
            });

            services.AddEntityFrameworkSqlServer().AddDbContext<BasicApiContext>(options =>
            {
                var connectionString = Configuration["Data:DefaultConnection:ConnectionStringBasicApi"];
                options.UseSqlServer(connectionString);
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(
                    "pet-store-reader",
                    builder => builder
                        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser()
                        .RequireClaim("scope", "pet-store-reader"));

                options.AddPolicy(
                    "pet-store-writer",
                    builder => builder
                        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser()
                        .RequireClaim("scope", "pet-store-writer"));
            });

            services
                .AddMvcCore()
                .AddJsonFormatters(json => json.ContractResolver = new CamelCasePropertyNamesContractResolver())
                .AddDataAnnotations();

            services.AddSingleton(new PetRepository());
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment hosting)
        {
            CreateDatabase(app.ApplicationServices, hosting.ContentRootPath);

            app.Use(next => async context =>
            {
                try
                {
                    await next(context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw;
                }
            });

            app.UseAuthentication();
            app.UseMvc();
        }

        private void CreateDatabase(IServiceProvider services, string contentRoot)
        {
            using (var serviceScope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var dbContext = services.GetRequiredService<BasicApiContext>();
                dbContext.Database.EnsureDeleted();
                Task.Delay(TimeSpan.FromSeconds(3)).Wait();
                dbContext.Database.EnsureCreated();

                using (var connection = dbContext.Database.GetDbConnection())
                {
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText = File.ReadAllText(Path.Combine(contentRoot, "seed.sql"));
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void Main(string[] args)
        {
            var host = CreateWebHostBuilder(args)
                .Build();

            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://+:5000")
                .UseConfiguration(new ConfigurationBuilder()
                    .AddCommandLine(args)
                    .Build())
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>();
    }
}
