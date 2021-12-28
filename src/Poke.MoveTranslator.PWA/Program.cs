using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Poke.MoveTranslator.PWA;
using MudBlazor.Services;
using Poke.MoveTranslator.PWA.Services;
using PokeApiNet;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddScoped<PokeApiClient>();

builder.Services.AddScoped<IPokeApiService, PokeApiServiceCachedByLocalStorage>((c) => 
    new PokeApiServiceCachedByLocalStorage(new PokeApiService(c.GetRequiredService<PokeApiClient>()), 
        c.GetRequiredService<ILocalStorageService>()));

await builder.Build().RunAsync();