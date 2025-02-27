using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(GameplayTag))]
public class GameplayTagDrawer : PropertyDrawer
{
    private bool foldout = false;
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        base.OnGUI(position, property, label);

        GameplayTag reference = (GameplayTag)property.boxedValue;

        List<GameplayTag> tags = GameplayTagsList.Instance.TagsRef;
        if (tags.Count == 0)
        {
            return;
        }

        foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, "Tag List");
        if (foldout)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                GameplayTag tag = tags[i];

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(tag.Name);
                bool enabled = reference.Match(tag);
                if (EditorGUILayout.Toggle(enabled))
                {
                    if (!enabled)
                    {
                        property.FindPropertyRelative("name").stringValue = tag.Name;
                        property.FindPropertyRelative("parentName").stringValue = tag.Parent.Name;
                    }
                }
                else
                {
                    if (enabled)
                    {
                        property.FindPropertyRelative("name").stringValue = tag.Parent.Name;
                        property.FindPropertyRelative("parentName").stringValue = tag.Parent.Parent.Name;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }
}
