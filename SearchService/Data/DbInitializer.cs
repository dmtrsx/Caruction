using MongoDB.Driver;
using MongoDB.Entities;
using SearchService.Models;
using SearchService.Services;
using System.Text.Json;

namespace SearchService.Data
{
    public class DbInitializer
    {
        public static async Task InitDb(WebApplication application)
        {
            await DB.InitAsync("SearchDB", MongoClientSettings
                .FromConnectionString(application.Configuration.GetConnectionString("MongoDbConnection")));

            await DB.Index<Item>()
                .Key(x => x.Make, KeyType.Text)
                .Key(x => x.Model, KeyType.Text)
                .Key(x => x.Color, KeyType.Text)
                .CreateAsync();

            var count = await DB.CountAsync<Item>();

            //Google this part how it works
            using var scope = application.Services.CreateScope();

            var httpClient = scope.ServiceProvider.GetRequiredService<AuctionSvcHttpClient>();

            //end googling part

            var items = await httpClient.GetItemsForSearchDb();

            Console.WriteLine(items.Count + "returned from service");

            if (items.Count > 0) await DB.SaveAsync(items);
        }
    }
}
