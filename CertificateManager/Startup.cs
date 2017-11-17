﻿using CertificateManager.Entities;
using CertificateManager.Logic;
using CertificateManager.Logic.ConfigurationProvider;
using CertificateManager.Logic.UXLogic;
using CertificateManager.Repository;
using CertificateServices;
using CertificateServices.ActiveDirectory;
using CertificateServices.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CertificateManager
{
    public class Startup
    {
        private IHostingEnvironment env;
        private EnvironmentInitializationProvider environmentInitializationProvider;
        public Startup(IHostingEnvironment env)
        {
            this.env = env;
            environmentInitializationProvider = new EnvironmentInitializationProvider(env);

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            ConfigureAutoMapper();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.ConfigureWritable<AppSettings>(Configuration.GetSection("AppSettings"), environmentInitializationProvider.GetAppSettingsFileName());

            services.Configure<IISOptions>(options => {
                options.AutomaticAuthentication = true;
            });

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.AccessDeniedPath = "/view/auth/forbidden";
                    options.LoginPath = "/view/auth/login";
                });
            //.AddOpenIdConnect("OidcPrimary",
            //    options =>
            //    {
            //        options.MetadataAddress = @"https://idp/oauth2/oidcdiscovery/.well-known/openid-configuration";
            //        //options.SaveTokens = true;
            //        options.ClientId = "";
            //        options.ClientSecret = "";
            //        options.RemoteAuthenticationTimeout = TimeSpan.FromHours(1);
            //        options.ResponseType = "id_token token";
            //        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            //        options.Authority = @"https://idp/oauth2/token";
            //        //options.
            //    }
            //);

            AppSettings appSettings = Configuration.GetSection("AppSettings").Get<AppSettings>();

            //options =>

            //{
            //    options.MetadataAddress = @"https://idp/oauth2/oidcdiscovery/.well-known/openid-configuration";
            //    options.SaveTokens = true;
            //    options.ClientId = "";

            //}

            string configDbPath = @"D:\db\config.db";
            bool initialSetupComplete = false;

            if (System.IO.File.Exists(configDbPath))
            {
                //initialSetupComplete = true;
            }
            else
            {
                initialSetupComplete = false;
            }


            

            // Add framework services.
            services.AddMvc().AddJsonOptions(options =>
            {
                options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            });

            LiteDbConfigurationRepository configurationRepository = new LiteDbConfigurationRepository(configDbPath);
            //RuntimeCacheRepository runtimeCacheRepository = new RuntimeCacheRepository(@"d:\db\runtimecache.db");
            RuntimeCacheRepository runtimeCacheRepository = null;

            services.AddSingleton<IConfigurationRepository>(configurationRepository);
            services.AddSingleton<ICertificateProvider>(new Win32CertificateProvider());

            services.AddSingleton<IActiveDirectoryAuthenticator>(new ActiveDirectoryAuthenticator());

            ICertificateRepository certificateRepository = new LiteDbCertificateRepository(@"d:\db\certs.db");
            //certificateRepository.DeleteAllCertificates();

            services.AddSingleton<ICertificateRepository>(certificateRepository);

            services.AddSingleton<IRuntimeConfigurationState>(
                new RuntimeConfigurationState(configurationRepository, runtimeCacheRepository)
                {
                    InitialSetupComplete = initialSetupComplete
                });

            services.AddSingleton<JavascriptConfigurationHelper>(new JavascriptConfigurationHelper(configurationRepository));
            services.AddSingleton<AuditLogic>( new AuditLogic(new LiteDbAuditRepository(@"d:\db\audit.db")));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                WorkstationDevelopment devEnv = new WorkstationDevelopment(@"D:\db\config.db");
                //devEnv.Setup();

                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            IRuntimeConfigurationState runtimeConfigurationState = app.ApplicationServices.GetService<IRuntimeConfigurationState>();

            if (env.IsDevelopment())
                runtimeConfigurationState.IsDevelopment = true;

            app.UseAuthentication();
            
           


            //runtimeConfigurationState.AlertApplicationStarted();

            //runtimeConfigurationState.Validate();


            app.UseStaticFiles();



            //app.UseCookieAuthentication(new CookieAuthenticationOptions()
            //{
            //    AuthenticationScheme = CookieAuthenticationDefaults.AuthenticationScheme,
            //    AutomaticAuthenticate = true,
                
            //});



            app.UseMvc();



        }

        public void ConfigureAutoMapper()
        {
            //AutoMapper.Mapper.
            //    CreateMap<, CertificateSubject>();
        }
    }
}
