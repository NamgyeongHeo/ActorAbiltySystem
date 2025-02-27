using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SubclassSelector))]
internal class SubclassSelectorEditor : PropertyDrawer
{
    string keyword = string.Empty;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty selectedTypeProp = property.FindPropertyRelative("selectedTypePath");
        SerializedProperty baseTypeProp = property.FindPropertyRelative("baseTypePath");

        EditorGUI.BeginProperty(position, label, property);

        Rect keywordFieldRect = new Rect(position.x, position.y, position.width, 18);
        Rect popupRect = new Rect(position.x, position.y + 20, position.width, 18);

        List<Type> selectableTypes = null;

        InheritsAttribute inherits = fieldInfo.GetCustomAttribute<InheritsAttribute>();
        Type baseType = inherits != null ? inherits.Type : typeof(object);
        baseTypeProp.stringValue = baseType.AssemblyQualifiedName;

        selectableTypes = new List<Type>();
        selectableTypes.AddRange(TypeCache.GetTypesDerivedFrom(baseType)
            .Where((Type type) => !(type.IsNotPublic || type.IsAbstract || type.IsInterface)));

        keyword = EditorGUI.TextField(keywordFieldRect, "Search", keyword, EditorStyles.textField);

        List<GUIContent> contents = new List<GUIContent>()
        {
            new GUIContent("(None)")
        };

        Dictionary<int, Type> popupIndexMap = new Dictionary<int, Type>()
        {
            { 0, null }
        };

        int selectedIdx = 0;
        for (int i = 0; i < selectableTypes.Count(); i++)
        {
            Type type = selectableTypes[i];
            string typePath = type.AssemblyQualifiedName;
            string typeName = type.Namespace != null ? $"{type.Namespace}.{type.Name}": type.Name;
            if (!typeName.Contains(keyword) && typePath != selectedTypeProp.stringValue)
            {
                continue;
            }

            if (typePath == selectedTypeProp.stringValue)
            {
                selectedIdx = popupIndexMap.Count;
            }

            contents.Add(new GUIContent(typeName));
            popupIndexMap.Add(popupIndexMap.Count, type);
        }

        selectedIdx = EditorGUI.Popup(popupRect, selectedIdx, contents.ToArray(), EditorStyles.popup);

        Type selectedType = popupIndexMap[selectedIdx];
        if (selectedType == null)
        {
            selectedTypeProp.stringValue = string.Empty;
            keyword = string.Empty;
        }
        else if (selectedTypeProp.stringValue != selectedType.AssemblyQualifiedName) 
        {
            selectedTypeProp.stringValue = selectedType.AssemblyQualifiedName;
            keyword = string.Empty;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return 40f;
    }
}