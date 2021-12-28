using Blazored.LocalStorage;
using PokeApiNet;

namespace Poke.MoveTranslator.PWA.Services;

public class PokeApiService : IPokeApiService, IDisposable
{
    private PokeApiClient PokeApi { get; }

    public PokeApiService(PokeApiClient pokeApi)
    {
        PokeApi = pokeApi;
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
}