using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace VueTemplate
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
            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(7);
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            }).AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "image/svg+xml", "application/xhml+xml", "image/x-icon" });
            })
            .Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            })
            .Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            })
            .AddResponseCaching();
        }

        private static readonly IEnumerable<string> validContentTypes = new HashSet<string>() { "text/html" };

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            var cachePeriod = env.IsDevelopment() ? "600" : "604900"; // dev 10 mins, otherwise 7 days and some change

            var currentNonce = string.Empty;

            //app.UseMiddleware<HtmlInjectorMiddleware>();

            app.UseResponseCompression()
                .Use(async (context, next) =>
                {
                    var originalBody = context.Response.Body;

                    currentNonce = GenerateNonce();

                    context.Request.EnableBuffering();

                    //TODO: need to replace any inline javascript with a hash or nonce
                    context.Response.Headers.Add("Content-Security-Policy", $"default-src 'self'; script-src 'self' 'nonce-{currentNonce}'; style-src 'self' use.fontawesome.com 'nonce-{currentNonce}' maxcdn.bootstrapcdn.com fonts.googleapis.com; font-src fonts.gstatic.com 'nonce-{currentNonce}'; img-src 'self'");
                    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                    context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
                    context.Response.Headers.Add("X-Xss-Protection", "1");

                    var originBody = context.Response.Body;

                    var memStream = new MemoryStream();
                    context.Response.Body = memStream;
                                        

                    await next().ConfigureAwait(false);

                    var contentType = context.Response.ContentType?.ToLower();
                    contentType = contentType?.Split(';', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                    if (contentType == "text/html")
                    {
                        using (var streamReader = new StreamReader(context.Response.Body))
                        {
                            // Read the body
                            context.Response.Body.Seek(0, SeekOrigin.Begin);
                            var responseBody = await streamReader.ReadToEndAsync();

                            responseBody = responseBody.Replace("__replaceme__", currentNonce);

                            // Create a new stream with the modified body, and reset the content length to match the new stream
                            var requestContent = new StringContent(responseBody, Encoding.UTF8, contentType);
                            context.Response.Body = await requestContent.ReadAsStreamAsync();//modified stream
                            context.Response.ContentLength = context.Response.Body.Length;
                        }
                    }                    
                    ReturnBody(context.Response, originBody);

                })
                .UseDefaultFiles(new DefaultFilesOptions { DefaultFileNames = new List<string> { "index.html" } })
                .UseStaticFiles(new StaticFileOptions()
                {
                    HttpsCompression = HttpsCompressionMode.Compress,
                    OnPrepareResponse = ctx =>
                    {
                        if (ctx.File.Name.EndsWith(".html"))
                        {
                            ctx.Context.Response.Headers.Add("Cache-Control", "no-cache, no-store");
                            ctx.Context.Response.Headers.Add("Expires", "-1");
                        }
                        else
                        {
                            ctx.Context.Response.Headers.Append("Cache-Control", $"public, max-age={cachePeriod}");
                        }
                    }
                });            
        }

        private void ReturnBody(HttpResponse response, Stream originBody)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            response.Body.CopyToAsync(originBody);
            response.Body = originBody;
        }

        private string GenerateNonce()
        {
            var ByteArray = new byte[20];
            using (var Rnd = RandomNumberGenerator.Create())
            {
                Rnd.GetBytes(ByteArray);
            }
            return Convert.ToBase64String(ByteArray);
        }
                
    }
}
