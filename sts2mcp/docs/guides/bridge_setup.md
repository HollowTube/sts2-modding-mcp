# Bridge Mod Setup (MCPTest)

The bridge mod is a C# mod that runs inside Slay the Spire 2 and opens a TCP server
for the MCP to query game state, play cards, navigate maps, and automate playtesting.

## How It Works

```
MCP Server (Python)  ──TCP JSON-RPC──>  MCPTest mod (C# inside game)
  bridge_client.py        port 21337       BridgeHandler.cs
```

All `bridge_*` tools communicate through this connection. Without the bridge mod loaded,
these tools return `"Bridge not running"` errors.

## Building MCPTest

Set `STS2_GAME_DIR` to your game install path, then build:

```bash
cd test_mod
STS2_GAME_DIR="/path/to/Slay the Spire 2" dotnet build MCPTest.csproj -c Debug
```

The csproj auto-detects the platform-specific data subfolder (Windows/Linux/macOS). You can also pass it directly: `dotnet build /p:Sts2GameDir="/path/to/game"`.

The built DLL lands in `test_mod/.godot/mono/temp/bin/Debug/` or `test_mod/bin/Debug/`.

## Installing MCPTest

Copy these files to the game's mods directory:

```
<game_dir>/mods/mcptest/
├── mcptest.dll          # MCPTest.dll from the build output, renamed to the manifest id
└── mod_manifest.json    # from test_mod/
```

Or use the MCP's own `install_mod` tool:

```
install_mod with project_dir="path/to/test_mod"
```

## Verifying the Connection

1. Launch Slay the Spire 2 with the MCPTest mod enabled
2. Use `bridge_ping` — should return `{"status": "ok", "mod": "MCPTest", ...}`
3. If it fails, check:
   - Is the game running? MCPTest only starts after the game reaches the main menu.
   - Is MCPTest in the mod list? Check the in-game mod manager.
   - Is port 21337 available? Another instance or firewall may block it.

## Protocol

The bridge uses newline-delimited JSON-RPC over TCP:

**Request:**
```json
{"method": "get_screen", "id": 1}
```

**Response:**
```json
{"result": {"screen": "MAIN_MENU"}, "id": 1}
```

Methods with parameters:
```json
{"method": "play_card", "params": {"card_index": 0, "target_index": 0}, "id": 2}
```

## Available Bridge Methods

### State Queries (safe, read-only)
- `ping` — Connection check + mod version + current screen
- `get_screen` — Current screen name (MAIN_MENU, MAP, COMBAT_PLAYER_TURN, EVENT, SHOP, REST_SITE, etc.)
- `get_run_state` — Act, floor, ascension, seed, player stats
- `get_combat_state` — Enemies with intent decomposition, hand with playability, energy, piles
- `get_player_state` — Full deck, relics, potions, gold, HP
- `get_map_state` — All map nodes with types, visited status, available paths
- `get_available_actions` — All currently legal actions
- `get_card_piles` — Detailed hand, draw, discard, exhaust pile contents

### Actions (modify game state)
- `play_card` — Play a card from hand (params: card_index, target_index)
- `end_turn` — End the current player turn
- `start_run` — Start a new run (params: character, ascension)
- `use_potion` — Use a potion (params: potion_index, target_index)
- `make_event_choice` — Select an event option (params: choice_index)
- `navigate_map` — Travel to a map node (params: row, col)
- `rest_site_choice` — Choose rest/smith/recall (params: choice)
- `shop_action` — Buy or remove at shop (params: action, index)
- `console` — Execute any dev console command (params: command)
- `manipulate_state` — Batch debug changes: hp, gold, energy, relics, cards, powers, fights

## Screen State Reference

| Screen | When | Available Actions |
|--------|------|-------------------|
| `MAIN_MENU` | Game launched, no run | `start_run` |
| `CHARACTER_SELECT` | Picking character | (use start_run instead) |
| `MAP` | Viewing map | `navigate_map` to available nodes |
| `COMBAT_PLAYER_TURN` | Your turn in combat | `play_card`, `end_turn`, `use_potion` |
| `COMBAT_ENEMY_TURN` | Enemy turn | (wait) |
| `EVENT` | Narrative event | `make_event_choice` |
| `SHOP` | Merchant room | `shop_action` |
| `REST_SITE` | Campfire | `rest_site_choice` |
| `TREASURE` | Treasure room | (auto-resolves or use console) |
| `REWARD` | Post-combat rewards | (use console) |
| `CARD_SELECTION` | Picking a card | (use console) |

## Troubleshooting

**"Bridge not running"**
- Game must be open AND past the loading screen
- MCPTest mod must be in the mods list and enabled
- Check `mods/mcptest/mod_manifest.json` exists and is valid JSON, and that the DLL is named `mcptest.dll` (the game loads `<manifest id>.dll`; on Linux the case must match)

**"Bridge timed out"**
- Game may be in a loading screen or transition
- MCPTest dispatches state reads on the main thread — if the game is frozen, so is the bridge
- Try `bridge_ping` to check if basic connectivity works

**"Empty response from bridge"**
- The game may have crashed or the mod threw an exception
- Check the Godot log: `%APPDATA%/Godot/app_userdata/Slay the Spire 2/logs/godot.log`

**Port 21337 already in use**
- Only one game instance can use the bridge at a time
- Kill any stale game processes
