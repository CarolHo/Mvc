// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BasicViews
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddEntityFrameworkSqlite()
                .AddDbContext<BasicViewsContext>();

            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            CreateDatabase(app.ApplicationServices);

            app.Use(next => async (context) =>
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

            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
        }

        private void CreateDatabase(IServiceProvider services)
        {
            using (var serviceScope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var dbContext = services.GetRequiredService<BasicViewsContext>();
                dbContext.Database.EnsureDeleted();
                Task.Delay(TimeSpan.FromSeconds(3)).Wait();
                dbContext.Database.EnsureCreated();
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
                .UseIISIntegration()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>();
    }
}
