using System.Collections.Generic;
using NUnit.Framework;
using Poke.MoveTranslator.PWA.Const;
using Poke.MoveTranslator.PWA.Extensions;
using PokeApiNet;

namespace Poke.MoveTranslator.UnitTests.PWA;

public class MoveExtensionsTests
{
    [Test]
    public void When_GetMoveName_And_NoMatch_And_NoEnglishName()
    {
        // Given
        Move move = new()
        {
            Name = "default-name",
            Names = new List<Names>(0),
        };

        // When
        string name = move.GetMoveName("xx");
        
        // Then
        Assert.AreEqual("default-name", name);
    }
    
    [Test]
    public void When_GetMoveName_And_NoMatch_And_WithEnglishName()
    {
        // Given
        Move move = new()
        {
            Name = "default-name",
            Names = new()
            {
                new Names() { Name = "english-name", Language = new NamedApiResource<Language>() { Name = PokeConst.EnglishLanguage } }
            }
        };

        // When
        string name = move.GetMoveName("xx");
        
        // Then
        Assert.AreEqual("english-name", name);
    }
    
    [Test]
    public void When_GetMoveName_And_WithMatch()
    {
        // Given
        Move move = new()
        {
            Names = new()
            {
                new Names() { Name = "xx-name", Language = new NamedApiResource<Language>() { Name = "xx" } }
            }
        };

        // When
        string name = move.GetMoveName("xx");
        
        // Then
        Assert.AreEqual("xx-name", name);
    }
}