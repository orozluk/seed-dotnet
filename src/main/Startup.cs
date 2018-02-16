﻿namespace Main
{
    using System.IO;
    using System.Text;

    using AutoMapper;

    using Main.Models;
    using Main.Services;
    using Main.ViewModels;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.PlatformAbstractions;
    using Microsoft.IdentityModel.Tokens;

    using Newtonsoft.Json.Serialization;

    using Swashbuckle.AspNetCore.Swagger;

    // This is 
    public class Startup
    {
        private readonly IConfigurationRoot config;

        private readonly IHostingEnvironment env;

        public Startup(IHostingEnvironment env)
        {
            this.env = env;

            var builder = new ConfigurationBuilder().SetBasePath(this.env.ContentRootPath)
                .AddJsonFile("appsettings.json").AddEnvironmentVariables();

            this.config = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            ILoggerFactory factory,
            SeedDotnetContextSeeData seeder)
        {
            // Map the view model objet with the internal model
            Mapper.Initialize(config => { config.CreateMap<PatientViewModel, Patient>().ReverseMap(); });

            // Configure how to display the errors and the level of severity
            if (env.IsEnvironment("Development"))
            {
                app.UseDeveloperExceptionPage();
                factory.AddDebug(LogLevel.Information);
            }
            else
            {
                factory.AddDebug(LogLevel.Error);
            }

            app.UseCors("MyPolicy");

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc(
                config =>
                    {
                        config.MapRoute(
                            name: "Default",
                            template: "{controller}/{action}/{id?}",
                            defaults: new { controller = "Home", action = "index" });
                    });

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Seed .Net"); });
            seeder.EnsureSeedData().Wait();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(this.config);
            if (this.env.IsEnvironment("Development") || this.env.IsEnvironment("Testing"))
            {
                // Here you can set the services implemented only for DEV and TEST
            }
            else
            {
                // Here you can set the services implemented only for Prodcution
            }

            // Allow use the API from other origins 
            services.AddCors(
                o => o.AddPolicy(
                    "MyPolicy",
                    builder => { builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().AllowCredentials(); }));

            // Set the context to the database
            services.AddDbContext<SeedDotnetContext>();

            // Set
            services.AddIdentity<UserManage, IdentityRole>(
                config =>
                    {
                        config.Password.RequireNonAlphanumeric = true;
                        config.Password.RequiredLength = 8;
                        config.Password.RequireDigit = true;
                        config.User.RequireUniqueEmail = true;
                    }).AddEntityFrameworkStores<SeedDotnetContext>();

            // Configure the authentication system
            services.AddAuthentication()
                .AddJwtBearer(cfg =>
                {
                    cfg.TokenValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.config["jwt:secretKey"])),
                        ValidIssuer = this.config["jwt:issuer"],
                        ValidateAudience = false,
                        ValidateLifetime = true
                    };
                });

            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IJwtHandler, JwtHandler>();
            services.AddScoped<IPasswordHasher<UserManage>, PasswordHasher<UserManage>>();
            services.AddScoped<ISeedDotnetRepository, SeedDotnetRepository>();
            services.AddTransient<SeedDotnetContextSeeData>();
            services.AddLogging();

            services.AddMvc(
                config =>
                    {
                        // You can configure that in production is needed Https but for other enviroments not needed
                        if (this.env.IsProduction())
                        {
                            config.Filters.Add(new RequireHttpsAttribute());
                        }
                    }).AddJsonOptions(
                config =>
                    {
                        config.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    });

            // Add Swagger reference to the project
            services.AddSwaggerGen(
                c =>
                    {
                        c.OperationFilter<AddRequiredHeaderParameter>();

                        c.SwaggerDoc(
                            "v1",
                            new Info
                                {
                                    Version = "v1",
                                    Title = "Seed DotNet",
                                    Description = "This is a seed project for a .Net WebApi",
                                    TermsOfService = "None",
                                });

                        // Set the comments path for the Swagger JSON and UI.
                        var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                        var xmlPath = Path.Combine(basePath, "seed_dotnet.xml");
                        c.IncludeXmlComments(xmlPath);
                    });
        }
    }
}