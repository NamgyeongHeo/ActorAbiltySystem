using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using System.Text;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System;
using System.Collections.ObjectModel;







#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameplayTagsList : ScriptableObject
{
    internal const string gameplayTagsListPath = "Assets/Resources/GameplayTags/List.asset";
    internal const string resourcesTagsListPath = "GameplayTags/List";

    [SerializeField]
    private List<GameplayTag> tags = new List<GameplayTag>();

    public ReadOnlyCollection<GameplayTag> Tags
    {
        get
        {
            return tags.AsReadOnly();
        }
    }

    internal List<GameplayTag> TagsRef
    {
        get
        {
            return tags;
        }
    }

    private static GameplayTagsList instance;
    public static GameplayTagsList Instance
    {
        get
        {
            if (instance == null)
            {
#if UNITY_EDITOR
                if (EditorApplication.isPlaying)
                {
                    instance = Resources.Load<GameplayTagsList>(resourcesTagsListPath);
                }
                else
                {
                    instance = GetOrCreateObject();
                }
#else
                instance = Resources.Load<GameplayTagsList>(resourcesTagsListPath);
#endif
            }

            return instance;
        }
    }

    public GameplayTag FindGameplayTag(string name)
    {
        return tags.Find((GameplayTag tag) => tag.Name == name);
    }

#if UNITY_EDITOR
    private static GameplayTagsList GetOrCreateObject()
    {
        GameplayTagsList settings = AssetDatabase.LoadAssetAtPath<GameplayTagsList>(gameplayTagsListPath);
        if (settings == null)
        {
            settings = CreateInstance<GameplayTagsList>();
            string[] splittedPath = gameplayTagsListPath.Split('/');
            for (int i = 0; i < splittedPath.Length - 2; i++)
            {
                string subfolder = string.Join('/', splittedPath[0..(i + 1)]);
                if (!AssetDatabase.IsValidFolder($"{subfolder}/{splittedPath[i + 1]}"))
                {
                    AssetDatabase.CreateFolder(subfolder, splittedPath[i + 1]);
                }
            }

            AssetDatabase.CreateAsset(settings, gameplayTagsListPath);
            AssetDatabase.SaveAssets();
        }

        return settings;
    }

    internal void RemoveTagFromList(GameplayTag tag)
    {
        if (!tag.IsValid())
        {
            return;
        }

        List<GameplayTag> children = tags.FindAll((GameplayTag item) => tag.Equals(item.Parent));
        foreach (GameplayTag child in children) 
        {
            RemoveTagFromList(child);
        }

        tags.Remove(tag);
    }
#endif
}

#if UNITY_EDITOR
internal class GameplayTagsListConstGenerator : ICodeGenerator
{
    public static CodeGenerationContext[] GenerateCode()
    {
        GameplayTagsList instance = GameplayTagsList.Instance;
        if (instance == null)
        {
            return null;
        }

        StringBuilder codeBuilder = new StringBuilder();
        codeBuilder.AppendLine("public static class GameplayTagsListConst");
        codeBuilder.AppendLine("{");
        foreach (GameplayTag tag in instance.Tags)
        {
            codeBuilder.AppendLine($"    public const string {tag.Name.Replace(GameplayTag.NAME_SEPARATOR, '_')} = \"{tag.Name}\";");
        }
        codeBuilder.AppendLine("}");

        string scriptLocation = $"{ICodeGenerator.DefaultFolderPath}/{nameof(GameplayTagsList)}.generated.cs";
        CodeGenerationContext context = new CodeGenerationContext(scriptLocation, codeBuilder.ToString());
        return new CodeGenerationContext[] { context };
    }
}
#endif

#if UNITY_EDITOR
public class GameplayTagsSettingsProvider : SettingsProvider
{
    string insertInput;

    GameplayTagsList tagsList;
    public GameplayTagsSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
    {

    }

    public override void OnActivate(string searchContext, VisualElement rootElement)
    {
        base.OnActivate(searchContext, rootElement);
        tagsList = GameplayTagsList.Instance;
    }

    public override void OnGUI(string searchContext)
    {
        base.OnGUI(searchContext);

        /*
        SerializedProperty namesProp = listObject.FindProperty("tagNames");

        List<string> names = new List<string>();
        for (int i = 0; i < namesProp.arraySize; i++)
        {
            names.Add(namesProp.GetArrayElementAtIndex(i).stringValue);
        }
        */

        EditorGUILayout.BeginHorizontal();
        insertInput = EditorGUILayout.TextField(insertInput);
        if (GUILayout.Button("Add"))
        {
            if (IsValidInput(insertInput))
            {
                string[] splitted = GameplayTag.SplitTagName(insertInput);
                for (int i = 0; i < splitted.Length; i++)
                {
                    GameplayTag newTag = new GameplayTag(string.Join(GameplayTag.NAME_SEPARATOR, splitted[0..i]),
                        string.Join(GameplayTag.NAME_SEPARATOR, splitted[0..(i + 1)]));

                    if (!tagsList.TagsRef.Contains(newTag))
                    {
                        tagsList.TagsRef.Add(newTag);
                        EditorUtility.SetDirty(tagsList);
                    }
                }

                insertInput = string.Empty;
            }
        }
        EditorGUILayout.EndHorizontal();

        /*
        List<string> elements = new List<string>();
        foreach (string name in names)
        {
            string[] splitted = GameplayTag.SplitTagName(name);
            for (int i = 0; i < splitted.Length; i++)
            {
                string[] subarray = splitted[0..(i + 1)];
                string element = string.Join(GameplayTag.NAME_SEPARATOR, subarray);
                if (!elements.Contains(element))
                {
                    elements.Add(element);
                }
            }
        }

        elements.Sort();
        */

        tagsList.TagsRef.Sort((GameplayTag a, GameplayTag b) => string.Compare(a.Name, b.Name));

        for (int i = 0; i < tagsList.TagsRef.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(tagsList.TagsRef[i].Name);
            if (GUILayout.Button("Remove"))
            {
                tagsList.RemoveTagFromList(tagsList.TagsRef[i]);
                EditorUtility.SetDirty(tagsList);
                i--;
            }

            EditorGUILayout.EndHorizontal();
        }

        
        //listObject.ApplyModifiedProperties();
    }

    private bool IsValidInput(string input)
    {
        string[] splitted = GameplayTag.SplitTagName(input);
        return splitted.All((str) => !string.IsNullOrEmpty(str));
    }

    [SettingsProvider]
    public static SettingsProvider CreateGameplayTagsSettingsProvider()
    {
        return new GameplayTagsSettingsProvider("Project/GameplayTags", SettingsScope.Project);
    }
}
#endif
