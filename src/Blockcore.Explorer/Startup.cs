using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Blockcore.Explorer.Handlers;
using Blockcore.Explorer.Services;
using Blockcore.Explorer.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.OpenApi.Models;

namespace Blockcore.Explorer
{
   public class Startup
   {
      private IConfiguration Configuration { get; }

      public Startup(IConfiguration configuration)
      {
         Configuration = configuration;

         CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
         CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
      }

      public void ConfigureServices(IServiceCollection services)
      {
         services.Configure<ChainSettings>(Configuration.GetSection("Chain"));
         services.Configure<ExplorerSettings>(Configuration.GetSection("Explorer"));
         services.Configure<NetworkSettings>(Configuration.GetSection("Network"));

         services.AddSingleton<BlockIndexService>();
         services.AddSingleton<TickerService>();
         services.AddSingleton<WeightService>();
         services.AddSingleton<CurrencyService>();
         services.AddHostedService<DataUpdateService>();

         services.AddMemoryCache();
         services.AddRazorPages();

         services.AddControllersWithViews().AddNewtonsoftJson(options =>
         {
            options.SerializerSettings.FloatFormatHandling = Newtonsoft.Json.FloatFormatHandling.DefaultValue;
         });

         services.AddLocalization();

         services.AddSwaggerGen(
             options =>
             {
                // TODO: Decide which version to use.
                string assemblyVersion = typeof(Startup).Assembly.GetName().Version.ToString();
                string fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
                string productVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

                options.SwaggerDoc("explorer", new OpenApiInfo { Description = "<a href=\"/block-explorer\">Back to Block Explorer...</a> ", Title = "Blockcore Explorer API", Version = fileVersion });

                // integrate xml comments
                if (File.Exists(XmlCommentsFilePath))
                {
                  options.IncludeXmlComments(XmlCommentsFilePath);
                }

                options.DescribeAllEnumsAsStrings();

                options.DescribeStringEnumsInCamelCase();
             });

         services.AddSwaggerGenNewtonsoftSupport();

         services.AddCors(o => o.AddPolicy("ExplorerPolicy", builder =>
         {
            builder.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
         }));
      }

      public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
      {
         if (env.EnvironmentName == "Developer")
         {
            app.UseDeveloperExceptionPage();
         }
         else
         {
            app.UseExceptionHandler("/Home/Error");
         }

         app.Map("/favicon", config => config.UseMiddleware<FavoriteIconHandler>());

         app.UseStaticFiles();

         app.UseCors("ExplorerPolicy");

         app.UseRouting();

         // Add Culture Detection Support
         var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures).Where(x => !x.IsNeutralCulture).ToList();
         var defaultCulture = new RequestCulture("en-US");
         defaultCulture.UICulture.NumberFormat.CurrencySymbol = "$";

         // Add some known cultures that doesn't parse well in Chrome/Firefox.
         foreach (KeyValuePair<string, string> culture in CurrencyService.CustomCultures)
         {
            allCultures.Add(new CultureInfo(culture.Key));
         }

         var requestOptions = new RequestLocalizationOptions
         {
            DefaultRequestCulture = defaultCulture,
            SupportedCultures = allCultures,
            SupportedUICultures = allCultures
         };

         requestOptions.RequestCultureProviders = new List<IRequestCultureProvider>
            {
                new QueryStringRequestCultureProvider { Options = requestOptions },
                new CookieRequestCultureProvider { Options = requestOptions },
                new AcceptLanguageHeaderRequestCultureProvider { Options = requestOptions }
            };

         app.UseRequestLocalization(requestOptions);

         app.UseEndpoints(endpoints =>
         {
            endpoints.MapDefaultControllerRoute();
            endpoints.MapRazorPages();
         });

         app.UseSwagger(c =>
         {
            c.RouteTemplate = "docs/{documentName}/openapi.json";
         });

         app.UseSwaggerUI(c =>
         {
            c.RoutePrefix = "docs";
            c.SwaggerEndpoint("/docs/explorer/openapi.json", "Blockcore Explorer API");
         });
      }

      static string XmlCommentsFilePath
      {
         get
         {
            string basePath = PlatformServices.Default.Application.ApplicationBasePath;
            string fileName = typeof(Startup).GetTypeInfo().Assembly.GetName().Name + ".xml";
            return Path.Combine(basePath, fileName);
         }
      }
   }
}
