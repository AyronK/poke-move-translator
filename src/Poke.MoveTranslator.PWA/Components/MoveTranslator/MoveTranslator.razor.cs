using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Poke.MoveTranslator.PWA.Services;
using PokeApiNet;

namespace Poke.MoveTranslator.PWA.Components;

public class MoveTranslatorBase : ComponentBase
{
    protected Dictionary<string, string> languages;
    [Inject]
    public ILocalStorageService LocalStorageService { get; set; }
    
    [Inject]
    public IPokemonApi PokeApi { get; set; }
    
    public string MoveName { get; set; }
    public bool IsLoading { get; set; }
    public bool IsInitializing { get; set; }
    public Move Move { get; set; }
    public string MoveEnglishName => Move?.Names.First(n => n.Language.Name == "en").Name;

    protected bool IsButtonDisabled => IsInitializing || IsLoading || string.IsNullOrWhiteSpace(Language) || string.IsNullOrWhiteSpace(MoveName);

    public string Language
    {
        get => _language;
        set
        {
            bool wasNull = string.IsNullOrWhiteSpace(_language);
            _language = value;
            if (!wasNull)
            {
                OnLanguageChange();
            }
        }
    }

    protected ObservableCollection<NameByLanguage> LastValues { get; set; }

    private void OnLanguageChange()
    {
        LocalStorageService.SetItemAsync("LastLanguage", Language);
    }

    private string _language;

    protected override async Task OnInitializedAsync()
    {
        IsInitializing = true;
        languages = await PokeApi.GetLanguages();
        Language = await LocalStorageService.GetItemAsync<string>("LastLanguage");
        LastValues = new ObservableCollection<NameByLanguage>(await LocalStorageService.GetItemAsync<NameByLanguage[]>("LastValues") ?? Array.Empty<NameByLanguage>());
        LastValues.CollectionChanged += async (_, a) =>
        {
            await LocalStorageService.SetItemAsync("LastValues", LastValues.ToArray());
            if (LastValues.Count >= 10 && a.Action == NotifyCollectionChangedAction.Add)
            {
                LastValues.RemoveAt(0);
            }
        };
        IsInitializing = false;
    }

    protected async Task LoadMove()
    {
        IsLoading = true;
        Move = await PokeApi.GetMove(MoveName, Language);

        NameByLanguage pair = new(MoveName, Language);
        if (!LastValues.Contains(pair))
        {
            LastValues.Add(pair);
        }

        IsLoading = false;
    }

    protected async Task OnLastValueClick(NameByLanguage lastValue)
    {
        MoveName = lastValue.Name;
        Language = lastValue.Language;
        await LoadMove();
    }

    protected record NameByLanguage(string Name, string Language);
}