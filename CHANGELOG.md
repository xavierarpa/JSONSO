# Changelog
All notable changes to this package will be documented in this file.

## [2.0.0] - 2026-01-27

### ⚠️ Breaking Changes
- `JsonValue` is no longer Unity-serializable (removed `[Serializable]` and `[SerializeField]`)
- Direct serialization of `JsonValue` fields will show a warning in the Inspector

### Added
- **String-backed storage**: `JsonScriptableObjectData` now stores JSON as a string internally
- `ISerializationCallbackReceiver` implementation for automatic serialization sync
- `MarkDirty()` method to mark cache as modified after direct Root changes
- `FlushToString()` method to force immediate serialization to string
- `RawJson` property to access the raw JSON string for debugging
- Warning message in PropertyDrawer when attempting to serialize `JsonValue` directly

### Changed
- `JsonScriptableObjectData.Root` is now lazy-loaded from the stored JSON string
- Cache is automatically rebuilt when accessing `Root` after deserialization
- JSON is stored with pretty-print format in the .asset file for better readability

### Fixed
- **Unity serialization depth limit**: No longer causes "Serialization depth limit 10 exceeded" warnings
- Deeply nested JSON structures are now fully supported without depth restrictions

### Improved
- Git diffs are now human-readable (JSON text instead of Unity's verbose serialization)
- Smaller .asset file sizes for complex JSON structures
- Better performance with lazy-loading (JSON parsed only when accessed)

## [1.0.0] - 2026-01-02
This is the first release of *JSONSO*.