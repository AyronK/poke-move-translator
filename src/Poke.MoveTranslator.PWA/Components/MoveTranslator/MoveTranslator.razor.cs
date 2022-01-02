using System.Collections.ObjectModel;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Poke.MoveTranslator.PWA.Const;
using Poke.MoveTranslator.PWA.Extensions;
using Poke.MoveTranslator.PWA.Services;
using Poke.MoveTranslator.PWA.Shared;
using PokeApiNet;

namespace Poke.MoveTranslator.PWA.Components;

public class MoveTranslatorBase : ComponentBase
{
    private const string SearchHistoryStorageKey = "SearchHistory";
    private const string LastLanguageStorageKey = "LastLanguage";
    
    private string _language;

    [Inject]
    public ILocalStorageService LocalStorageService { get; set; }

    [Inject]
    public IPokemonApi PokeApi { get; set; }

    [CascadingParameter]
    public ErrorHandler ErrorHandler { get; set; }

    protected bool IsLoading { get; private set; }
    protected bool IsInitializing { get; private set; }
    protected Move Move { get; private set; }
    protected Dictionary<string, string> Languages { get; private set; }
    protected Dictionary<string, string> SearchSuggestions { get; } = new();
    protected string MoveEnglishName => Move?.GetMoveName(PokeConst.EnglishLanguage);
    protected bool IsMoveLoadDisabled => IsInitializing || IsLoading || string.IsNullOrWhiteSpace(MoveName);
    protected ObservableCollection<NameByLanguage> SearchHistory { get; private set; }
    protected string MoveName { get; set; }
    protected string Language
    {
        get => _language;
        set
        {
            _language = value;

            OnLanguageChange();
        }
    }

    private void OnLanguageChange()
    {
        if (Language is not null)
        {
            LocalStorageService.SetItemAsync(LastLanguageStorageKey, Language).AndForget();
        }
    }

    public MoveTranslatorBase()
    {
        _language = PokeConst.EnglishLanguage;
        MoveName = string.Empty;
    }

    protected override async Task OnInitializedAsync()
    {
        IsInitializing = true;
        Languages = await PokeApi.GetLanguages();
        Language = await LocalStorageService.GetItemAsync<string>(LastLanguageStorageKey) ?? PokeConst.EnglishLanguage;
        NameByLanguage[] searchHistoryFromLocalStorage = await LocalStorageService.GetItemAsync<NameByLanguage[]>(SearchHistoryStorageKey) ?? Array.Empty<NameByLanguage>();

        if (searchHistoryFromLocalStorage.Length > 3)
        {
            searchHistoryFromLocalStorage = searchHistoryFromLocalStorage.TakeLast(3).ToArray();
        }
        
        SearchHistory = new ObservableCollection<NameByLanguage>(searchHistoryFromLocalStorage);
        SearchHistory.CollectionChanged += async (_, a) => { await LocalStorageService.SetItemAsync(SearchHistoryStorageKey, SearchHistory.ToArray()); };
        IsInitializing = false;
    }

    protected async Task LoadMove()
    {
        if (IsMoveLoadDisabled)
        {
            return;
        }

        IsLoading = true;

        if (SearchSuggestions.ContainsValue(MoveName))
        {
            Move = await PokeApi.GetMove(SearchSuggestions.Single(k => k.Value == MoveName).Key, PokeConst.EnglishLanguage);
        }
        else
        {
            Move = await PokeApi.GetMove(MoveName.Trim(), Language);
        }

        SearchSuggestions.Clear();

        if (Move is null)
        {
            Move[] other = await PokeApi.SearchMoves(MoveName.Trim(), Language);

            if (other.Length == 1)
            {
                Move = other[0];
            }
            else if (other.Length == 0)
            {
                Console.WriteLine("Not found");
            }
            else
            {
                foreach (Move move in other)
                {
                    SearchSuggestions.Add(move.Name, move.GetMoveName(Language));
                }
            }
        }

        if (Move is not null)
        {
            MoveName = Move.GetMoveName(Language);
            NameByLanguage nameByLanguage = new(MoveName, Language);

            if (!SearchHistory.Contains(nameByLanguage))
            {
                if (SearchHistory.Count >= 3)
                {
                    SearchHistory.RemoveAt(0);
                }

                SearchHistory.Insert(0, nameByLanguage);
            }
        }

        IsLoading = false;
    }

    protected async Task LoadItemFromHistory(NameByLanguage nameByLanguage)
    {
        MoveName = nameByLanguage.Name;
        Language = nameByLanguage.Language;
        await LoadMove();
    }
}

public record NameByLanguage(string Name, string Language);