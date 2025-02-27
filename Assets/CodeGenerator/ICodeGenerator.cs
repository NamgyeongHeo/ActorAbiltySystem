public interface ICodeGenerator
{
    public const string FileNameSuffix = ".generated.cs";
    public const string DefaultFolderPath = "CodeGenerator/Generated";

    [StaticAbstract]
    static CodeGenerationContext[] GenerateCode()
    {
        return null;
    }
}