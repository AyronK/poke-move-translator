using Blazored.LocalStorage;

namespace Poke.MoveTranslator.PWA.Extensions;

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