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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace JSONSO
{
    /// <summary>
    /// Base ScriptableObject that allows bidirectional conversion with JSON.
    /// Inherit from this class to create JSON-serializable ScriptableObjects.
    /// </summary>
    public abstract class JsonScriptableObject : ScriptableObject
    {
        #region Events
        
        /// <summary>
        /// Invoked before serializing to JSON.
        /// Useful for preparing data before serialization.
        /// </summary>
        protected virtual void OnBeforeSerialize() { }
        
        /// <summary>
        /// Invoked after deserializing from JSON.
        /// Useful for validating or processing data after loading.
        /// </summary>
        protected virtual void OnAfterDeserialize() { }
        
        #endregion

        #region To JSON
        
        /// <summary>
        /// Converts this ScriptableObject to a JSON string.
        /// </summary>
        /// <param name="prettyPrint">If true, the JSON will have readable format with indentation.</param>
        /// <returns>JSON string representing the ScriptableObject data.</returns>
        public virtual string ToJson(bool prettyPrint = false)
        {
            OnBeforeSerialize();
            return JsonUtility.ToJson(this, prettyPrint);
        }

        /// <summary>
        /// Converts this ScriptableObject to an object of the specified type.
        /// </summary>
        /// <typeparam name="T">Target type to convert to.</typeparam>
        /// <returns>New instance of T with the data from this ScriptableObject.</returns>
        public T ToObject<T>()
        {
            string json = ToJson();
            return JsonUtility.FromJson<T>(json);
        }

        /// <summary>
        /// Saves this ScriptableObject as a JSON file at the specified path.
        /// </summary>
        /// <param name="filePath">Full path of the file where to save the JSON.</param>
        /// <param name="prettyPrint">If true, the JSON will have readable format.</param>
        public void SaveToFile(string filePath, bool prettyPrint = true)
        {
            string json = ToJson(prettyPrint);
            string directory = Path.GetDirectoryName(filePath);
            
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(filePath, json);
            
#if UNITY_EDITOR
            Debug.Log($"[JsonScriptableObject] Saved to: {filePath}");
#endif
        }

        #endregion

        #region From JSON
        
        /// <summary>
        /// Overwrites this ScriptableObject's data with the JSON data.
        /// </summary>
        /// <param name="json">JSON string with the data to load.</param>
        public virtual void FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[JsonScriptableObject] JSON string is null or empty.");
                return;
            }
            
            JsonUtility.FromJsonOverwrite(json, this);
            OnAfterDeserialize();
        }

        /// <summary>
        /// Loads data from a JSON file and applies it to this ScriptableObject.
        /// </summary>
        /// <param name="filePath">Path of the JSON file to load.</param>
        /// <returns>True if the load was successful, false otherwise.</returns>
        public bool LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[JsonScriptableObject] File not found: {filePath}");
                return false;
            }
            
            try
            {
                string json = File.ReadAllText(filePath);
                FromJson(json);
                
#if UNITY_EDITOR
                Debug.Log($"[JsonScriptableObject] Loaded from: {filePath}");
#endif
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonScriptableObject] Error loading file: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads data from an object by converting it to JSON first.
        /// Maps fields and properties from the source object to this ScriptableObject.
        /// </summary>
        /// <param name="obj">Source object to load data from.</param>
        /// <returns>True if the load was successful, false otherwise.</returns>
        public bool LoadFromObject(object obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("[JsonScriptableObject] Source object is null.");
                return false;
            }

            try
            {
                string json = JsonUtility.ToJson(obj);
                FromJson(json);
                
#if UNITY_EDITOR
                Debug.Log($"[JsonScriptableObject] Loaded from object of type: {obj.GetType().Name}");
#endif
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonScriptableObject] Error loading from object: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Recursively copies data from source object to target object using reflection.
        /// </summary>
        private static void CopyObjectData(object source, object target)
        {
            if (source == null || target == null) return;

            Type sourceType = source.GetType();
            Type targetType = target.GetType();

            // Get all fields from target (including private ones for Unity serialization)
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            
            // Process fields
            foreach (FieldInfo targetField in targetType.GetFields(bindingFlags))
            {
                // Skip non-serializable fields
                if (!IsFieldSerializable(targetField)) continue;

                // Try to find matching field in source
                FieldInfo sourceField = sourceType.GetField(targetField.Name, bindingFlags);
                PropertyInfo sourceProperty = sourceType.GetProperty(targetField.Name, BindingFlags.Public | BindingFlags.Instance);

                object sourceValue = null;
                Type sourceValueType = null;

                if (sourceField != null)
                {
                    sourceValue = sourceField.GetValue(source);
                    sourceValueType = sourceField.FieldType;
                }
                else if (sourceProperty != null && sourceProperty.CanRead)
                {
                    sourceValue = sourceProperty.GetValue(source);
                    sourceValueType = sourceProperty.PropertyType;
                }
                else
                {
                    continue; // No matching member found
                }

                // Copy the value
                SetFieldValue(targetField, target, sourceValue, sourceValueType);
            }

            // Process properties (for ScriptableObjects that use properties)
            foreach (PropertyInfo targetProperty in targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!targetProperty.CanWrite) continue;

                PropertyInfo sourceProperty = sourceType.GetProperty(targetProperty.Name, BindingFlags.Public | BindingFlags.Instance);
                FieldInfo sourceField = sourceType.GetField(targetProperty.Name, bindingFlags);

                object sourceValue = null;
                Type sourceValueType = null;

                if (sourceProperty != null && sourceProperty.CanRead)
                {
                    sourceValue = sourceProperty.GetValue(source);
                    sourceValueType = sourceProperty.PropertyType;
                }
                else if (sourceField != null)
                {
                    sourceValue = sourceField.GetValue(source);
                    sourceValueType = sourceField.FieldType;
                }
                else
                {
                    continue;
                }

                try
                {
                    object convertedValue = ConvertValue(sourceValue, sourceValueType, targetProperty.PropertyType);
                    if (convertedValue != null || !targetProperty.PropertyType.IsValueType)
                    {
                        targetProperty.SetValue(target, convertedValue);
                    }
                }
                catch (Exception)
                {
                    // Skip properties that can't be set
                }
            }
        }

        /// <summary>
        /// Checks if a field should be serialized (Unity serialization rules).
        /// </summary>
        private static bool IsFieldSerializable(FieldInfo field)
        {
            // Public fields are serializable by default
            if (field.IsPublic) return true;

            // Private/protected fields need [SerializeField] attribute
            if (field.GetCustomAttribute<SerializeField>() != null) return true;

            return false;
        }

        /// <summary>
        /// Sets the value of a field, handling type conversion and nested objects.
        /// </summary>
        private static void SetFieldValue(FieldInfo targetField, object target, object sourceValue, Type sourceValueType)
        {
            if (sourceValue == null)
            {
                if (!targetField.FieldType.IsValueType)
                {
                    targetField.SetValue(target, null);
                }
                return;
            }

            object convertedValue = ConvertValue(sourceValue, sourceValueType, targetField.FieldType);
            if (convertedValue != null || !targetField.FieldType.IsValueType)
            {
                targetField.SetValue(target, convertedValue);
            }
        }

        /// <summary>
        /// Converts a value from source type to target type, handling nested objects and collections.
        /// </summary>
        private static object ConvertValue(object sourceValue, Type sourceType, Type targetType)
        {
            if (sourceValue == null) return null;

            // Direct assignment if types are compatible
            if (targetType.IsAssignableFrom(sourceType))
            {
                return sourceValue;
            }

            // Handle primitive types and strings
            if (IsPrimitiveOrString(targetType))
            {
                return ConvertPrimitive(sourceValue, targetType);
            }

            // Handle enums
            if (targetType.IsEnum)
            {
                return ConvertToEnum(sourceValue, targetType);
            }

            // Handle arrays
            if (targetType.IsArray)
            {
                return ConvertToArray(sourceValue, targetType);
            }

            // Handle generic lists
            if (IsGenericList(targetType))
            {
                return ConvertToList(sourceValue, targetType);
            }

            // Handle dictionaries (note: Unity doesn't serialize dictionaries by default)
            if (IsGenericDictionary(targetType))
            {
                return ConvertToDictionary(sourceValue, targetType);
            }

            // Handle nested objects (classes/structs)
            if (targetType.IsClass || (targetType.IsValueType && !targetType.IsPrimitive))
            {
                return ConvertToNestedObject(sourceValue, targetType);
            }

            return null;
        }

        private static bool IsPrimitiveOrString(Type type)
        {
            return type.IsPrimitive || type == typeof(string) || type == typeof(decimal);
        }

        private static object ConvertPrimitive(object value, Type targetType)
        {
            try
            {
                if (targetType == typeof(string))
                {
                    return value.ToString();
                }
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }
        }

        private static object ConvertToEnum(object value, Type enumType)
        {
            try
            {
                if (value is string strValue)
                {
                    return Enum.Parse(enumType, strValue, true);
                }
                if (value is int intValue)
                {
                    return Enum.ToObject(enumType, intValue);
                }
                return Enum.ToObject(enumType, Convert.ToInt32(value));
            }
            catch
            {
                return Enum.GetValues(enumType).GetValue(0);
            }
        }

        private static object ConvertToArray(object sourceValue, Type targetArrayType)
        {
            Type elementType = targetArrayType.GetElementType();
            if (elementType == null) return null;

            if (sourceValue is IEnumerable sourceEnumerable)
            {
                var sourceList = new List<object>();
                foreach (var item in sourceEnumerable)
                {
                    sourceList.Add(item);
                }

                Array targetArray = Array.CreateInstance(elementType, sourceList.Count);
                for (int i = 0; i < sourceList.Count; i++)
                {
                    object convertedItem = ConvertValue(sourceList[i], sourceList[i]?.GetType() ?? elementType, elementType);
                    targetArray.SetValue(convertedItem, i);
                }
                return targetArray;
            }

            return Array.CreateInstance(elementType, 0);
        }

        private static bool IsGenericList(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }

        private static object ConvertToList(object sourceValue, Type targetListType)
        {
            Type elementType = targetListType.GetGenericArguments()[0];
            IList targetList = (IList)Activator.CreateInstance(targetListType);

            if (sourceValue is IEnumerable sourceEnumerable)
            {
                foreach (var item in sourceEnumerable)
                {
                    object convertedItem = ConvertValue(item, item?.GetType() ?? elementType, elementType);
                    targetList.Add(convertedItem);
                }
            }

            return targetList;
        }

        private static bool IsGenericDictionary(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }

        private static object ConvertToDictionary(object sourceValue, Type targetDictType)
        {
            Type[] genericArgs = targetDictType.GetGenericArguments();
            Type keyType = genericArgs[0];
            Type valueType = genericArgs[1];

            IDictionary targetDict = (IDictionary)Activator.CreateInstance(targetDictType);

            if (sourceValue is IDictionary sourceDict)
            {
                foreach (DictionaryEntry entry in sourceDict)
                {
                    object convertedKey = ConvertValue(entry.Key, entry.Key?.GetType() ?? keyType, keyType);
                    object convertedValue = ConvertValue(entry.Value, entry.Value?.GetType() ?? valueType, valueType);
                    if (convertedKey != null)
                    {
                        targetDict.Add(convertedKey, convertedValue);
                    }
                }
            }

            return targetDict;
        }

        private static object ConvertToNestedObject(object sourceValue, Type targetType)
        {
            // Handle Unity types specially
            if (typeof(UnityEngine.Object).IsAssignableFrom(targetType))
            {
                // Can't create Unity objects via reflection, just return if compatible
                if (targetType.IsAssignableFrom(sourceValue.GetType()))
                {
                    return sourceValue;
                }
                return null;
            }

            // Create new instance of target type
            object targetInstance;
            try
            {
                if (targetType.IsValueType)
                {
                    targetInstance = Activator.CreateInstance(targetType);
                }
                else
                {
                    // Try to find a parameterless constructor
                    ConstructorInfo constructor = targetType.GetConstructor(Type.EmptyTypes);
                    if (constructor == null)
                    {
                        return null;
                    }
                    targetInstance = constructor.Invoke(null);
                }
            }
            catch
            {
                return null;
            }

            // Recursively copy data
            CopyObjectData(sourceValue, targetInstance);
            return targetInstance;
        }

        #endregion

        #region Static Factory Methods
        
        /// <summary>
        /// Creates a new instance of a JsonScriptableObject from a JSON string.
        /// </summary>
        /// <typeparam name="T">Type of ScriptableObject to create.</typeparam>
        /// <param name="json">JSON string with the data.</param>
        /// <returns>New instance with the JSON data.</returns>
        public static T CreateFromJson<T>(string json) where T : JsonScriptableObject
        {
            T instance = CreateInstance<T>();
            instance.FromJson(json);
            return instance;
        }

        /// <summary>
        /// Creates a new instance of a JsonScriptableObject from a JSON file.
        /// </summary>
        /// <typeparam name="T">Type of ScriptableObject to create.</typeparam>
        /// <param name="filePath">Path of the JSON file.</param>
        /// <returns>New instance with the file data, or null if it fails.</returns>
        public static T CreateFromFile<T>(string filePath) where T : JsonScriptableObject
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[JsonScriptableObject] File not found: {filePath}");
                return null;
            }
            
            try
            {
                string json = File.ReadAllText(filePath);
                return CreateFromJson<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonScriptableObject] Error creating from file: {e.Message}");
                return null;
            }
        }

        #endregion

        #region Utility Methods
        
        /// <summary>
        /// Creates a deep copy of this ScriptableObject using JSON as intermediary.
        /// </summary>
        /// <typeparam name="T">Type of ScriptableObject.</typeparam>
        /// <returns>New cloned instance.</returns>
        public T Clone<T>() where T : JsonScriptableObject
        {
            return CreateFromJson<T>(ToJson());
        }

        /// <summary>
        /// Copies the data from another JsonScriptableObject of the same type.
        /// </summary>
        /// <param name="source">Source ScriptableObject of the data.</param>
        public void CopyFrom(JsonScriptableObject source)
        {
            if (source == null)
            {
                Debug.LogWarning("[JsonScriptableObject] Source is null.");
                return;
            }
            
            if (source.GetType() != this.GetType())
            {
                Debug.LogWarning("[JsonScriptableObject] Type mismatch. Cannot copy from different type.");
                return;
            }
            
            FromJson(source.ToJson());
        }

        /// <summary>
        /// Compares if two JsonScriptableObjects have the same data.
        /// </summary>
        /// <param name="other">Other ScriptableObject to compare.</param>
        /// <returns>True if the JSON data is identical.</returns>
        public bool DataEquals(JsonScriptableObject other)
        {
            if (other == null) return false;
            return ToJson() == other.ToJson();
        }

        #endregion
    }
}
