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
    /// Allows editing individual JsonValue fields in the inspector.
    /// </summary>
    [CustomPropertyDrawer(typeof(JsonValue))]
    public class JsonValueDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            // Draw the label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            
            // Get the type field
            var typeProp = property.FindPropertyRelative("_type");
            var type = (JsonValueType)typeProp.enumValueIndex;

            // Draw the type dropdown
            float typeWidth = 80f;
            Rect typeRect = new Rect(position.x, position.y, typeWidth, position.height);
            EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);

            // Draw the value based on type
            float valueX = position.x + typeWidth + 5;
            float valueWidth = position.width - typeWidth - 5;
            Rect valueRect = new Rect(valueX, position.y, valueWidth, position.height);

            switch (type)
            {
                case JsonValueType.String:
                    var stringProp = property.FindPropertyRelative("_stringValue");
                    EditorGUI.PropertyField(valueRect, stringProp, GUIContent.none);
                    break;

                case JsonValueType.Number:
                    var numberProp = property.FindPropertyRelative("_numberValue");
                    EditorGUI.PropertyField(valueRect, numberProp, GUIContent.none);
                    break;

                case JsonValueType.Boolean:
                    var boolProp = property.FindPropertyRelative("_boolValue");
                    EditorGUI.PropertyField(valueRect, boolProp, GUIContent.none);
                    break;

                case JsonValueType.Object:
                    EditorGUI.LabelField(valueRect, "(Use the custom editor to edit)");
                    break;

                case JsonValueType.Array:
                    EditorGUI.LabelField(valueRect, "(Use the custom editor to edit)");
                    break;

                case JsonValueType.Null:
                    EditorGUI.LabelField(valueRect, "null");
                    break;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
