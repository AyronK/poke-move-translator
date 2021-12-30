using Blazored.LocalStorage;

namespace Poke.MoveTranslator.PWA.Extensions;

public static class LocalStorageServiceExtensions
{
    public static async Task<T> GetOrAddAsync<T>(this ILocalStorageService localStorageService, string key, Func<Task<T>> valueFactory) where T : class
    {
        bool exists = await localStorageService.ContainKeyAsync(key);

        if (exists)
        {
            return await localStorageService.GetItemAsync<T>(key);
        }

        T value = await valueFactory();

        if (value is null)
        {
            return null;
        }

        await localStorageService.SetItemAsync(key, value);
        return value;
    }
}