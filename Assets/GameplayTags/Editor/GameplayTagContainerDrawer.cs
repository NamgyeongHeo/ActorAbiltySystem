using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(GameplayTagContainer))]
public class GameplayTagContainerDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        base.OnGUI(position, property, label);
        GameplayTagContainer reference = property.boxedValue as GameplayTagContainer;

        GameplayTagsList tagsList = GameplayTagsList.Instance;
        GameplayTag[] invalidTags = reference.tags.Where((GameplayTag tag) => !tagsList.TagsRef.Contains(tag)).ToArray();
        foreach (GameplayTag tag in invalidTags)
        {
            reference.tags.Remove(tag);
        }

        for (int i = 0; i < tagsList.TagsRef.Count; i++)
        {
            GameplayTag tag = tagsList.TagsRef[i];

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(tag.Name);
            bool contains = reference.HasTag(tag);
            if (EditorGUILayout.Toggle(contains))
            {
                if (!contains)
                {
                    reference.AddTag(tag);
                }
            }
            else if (contains)
            {
                reference.RemoveTag(tag);
            }

            EditorGUILayout.EndHorizontal();
        }

        property.boxedValue = reference;
    }
}