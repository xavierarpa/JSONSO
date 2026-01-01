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
using UnityEditor;

namespace JSONSO.Editor
{
    /// <summary>
    /// Custom editor for JsonScriptableObjectData.
    /// Provides a tree view for editing JSON data with reordering support.
    /// </summary>
    [CustomEditor(typeof(JsonScriptableObjectData))]
    public class JsonScriptableObjectDataEditor : UnityEditor.Editor
    {
        private JsonScriptableObjectData _target;
        private bool _showJsonPreview = false;
        private Vector2 _jsonPreviewScroll;
        private string _jsonPreview = "";
        
        // Unique prefix for SessionState based on the asset
        private string _sessionStatePrefix;

        // Layout constants
        private const float KEY_WIDTH = 120f;
        private const float TYPE_WIDTH = 75f;
        private const float COUNT_WIDTH = 35f;
        private const float BUTTON_WIDTH = 20f;
        private const float INDEX_WIDTH = 35f;

        private void OnEnable()
        {
            if (target != null)
            {
                _target = target as JsonScriptableObjectData;
                // Create unique prefix based on asset's InstanceID
                _sessionStatePrefix = $"JsonEditor_{target.GetInstanceID()}_";
            }
        }

        /// <summary>
        /// Gets the foldout state from SessionState.
        /// </summary>
        private bool GetFoldoutState(string key, bool defaultValue = false)
        {
            return SessionState.GetBool(_sessionStatePrefix + key, defaultValue);
        }

