using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal static class CodeGenerator
{
    [InitializeOnLoadMethod]
    static void Initialize()
    {
        Generate();
    }

    [MenuItem("CodeGenerator/Generate")]
    internal static void Generate()
    {
        List<string> generatedCodePaths = new List<string>();

        bool changed = false;
        TypeCache.TypeCollection generatorTypes = TypeCache.GetTypesDerivedFrom(typeof(ICodeGenerator));
        foreach (Type generatorType in generatorTypes)
        {
            object contextValue = null;
            if (!StaticAbstractMessageSender<ICodeGenerator>.Call(generatorType, null, ref contextValue, nameof(ICodeGenerator.GenerateCode))) 
            {
                continue;
            }

            CodeGenerationContext[] contexts = contextValue as CodeGenerationContext[];
            if (contexts == null)
            {
                continue;
            }

            foreach (CodeGenerationContext context in contexts)
            {
                if (!context.IsValid || !context.path.EndsWith(ICodeGenerator.FileNameSuffix) || generatedCodePaths.Contains(context.path))
                {
                    continue;
                }

                generatedCodePaths.Add($"Assets/{context.path}");
                if (GenerateScriptFile(context))
                {
                    changed = true;
                }
            }
        }

        string[] fileNames = AssetDatabase.FindAssets(string.Empty)
            .Select((guid) => AssetDatabase.GUIDToAssetPath(guid))
            .Where((path) => path.EndsWith(ICodeGenerator.FileNameSuffix)).ToArray();
        foreach (string fileName in fileNames)
        {
            if (!generatedCodePaths.Contains(fileName))
            {
                AssetDatabase.DeleteAsset(fileName);
            }
        }

        if (changed)
        {
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }
    }

    private static bool GenerateScriptFile(CodeGenerationContext context)
    {
        string path = $"{Application.dataPath}/{context.path}";
        string dirPath = Path.GetDirectoryName(path);
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        if (File.Exists(path))
        {
            string code = File.ReadAllText(path);
            if (code == context.code)
            {
                return false;
            }
        }

        File.WriteAllText(path, context.code);
        return true;
    }
}
