using Blazored.LocalStorage;
using Poke.MoveTranslator.PWA.Extensions;
using PokeApiNet;

namespace Poke.MoveTranslator.PWA.Services.Cache;

public class PokemonApiLocalStorageCache : IPokemonApi
{
    private const string LocalStorageKeyPrefix = "Poke.API";
    
    private IPokemonApi ApiService { get; }
    private ILocalStorageService LocalStorage { get; }

    public PokemonApiLocalStorageCache(IPokemonApi apiService, ILocalStorageService localStorage)
    {
        ApiService = apiService;
        LocalStorage = localStorage;
    }

    public async Task<Dictionary<string,string>> GetLanguages()
    {
        return await LocalStorage.GetOrCreateAsync(LocalStorageKeyPrefix + "/languages", ApiService.GetLanguages);
    }

    public Task<Move> GetMove(string name, string language)
    {
        return LocalStorage.GetOrCreateAsync(LocalStorageKeyPrefix + "/" + language + "/move/" + name.ToLowerInvariant(), () => ApiService.GetMove(name, language));
    }

    public Task<Move[]> SearchMoves(string searchByName, string language) 
        => ApiService.SearchMoves(searchByName, language); // do not store search in local storage
}