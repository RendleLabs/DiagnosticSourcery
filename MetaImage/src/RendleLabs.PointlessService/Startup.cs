using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RendleLabs.PointlessService.Diagnostics;
using RendleLabs.PointlessService.Extensions;
using RendleLabs.PointlessService.Options;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace RendleLabs.PointlessService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTracing();
            
//            services.AddHostedService<ConsoleMonitor>();
            services.Configure<InfluxDBOptions>(Configuration.GetSection("InfluxDB"));
            services.AddHostedService<InfluxDBMonitor>();

            services.AddHttpClient();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseTracing();
            
            app.UseMvc();
        }
    }
}