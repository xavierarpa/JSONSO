/*
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
*/
using System;
using UnityEngine;

namespace JSONSO
{
    /// <summary>
    /// Generic ScriptableObject that behaves like a JSON.
    /// Uses string-backed storage to avoid Unity's serialization depth limit.
    /// The JsonValue tree is built on-demand and cached in memory.
    /// 
    /// Usage example:
    /// <code>
    /// var data = ScriptableObject.CreateInstance&lt;JsonScriptableObjectData&gt;();
    /// data.Root["name"] = "Player1";
    /// data.Root["level"] = 10;
    /// data.Root["stats"] = JsonValue.Object();
    /// data.Root["stats"]["health"] = 100;
    /// data.Root["stats"]["mana"] = 50;
    /// data.Root["inventory"] = JsonValue.Array();
    /// data.Root["inventory"].Add("sword");
    /// data.Root["inventory"].Add("shield");
    /// 
    /// string json = data.ToJson(true);
    /// // Result:
    /// // {
    /// //   "name": "Player1",
    /// //   "level": 10,
    /// //   "stats": {
    /// //     "health": 100,
    /// //     "mana": 50
    /// //   },
    /// //   "inventory": ["sword", "shield"]
    /// // }
    /// </code>
    /// </summary>
    [CreateAssetMenu(fileName = "NewJsonData", menuName = "JSONSO/Json Data", order = 99999999)]
    public class JsonScriptableObjectData : JsonScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField, TextArea(3, 15)] 
        private string _jsonString = "{}";
        
        [NonSerialized]
        private JsonValue _cachedRoot;
        
        [NonSerialized]
        private bool _isCacheDirty;

        /// <summary>
        /// JSON root. It's an object (dictionary) where you can add properties.
        /// The JsonValue is built lazily from the stored JSON string.
        /// </summary>
        public JsonValue Root
        {
            get
            {
                if (_cachedRoot == null)
                {
                    RebuildCache();
                }
                return _cachedRoot;
            }
            set
            {
                _cachedRoot = value ?? JsonValue.Object();
                _isCacheDirty = true;
            }
        }
        
        /// <summary>
        /// Rebuilds the cached JsonValue from the stored JSON string.
        /// </summary>
        private void RebuildCache()
        {
            try
            {
                if (string.IsNullOrEmpty(_jsonString) || _jsonString.Trim() == "")
                {
                    _cachedRoot = JsonValue.Object();
                }
                else
                {
                    _cachedRoot = JsonValue.Parse(_jsonString);
                    if (_cachedRoot == null || !_cachedRoot.IsObject)
                    {
                        _cachedRoot = JsonValue.Object();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonScriptableObjectData] Failed to parse JSON: {e.Message}");
                _cachedRoot = JsonValue.Object();
            }
            _isCacheDirty = false;
        }
        
        /// <summary>
        /// Marks the cache as dirty, causing it to be serialized on the next Unity serialization pass.
        /// Call this after modifying the Root directly.
        /// </summary>
        public void MarkDirty()
        {
            _isCacheDirty = true;
        }
        
        /// <summary>
        /// Forces the JSON string to be updated from the current cache.
        /// Useful before saving or when you need the string immediately.
        /// </summary>
        public void FlushToString()
        {
            if (_cachedRoot != null)
            {
                _jsonString = _cachedRoot.ToJson(true);
                _isCacheDirty = false;
            }
        }

        /// <summary>
        /// Direct access to root properties.
        /// </summary>
        public JsonValue this[string key]
        {
            get => Root[key];
            set => Root[key] = value;
        }

        /// <summary>
        /// Checks if a key exists in the root.
        /// </summary>
        public bool HasKey(string key) => Root.HasKey(key);

        /// <summary>
        /// Removes a key from the root.
        /// </summary>
        public bool Remove(string key) => Root.Remove(key);

        /// <summary>
        /// Number of properties in the root.
        /// </summary>
        public int Count => Root.Count;

        /// <summary>
        /// Clears all data.
        /// </summary>
        public void Clear()
        {
            _jsonString = "{}";
            _cachedRoot = JsonValue.Object();
            _isCacheDirty = false;
        }

        /// <summary>
        /// Converts to JSON string.
        /// </summary>
        public override string ToJson(bool prettyPrint = false)
        {
            OnBeforeSerialize();
            if (_cachedRoot != null)
            {
                return _cachedRoot.ToJson(prettyPrint);
            }
            return prettyPrint ? FormatJson(_jsonString) : _jsonString;
        }
        
        /// <summary>
        /// Simple JSON formatter for pretty print when cache is not available.
        /// </summary>
        private static string FormatJson(string json)
        {
            // If we have no cache, just parse and format
            var parsed = JsonValue.Parse(json);
            return parsed?.ToJson(true) ?? json;
        }

        /// <summary>
        /// Loads from JSON string.
        /// </summary>
        public override void FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[JsonScriptableObjectData] JSON string is null or empty.");
                return;
            }

            _jsonString = json;
            _cachedRoot = null; // Invalidate cache, will rebuild on next access
            _isCacheDirty = false;
            OnAfterDeserialize();
        }
        
        #region ISerializationCallbackReceiver
        
        /// <summary>
        /// Called by Unity before serializing to disk.
        /// Flushes the cached JsonValue back to the JSON string.
        /// </summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // Only update the string if we have a cache and it was modified
            if (_cachedRoot != null && _isCacheDirty)
            {
                _jsonString = _cachedRoot.ToJson(true);
                _isCacheDirty = false;
            }
        }
        
        /// <summary>
        /// Called by Unity after deserializing from disk.
        /// Invalidates the cache so it rebuilds on next access.
        /// </summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Don't rebuild here - lazy load on first access
            // Just invalidate the cache
            _cachedRoot = null;
            _isCacheDirty = false;
        }
        
        #endregion
        
        /// <summary>
        /// Gets the raw JSON string (for debugging/inspection).
        /// </summary>
        public string RawJson => _jsonString;
    }
}
