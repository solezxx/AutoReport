// TokenLoader.cs
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public static class TokenLoader
{
    public static List<TokenAccount> LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<TokenAccount>>(json);
    }
}