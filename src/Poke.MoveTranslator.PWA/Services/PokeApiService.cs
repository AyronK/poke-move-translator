using System.Text.Json.Serialization;
using Blazored.LocalStorage;
using GraphQL;
using GraphQL.Client.Http;
using Newtonsoft.Json;
using PokeApiNet;

namespace Poke.MoveTranslator.PWA.Services;

public class PokeApiService : IPokeApiService, IDisposable
{
    private PokeApiClient PokeApi { get; }
    private GraphQLHttpClient GraphQLClient { get; }

    public PokeApiService(PokeApiClient pokeApi, GraphQLHttpClient graphQLClient)
    {
        PokeApi = pokeApi;
        GraphQLClient = graphQLClient;
    }

    public async Task<string[]> GetLanguages()
    {
        NamedApiResourceList<Language> result = await PokeApi.GetNamedResourcePageAsync<Language>(100, 0);
        return result.Results.Select(l => l.Name).ToArray();
    }

    public async Task<Move> GetMove(string englishName)
    {
        Move result = await PokeApi.GetResourceAsync<Move>(englishName);
        return result;
    }

    private async Task<Move> GetMove(int id)
    {
        Move result = await PokeApi.GetResourceAsync<Move>(id);
        return result;
    }

    public async Task<Move> GetMove(string name, string language)
    {
        string query = $"query samplePokeAPIquery {{\n  pokemon_v2_move(limit: 1, where: {{pokemon_v2_movenames: {{name: {{_similar: \"{name}\"}}, _and: {{pokemon_v2_language: {{name: {{_eq: \"{language}\"}}}}}}}}}}) {{\n    id\n  }}\n}}\n";
        var moveRequest = new GraphQLRequest {
            Query = query,
            OperationName = "samplePokeAPIquery"
        };
        var result = await GraphQLClient.SendQueryAsync<MoveQL>(moveRequest);
        if (result.Data.PokemonV2Move.Length == 1)
        {
            return await GetMove(result.Data.PokemonV2Move[0].Id);
        }

        throw new Exception();
    }
    
    private class MoveQL
    {
        [JsonProperty("pokemon_v2_move")]
        public PokemonV2Move[] PokemonV2Move { get; set; }
    }

    private class PokemonV2Move
    {
        [JsonProperty("id")]
        public int Id { get; set; }
    }

    public void Dispose()
    {
        PokeApi.Dispose();
    }
}

public class PokeApiServiceCachedByLocalStorage : IPokeApiService
{
    private const string LocalStorageKeyPrefix = "Poke.API";
    
    private IPokeApiService ApiService { get; }
    private ILocalStorageService LocalStorage { get; }

    public PokeApiServiceCachedByLocalStorage(IPokeApiService apiService, ILocalStorageService localStorage)
    {
        ApiService = apiService;
        LocalStorage = localStorage;
    }

    public async Task<string[]> GetLanguages()
    {
        return await LocalStorage.GetOrAddAsync(LocalStorageKeyPrefix + "/languages", ApiService.GetLanguages);
    }

    public Task<Move> GetMove(string englishName)
    {
        return LocalStorage.GetOrAddAsync(LocalStorageKeyPrefix + "/move/" + englishName, () => ApiService.GetMove(englishName));
    }

    public Task<Move> GetMove(string name, string language)
    {
        return LocalStorage.GetOrAddAsync(LocalStorageKeyPrefix + "/" + language + "/move/" + name, () => ApiService.GetMove(name, language));
    }
}

public static class LocalStorageServiceExtensions
{
    public static async Task<T> GetOrAddAsync<T>(this ILocalStorageService localStorageService, string key, Func<Task<T>> valueFactory)
    {
        bool exists = await localStorageService.ContainKeyAsync(key);

        if (!exists)
        {
            T value = await valueFactory();
            await localStorageService.SetItemAsync(key, value);
        }

        return await localStorageService.GetItemAsync<T>(key);
    }
}

public interface IPokeApiService
{
    Task<string[]> GetLanguages();
    Task<Move> GetMove(string englishName);
    Task<Move> GetMove(string name, string language);
}