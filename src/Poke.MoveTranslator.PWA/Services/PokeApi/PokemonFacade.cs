using System.Text.Json.Serialization;
using GraphQL;
using GraphQL.Client.Abstractions;
using PokeApiNet;

namespace Poke.MoveTranslator.PWA.Services.PokeApi;

public class PokemonFacade : IPokemonApi, IDisposable
{
    private PokeApiClient PokeApi { get; }
    private IGraphQLClient GraphQLClient { get; }

    public PokemonFacade(PokeApiClient pokeApi, IGraphQLClient graphQLClient)
    {
        PokeApi = pokeApi;
        GraphQLClient = graphQLClient;
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
        
        const string query = @"
query getMoveByNameAndLanguage($name: String, $language: String) {
  move: pokemon_v2_move(limit: 1, where: {pokemon_v2_movenames: {name: {_ilike: $name}, _and: {pokemon_v2_language: {name: {_eq: $language}}}}}) {
    id
  }
}";
        GraphQLRequest moveRequest = new () {
            Query = query,
            OperationName = "getMoveByNameAndLanguage",
            Variables = new { name, language }
        };
        
        GraphQLResponse<PokemonMoveCollectionGraphQL> result = await GraphQLClient.SendQueryAsync<PokemonMoveCollectionGraphQL>(moveRequest);
        
        if (result.Data.Move.Length == 1)
        {
            return await GetMove(result.Data.Move[0].Id);
        }

        return null;
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