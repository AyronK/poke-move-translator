using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Poke.MoveTranslator.PWA.Extensions;
using Poke.MoveTranslator.PWA.Services;
using PokeApiNet;

namespace Poke.MoveTranslator.PWA.Components;

public class MoveTranslatorBase : ComponentBase
{
    private const string SearchHistoryStorageKey = "SearchHistory";
    private const string LastLanguageStorageKey = "LastLanguage";
    private string _language;
    private string _moveName;

    [Inject]
    public ILocalStorageService LocalStorageService { get; set; }

    [Inject]
    public IPokemonApi PokeApi { get; set; }

    protected bool IsLoading { get; private set; }
    protected bool IsInitializing { get; private set; }
    protected Move Move { get; private set; }
    protected Dictionary<string, string> Languages { get; private set; }
    protected List<string> MoveNameSuggestions { get; } = new();
    protected string MoveEnglishName => Move?.GetMoveName("en");
    protected bool IsButtonDisabled => IsInitializing || IsLoading || string.IsNullOrWhiteSpace(MoveName);
    protected ObservableCollection<NameByLanguage> SearchHistory { get; private set; }

    protected string MoveName
    {
        get => _moveName;
        set
        {
            _moveName = value;
            MoveNameSuggestions.Clear();
        }
    }

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

    protected override async Task OnInitializedAsync()
    {
        IsInitializing = true;
        Languages = await PokeApi.GetLanguages();
        Language = await LocalStorageService.GetItemAsync<string>(LastLanguageStorageKey);
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
        Move = await PokeApi.GetMove(MoveName.Trim(), Language);

        if (Move is null)
        {
            Move[] other = await PokeApi.SearchMoves(MoveName.Trim(), Language);

            if (other.Length == 1)
            {
                Move = other[0];
                MoveName = other[0].GetMoveName(Language);
            }
            else if (other.Length == 0)
            {
                Console.WriteLine("Not found");
            }
            else
            {
                MoveNameSuggestions.AddRange(other.Select(m => m.GetMoveName(Language)));
            }
        }

        if (Move is not null)
        {
            NameByLanguage nameByLanguage = new(Move.GetMoveName(Language), Language);

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