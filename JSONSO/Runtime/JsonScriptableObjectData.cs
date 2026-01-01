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
using UnityEngine;

namespace JSONSO
{
    /// <summary>
    /// Generic ScriptableObject that behaves like a JSON.
    /// Contains a root JsonValue that can be an object with nested properties.
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
    /// // Resultado:
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
    public class JsonScriptableObjectData : JsonScriptableObject
    {
        [SerializeField] 
        private JsonValue _root = JsonValue.Object();

        /// <summary>
        /// JSON root. It's an object (dictionary) where you can add properties.
        /// </summary>
        public JsonValue Root
        {
            get
            {
                if (_root == null || !_root.IsObject)
                {
                    _root = JsonValue.Object();
                }
                return _root;
            }
            set => _root = value;
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
            _root = JsonValue.Object();
        }

        /// <summary>
        /// Converts to JSON string.
        /// </summary>
        public override string ToJson(bool prettyPrint = false)
        {
            OnBeforeSerialize();
            return Root.ToJson(prettyPrint);
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

            _root = JsonValue.Parse(json);
            OnAfterDeserialize();
        }
    }
}
