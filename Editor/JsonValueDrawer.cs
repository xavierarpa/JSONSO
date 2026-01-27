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
    /// PropertyDrawer for JsonValue.
    /// Note: JsonValue is NOT Unity-serializable by design to avoid depth limit issues.
    /// This drawer shows a warning message if someone attempts to use JsonValue as a serialized field.
    /// Use JsonScriptableObjectData instead, which stores JSON as string and provides lazy-loaded access.
    /// </summary>
    [CustomPropertyDrawer(typeof(JsonValue))]
    public class JsonValueDrawer : PropertyDrawer
    {
        private static readonly Color WarningColor = new Color(1f, 0.8f, 0.2f, 0.3f);
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            // Draw warning background
            EditorGUI.DrawRect(position, WarningColor);
            
            // Draw the label
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            EditorGUI.LabelField(labelRect, label);
            
            // Draw warning message
            Rect messageRect = new Rect(position.x + EditorGUIUtility.labelWidth + 2, position.y, 
                position.width - EditorGUIUtility.labelWidth - 2, position.height);
            
            EditorGUI.HelpBox(messageRect, "JsonValue is not serializable. Use JsonScriptableObjectData instead.", MessageType.Warning);
            
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2 + 4;
        }
    }
}
