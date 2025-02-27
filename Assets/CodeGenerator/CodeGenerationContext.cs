public struct CodeGenerationContext
{
    public string path;
    public string code;

    public CodeGenerationContext(string path, string code)
    {
        this.path = path;
        this.code = code;
    }

    public bool IsValid
    {
        get
        {
            return !string.IsNullOrWhiteSpace(path) && !string.IsNullOrEmpty(code);
        }
    }
}