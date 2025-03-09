using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class AbilityBodyGenerator : ICodeGenerator
{
    private const string GenerationFolderPath = "Abilities/Generated";

    private const string MethodName_ProcessActivateAbilityEvent = "ProcessActivateAbilityEvent";
    private const string MethodName_ProcessCancelAbilityEvent = "ProcessCancelAbilityEvent";

    private const string ParamName_AbilityEvent = "abilityEvent";

    public static CodeGenerationContext[] GenerateCode()
    {
        TypeCache.TypeCollection abilityTypeCollection = TypeCache.GetTypesDerivedFrom<Ability>();
        List<CodeGenerationContext> contexts = new List<CodeGenerationContext>();
        foreach (Type abilityType in abilityTypeCollection)
        {
            contexts.Add(GetBodyGeneration(abilityType));
        }

        return contexts.ToArray();
    }

    private static CodeGenerationContext GetBodyGeneration(Type abilityType)
    {
        bool needFile = false;

        if (abilityType == null)
        {
            return default;
        }

        string[] guids = AssetDatabase.FindAssets($"{abilityType.Name}", new string[] { "Assets" });
        string path = guids.Select((guid) => AssetDatabase.GUIDToAssetPath(guid)).FirstOrDefault((path) => !path.EndsWith(ICodeGenerator.FileNameSuffix));
        path = Path.GetRelativePath("Assets/", path);
        path = Path.ChangeExtension(path, ICodeGenerator.FileNameSuffix);
        path = path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        Type[] interfaceTypes = abilityType.GetInterfaces();
        Array.Reverse(interfaceTypes);

        string abilityName = abilityType.Name;
        string namespaceName = abilityType.Namespace;

        StringBuilder codeBuilder = new StringBuilder();
        /*
        if (!string.IsNullOrEmpty(namespaceName))
        {
            codeBuilder.AppendLine($"namespace {namespaceName}");
            codeBuilder.AppendLine("{");
        }
        */
        codeBuilder.AppendLine($"public partial class {abilityName} : {nameof(Ability)}");
        codeBuilder.AppendLine("{");
        codeBuilder.AppendLine($"    protected override void {MethodName_ProcessActivateAbilityEvent}({nameof(AbilityEvent)} {ParamName_AbilityEvent})");
        codeBuilder.AppendLine("    {");
        foreach (Type interfaceType in
            interfaceTypes.Where((type) => type.GetGenericTypeDefinition() == typeof(IActivateAbilityEventListener<>)))
        {
            needFile = true;

            Type eventType = interfaceType.GetGenericArguments()[0];
            codeBuilder.AppendLine($"        if (abilityEvent is {eventType})");
            codeBuilder.AppendLine("        {");
            codeBuilder.AppendLine($"            Activate(abilityEvent as {eventType.Name});");
            codeBuilder.AppendLine("        }");
        }
        codeBuilder.AppendLine("    }");
        codeBuilder.AppendLine();
        codeBuilder.AppendLine($"    protected override void {MethodName_ProcessCancelAbilityEvent}({nameof(AbilityEvent)} {ParamName_AbilityEvent})");
        codeBuilder.AppendLine("    {");
        foreach (Type interfaceType in
            interfaceTypes.Where((type) => type.GetGenericTypeDefinition() == typeof(ICancelAbilityEventListener<>))) 
        {
            needFile = true;

            Type eventType = interfaceType.GetGenericArguments()[0];
            codeBuilder.AppendLine($"        if (abilityEvent is {eventType})");
            codeBuilder.AppendLine("        {");
            codeBuilder.AppendLine($"            Cancel(abilityEvent as {eventType.Name});");
            codeBuilder.AppendLine("        }");
        }
        codeBuilder.AppendLine("    }");
        codeBuilder.AppendLine("}");
        /*
        if (!string.IsNullOrEmpty(namespaceName))
        {
            codeBuilder.AppendLine("}");
        }
        */
        if (!needFile)
        {
            return default;
        }

        return new CodeGenerationContext(path, codeBuilder.ToString());
    }
}
