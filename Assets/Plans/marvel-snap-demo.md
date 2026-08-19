# Project Overview
- **Game Title**: Marvel Snap Demo
- **High-Level Concept**: A simplified implementation of Marvel Snap mechanics (3 locations, simultaneous turn submission, priority-based reveal, power calculation) using the BattleCardGameFramework.
- **Players**: 2 Players (Local Multiplayer or Human vs AI).
- **Inspiration**: Marvel Snap.
- **Tone / Art Direction**: Functional Demo (Console/Log-based or simple UI).
- **Target Platform**: PC.
- **Render Pipeline**: URP.

# Game Mechanics
## Core Gameplay Loop
1. **Turn Start**: Both players receive Energy equal to the current Turn (1 to 6).
2. **Play Phase**: Players simultaneously play cards from their Hand to one of the 3 Locations. Cards are placed "Face Down" (moved to a `Pending` collection) and Energy is spent.
3. **Turn End**: Once both players confirm, the turn resolves:
    - Determine **Priority** (who is currently winning the most locations/power).
    - Reveal cards for the player with Priority at each location.
    - Trigger **On Reveal** effects.
    - Reveal cards for the other player.
    - Trigger **On Reveal** effects.
4. **Game End**: After 6 turns, calculate the winner based on locations won.

## Controls and Input Methods
- **Play Card**: `game.Execute(new PlayCardAction(player, card, locationIndex))`.
- **End Turn**: `game.Execute(new ResolveTurnAction())`.

# UI
- A simple console-based output (like the Hearthstone demo) or a basic WorldSpace UI showing the 3 Locations, Power, and Cards.
- Wireframe:
    - Top: Opponent Hand / Deck.
    - Middle: 3 Locations (Location 1, Location 2, Location 3) each showing Player 1 Power vs Player 2 Power.
    - Bottom: Player Hand / Deck / Energy.

# Key Asset & Context
- **Namespace**: `snap`
- **Location Structure**: Each location will have a `SnapLocation` object containing:
    - `ICardCollection` for each player (Board and Pending).
    - `Power` calculation logic.
- **Stat Keys**: `Cost`, `Power`, `Energy`, `Turn`.
- **Collection Keys**: `Hand`, `Deck`, `Board1`, `Board2`, `Board3`, `Pending1`, `Pending2`, `Pending3`.

# Implementation Steps
1. **Define Constants and Base Classes**
    - Create `SnapConstants.cs` for keys.
    - Create `SnapCard.cs`, `SnapPlayer.cs`, `SnapGameState.cs`.
    - Assigned role: developer
    - Dependencies: None

2. **Implement Core Game Logic**
    - Create `SnapGame.cs` with turn management.
    - Create `SnapLocation.cs` to handle location-specific data.
    - Assigned role: developer
    - Dependencies: Step 1

3. **Implement Actions**
    - `PlayCardAction`: Validate energy, move card to `Pending` collection.
    - `RevealCardAction`: Move card from `Pending` to `Board`, set `IsRevealed`, trigger reactions.
    - `ResolveTurnAction`: Determine priority and execute `RevealCardAction`s in sequence.
    - `ModifyEnergyAction`: Update player energy.
    - Assigned role: developer
    - Dependencies: Step 2

4. **Implement Reactions and Components**
    - `SnapCardComponent`: Basic stats.
    - `OnRevealComponent`: Implementation of `ICardReaction` listening for `RevealCardAction`.
    - `OngoingComponent`: Implementation of `ICardReaction` updating stats.
    - Assigned role: developer
    - Dependencies: Step 3

5. **Demo Setup & Testing**
    - Create a `Program.cs` or a Test Scene to initialize the game with sample cards (e.g., Hawkeye, Medusa, Iron Man).
    - Verify simultaneous play and priority reveal.
    - Assigned role: developer
    - Dependencies: Step 4

# Verification & Testing
- **Unit Tests**:
    - Verify energy is spent when playing a card.
    - Verify priority is correctly calculated based on total power.
    - Verify "On Reveal" triggers only when the card is revealed, not when played.
- **Manual Checks**:
    - Play a game through 6 turns and ensure the winner is declared correctly.
    - Test tie-breaking logic (Total Power).
