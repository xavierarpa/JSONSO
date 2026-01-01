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
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace JSONSO
{
    /// <summary>
    /// Represents a dynamic JSON value that can be:
    /// - null
    /// - string
    /// - number (float)
    /// - boolean
    /// - object (nested dictionary)
    /// - array (list of values)
    /// </summary>
    [Serializable]
    public class JsonValue
    {
        [SerializeField] private JsonValueType _type = JsonValueType.Null;
        [SerializeField] private string _stringValue;
        [SerializeField] private float _numberValue;
        [SerializeField] private bool _boolValue;
        [SerializeField] private List<JsonKeyValue> _objectValue = new List<JsonKeyValue>();
        [SerializeField] private List<JsonValue> _arrayValue = new List<JsonValue>();

        public JsonValueType Type => _type;

        #region Constructors

        public JsonValue()
        {
            _type = JsonValueType.Null;
        }

        public JsonValue(string value)
        {
            _type = JsonValueType.String;
            _stringValue = value;
        }

        public JsonValue(float value)
        {
            _type = JsonValueType.Number;
            _numberValue = value;
        }

        public JsonValue(int value)
        {
            _type = JsonValueType.Number;
            _numberValue = value;
        }

        public JsonValue(bool value)
        {
            _type = JsonValueType.Boolean;
            _boolValue = value;
        }

        public JsonValue(Dictionary<string, JsonValue> obj)
        {
            _type = JsonValueType.Object;
            _objectValue = new List<JsonKeyValue>();
            foreach (var kvp in obj)
            {
                _objectValue.Add(new JsonKeyValue(kvp.Key, kvp.Value));
            }
        }

        public JsonValue(List<JsonValue> array)
        {
            _type = JsonValueType.Array;
            _arrayValue = new List<JsonValue>(array);
        }

        #endregion

        #region Static Factory Methods

        public static JsonValue Null() => new JsonValue();
        public static JsonValue String(string value) => new JsonValue(value);
        public static JsonValue Number(float value) => new JsonValue(value);
        public static JsonValue Number(int value) => new JsonValue(value);
        public static JsonValue Bool(bool value) => new JsonValue(value);
        
        public static JsonValue Object()
        {
            var value = new JsonValue();
            value._type = JsonValueType.Object;
            value._objectValue = new List<JsonKeyValue>();
            return value;
        }

        public static JsonValue Array()
        {
            var value = new JsonValue();
            value._type = JsonValueType.Array;
            value._arrayValue = new List<JsonValue>();
            return value;
        }

        #endregion

        #region Getters

        public string AsString => _type == JsonValueType.String ? _stringValue : null;
        public float AsFloat => _type == JsonValueType.Number ? _numberValue : 0f;
        public int AsInt => _type == JsonValueType.Number ? (int)_numberValue : 0;
        public bool AsBool => _type == JsonValueType.Boolean && _boolValue;
        public bool IsNull => _type == JsonValueType.Null;
        public bool IsObject => _type == JsonValueType.Object;
        public bool IsArray => _type == JsonValueType.Array;

        /// <summary>
        /// Gets the internal dictionary if it's an Object.
        /// </summary>
        public Dictionary<string, JsonValue> AsObject
        {
            get
            {
                if (_type != JsonValueType.Object) return null;
                var dict = new Dictionary<string, JsonValue>();
                foreach (var kvp in _objectValue)
                {
                    dict[kvp.key] = kvp.value;
                }
                return dict;
            }
        }

        /// <summary>
        /// Gets the internal list if it's an Array.
        /// </summary>
        public List<JsonValue> AsArray => _type == JsonValueType.Array ? _arrayValue : null;

        #endregion

        #region Object Operations

        /// <summary>
        /// Index access for objects (dictionaries).
        /// </summary>
        public JsonValue this[string key]
        {
            get
            {
                if (_type != JsonValueType.Object) return null;
                foreach (var kvp in _objectValue)
                {
                    if (kvp.key == key) return kvp.value;
                }
                return null;
            }
            set
            {
                if (_type != JsonValueType.Object)
                {
                    _type = JsonValueType.Object;
                    _objectValue = new List<JsonKeyValue>();
                }

                for (int i = 0; i < _objectValue.Count; i++)
                {
                    if (_objectValue[i].key == key)
                    {
                        _objectValue[i] = new JsonKeyValue(key, value);
                        return;
                    }
                }
                _objectValue.Add(new JsonKeyValue(key, value));
            }
        }

        /// <summary>
        /// Index access for arrays.
        /// </summary>
        public JsonValue this[int index]
        {
            get
            {
                if (_type != JsonValueType.Array || index < 0 || index >= _arrayValue.Count)
                    return null;
                return _arrayValue[index];
            }
            set
            {
                if (_type != JsonValueType.Array) return;
                if (index >= 0 && index < _arrayValue.Count)
                    _arrayValue[index] = value;
            }
        }

        /// <summary>
        /// Checks if a key exists in the object.
        /// </summary>
        public bool HasKey(string key)
        {
            if (_type != JsonValueType.Object) return false;
            foreach (var kvp in _objectValue)
            {
                if (kvp.key == key) return true;
            }
            return false;
        }

        /// <summary>
        /// Removes a key from the object.
        /// </summary>
        public bool Remove(string key)
        {
            if (_type != JsonValueType.Object) return false;
            for (int i = 0; i < _objectValue.Count; i++)
            {
                if (_objectValue[i].key == key)
                {
                    _objectValue.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Renames a key while maintaining its original position.
        /// </summary>
        public bool RenameKeyInPlace(string oldKey, string newKey)
        {
            if (_type != JsonValueType.Object) return false;
            for (int i = 0; i < _objectValue.Count; i++)
            {
                if (_objectValue[i].key == oldKey)
                {
                    _objectValue[i] = new JsonKeyValue(newKey, _objectValue[i].value);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets all keys from the object.
        /// </summary>
        public IEnumerable<string> Keys
        {
            get
            {
                if (_type != JsonValueType.Object) yield break;
                foreach (var kvp in _objectValue)
                {
                    yield return kvp.key;
                }
            }
        }

        /// <summary>
        /// Number of elements (for Object or Array).
        /// </summary>
        public int Count
        {
            get
            {
                return _type switch
                {
                    JsonValueType.Object => _objectValue?.Count ?? 0,
                    JsonValueType.Array => _arrayValue?.Count ?? 0,
                    _ => 0
                };
            }
        }

        #endregion

        #region Array Operations

        /// <summary>
        /// Adds a value to the array.
        /// </summary>
        public void Add(JsonValue value)
        {
            if (_type != JsonValueType.Array)
            {
                _type = JsonValueType.Array;
                _arrayValue = new List<JsonValue>();
            }
            _arrayValue.Add(value);
        }

        /// <summary>
        /// Removes an element from the array by index.
        /// </summary>
        public void RemoveAt(int index)
        {
            if (_type == JsonValueType.Array && index >= 0 && index < _arrayValue.Count)
            {
                _arrayValue.RemoveAt(index);
            }
        }

        /// <summary>
        /// Inserts an element at a specific position in the array.
        /// </summary>
        public void InsertAt(int index, JsonValue value)
        {
            if (_type != JsonValueType.Array)
            {
                _type = JsonValueType.Array;
                _arrayValue = new List<JsonValue>();
            }
            
            if (index < 0) index = 0;
            if (index >= _arrayValue.Count)
            {
                _arrayValue.Add(value);
            }
            else
            {
                _arrayValue.Insert(index, value);
            }
        }

        #endregion

        #region JSON Serialization

        /// <summary>
        /// Converts this value to a JSON string.
        /// </summary>
        public string ToJson(bool prettyPrint = false)
        {
            var sb = new StringBuilder();
            WriteJson(sb, prettyPrint, 0);
            return sb.ToString();
        }

        private void WriteJson(StringBuilder sb, bool prettyPrint, int indent)
        {
            string newline = prettyPrint ? "\n" : "";
            string indentStr = prettyPrint ? new string(' ', indent * 2) : "";
            string nextIndent = prettyPrint ? new string(' ', (indent + 1) * 2) : "";

            switch (_type)
            {
                case JsonValueType.Null:
                    sb.Append("null");
                    break;

                case JsonValueType.String:
                    sb.Append('"');
                    sb.Append(EscapeString(_stringValue ?? ""));
                    sb.Append('"');
                    break;

                case JsonValueType.Number:
                    sb.Append(_numberValue.ToString(CultureInfo.InvariantCulture));
                    break;

                case JsonValueType.Boolean:
                    sb.Append(_boolValue ? "true" : "false");
                    break;

                case JsonValueType.Object:
                    sb.Append('{');
                    if (_objectValue.Count > 0)
                    {
                        sb.Append(newline);
                        for (int i = 0; i < _objectValue.Count; i++)
                        {
                            var kvp = _objectValue[i];
                            sb.Append(nextIndent);
                            sb.Append('"');
                            sb.Append(EscapeString(kvp.key));
                            sb.Append('"');
                            sb.Append(prettyPrint ? ": " : ":");
                            kvp.value?.WriteJson(sb, prettyPrint, indent + 1);
                            if (i < _objectValue.Count - 1) sb.Append(',');
                            sb.Append(newline);
                        }
                        sb.Append(indentStr);
                    }
                    sb.Append('}');
                    break;

                case JsonValueType.Array:
                    sb.Append('[');
                    if (_arrayValue.Count > 0)
                    {
                        sb.Append(newline);
                        for (int i = 0; i < _arrayValue.Count; i++)
                        {
                            sb.Append(nextIndent);
                            _arrayValue[i]?.WriteJson(sb, prettyPrint, indent + 1);
                            if (i < _arrayValue.Count - 1) sb.Append(',');
                            sb.Append(newline);
                        }
                        sb.Append(indentStr);
                    }
                    sb.Append(']');
                    break;
            }
        }

        private static string EscapeString(string str)
        {
            return str
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        #endregion

        #region Parsing JSON

        /// <summary>
        /// Parses a JSON string and returns a JsonValue.
        /// </summary>
        public static JsonValue Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return Null();
            int index = 0;
            return ParseValue(json, ref index);
        }

        private static JsonValue ParseValue(string json, ref int index)
        {
            SkipWhitespace(json, ref index);
            if (index >= json.Length) return Null();

            char c = json[index];

            if (c == '{') return ParseObject(json, ref index);
            if (c == '[') return ParseArray(json, ref index);
            if (c == '"') return ParseString(json, ref index);
            if (c == 't' || c == 'f') return ParseBool(json, ref index);
            if (c == 'n') return ParseNull(json, ref index);
            if (c == '-' || char.IsDigit(c)) return ParseNumber(json, ref index);

            return Null();
        }

        private static JsonValue ParseObject(string json, ref int index)
        {
            var obj = Object();
            index++; // skip '{'
            SkipWhitespace(json, ref index);

            while (index < json.Length && json[index] != '}')
            {
                SkipWhitespace(json, ref index);
                if (json[index] == '}') break;

                // Parse key
                var keyValue = ParseString(json, ref index);
                string key = keyValue.AsString;

                SkipWhitespace(json, ref index);
                if (json[index] == ':') index++; // skip ':'
                SkipWhitespace(json, ref index);

                // Parse value
                var value = ParseValue(json, ref index);
                obj[key] = value;

                SkipWhitespace(json, ref index);
                if (json[index] == ',') index++; // skip ','
            }

            if (index < json.Length) index++; // skip '}'
            return obj;
        }

        private static JsonValue ParseArray(string json, ref int index)
        {
            var arr = Array();
            index++; // skip '['
            SkipWhitespace(json, ref index);

            while (index < json.Length && json[index] != ']')
            {
                SkipWhitespace(json, ref index);
                if (json[index] == ']') break;

                var value = ParseValue(json, ref index);
                arr.Add(value);

                SkipWhitespace(json, ref index);
                if (json[index] == ',') index++; // skip ','
            }

            if (index < json.Length) index++; // skip ']'
            return arr;
        }

        private static JsonValue ParseString(string json, ref int index)
        {
            index++; // skip opening '"'
            var sb = new StringBuilder();

            while (index < json.Length && json[index] != '"')
            {
                if (json[index] == '\\' && index + 1 < json.Length)
                {
                    index++;
                    char escaped = json[index];
                    sb.Append(escaped switch
                    {
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        _ => escaped
                    });
                }
                else
                {
                    sb.Append(json[index]);
                }
                index++;
            }

            if (index < json.Length) index++; // skip closing '"'
            return String(sb.ToString());
        }

        private static JsonValue ParseNumber(string json, ref int index)
        {
            int start = index;
            if (json[index] == '-') index++;

            while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.'))
            {
                index++;
            }

            string numStr = json.Substring(start, index - start);
            if (float.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
            {
                return Number(result);
            }
            return Number(0);
        }

        private static JsonValue ParseBool(string json, ref int index)
        {
            if (json.Substring(index, 4) == "true")
            {
                index += 4;
                return Bool(true);
            }
            if (json.Substring(index, 5) == "false")
            {
                index += 5;
                return Bool(false);
            }
            return Bool(false);
        }

        private static JsonValue ParseNull(string json, ref int index)
        {
            index += 4; // skip "null"
            return Null();
        }

        private static void SkipWhitespace(string json, ref int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
            {
                index++;
            }
        }

        #endregion

        #region Implicit Conversions

        public static implicit operator JsonValue(string value) => new JsonValue(value);
        public static implicit operator JsonValue(float value) => new JsonValue(value);
        public static implicit operator JsonValue(int value) => new JsonValue(value);
        public static implicit operator JsonValue(bool value) => new JsonValue(value);

        #endregion

        public override string ToString() => ToJson(false);
    }

}
