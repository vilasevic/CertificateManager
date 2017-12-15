﻿using CertificateManager.Entities;
using CertificateManager.Logic;
using CertificateManager.Logic.ActiveDirectory;
using CertificateManager.Logic.ActiveDirectory.Interfaces;
using CertificateManager.Logic.ConfigurationProvider;
using CertificateManager.Logic.InitialSetupDependencies;
using CertificateManager.Logic.Interfaces;
using CertificateManager.Logic.MvcMiddleware;
using CertificateManager.Logic.UXLogic;
using CertificateManager.Repository;
using CertificateServices;
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
        DatabaseLocator databaseLocator;
        private bool initialSetupComplete = false;
        private IHostingEnvironment env;
        private EnvironmentInitializationProvider environmentInitializationProvider;

        //private static CancellationTokenSource cancelTokenSource = new System.Threading.CancellationTokenSource();

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

            AppSettings appSettings = Configuration.GetSection("AppSettings").Get<AppSettings>();

            databaseLocator = new DatabaseLocator(appSettings);

            if(databaseLocator.ConfigurationRepositoryExists())
            {
                initialSetupComplete = true;
                InitializeApp(services, appSettings);
            }
            else
            {
                initialSetupComplete = false;
                InitializeSetup(services);
            }

            // Add framework services.
            services.AddMvc().AddJsonOptions(options =>
            {
                options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                //WorkstationDevelopment devEnv = new WorkstationDevelopment(@"D:\db\config.db");
                //devEnv.Setup();

                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            //IRuntimeConfigurationState runtimeConfigurationState = app.ApplicationServices.GetService<IRuntimeConfigurationState>();

            //if (env.IsDevelopment())
            //    runtimeConfigurationState.IsDevelopment = true;


            app.UseAuthentication();
            
            app.UseStaticFiles();

            app.UseMiddleware(typeof(ErrorHandlingMiddleware));

            app.UseMvc();
        }

        private void RequireInitialSetup(IApplicationBuilder app)
        {
            //app.UseMvc(routes => routes.MapRoute().DefaultHandler)


            //app.Run(context =>
            //{
            //    if(context.Request.Path.Value != "/initial-setup") context.Response.Redirect("/initial-setup");
            //    return Task.FromResult<object>(null);
            //});
        }


        private void InitializeSetup(IServiceCollection services)
        {
            services.AddSingleton<IRuntimeConfigurationState>(
                new RuntimeConfigurationState(null, null)
                {
                    InitialSetupComplete = false
                }
            );

            services.AddSingleton<IClientsideConfigurationProvider>(new ClientsideConfigurationProviderInitialSetup());

            services.AddSingleton<IAuditLogic>(new AuditLogicInitialSetup());
            //IdentityAuthenticationLogic(configurationRepository, activeDirectoryAuthenticator)

            services.AddSingleton<IdentityAuthenticationLogic>(new IdentityAuthenticationLogic(null, null));
        }

        private void InitializeApp(IServiceCollection services, AppSettings appSettings)
        {
            LiteDbConfigurationRepository configurationRepository = new LiteDbConfigurationRepository(databaseLocator.GetConfigurationRepositoryConnectionString());

            AppConfig appConfig = configurationRepository.GetAppConfig();

            ActiveDirectoryRepository activeDirectory = new ActiveDirectoryRepository();

            EncryptionProvider cipher = new EncryptionProvider(appConfig.EncryptionKey);

            services.AddSingleton<EncryptionProvider>(cipher);

            services.AddSingleton<IActiveDirectoryAuthenticator>(activeDirectory);
            services.AddSingleton<IActiveDirectoryRepository>(activeDirectory);

            IdentityAuthenticationLogic identityAuthenticationLogic = new IdentityAuthenticationLogic(configurationRepository, activeDirectory);

            services.AddSingleton<IdentityAuthenticationLogic>();

            ICertificateRepository certificateRepository = new LiteDbCertificateRepository(databaseLocator.GetCertificateRepositoryConnectionString());

            RuntimeCacheRepository runtimeCacheRepository = null;

            LiteDbAuditRepository auditRepository = new LiteDbAuditRepository(databaseLocator.GetAuditRepositoryConnectionString());

            IAuditLogic auditLogic = new AuditLogic(auditRepository, configurationRepository);

            services.AddSingleton<IAuditLogic>(auditLogic);

            IAuthorizationLogic authorizationLogic = new AuthorizationLogic(configurationRepository, auditLogic);

            RoleManagementLogic roleManagementLogic = new RoleManagementLogic(configurationRepository, authorizationLogic);

            services.AddSingleton<RoleManagementLogic>(roleManagementLogic);

            UserManagementLogic userManagementLogic = new UserManagementLogic(configurationRepository, authorizationLogic);

            services.AddSingleton<UserManagementLogic>(userManagementLogic);

            SecurityPrincipalLogic securityPrincipalLogic = new SecurityPrincipalLogic(roleManagementLogic, userManagementLogic);

            services.AddSingleton<SecurityPrincipalLogic>();

            services.AddSingleton<AdcsTemplateLogic>(new AdcsTemplateLogic(configurationRepository, activeDirectory));

            services.AddSingleton<IAuthorizationLogic>(authorizationLogic);

            services.AddSingleton<IConfigurationRepository>(configurationRepository);

            services.AddSingleton<ICertificateProvider>(new Win32CertificateProvider());
          
            services.AddSingleton<ICertificateRepository>(certificateRepository);

            services.AddSingleton<IRuntimeConfigurationState>(
                new RuntimeConfigurationState(configurationRepository, runtimeCacheRepository)
                {
                    InitialSetupComplete = initialSetupComplete
                });

            services.AddSingleton<IClientsideConfigurationProvider>(new ClientsideConfigurationProvider(configurationRepository));

            CertificateManagementLogic certificateManagementLogic = new CertificateManagementLogic(
                    configurationRepository,
                    certificateRepository,
                    authorizationLogic,
                    auditLogic,
                    securityPrincipalLogic,
                    cipher);

            services.AddSingleton<CertificateManagementLogic>(certificateManagementLogic);

            services.AddSingleton<AnalyticsLogic>(new AnalyticsLogic(configurationRepository, certificateRepository, auditRepository));

            services.AddSingleton<DataRenderingProvider>(new DataRenderingProvider());
        }

        public void ConfigureAutoMapper()
        {

        }



        private void ConfigureOidc()
        {
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



            //options =>

            //{
            //    options.MetadataAddress = @"https://idp/oauth2/oidcdiscovery/.well-known/openid-configuration";
            //    options.SaveTokens = true;
            //    options.ClientId = "";

            //}
        }
    }
}
