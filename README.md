# State

> Server-authoritative player data storage framework for Valheim mods.

State provides extensible, server-authoritative JSON player data storage with automatic client synchronization. It's the foundation for persistent player data in the mod ecosystem.

## Features

- **Server Authority** - All writes go through server, clients get synced cache
- **Extensible Storage** - Mods register typed data modules with unique IDs
- **Auto Sync** - Data automatically synced to clients on connect/change
- **JSON Persistence** - Human-readable storage with versioning and backups

## Requirements

- [BepInEx 5.4.x](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
- [Jotunn 2.20.0+](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/)

## Installation

1. Install BepInEx and Jotunn
2. Extract `State.dll` to `BepInEx/plugins/`

## API Reference

### Defining Player Data

```csharp
using State;

// Define your custom data class
public class MyModData : IPlayerData
{
    public int Score { get; set; } = 0;
    public List<string> Unlocks { get; set; } = new();
    public Dictionary<string, int> Progress { get; set; } = new();

    public void Initialize() { }  // Called for new players

    public string Serialize() => JsonUtility.ToJson(this);

    public void Deserialize(string data)
    {
        if (!string.IsNullOrEmpty(data))
            JsonUtility.FromJsonOverwrite(data, this);
    }

    public bool Validate() => true;
}
```

### Registration and Access

```csharp
using State;

// Register in your mod's Awake()
Store.Register<MyModData>("mymod");

// Access player data
var data = Store.Get<MyModData>(player, "mymod");
data.Score += 100;
Store.MarkDirty(player, "mymod");  // Triggers sync to clients

// Check if registered
bool registered = Store.IsRegistered("mymod");

// Get all registered modules
var modules = Store.GetRegisteredModules();
```

### Server-Authoritative Pattern

```csharp
// IMPORTANT: All writes must go through the server
public void AddScore(Player player, int amount)
{
    // Only execute on server
    if (!ZNet.instance.IsServer()) return;

    var data = Store.Get<MyModData>(player, "mymod");
    data.Score += amount;
    Store.MarkDirty(player, "mymod");
}

// Reading works on both client and server (client has synced cache)
public int GetScore(Player player)
{
    var data = Store.Get<MyModData>(player, "mymod");
    return data.Score;
}
```

### Events

```csharp
// Called when any data module changes
Store.OnDataChanged += (playerId, moduleId) =>
{
    Plugin.Log.LogInfo($"Data changed: {moduleId} for player {playerId}");
};

// Called when data is synced to client
Store.OnDataSynced += (moduleId) =>
{
    Plugin.Log.LogInfo($"Data synced: {moduleId}");
};
```

## Architecture

### Data Flow

```
Server                              Client
  |                                   |
  +- Store (authoritative)            |
  |   +- Validates all writes         |
  |   +- Persists to JSON             |
  |   +- Triggers sync -------------->+- Store (cache)
  |                                   |   +- Read-only access
  |                                   |
  +- World Save <---------------------+
      +- {SaveFolder}/State/{World}/playerdata.json
```

### Persistence Format

```json
{
  "version": 1,
  "savedAt": "2024-01-15T10:30:00Z",
  "modules": {
    "vital_level": {
      "12345678": { "Level": 50, "TotalXP": 1500000 }
    },
    "viking": {
      "12345678": { "StartingPoint": "Warrior", "AllocatedNodes": {...} }
    },
    "mymod": {
      "12345678": { "Score": 100, "Unlocks": ["item1", "item2"] }
    }
  }
}
```

## Complete Example

```csharp
using BepInEx;
using State;

[BepInPlugin("com.author.mymod", "MyMod", "1.0.0")]
[BepInDependency("com.slatyo.state")]
public class MyMod : BaseUnityPlugin
{
    public class MyData : IPlayerData
    {
        public int Kills { get; set; } = 0;
        public int Deaths { get; set; } = 0;

        public void Initialize() { }
        public string Serialize() => JsonUtility.ToJson(this);
        public void Deserialize(string data) => JsonUtility.FromJsonOverwrite(data, this);
        public bool Validate() => Kills >= 0 && Deaths >= 0;
    }

    private void Awake()
    {
        // Register custom data
        Store.Register<MyData>("mymod.stats");

        Logger.LogInfo("MyMod initialized with State");
    }

    public void OnPlayerKill(Player killer)
    {
        if (!ZNet.instance.IsServer()) return;

        var data = Store.Get<MyData>(killer, "mymod.stats");
        data.Kills++;
        Store.MarkDirty(killer, "mymod.stats");
    }

    public void OnPlayerDeath(Player player)
    {
        if (!ZNet.instance.IsServer()) return;

        var data = Store.Get<MyData>(player, "mymod.stats");
        data.Deaths++;
        Store.MarkDirty(player, "mymod.stats");
    }
}
```

## License

MIT License - See LICENSE file for details.
