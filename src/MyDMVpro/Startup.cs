using Joonasw.AspNetCore.SecurityHeaders;
//using Microsoft.AspNetCore.Authentication.AzureAD.UI;
//using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using MyDMVpro.Common;
using MyDMVpro.Models;
using MyDMVpro.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyDMVpro
{
    public class Startup
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILoggerFactory _loggerFactory;

        public Startup(IWebHostEnvironment env, IConfiguration config, ILoggerFactory loggerFactory)
        {
            _env = env;
            _config = config;
            _loggerFactory = loggerFactory;

            MyDMVpro.Controllers.VendorController.s_rootFolder = env.WebRootPath;
        }

        public IConfiguration Configuration { get { return _config; } }

        private string MakeReadOnlyConnectionString(string connectionString)
        {
            Microsoft.Data.SqlClient.SqlConnectionStringBuilder builder = new(connectionString);
            builder.ApplicationIntent = Microsoft.Data.SqlClient.ApplicationIntent.ReadOnly;
            return builder.ConnectionString;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();

            string connectionString = Configuration.GetConnectionString("DmvConnection");
            string readOnlyConnectionString = MakeReadOnlyConnectionString(connectionString);

            services.AddDbContext<MaggardDMVContext>(options =>
            {
                options.UseSqlServer(connectionString);
#if DEBUG
                options.EnableSensitiveDataLogging();
#endif
            });
            services.AddDbContext<RequestStatusDbContext>(options =>
            {
                if (_config.GetValue<bool>("AppSettings:UseReadOnlyConnectionForViews", true))
                {
                    options.UseSqlServer(readOnlyConnectionString);
                }
                else
                {
                    options.UseSqlServer(connectionString);
                }
#if DEBUG
                options.EnableSensitiveDataLogging();
#endif
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
                options.Secure = CookieSecurePolicy.Always;
            });
            services.AddAntiforgery(options => { options.Cookie.SecurePolicy = CookieSecurePolicy.Always; });
            services.AddMicrosoftIdentityWebAppAuthentication(Configuration, "AzureAdB2C");
            services.Configure<OpenIdConnectOptions>(Configuration.GetSection("AzureAdB2C"));

            services.Configure<MicrosoftIdentityOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Events ??= new OpenIdConnectEvents();
                options.Events.OnTokenValidated += OnTokenValidated;
            });
            services.Configure<EncryptionSettings>(Configuration.GetSection("EncryptionSettings"));
            services.Configure<OCRSettings>(Configuration.GetSection("OCRSettings"));
            services.AddDistributedMemoryCache();
            services.AddCors(options =>
                {
                    options.AddPolicy("AllowOCR",
                        builder =>
                        {
                            builder.WithOrigins(Configuration["OCRSettings:OCRWebBaseUrl"],
                                Configuration["OCRSettings:OCRBackendBaseUrl"])
                                   .AllowAnyMethod()
                                   .AllowAnyHeader();
                        });

                    options.AddPolicy("AllowStripe", builder => {
                        builder.WithOrigins(Configuration["Stripe:StripeApiUrl"],
                                Configuration["Stripe:StripeDashboardUrl"])
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                          });
                });

            //services.AddCors(options =>
            //{
            //    // Add back if we need add a JotForm widget to validate VIN input
            //    //options.AddPolicy("JotForms",
            //    //    policy =>
            //    //    {
            //    //        policy.WithOrigins("https://mdpwidgets.blob.core.windows.net")
            //    //                .AllowCredentials()
            //    //                .AllowAnyHeader();
            //    //    });

            //    //options.AddPolicy("AnotherPolicy",
            //    //    policy =>
            //    //    {
            //    //        policy.WithOrigins("http://www.contoso.com")
            //    //                            .AllowAnyHeader()
            //    //                            .AllowAnyMethod();
            //    //    });
            //});


            /*
             * According to the Microsoft documentation, the SetCompatibilityVersion method is a no-op for
             * ASP.NET Core 3.0 apps and later versions, including .NET 6.
             * That means it has no impact on the application and we can remove it from the code.
             */
            services.AddMvc().AddMvcOptions(options =>
            {
                //options.EnableEndpointRouting = false;
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            }).AddMicrosoftIdentityUI();

            services.AddCsp(nonceByteAmount: 32);
            /*
                        services.AddHsts(options =>
                        {
                            options.Preload = true;
                            options.IncludeSubDomains = true;
                            options.MaxAge = TimeSpan.FromDays(366);
                        });
            */
            services.AddAuthorization(options =>
            {
                options.AddPolicy("ActiveUser",
                    policyBuilder => policyBuilder.RequireAssertion(
                        context => context.User.HasClaim(claim =>
                                       claim.Type == MyDmvProClaims.GroupId ||
                                       claim.Type == MyDmvProClaims.VendorId
                                    )
                    )
                );
                options.AddPolicy("VendorAgentOnly", policy => policy.RequireClaim(MyDmvProClaims.VendorId));
                options.AddPolicy("VendorAdminOnly", policy => policy.RequireClaim(MyDmvProClaims.IsVendorAdmin));
                options.AddPolicy("GroupMemberOnly", policy => policy.RequireClaim(MyDmvProClaims.GroupId));
                options.AddPolicy("GroupAdminOnly", policy => policy.RequireClaim(MyDmvProClaims.IsGroupAdmin));
                options.AddPolicy("SysAdminOnly", policy => policy.RequireClaim(MyDmvProClaims.IsSysAdmin));
            });

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.Cookie.HttpOnly = true;
                options.Cookie.Name = ".myDmvPro.Session";
            });

            // Add application services.
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();
            services.AddTransient<IAppVersionService, AppVersionService>();

            services.AddSingleton<IConfiguration>(Configuration);
            IConfiguration appSettings = Configuration.GetSection("AppSettings");

            services.Configure<AutoIMSSettings>(Configuration.GetSection("AutoIMS"));
            services.Configure<SmtpSettings>(Configuration.GetSection("Smtp"));

            //services.Configure<AzureADB2COptions>(Configuration.GetSection("AzureADB2C"));

            MyDMVpro.Common.ConfigurationHelper.Configuration = Configuration;
            MyDMVpro.Common.LoggerHelper.Logger = _loggerFactory.CreateLogger("myDMVpro");

            // This will make Json serialize in Pascal casing MyVariable instead of Java casing myVariable 
            //    it will break views unless all javascript is changed accordingly
            //services.AddMvc()
            //    .AddJsonOptions(options => options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver());
        }
        // [C# Code]
        private void LogException(string message)
        {
            _loggerFactory.CreateLogger("MyDMVpro").LogError(message);
        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            /*loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();*/

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                //app.UseDatabaseErrorPage(); is deprecated and has been replaced with
                //the DeveloperExceptionPageMiddleware already included in the line above.
                //app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
                //app.UseHsts(new Joonasw.AspNetCore.SecurityHeaders.HstsOptions()
                //{
                //     Duration = TimeSpan.FromDays(366),
                //     IncludeSubDomains = true,
                //     Preload = true
                //});
            }

            app.UseCsp(csp =>
            {
                csp.AllowScripts
                        .FromSelf()
                        .From("https://mydmvpro.b2clogin.com")
                        .From("https://cdnjs.cloudflare.com")
                        .From("https://maxcdn.bootstrapcdn.com")
                        .From("https://cdn.datatables.net")
                        .From("https://az416426.vo.msecnd.net")
                        .From("https://js.monitor.azure.com")
                        .AllowUnsafeEval()
                        .AllowUnsafeInline();
                //.AddNonce();

                csp.AllowStyles
                        .FromSelf()
                        .From("https://mydmvpro.b2clogin.com")
                        .From("https://cdnjs.cloudflare.com")
                        .From("https://maxcdn.bootstrapcdn.com")
                        .From("https://cdn.datatables.net")
                        .AllowUnsafeInline();
#if USE_FRAMES
                csp.AllowFrames
                        .FromSelf()
                        .From("https://form.jotform.com");

                csp.AllowFraming
                        .FromSelf()
                        .From("https://form.jotform.com");
#endif
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseRouting();
            app.UseCors();

            app.UseAuthentication();

            //Add middleware here
            app.UseMiddleware<RequestLoggingMiddleware>();

            app.UseSession();

            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("X-Frame-Options", "sameorigin");
                await next();
            });

            /*app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });*/
            //UseMvs is marked obsolete in this version of .NET.

            //app.UseRouting();

            app.UseAuthorization(); //This method was added as per instructions of the error message when omitted.
                                    //The message states it must go between UseRouting() and UseEndpoints().
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        public Task OnTokenValidated(TokenValidatedContext context)
        {
            try
            {
                string userSID = null;
                Claim claim = context.Principal.FindFirst(ClaimTypes.NameIdentifier);
                if (claim != null)
                    userSID = claim.Value;

                ClaimValues claimValues = Common.DataHelpers.GetClaimsForUser(userSID);
                if (claimValues.Count > 0)
                {
                    ClaimsIdentity claimsIdentity = (ClaimsIdentity)context.Principal.Identity;
                    foreach (KeyValuePair<string, string> kvp in claimValues)
                    {
                        claimsIdentity.AddClaim(new Claim(kvp.Key, kvp.Value ?? ""));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }
            return Task.CompletedTask;
        }
    }
}
