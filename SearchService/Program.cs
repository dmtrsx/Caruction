using MassTransit;
using MongoDB.Driver;
using MongoDB.Entities;
using Polly;
using Polly.Extensions.Http;
using SearchService.Consumers;
using SearchService.Data;
using SearchService.Models;
using SearchService.Services;
using System.Net;

namespace SearchService
{
    public class Program
    {
        private static IAsyncPolicy<HttpResponseMessage> GetPolicy() => HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
            .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(3));
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddHttpClient<AuctionSvcHttpClient>().AddPolicyHandler(GetPolicy());
            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            builder.Services.AddMassTransit(x =>
            {
                x.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();

                x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("search", false));

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.ReceiveEndpoint("search-auction-created", e => {
                        e.UseMessageRetry(r => r.Interval(5, 5));

                        e.ConfigureConsumer<AuctionCreatedConsumer>(context);
                    });

                    cfg.ConfigureEndpoints(context);
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseAuthorization();

            app.MapControllers();
            

            app.Lifetime.ApplicationStarted.Register(async () =>
            {
                await DbInitializer.InitDb(app);
            });

            app.Run();
        }
    }
}