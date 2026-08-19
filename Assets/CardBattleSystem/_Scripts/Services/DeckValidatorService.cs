using csbcgf;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BattleCardGameFramework;

/// <summary>
/// Service responsible for validating and constructing a final ICardCollection from user-selected cards,
/// ensuring the deck adheres to game rules (size, uniqueness).
/// </summary>
public static class DeckValidatorService
{
    // NOTE: This service requires access to the card static data dictionary.
    // For simulation purposes, we assume it's passed in or globally accessible for validation checks.
    public static ICardCollection ValidateAndBuild(List<CardClientStateDTO> selectedCards, Dictionary<string, CardStaticData> staticDataDict)
    {
        if (selectedCards == null || !selectedCards.Any())
        {
            Debug.LogError("Deck validation failed: No cards provided.");
            return null;
        }

        // 1. Basic Size Check
        if (selectedCards.Count != 40)
        {
            Debug.LogError($"Deck validation failed: Incorrect size. Found {selectedCards.Count}, expected 40.");
            return null;
        }

        // 2. Uniqueness and Validity Check
        HashSet<string> uniqueCardIds = new HashSet<string>();
        foreach (var dto in selectedCards)
        {
            // Check if card ID is already used OR if the card type doesn't exist in static data.
            if (!uniqueCardIds.Add(dto.Id.ToString()))
            {
                Debug.LogError($"Deck validation failed: Card ID {dto.Id} is duplicated.");
                return null;
            }
            // Optional: Check against staticDataDict to ensure it's a valid, existing card type
            if (!staticDataDict.ContainsKey(dto.CardType))
            {
                 Debug.LogError($"Deck validation failed: Card Type {dto.CardType} is unknown.");
                 return null;
            }
        }

        // 3. Construction of the ICardCollection
        DeckCollection finalDeck = new(); // Assuming DeckCollection implements ICardCollection
        foreach (var dto in selectedCards)
        {
            // In a real scenario, we would instantiate the actual card object from the DTO/static data.
            // For this plan, we simulate adding it to the collection.
            finalDeck.AddCard(dto);
        }

        Debug.Log("Deck validation successful! Deck contains 40 unique cards.");
        return finalDeck;
    }
}

// NOTE TO SELF: I need to ensure that 'DeckCollection' is either a concrete class implementing
// ICardCollection or modify the existing framework code (e.g., in GameState) to accept this new structure.
// For now, the service logic stands on its own.

public class DeckCollection : ICardCollection
{
    public ICard this[int index] => throw new NotImplementedException();

    public IEnumerable<ICard> Cards => throw new NotImplementedException();

    public int Size => throw new NotImplementedException();

    public int? MaxSize { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public bool IsEmpty => throw new NotImplementedException();

    public bool IsFull => throw new NotImplementedException();

    public ICard First => throw new NotImplementedException();

    public ICard Last => throw new NotImplementedException();

    public IPlayer Owner { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public void AddCard(CardClientStateDTO card)
    {

    }

    public void Add(ICard card)
    {
        throw new NotImplementedException();
    }

    public bool Contains(ICard card)
    {
        throw new NotImplementedException();
    }

    public void Remove(ICard card)
    {
        throw new NotImplementedException();
    }

    public void Shuffle()
    {
        throw new NotImplementedException();
    }
}