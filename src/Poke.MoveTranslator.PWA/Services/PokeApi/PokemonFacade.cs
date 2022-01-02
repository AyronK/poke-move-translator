using System.Net;
using System.Text.Json.Serialization;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Poke.MoveTranslator.PWA.Const;
using Poke.MoveTranslator.PWA.Extensions;
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

    public async Task<Dictionary<string, string>> GetLanguages()
    {
        NamedApiResourceList<Language> languageCodes = await PokeApi.GetNamedResourcePageAsync<Language>();
        List<Language> languages = await PokeApi.GetResourceAsync(languageCodes.Results.Select(r => r));

        return languages.OrderBy(GetDisplayName).ToDictionary(l => l.Name, GetDisplayName);
    }

    private static string GetDisplayName(Language language)
    {
        string nativeName = language.Names.FirstOrDefault(n => n.Language.Name == language.Name)?.Name;
        return nativeName ?? language.Names.FirstOrDefault(n => n.Language.Name == PokeConst.EnglishLanguage)?.Name ?? language.Name;
    }

    public async Task<Move> GetMove(string name, string language)
    {
        if (language == PokeConst.EnglishLanguage && await GetMoveByEnglishName(name) is { } moveByEnglishNameResult)
        {
            return moveByEnglishNameResult;
        }

        string cacheKey = $"{nameof(PokemonFacade)}.{nameof(GetMove)}.({name},({language})";

        GraphQLResponse<PokemonMoveCollectionGraphQL> moveResult = await MemoryCache
            .GetOrCreateAsync(cacheKey, async (_) => await QueryMoveByNameAndLanguage(name, language, 1));

        if (moveResult.Data.Moves.Length == 1)
        {
            return await GetMove(moveResult.Data.Moves[0].Id);
        }

        return null;
    }

    public async Task<Move[]> SearchMoves(string searchByName, string language)
    {
        string cacheKey = $"{nameof(PokemonFacade)}.{nameof(SearchMoves)}.({searchByName},({language})";

        GraphQLResponse<PokemonMoveCollectionGraphQL> moveResult = await MemoryCache
            .GetOrCreateAsync(cacheKey, async (_) => await QueryMoveByNameAndLanguage(searchByName + "%", language, 3));

        Move[] moves = await Task.WhenAll(moveResult.Data.Moves.Select(async m => await GetMove(m.Id)));
        return moves.OrderBy(r => r.GetMoveName(language)).ToArray();
    }

    private async Task<GraphQLResponse<PokemonMoveCollectionGraphQL>> QueryMoveByNameAndLanguage(string name, string language, int limit)
    {
        const string query = @"
query getMoveByNameAndLanguage($name: String, $language: String, $limit: Int) {
  move: pokemon_v2_move(limit: $limit, where: {pokemon_v2_movenames: {name: {_ilike: $name}, _and: {pokemon_v2_language: {name: {_eq: $language}}}}}) {
    id
  }
}";

        GraphQLRequest moveRequest = new()
        {
            Query = query,
            OperationName = "getMoveByNameAndLanguage",
            Variables = new { name, language, limit }
        };

        return await GraphQLClient.SendQueryAsync<PokemonMoveCollectionGraphQL>(moveRequest);
    }

    private async Task<Move> GetMoveByEnglishName(string englishName)
    {
        try
        {
            Move result = await PokeApi.GetResourceAsync<Move>(englishName);
            return result;
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private async Task<Move> GetMove(int id)
    {
        Move result = await PokeApi.GetResourceAsync<Move>(id);
        return result;
    }

    private class PokemonMoveCollectionGraphQL
    {
        [JsonPropertyName("move")]
        public PokemonMoveGraphQL[] Moves { get; set; }
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