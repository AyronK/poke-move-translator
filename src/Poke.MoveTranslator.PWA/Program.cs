using Blazored.LocalStorage;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Caching.Memory;
using MudBlazor.Services;
using Poke.MoveTranslator.PWA.Services;
using Poke.MoveTranslator.PWA.Services.PokeApi;
using PokeApiNet;

namespace Poke.MoveTranslator.PWA;

public static class Program
{
    public static async Task Main(string[] args)
    {
        WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        ConfigureServices(builder);

        await builder.Build().RunAsync();
    }

    private static void ConfigureServices(WebAssemblyHostBuilder builder)
    {
        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
        builder.Services.AddMemoryCache();
        builder.Services.AddMudServices();
        builder.Services.AddBlazoredLocalStorage();

        builder.Services.AddScoped<PokeApiClient>();
        builder.Services.AddScoped(PokemonApiFactory);
    }

    private static IPokemonApi PokemonApiFactory(IServiceProvider c)
    {
        GraphQLHttpClient pokemonGraphQLApi = new("https://beta.pokeapi.co/graphql/v1beta", new SystemTextJsonSerializer());
        PokeApiClient pokeApiClient = c.GetRequiredService<PokeApiClient>();
        IMemoryCache memoryCache = c.GetRequiredService<IMemoryCache>();
        return new PokemonFacade(pokeApiClient, pokemonGraphQLApi, memoryCache);
    }
}