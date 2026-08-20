# Project Overview
- Game Title: Scalpers Ruin Everything
- High-Level Concept: A multiplayer procedural dungeon crawler where progression is gated by winning various card games (Hearthstone, Snap, Mahjong).
- Players: Multiplayer (NGO-based)
- Target Platform: PC (StandaloneWindows64)
- Render Pipeline: URP
- Input System: New Input System

# Game Mechanics
## Core Gameplay Loop
1. Players spawn in a procedural dungeon.
2. Players explore rooms until they find a "Game Table".
3. Interacting with the Game Table starts a card game of a specific type (Hearthstone, Snap, or Mahjong) assigned to that room.
4. Winning the card game unlocks the exit door to the next room.
5. The process repeats until the final boss room is reached and cleared.

## Controls and Input Methods
- WASD for movement.
- Mouse to look.
- 'E' (Interact) to interact with Game Tables and Doors.
- UITK-based interface for card games.

# UI
- **Dungeon HUD**: Shows current room game type and status (Locked/Unlocked).
- **Card Game UI**: Existing UITK screens for Hearthstone, Snap, and Mahjong.

# Key Asset & Context
- `DungeonGenerator`: New script to handle procedural layout and spawning.
- `DungeonRoom`: New script to manage individual room state and card game triggers.
- `DungeonDoor`: New script for the visual/physical lock.
- `RoomPrefab`: A new prefab containing room geometry, a Game Table, and an Exit Door.
- `CardBattleManager`: Existing script to be integrated with the room system.

# Implementation Steps

## Step 1: Core Room & Door Components
1. Create `DungeonRoom.cs` to store the room's card game type (`ModuleType`) and completion status (`isCompleted`) as `NetworkVariable`s.
2. Create `DungeonDoor.cs` to handle the visual/physical state of the door (Open/Closed) based on the room's status.
3. Create a basic `RoomPrefab` using ProBuilder or Cubes. It should include:
    - A floor, 4 walls, and a ceiling.
    - An entrance point and an exit point.
    - A "Game Table" object with a collider for interaction.
    - A `DungeonRoom` component on the root.

## Step 2: Dungeon Generator
1. Create `DungeonGenerator.cs` to handle the layout.
2. Implement a simple linear generation:
    - Server-only logic in `OnNetworkSpawn`.
    - Spawn a sequence of rooms, aligning them end-to-end.
    - Assign a random `ModuleType` to each room's `NetworkVariable`.
3. Ensure the generator spawns rooms as `NetworkObject`s so they are synced to all clients.

## Step 3: Interaction & Game Trigger
1. Modify `PlayerInteraction.cs` to support interacting with `DungeonRoom` (or a new `IInteractable` interface).
2. When a player interacts with a Game Table:
    - The `DungeonRoom` calls `CardBattleManager.activeModule = this.roomGameType.Value`.
    - The `DungeonRoom` calls `CardBattleManager.StartGameAsync()`.
3. Update `CardBattleManager.cs` to ensure `activeModule` can be set dynamically before starting a game.

## Step 4: Game Win & Progression
1. In `DungeonRoom.cs`, subscribe to `CardBattleManager.OnGameStateUpdated`.
2. Check for `state.IsGameOver`.
3. If the game is won, send an RPC to the server to set `isCompleted = true` for that room.
4. The `DungeonDoor` should react to the `isCompleted` change and open (e.g., play animation or disable collider).

## Step 5: Scene Integration
1. Add the `DungeonGenerator` to the `Multiplayer` scene.
2. Configure spawn points to be at the entrance of the first room.

# Verification & Testing
- **Manual Test**: Run two instances of the game. Verify the dungeon layout is identical on both.
- **Manual Test**: Interact with a Game Table. Verify the correct card game UI appears.
- **Manual Test**: Win a card game (or mock a win). Verify the door opens for all players.
- **Edge Case**: Ensure players can't skip rooms by interacting through walls.
- **Edge Case**: Verify behavior if multiple players interact with the table simultaneously.
