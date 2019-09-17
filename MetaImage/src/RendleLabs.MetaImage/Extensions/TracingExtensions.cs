using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Collector.AspNetCore;
using OpenTelemetry.Collector.Dependencies;
using OpenTelemetry.Exporter.Jaeger;
using OpenTelemetry.Trace;
using OpenTelemetry.Trace.Sampler;

namespace RendleLabs.MetaImage.Extensions
{
    internal static class TracingExtensions
    {
        public static IServiceCollection AddTracing(this IServiceCollection services)
        {
            services.AddSingleton(_ =>
            {
                var exporter =
                    new JaegerExporter(new JaegerExporterOptions
                    {
                        ServiceName = "meta-image",
                        AgentHost = "localhost",
                        AgentPort = 6831
                    }, Tracing.SpanExporter);
                exporter.Start();
                return exporter;
            });
            services.AddSingleton(Tracing.Tracer);
            services.AddSingleton(Samplers.AlwaysSample);
            services.AddSingleton<RequestsCollectorOptions>();
            services.AddSingleton<RequestsCollector>();
            services.AddSingleton<DependenciesCollectorOptions>();
            services.AddSingleton<DependenciesCollector>();

            return services;
        }

        public static void UseTracing(this IApplicationBuilder app)
        {
            app.ApplicationServices.GetService<RequestsCollector>(); // get it instantiated
            app.ApplicationServices.GetService<DependenciesCollector>(); // get it instantiated 
            app.ApplicationServices.GetService<JaegerExporter>();
        }
    }
}