using Poke.MoveTranslator.PWA.Const;
using PokeApiNet;

namespace Poke.MoveTranslator.PWA.Extensions;

public static class MoveExtensions
{
    
    public static string GetMoveName(this Move move, string language)
    {
        return move.GetMoveNameOrNull(language) ?? move.GetMoveNameOrNull(PokeConst.EnglishLanguage) ?? move.Name;
    }

    private static string GetMoveNameOrNull(this Move move, string language)
    {
        return move.Names.FirstOrDefault(n => n.Language.Name == language)?.Name;
    }
}