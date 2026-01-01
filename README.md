![JSONSO](https://img.shields.io/badge/JSONSO-JSON%20ScriptableObject-blue?style=for-the-badge&logo=unity)

JSONSO - JSON ScriptableObject for Unity
===

[![Unity](https://img.shields.io/badge/Unity-2019+-black.svg)](https://unity3d.com/pt/get-unity/download/archive)
[![MIT License](https://img.shields.io/badge/License-MIT-green.svg)](https://choosealicense.com/licenses/mit/)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-blueviolet)](https://makeapullrequest.com)

A powerful Unity tool for seamless bidirectional conversion between JSON and ScriptableObjects. Create, edit, save, and load JSON data directly in the Unity Editor with full serialization support.

## ✨ Features

- 🔄 **Bidirectional Conversion** - Convert ScriptableObjects to JSON and vice versa
- 📝 **Dynamic JSON Data** - Create flexible JSON structures without predefined classes
- 🎨 **Custom Editor** - Visual tree-view editor for JSON data manipulation
- 💾 **File I/O** - Save and load JSON files directly from ScriptableObjects
- 🔗 **Object Mapping** - Map any C# object to a ScriptableObject and back
- 📦 **Unity Serialization** - Full support for Unity's serialization system
- 🎯 **Type Safe** - Strongly typed value access with `JsonValue`

## 📦 Installation

### Via Git URL (Package Manager)

1. Open Package Manager in Unity (`Window > Package Manager`)
2. Click the `+` button and select `Add package from git URL...`
3. Enter the following URL:

```bash
https://github.com/xavierarpa/JSONSO.git
```

### Via .unitypackage

1. Download the latest `.unitypackage` from [Releases](https://github.com/xavierarpa/JSONSO/releases)
2. Import it into your Unity project (`Assets > Import Package > Custom Package`)

### Manual Installation

1. Download or clone the repository
2. Copy the `JSONSO` folder into your project's `Assets` folder

## 🚀 Quick Start

### Creating a JSON ScriptableObject

Right-click in the Project window and select `Create > JSONSO > Json Data` to create a new JSON data asset.

### Basic Usage with JsonScriptableObjectData

```csharp
using JSONSO;
using UnityEngine;

public class Example : MonoBehaviour
{
    public JsonScriptableObjectData jsonData;

    void Start()
    {
        // Set values directly
        jsonData["playerName"] = "Hero";
        jsonData["level"] = 25;
        jsonData["isActive"] = true;
        
        // Create nested objects
        jsonData["stats"] = JsonValue.Object();
        jsonData["stats"]["health"] = 100;
        jsonData["stats"]["mana"] = 50;
        jsonData["stats"]["strength"] = 15;
        
        // Create arrays
        jsonData["inventory"] = JsonValue.Array();
        jsonData["inventory"].Add("sword");
        jsonData["inventory"].Add("shield");
        jsonData["inventory"].Add("potion");
        
        // Convert to JSON string
        string json = jsonData.ToJson(prettyPrint: true);
        Debug.Log(json);
    }
}
```

**Output:**
```json
{
  "playerName": "Hero",
  "level": 25,
  "isActive": true,
  "stats": {
    "health": 100,
    "mana": 50,
    "strength": 15
  },
  "inventory": ["sword", "shield", "potion"]
}
```

### Creating Custom JSON ScriptableObjects

Inherit from `JsonScriptableObject` to create your own typed ScriptableObjects with JSON support:

```csharp
using JSONSO;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Game/Player Data")]
public class PlayerData : JsonScriptableObject
{
    public string playerName;
    public int level;
    public float experience;
    public bool isPremium;
    
    protected override void OnBeforeSerialize()
    {
        // Called before converting to JSON
        // Useful for data preparation
    }
    
    protected override void OnAfterDeserialize()
    {
        // Called after loading from JSON
        // Useful for validation
    }
}
```

### Converting to/from JSON

```csharp
// Convert ScriptableObject to JSON
string json = playerData.ToJson(prettyPrint: true);

// Load JSON into ScriptableObject
playerData.FromJson(jsonString);

// Save to file
playerData.SaveToFile("path/to/player.json");

// Load from file
playerData.LoadFromFile("path/to/player.json");
```

### Object Mapping

Convert any C# object to a ScriptableObject:

```csharp
// Your existing class
public class GameSave
{
    public string saveName;
    public int score;
    public float playTime;
}

// Load object data into ScriptableObject
var gameSave = new GameSave { saveName = "Save1", score = 1000, playTime = 3600f };
playerData.LoadFromObject(gameSave);

// Convert ScriptableObject to object
GameSave loadedSave = playerData.ToObject<GameSave>();
```

## 📚 API Reference

### JsonScriptableObject (Base Class)

| Method | Description |
|--------|-------------|
| `ToJson(bool prettyPrint)` | Converts the ScriptableObject to a JSON string |
| `FromJson(string json)` | Loads data from a JSON string |
| `ToObject<T>()` | Converts to an object of type T |
| `LoadFromObject(object obj)` | Loads data from any object |
| `SaveToFile(string path)` | Saves to a JSON file |
| `LoadFromFile(string path)` | Loads from a JSON file |

### JsonScriptableObjectData

| Property/Method | Description |
|-----------------|-------------|
| `Root` | The root JsonValue object |
| `this[string key]` | Direct access to root properties |
| `HasKey(string key)` | Checks if a key exists |
| `Remove(string key)` | Removes a key |
| `Count` | Number of root properties |
| `Clear()` | Clears all data |

### JsonValue

| Factory Method | Description |
|----------------|-------------|
| `JsonValue.Null()` | Creates a null value |
| `JsonValue.String(value)` | Creates a string value |
| `JsonValue.Number(value)` | Creates a numeric value |
| `JsonValue.Bool(value)` | Creates a boolean value |
| `JsonValue.Object()` | Creates an empty object (dictionary) |
| `JsonValue.Array()` | Creates an empty array |
| `JsonValue.Parse(json)` | Parses a JSON string |

| Property | Description |
|----------|-------------|
| `Type` | The JsonValueType of this value |
| `AsString` | Gets as string |
| `AsFloat` | Gets as float |
| `AsInt` | Gets as int |
| `AsBool` | Gets as bool |
| `AsObject` | Gets as Dictionary<string, JsonValue> |
| `AsArray` | Gets as List<JsonValue> |
| `IsNull` | Checks if null |
| `IsObject` | Checks if object |
| `IsArray` | Checks if array |

### JsonValueType

```csharp
public enum JsonValueType
{
    Null,
    String,
    Number,
    Boolean,
    Object,   // Nested dictionary
    Array     // List of JsonValues
}
```

## 🎨 Editor Features

The custom editor provides:

- **Visual Tree View** - Navigate and edit nested JSON structures
- **Add Property Button** - Easily add new properties
- **Type Selection** - Change value types on the fly
- **JSON Preview** - See the raw JSON output
- **Drag & Reorder** - Organize your data structure
- **Clear All** - Reset the entire data

## 📁 Project Structure

```
JSONSO/
├── Runtime/
│   ├── JsonScriptableObject.cs      # Base class for JSON-serializable SOs
│   ├── JsonScriptableObjectData.cs  # Generic JSON data container
│   ├── JsonValue.cs                 # Dynamic JSON value type
│   ├── JsonKeyValue.cs              # Key-value pair for serialization
│   ├── JsonValueType.cs             # Value type enumeration
│   └── JSONSO.asmdef
├── Editor/
│   ├── JsonScriptableObjectEditor.cs # Custom inspector
│   ├── JsonValueDrawer.cs            # Property drawer
│   └── JSONSO.Editor.asmdef
└── README.md
```

## 💡 Use Cases

- **Game Configuration** - Store game settings as editable JSON
- **Save System** - Create flexible save files
- **Data Import/Export** - Exchange data with external tools
- **API Integration** - Parse and store API responses
- **Localization** - Manage translation data
- **Level Data** - Store level configurations
- **Debug Tools** - Inspect and modify runtime data

## 🔧 Requirements

- Unity 2019.4 or higher
- .NET Standard 2.0 / .NET 4.x

## 📄 License

```
MIT License

Copyright (c) 2026 Xavier Arpa López Thomas Peter ('xavierarpa')

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📧 Contact

- **Author**: Xavier Arpa López Thomas Peter
- **Email**: [arpaxavier@gmail.com](mailto:arpaxavier@gmail.com)
- **GitHub**: [@xavierarpa](https://github.com/xavierarpa)
