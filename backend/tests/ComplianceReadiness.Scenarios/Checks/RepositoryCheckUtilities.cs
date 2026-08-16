namespace AfriWallet.Compliance.Readiness.Checks;
internal static class RepositoryCheckUtilities
{
    public static string Resolve(string root, params string[] parts) { var path=root; foreach(var part in parts) path=Path.Combine(path,part); return Path.GetFullPath(path); }
    public static bool DirectoryExists(string root, params string[] parts) => Directory.Exists(Resolve(root,parts));
    public static IEnumerable<string> EnumerateTextFiles(string directory) { if(!Directory.Exists(directory)) return []; var ext=new HashSet<string>(StringComparer.OrdinalIgnoreCase){".cs",".csproj",".json",".md",".ps1",".yml",".yaml"}; return Directory.EnumerateFiles(directory,"*",SearchOption.AllDirectories).Where(p=>ext.Contains(Path.GetExtension(p))).Where(p=>!p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",StringComparison.OrdinalIgnoreCase)&&!p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",StringComparison.OrdinalIgnoreCase)); }
}