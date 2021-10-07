using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using snsrpi.Services;
using snsrpi.Models;
using snsrpi.Interfaces;

namespace snsrpi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Determine if demo or not based on environment setting
            string demoEnv = System.Environment.GetEnvironmentVariable("DEMO");
            var demo = demoEnv != "false";
            
            services.AddControllers();
            // Initialise logging object to pass down to class
            var logger = LoggerFactory.Create(logging => logging.AddConsole()).CreateLogger<LoggerManagerService>();
            
            //Initialise LoggerManager as a singleton. Make sure it is created at startup 
            var service = new LoggerManagerService(demo, logger);
            services.AddSingleton<ILoggerManager>(service);
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "snsrpi-device", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "testaspnet v1"));
            }

            // Can uncomment this if you want. As this API is used only locally we don't need HTTPS
            // app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
