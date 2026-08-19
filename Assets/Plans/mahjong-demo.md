# Project Overview
- **Game Title**: Mahjong Demo
- **High-Level Concept**: A simplified Mahjong implementation using the `BattleCardGameFramework`. It demonstrates how the framework's Card, Collection, and Action systems can be adapted for tile-based games.
- **Players**: Single player (interactive) vs 3 AI (automatic or simplified).
- **Target Platform**: PC / Unity Editor.
- **Render Pipeline**: URP (consistent with project settings).

# Game Mechanics
## Core Gameplay Loop
1. **Initialize**: 36 tiles (One suit: Dots 1-9, 4 of each) are shuffled into the 'Wall'.
2. **Deal**: Players receive 7 tiles (Simplified hand size for demo).
3. **Turn Start**: Active player draws a tile from the Wall.
4. **Action**: Active player chooses a tile to discard to the 'River'.
5. **Check Win**: After draw or discard, check if the hand is a 'Winning Hand' (2 melds of 3 + 1 pair).
6. **Next Turn**: Turn passes to the next player.

## Controls and Input Methods
- **UI Toolkit**: On-screen buttons for "Draw", "Discard", and "Next Turn".
- **Selection**: Click on a tile in the hand to mark it for discard.

# UI
- **Hand Display**: A row of tiles owned by the player.
- **River Display**: A grid showing discarded tiles.
- **Wall Count**: A label showing how many tiles remain in the wall.
- **Game Log**: A text area showing recent actions (e.g., "Player 1 discarded Dot 5").

# Key Asset & Context
- **Namespace**: `mahjong`
- **StatKeys**: `Suit`, `Value`.
- **CollectionKeys**: `Wall`, `Hand`, `River`.
- **Classes**:
    - `MahjongTile`: Inherits from `csbcgf.Card`.
    - `MahjongPlayer`: Inherits from `csbcgf.Player`.
    - `MahjongGameState`: Inherits from `csbcgf.GameState`.
    - `MahjongGame`: Inherits from `csbcgf.Game<MahjongGameState>`.
    - `MahjongDrawTileAction`: Inherits from `csbcgf.Action<MahjongGameState>`.
    - `MahjongDiscardTileAction`: Inherits from `csbcgf.Action<MahjongGameState>`.

# Implementation Steps
## 1. Core Framework Extension
- **Description**: Define constants and implement base Mahjong classes.
- **Files**:
    - `Assets/CardBattleSystem/_Scripts/BattleCardGameFramework/demos/mahjong/src/MahjongConstants.cs` (Enums, StatKeys)
    - `Assets/CardBattleSystem/_Scripts/BattleCardGameFramework/demos/mahjong/src/MahjongTile.cs`
    - `Assets/CardBattleSystem/_Scripts/BattleCardGameFramework/demos/mahjong/src/MahjongPlayer.cs`
    - `Assets/CardBattleSystem/_Scripts/BattleCardGameFramework/demos/mahjong/src/MahjongGameState.cs`
- **Assigned Role**: developer
- **Dependencies**: None
- **Parallelizable**: No

## 2. Actions and Rules
- **Description**: Implement game actions and win-check logic.
- **Files**:
    - `Assets/CardBattleSystem/_Scripts/BattleCardGameFramework/demos/mahjong/src/Actions/MahjongDrawTileAction.cs`
    - `Assets/CardBattleSystem/_Scripts/BattleCardGameFramework/demos/mahjong/src/Actions/MahjongDiscardTileAction.cs`
    - `Assets/CardBattleSystem/_Scripts/BattleCardGameFramework/demos/mahjong/src/Util/MahjongHandCalculator.cs`
- **Assigned Role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

## 3. Game and UI Implementation
- **Description**: Implement the `MahjongGame` manager and the UI Toolkit controller.
- **Files**:
    - `Assets/CardBattleSystem/_Scripts/BattleCardGameFramework/demos/mahjong/src/MahjongGame.cs`
    - `Assets/CardBattleSystem/_Scripts/BattleCardGameFramework/demos/mahjong/UI/MahjongGameUI.cs`
    - `Assets/CardBattleSystem/_Scripts/BattleCardGameFramework/demos/mahjong/mahjongDemo.asmdef`
- **Assigned Role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

# Verification & Testing
- **Unit Test**: Verify `MahjongHandCalculator` with predefined winning/losing hands (e.g., `111, 222, 33` is a win).
- **Manual Check**: Run the demo scene, draw/discard tiles, and ensure the Wall count decreases and River count increases.
- **Console Logs**: Use `Debug.Log` to track the state transitions.
