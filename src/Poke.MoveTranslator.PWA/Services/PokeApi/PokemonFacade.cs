using System.Text.Json.Serialization;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using PokeApiNet;

namespace Poke.MoveTranslator.PWA.Services.PokeApi;

public class PokemonFacade : IPokemonApi, IDisposable
{
    private PokeApiClient PokeApi { get; }
    private IGraphQLClient GraphQLClient { get; }
    private IMemoryCache MemoryCache { get; }

    public PokemonFacade(PokeApiClient pokeApi, IGraphQLClient graphQLClient, IMemoryCache memoryCache)
    {
        PokeApi = pokeApi;
        GraphQLClient = graphQLClient;
        MemoryCache = memoryCache;
    }

    public async Task<Dictionary<string,string>> GetLanguages()
    {
        NamedApiResourceList<Language> languageCodes = await PokeApi.GetNamedResourcePageAsync<Language>();
        List<Language> languages = await PokeApi.GetResourceAsync(languageCodes.Results.Select(r => r));

        return languages.OrderBy(GetDisplayName).ToDictionary(l => l.Name, GetDisplayName);
    }

    private static string GetDisplayName(Language language)
    {
        string nativeName = language.Names.FirstOrDefault(n => n.Language.Name == language.Name)?.Name;
        return nativeName ?? language.Names.FirstOrDefault(n => n.Language.Name == "en")?.Name ?? language.Name;
    }

    public async Task<Move> GetMove(string name, string language = "en")
    {
        if (language == "en")
        {
            return await GetMoveByEnglishName(name);
        }

        string cacheKey = $"{nameof(PokemonFacade)}.{nameof(GetMove)}.({name},({language})";
        
        GraphQLResponse<PokemonMoveCollectionGraphQL> moveResult = await MemoryCache
            .GetOrCreateAsync(cacheKey, async (_) => await GetMoveFromGraphQL(name, language));

        if (moveResult.Data.Move.Length == 1)
        {
            return await GetMove(moveResult.Data.Move[0].Id);
        }

        return null;
    }

    private async Task<GraphQLResponse<PokemonMoveCollectionGraphQL>> GetMoveFromGraphQL(string name, string language)
    {
        const string query = @"
query getMoveByNameAndLanguage($name: String, $language: String) {
  move: pokemon_v2_move(limit: 1, where: {pokemon_v2_movenames: {name: {_ilike: $name}, _and: {pokemon_v2_language: {name: {_eq: $language}}}}}) {
    id
  }
}";
        GraphQLRequest moveRequest = new()
        {
            Query = query,
            OperationName = "getMoveByNameAndLanguage",
            Variables = new { name, language }
        };

        return await GraphQLClient.SendQueryAsync<PokemonMoveCollectionGraphQL>(moveRequest);
    }

    private async Task<Move> GetMoveByEnglishName(string englishName)
    {
        Move result = await PokeApi.GetResourceAsync<Move>(englishName);
        return result;
    }

    private async Task<Move> GetMove(int id)
    {
        Move result = await PokeApi.GetResourceAsync<Move>(id);
        return result;
    }
    
    private class PokemonMoveCollectionGraphQL
    {
        [JsonPropertyName("move")]
        public PokemonMoveGraphQL[] Move { get; set; }
    }

    private class PokemonMoveGraphQL
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    public void Dispose()
    {
        PokeApi.Dispose();
        GraphQLClient.Dispose();
    }
}