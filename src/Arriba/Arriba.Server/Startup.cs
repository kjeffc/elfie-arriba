using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Arriba.Client.Serialization.Json;
using Arriba.Communication;
using Arriba.Composition;
using Arriba.Configuration;
using Arriba.Extensions;
using Arriba.Owin;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Arriba.Server
{
    public class Startup
    {
        private IArribaServerConfiguration serverConfig;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var configLoader = new ArribaConfigurationLoader(new string[] { });
            serverConfig = configLoader.Bind<ArribaServerConfiguration>("ArribaServer");
            // TODO: Remove this setting and fix locations where sync IO happens
            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            services.AddApplicationInsights(serverConfig.AppInsights);

            services.AddCors(cors =>
            {
                cors.AddDefaultPolicy(builder =>
                                        {
                                            builder.WithOrigins(new[] { serverConfig.FrontendBaseUrl })
                                                .AllowAnyMethod()
                                                .AllowCredentials()
                                                .AllowAnyHeader();
                                        });
            });

            //ASP.NET Composition
            services.AddSingleton(serverConfig);
            services.AddSingleton((_) => serverConfig.OAuthConfig);
            services.AddOAuth(serverConfig);
            services.AddControllers().AddNewtonsoftJson(options =>
            {
                foreach (var converter in ConverterFactory.GetArribaConverters())
                {
                    options.SerializerSettings.Converters.Add(converter);
                }
            });

            //Arriba Composition
            services.AddSingleton<ISecurityConfiguration>(serverConfig);
            services.AddArribaServices(serverConfig);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseCors();
            app.UseArribaExceptionMiddleware();

            if (serverConfig.EnabledAuthentication)
                app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                var fallback = endpoints.MapFallback(HandleArribaRequest);
                if (serverConfig.EnabledAuthentication)
                    fallback.RequireAuthorization();
            });
        }

        private async Task HandleArribaRequest(HttpContext context)
        {
            var server = context.RequestServices.GetService<ApplicationServer>();
            var request = new ArribaHttpContextRequest(context, server.ReaderWriter);
            var response = await server.HandleAsync(request, false);
            await Write(request, response, server.ReaderWriter, context);
        }

        private async Task Write(ArribaHttpContextRequest request, IResponse response, IContentReaderWriterService readerWriter, HttpContext context)
        {
            var responseHeaders = context.Response.Headers;
            var responseBody = context.Response.Body;

            // Status Code
            //environment["owin.ResponseStatusCode"] = ResponseStatusToHttpStatusCode(response);

            // For stream responses we just write the content directly back to the context 
            IStreamWriterResponse streamedResponse = response as IStreamWriterResponse;

            if (streamedResponse != null)
            {
                responseHeaders["Content-Type"] = new[] { streamedResponse.ContentType };
                await streamedResponse.WriteToStreamAsync(responseBody);
            }
            else if (response.ResponseBody != null)
            {
                // Default to application/json output
                const string DefaultContentType = "application/json";

                string accept;
                if (!request.Headers.TryGetValue("Accept", out accept))
                {
                    accept = DefaultContentType;
                }

                // Split and clean the accept header and prefer output content types requested by the client,
                // always falls back to json if no match is found. 
                //IEnumerable<string> contentTypes = accept.Split(';').Where(a => a != "*/*");
                var writer = readerWriter.GetWriter(DefaultContentType, response.ResponseBody);

                // NOTE: One must set the content type *before* writing to the output stream. 
                responseHeaders["Content-Type"] = new[] { writer.ContentType };

                Exception writeException = null;

                try
                {
                    await writer.WriteAsync(request, responseBody, response.ResponseBody);
                }
                catch (Exception e)
                {
                    writeException = e;
                    Trace.TraceError(e.ToString());
                }

                if (writeException != null)
                {
                    if (!context.Response.HasStarted)
                    {
                        context.Response.StatusCode = 500;

                        if (responseBody.CanWrite)
                        {
                            using (var failureWriter = new StreamWriter(responseBody))
                            {
                                var message = String.Format("ERROR: Content writer {0} for content type {1} failed with exception {2}", writer.GetType(), writer.ContentType, writeException.GetType().Name);
                                await failureWriter.WriteAsync(message);
                            }
                        }
                    }
                }
            }

            response.Dispose();
        }
    }
}
