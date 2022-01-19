using _testapp_dotnet_web_aws.Clients;
using _testapp_dotnet_web_aws.Data;
using _testapp_dotnet_web_aws.Extensions;
using _testapp_dotnet_web_aws.StartupExtensions;
using _testapp_dotnet_web_aws.v1.Services;
using Amazon.SQS;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace _testapp_dotnet_web_aws
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
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(Configuration.GetConnectionString()));

            services.AddControllers();
            services.AddHttpClient();
            services.AddHttpContextAccessor();

            services.AddSwaggerSupport(Configuration);

            //services.AddRazorPages();

            services.AddAutoMapper(typeof(Startup));

            services.AddSingleton<IAmazonSQS>(x => SqsClientFactory.CreateClient(Configuration));
            services.AddSingleton<ISqsService, SqsService>();
            //services.AddSingleton<ISqsConsumerService, SqsConsumerService>();

            services.AddScoped<IMessageService, MessageService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider, ILogger<Startup> logger)
        {
            logger.LogInformation("CONFIGURE starting...");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                //c.OAuthClientId("jdms:swagger");

                c.RoutePrefix = "swagger";
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    c.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", $"Version {description.GroupName}");
                }
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapRazorPages();
            //});
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

            UseDatabase(app);

            logger.LogInformation("CONFIGURE Complete");
        }

        private void UseDatabase(IApplicationBuilder app)
        {
            string connectionString = Configuration.GetConnectionString();

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(connectionString);
            //optionsBuilder.UseLazyLoadingProxies();

            using var context = new AppDbContext(optionsBuilder.Options);
            context.Database.Migrate();
        }
    }
}