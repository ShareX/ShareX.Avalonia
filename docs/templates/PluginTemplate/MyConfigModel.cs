#region License Information (GPL v3)
// Your plugin license header
#endregion

namespace MyPlugin.Provider;

public class MyConfigModel
{
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://example.com/upload";
}
