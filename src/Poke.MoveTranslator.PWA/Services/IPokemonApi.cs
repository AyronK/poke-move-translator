using PokeApiNet;

namespace Poke.MoveTranslator.PWA.Services;

public interface IPokemonApi
{
    Task<Dictionary<string,string>> GetLanguages();
    Task<Move> GetMove(string name, string language);
    Task<Move[]> SearchMoves(string searchByName, string language);
}