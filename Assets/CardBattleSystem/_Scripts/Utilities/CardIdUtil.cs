using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Utility class to generate and retrieve unique identifiers for card types based on their C# type's runtime hash code.
/// This replaces string or int IDs with a stable, memory-addressable ID (the Type Hash Code).
/// </summary>
public static class CardIdUtil
{
    /// <summary>
    /// Calculates the unique identifier for a given card type based on its C# type name/class.
    /// </summary>
    /// <param name="cardTypeName">The fully qualified name of the class.</param>
    /// <returns>A unique string representing the hash code.</returns>
    public static string GetHashCodeId(string cardTypeName)
    {
        if (string.IsNullOrWhiteSpace(cardTypeName))
            return Guid.NewGuid().ToString(); // Fallback for safety

        // Combine type name with a prefix to ensure uniqueness in case of namespace collisions
        return $"{cardTypeName}_HASH_{Mathf.Abs(System.Reflection.Assembly.GetExecutingAssembly().GetType(cardTypeName).GetHashCode())}";
    }

    /// <summary>
    /// Helper to calculate and return the hash ID for an existing ScriptableObject asset, if possible.
    /// </summary>
    public static string CalculateAssetHashId<T>(T asset) where T : ScriptableObject 
    {
        return GetHashCodeId(typeof(T).FullName);
    }
}