        /// <summary>
        /// Saves the foldout state to SessionState.
        /// </summary>
        private void SetFoldoutState(string key, bool value)
        {
            SessionState.SetBool(_sessionStatePrefix + key, value);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("JSON Data Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Action buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Property", GUILayout.Height(25)))
            {
                AddProperty(_target.Root, "newKey");
            }
            if (GUILayout.Button("Clear All", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Clear All", "Are you sure you want to delete all data?", "Yes", "No"))
                {
                    _target.Clear();
                    EditorUtility.SetDirty(_target);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Draw root properties
            if (_target.Root != null && _target.Root.IsObject)
            {
                DrawObjectProperties(_target.Root, 0);
            }

            EditorGUILayout.Space(15);
            DrawJsonPreviewSection();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws all properties of a JSON object with reordering support.
        /// </summary>
        private void DrawObjectProperties(JsonValue obj, int depth)
        {
            if (!obj.IsObject) return;

            var keys = new System.Collections.Generic.List<string>(obj.Keys);
            int count = keys.Count;

            for (int i = 0; i < count; i++)
            {
                string key = keys[i];
                var value = obj[key];
                if (value == null) continue;

                // Draw property with index for reordering
                DrawProperty(obj, key, value, depth, i, count);
            }
        }

        /// <summary>
        /// Draws a single property with controls and reordering buttons.
        /// </summary>
        private void DrawProperty(JsonValue parent, string key, JsonValue value, int depth, int index, int totalCount)
        {
            EditorGUI.indentLevel = depth;

            // Background color based on depth
            Color bgColor = depth % 2 == 0 
                ? new Color(0.8f, 0.8f, 0.8f, 0.1f) 
                : new Color(0.6f, 0.6f, 0.6f, 0.1f);
            
            Rect boxRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(boxRect, bgColor);

            EditorGUILayout.BeginHorizontal();

            // Spacing for hierarchy
            GUILayout.Space(depth * 15);

            // Reorder buttons (▲ ▼)
            EditorGUI.BeginDisabledGroup(index == 0);
            if (GUILayout.Button("▲", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(18)))
            {
                MoveObjectProperty(parent, key, -1);
                EditorUtility.SetDirty(_target);
                GUIUtility.ExitGUI();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(index >= totalCount - 1);
            if (GUILayout.Button("▼", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(18)))
            {
                MoveObjectProperty(parent, key, 1);
                EditorUtility.SetDirty(_target);
                GUIUtility.ExitGUI();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(8);

            // Foldout for Object/Array
            if (value.IsObject || value.IsArray)
            {
                string foldoutKey = $"{depth}_{key}";
                bool currentState = GetFoldoutState(foldoutKey, false);
                bool newState = EditorGUILayout.Foldout(currentState, GUIContent.none, true);
                if (newState != currentState)
                    SetFoldoutState(foldoutKey, newState);
            }

            // Key field
            string newKey = EditorGUILayout.TextField(key, GUILayout.Width(KEY_WIDTH));
            if (newKey != key && !string.IsNullOrEmpty(newKey))
            {
                RenameKey(parent, key, newKey);
                EditorUtility.SetDirty(_target);
                GUIUtility.ExitGUI();
            }

            // Type selector
            var newType = (JsonValueType)EditorGUILayout.EnumPopup(value.Type, GUILayout.Width(TYPE_WIDTH));
            if (newType != value.Type)
            {
                parent[key] = CreateDefaultValue(newType);
                EditorUtility.SetDirty(_target);
            }
            else
            {
                // Draw value based on type
                DrawValueField(parent, key, value);
            }

            GUILayout.FlexibleSpace();

            // Add and delete buttons on the same line
            if (value.IsObject || value.IsArray)
            {
                if (GUILayout.Button("+", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(18)))
                {
                    if (value.IsObject)
                    {
                        AddProperty(value, "newKey");
                    }
                    else
                    {
                        value.Add(JsonValue.String(""));
                    }
                    EditorUtility.SetDirty(_target);
                }
            }

            // Duplicate button
            if (GUILayout.Button("D", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(18)))
            {
                DuplicateProperty(parent, key, value);
                EditorUtility.SetDirty(_target);
                GUIUtility.ExitGUI();
            }

            // Delete button
            if (GUILayout.Button("×", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(18)))
            {
                parent.Remove(key);
                EditorUtility.SetDirty(_target);
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();

            // Draw nested content only if foldout is open
            if (value.IsObject || value.IsArray)
            {
                string foldoutKey = $"{depth}_{key}";
                bool isExpanded = GetFoldoutState(foldoutKey, false);
                
                if (isExpanded)
                {
                    GUILayout.Space(4);
                    
                    if (value.IsObject)
                    {
                        DrawObjectProperties(value, depth + 1);
                    }
                    else if (value.IsArray)
                    {
                        DrawArrayElements(value, depth + 1);
                    }
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        /// <summary>
        /// Draws array elements with reordering support.
        /// </summary>
        private void DrawArrayElements(JsonValue array, int depth)
        {
            if (!array.IsArray) return;

            int count = array.Count;
            for (int i = 0; i < count; i++)
            {
                var element = array[i];
                if (element == null) continue;

                DrawArrayElement(array, i, element, depth, count);
            }
        }

        /// <summary>
        /// Draws a single array element with reordering buttons.
        /// </summary>
        private void DrawArrayElement(JsonValue array, int index, JsonValue element, int depth, int totalCount)
        {
            EditorGUI.indentLevel = depth;

            Color bgColor = depth % 2 == 0 
                ? new Color(0.7f, 0.85f, 0.7f, 0.1f) 
                : new Color(0.6f, 0.75f, 0.6f, 0.1f);

            Rect boxRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(boxRect, bgColor);

            EditorGUILayout.BeginHorizontal();

            // Spacing for hierarchy
            GUILayout.Space(depth * 15);

            // Reorder buttons (▲ ▼)
            EditorGUI.BeginDisabledGroup(index == 0);
            if (GUILayout.Button("▲", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(18)))
            {
                MoveArrayElement(array, index, -1);
                EditorUtility.SetDirty(_target);
                GUIUtility.ExitGUI();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(index >= totalCount - 1);
            if (GUILayout.Button("▼", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(18)))
            {
                MoveArrayElement(array, index, 1);
                EditorUtility.SetDirty(_target);
                GUIUtility.ExitGUI();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(8);

            // Foldout for Object/Array
            if (element.IsObject || element.IsArray)
            {
                string foldoutKey = $"arr_{depth}_{index}";
                bool currentState = GetFoldoutState(foldoutKey, false);
                bool newState = EditorGUILayout.Foldout(currentState, GUIContent.none, true);
                if (newState != currentState)
                    SetFoldoutState(foldoutKey, newState);
            }

            // Index label
            EditorGUILayout.LabelField($"[{index}]", GUILayout.Width(INDEX_WIDTH));

            // Type selector
            var newType = (JsonValueType)EditorGUILayout.EnumPopup(element.Type, GUILayout.Width(TYPE_WIDTH));
            if (newType != element.Type)
            {
                array[index] = CreateDefaultValue(newType);
                EditorUtility.SetDirty(_target);
            }
            else
            {
                // Draw value based on type
                DrawArrayValueField(array, index, element);
            }

            GUILayout.FlexibleSpace();

            // Add and delete buttons on the same line
            if (element.IsObject || element.IsArray)
            {
                if (GUILayout.Button("+", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(18)))
                {
                    if (element.IsObject)
                    {
                        AddProperty(element, "newKey");
                    }
                    else
                    {
                        element.Add(JsonValue.String(""));
                    }
                    EditorUtility.SetDirty(_target);
                }
            }

            // Duplicate button
            if (GUILayout.Button("D", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(18)))
            {
                DuplicateArrayElement(array, index, element);
                EditorUtility.SetDirty(_target);
                GUIUtility.ExitGUI();
            }

            // Delete button
            if (GUILayout.Button("×", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(18)))
            {
                array.RemoveAt(index);
                EditorUtility.SetDirty(_target);
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();

            // Draw nested content only if foldout is open
            if (element.IsObject || element.IsArray)
            {
                string foldoutKey = $"arr_{depth}_{index}";
                bool isExpanded = GetFoldoutState(foldoutKey, false);
                
                if (isExpanded)
                {
                    GUILayout.Space(4);
                    
                    if (element.IsObject)
                    {
                        DrawObjectProperties(element, depth + 1);
                    }
                    else if (element.IsArray)
                    {
                        DrawArrayElements(element, depth + 1);
                    }
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        /// <summary>
        /// Draws the input field for a property value.
        /// </summary>
        private void DrawValueField(JsonValue parent, string key, JsonValue value)
        {
            switch (value.Type)
            {
                case JsonValueType.String:
                    string strVal = EditorGUILayout.TextField(value.AsString ?? "");
                    if (strVal != value.AsString)
                    {
                        parent[key] = JsonValue.String(strVal);
                        EditorUtility.SetDirty(_target);
                    }
                    break;

                case JsonValueType.Number:
                    float numVal = EditorGUILayout.FloatField(value.AsFloat);
                    if (!Mathf.Approximately(numVal, value.AsFloat))
                    {
                        parent[key] = JsonValue.Number(numVal);
                        EditorUtility.SetDirty(_target);
                    }
                    break;

                case JsonValueType.Boolean:
                    bool boolVal = EditorGUILayout.Toggle(value.AsBool);
                    if (boolVal != value.AsBool)
                    {
                        parent[key] = JsonValue.Bool(boolVal);
                        EditorUtility.SetDirty(_target);
                    }
                    break;

                case JsonValueType.Object:
                    EditorGUILayout.LabelField($"{{ {value.Count} props }}", GUILayout.Width(COUNT_WIDTH + 50));
                    break;

                case JsonValueType.Array:
                    EditorGUILayout.LabelField($"[ {value.Count} items ]", GUILayout.Width(COUNT_WIDTH + 50));
                    break;

                case JsonValueType.Null:
                    EditorGUILayout.LabelField("null");
                    break;
            }
        }

        /// <summary>
        /// Draws the input field for an array element value.
        /// </summary>
        private void DrawArrayValueField(JsonValue array, int index, JsonValue element)
        {
            switch (element.Type)
            {
                case JsonValueType.String:
                    string strVal = EditorGUILayout.TextField(element.AsString ?? "");
                    if (strVal != element.AsString)
                    {
                        array[index] = JsonValue.String(strVal);
                        EditorUtility.SetDirty(_target);
                    }
                    break;

                case JsonValueType.Number:
                    float numVal = EditorGUILayout.FloatField(element.AsFloat);
                    if (!Mathf.Approximately(numVal, element.AsFloat))
                    {
                        array[index] = JsonValue.Number(numVal);
                        EditorUtility.SetDirty(_target);
                    }
                    break;

                case JsonValueType.Boolean:
                    bool boolVal = EditorGUILayout.Toggle(element.AsBool);
                    if (boolVal != element.AsBool)
                    {
                        array[index] = JsonValue.Bool(boolVal);
                        EditorUtility.SetDirty(_target);
                    }
                    break;

                case JsonValueType.Object:
                    EditorGUILayout.LabelField($"{{ {element.Count} props }}", GUILayout.Width(COUNT_WIDTH + 50));
                    break;

                case JsonValueType.Array:
                    EditorGUILayout.LabelField($"[ {element.Count} items ]", GUILayout.Width(COUNT_WIDTH + 50));
                    break;

                case JsonValueType.Null:
                    EditorGUILayout.LabelField("null");
                    break;
            }
        }

        /// <summary>
        /// Moves a property within an object (reorder).
        /// </summary>
        private void MoveObjectProperty(JsonValue obj, string key, int direction)
        {
            var keys = new System.Collections.Generic.List<string>(obj.Keys);
            int currentIndex = keys.IndexOf(key);
            int newIndex = currentIndex + direction;

            if (newIndex < 0 || newIndex >= keys.Count) return;

            // We need to rebuild the object with the new order
            var values = new System.Collections.Generic.List<(string k, JsonValue v)>();
            foreach (var k in keys)
            {
                values.Add((k, obj[k]));
            }

            // Swap
            var temp = values[currentIndex];
            values[currentIndex] = values[newIndex];
            values[newIndex] = temp;

            // Clear and rebuild
            foreach (var k in keys)
            {
                obj.Remove(k);
            }
            foreach (var (k, v) in values)
            {
                obj[k] = v;
            }
        }

        /// <summary>
        /// Moves an element within an array (reorder).
        /// </summary>
        private void MoveArrayElement(JsonValue array, int index, int direction)
        {
            int newIndex = index + direction;
            if (newIndex < 0 || newIndex >= array.Count) return;

            var temp = array[index];
            array[index] = array[newIndex];
            array[newIndex] = temp;
        }

        /// <summary>
        /// Creates a new value with the default for the specified type.
        /// </summary>
        private JsonValue CreateDefaultValue(JsonValueType type)
        {
            return type switch
            {
                JsonValueType.String => JsonValue.String(""),
                JsonValueType.Number => JsonValue.Number(0),
                JsonValueType.Boolean => JsonValue.Bool(false),
                JsonValueType.Object => JsonValue.Object(),
                JsonValueType.Array => JsonValue.Array(),
                _ => JsonValue.Null()
            };
        }

        /// <summary>
        /// Adds a new property with a unique name.
        /// </summary>
        private void AddProperty(JsonValue obj, string baseKey)
        {
            string key = baseKey;
            int counter = 1;
            while (obj.HasKey(key))
            {
                key = $"{baseKey}{counter}";
                counter++;
            }
            obj[key] = JsonValue.String("");
            EditorUtility.SetDirty(_target);
        }

        /// <summary>
        /// Renames a key in the object maintaining the same position.
        /// </summary>
        private void RenameKey(JsonValue obj, string oldKey, string newKey)
        {
            if (obj.HasKey(newKey)) return;
            obj.RenameKeyInPlace(oldKey, newKey);
        }

        /// <summary>
        /// Duplicates a property in the object.
        /// </summary>
        private void DuplicateProperty(JsonValue obj, string key, JsonValue value)
        {
            string newKey = key + "_copy";
            int counter = 1;
            while (obj.HasKey(newKey))
            {
                newKey = $"{key}_copy{counter}";
                counter++;
            }
            obj[newKey] = CloneJsonValue(value);
        }

        /// <summary>
        /// Duplicates an element in the array.
        /// </summary>
        private void DuplicateArrayElement(JsonValue array, int index, JsonValue element)
        {
            var clone = CloneJsonValue(element);
            array.InsertAt(index + 1, clone);
        }

        /// <summary>
        /// Creates a deep clone of a JsonValue.
        /// </summary>
        private JsonValue CloneJsonValue(JsonValue source)
        {
            if (source == null) return JsonValue.Null();
            
            switch (source.Type)
            {
                case JsonValueType.Null:
                    return JsonValue.Null();
                case JsonValueType.String:
                    return JsonValue.String(source.AsString);
                case JsonValueType.Number:
                    return JsonValue.Number(source.AsFloat);
                case JsonValueType.Boolean:
                    return JsonValue.Bool(source.AsBool);
                case JsonValueType.Object:
                    var obj = JsonValue.Object();
                    foreach (var k in source.Keys)
                    {
                        obj[k] = CloneJsonValue(source[k]);
                    }
                    return obj;
                case JsonValueType.Array:
                    var arr = JsonValue.Array();
                    for (int i = 0; i < source.Count; i++)
                    {
                        arr.Add(CloneJsonValue(source[i]));
                    }
                    return arr;
                default:
                    return JsonValue.Null();
            }
        }

        /// <summary>
        /// Draws the JSON preview section.
        /// </summary>
        private void DrawJsonPreviewSection()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Spacing to separate from foldout
            GUILayout.Space(12);
            
            _showJsonPreview = EditorGUILayout.Foldout(_showJsonPreview, "JSON Preview", true);
            
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Copy", GUILayout.Width(60)))
            {
                GUIUtility.systemCopyBuffer = _target.ToJson(true);
                Debug.Log("JSON copied to clipboard!");
            }
            if (GUILayout.Button("Export", GUILayout.Width(60)))
            {
                ExportJson();
            }
            if (GUILayout.Button("Import", GUILayout.Width(60)))
            {
                ImportJson();
            }
            EditorGUILayout.EndHorizontal();

            if (_showJsonPreview)
            {
                _jsonPreview = _target.ToJson(true);
                
                EditorGUILayout.Space(5);
                _jsonPreviewScroll = EditorGUILayout.BeginScrollView(_jsonPreviewScroll, GUILayout.Height(200));
                
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextArea(_jsonPreview, GUILayout.ExpandHeight(true));
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.EndScrollView();
            }
        }

        /// <summary>
        /// Exports the JSON to a file.
        /// </summary>
        private void ExportJson()
        {
            string path = EditorUtility.SaveFilePanel("Export JSON", "", _target.name, "json");
            if (!string.IsNullOrEmpty(path))
            {
                _target.SaveToFile(path, true);
            }
        }

        /// <summary>
        /// Imports JSON from a file.
        /// </summary>
        private void ImportJson()
        {
            string path = EditorUtility.OpenFilePanel("Import JSON", "", "json");
            if (!string.IsNullOrEmpty(path))
            {
                _target.LoadFromFile(path);
                EditorUtility.SetDirty(_target);
            }
        }
    }
}
