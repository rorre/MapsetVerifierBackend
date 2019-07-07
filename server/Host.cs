using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MapsetVerifierBackend.server
{
    public class Host
    {
        public const string hubUrl = "/mapsetverifier/signalr";

        public static void Initialize()
        {
            IWebHost myHost = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<Startup>()
                .Build();

            myHost.Run();
        }

        public class Startup
        {
            public Startup(IHostingEnvironment env)
            {
                Console.WriteLine("Startup.");
            }

            // This method gets called by the runtime. Use this method to add services to the container.
            public void ConfigureServices(IServiceCollection services)
            {
                Console.WriteLine("Confugire Services.");

                // Add framework services.
                services.AddMvc().AddJsonOptions(options =>
                {
                    //return json format with Camel Case
                    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
                    Console.WriteLine("Json Options.");
                });

                services.AddSignalR();
            }

            // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
            public void Configure(IApplicationBuilder app)
            {
                Console.WriteLine("Confugire.");

                app.UseSignalR(routes =>
                {
                    routes.MapHub<SignalHub>(hubUrl);
                });

                app.UseMvc();
            }
        }
    }
}
