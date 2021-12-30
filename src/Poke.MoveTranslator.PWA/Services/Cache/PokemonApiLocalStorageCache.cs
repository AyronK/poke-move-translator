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