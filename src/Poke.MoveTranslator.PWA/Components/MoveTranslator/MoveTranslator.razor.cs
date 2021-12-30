using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Poke.MoveTranslator.PWA.Services;
using PokeApiNet;

namespace Poke.MoveTranslator.PWA.Components;

public class MoveTranslatorBase : ComponentBase
{
    private const string SearchHistoryStorageKey = "SearchHistory";
    private string _language;

    [Inject]
    public ILocalStorageService LocalStorageService { get; set; }
    
    [Inject]
    public IPokemonApi PokeApi { get; set; }

    protected bool IsLoading { get; private set; }
    protected bool IsInitializing { get; private set; }
    protected Move Move { get; private set; }
    protected Dictionary<string, string> Languages { get; private set; }
    protected string MoveEnglishName => Move?.Names.First(n => n.Language.Name == "en").Name;
    protected bool IsButtonDisabled => IsInitializing || IsLoading || string.IsNullOrWhiteSpace(Language) || string.IsNullOrWhiteSpace(MoveName);
    protected ObservableCollection<NameByLanguage> SearchHistory { get; private set; }

    protected string MoveName { get; set; }
    protected string Language
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

    private void OnLanguageChange()
    {
        LocalStorageService.SetItemAsync("LastLanguage", Language).AndForget();
    }

    protected override async Task OnInitializedAsync()
    {
        IsInitializing = true;
        Languages = await PokeApi.GetLanguages();
        Language = await LocalStorageService.GetItemAsync<string>(SearchHistoryStorageKey);
        SearchHistory = new ObservableCollection<NameByLanguage>(await LocalStorageService.GetItemAsync<NameByLanguage[]>(SearchHistoryStorageKey) ?? Array.Empty<NameByLanguage>());
        SearchHistory.CollectionChanged += async (_, a) =>
        {
            await LocalStorageService.SetItemAsync(SearchHistoryStorageKey, SearchHistory.ToArray());
            if (SearchHistory.Count >= 10 && a.Action == NotifyCollectionChangedAction.Add)
            {
                SearchHistory.RemoveAt(0);
            }
        };
        IsInitializing = false;
    }

    protected async Task LoadMove()
    {
        IsLoading = true;
        
        Move = await PokeApi.GetMove(MoveName, Language);

        if (Move is not null)
        {
            string nameInSearchedLanguage = Move.Names.FirstOrDefault(n => n.Language.Name == Language)?.Name;
            NameByLanguage nameByLanguage = new(nameInSearchedLanguage ?? MoveName, Language);
            
            if (!SearchHistory.Contains(nameByLanguage))
            {
                SearchHistory.Add(nameByLanguage);
            }
        }

        IsLoading = false;
    }

    protected async Task OnLastValueClick(NameByLanguage nameByLanguage)
    {
        MoveName = nameByLanguage.Name;
        Language = nameByLanguage.Language;
        await LoadMove();
    }

    protected record NameByLanguage(string Name, string Language);
}