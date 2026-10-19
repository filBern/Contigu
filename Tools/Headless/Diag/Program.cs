using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

/// <summary>dotnet run -- &lt;repoRoot&gt; &lt;unityModulesDir&gt; : prints normalized diagnostics (id + message, no file/line) sorted with counts.</summary>
public static class Program
{
    public static int Main(string[] args)
    {
        string root = args[0];
        string modulesDir = args[1];
        var defines = args.Skip(2).ToArray();
        var files = new List<string>();
        foreach (var dir in new[] { "Core", "Data", "Presentation" })
        {
            files.AddRange(Directory.GetFiles(Path.Combine(root, "Assets/Scripts", dir), "*.cs", SearchOption.AllDirectories));
        }
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9).WithPreprocessorSymbols(defines);
        var trees = files.Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), options, f)).ToList();

        var refs = new List<MetadataReference>();
        string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        foreach (var p in tpa.Split(Path.PathSeparator))
        {
            refs.Add(MetadataReference.CreateFromFile(p));
        }
        foreach (var dll in Directory.GetFiles(modulesDir, "*.dll"))
        {
            refs.Add(MetadataReference.CreateFromFile(dll));
        }
        var compilation = CSharpCompilation.Create("all", trees, refs, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Disable));
        var diags = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);
        foreach (var g in diags.GroupBy(d => d.Id + " " + d.GetMessage()).OrderByDescending(g => g.Count()))
        {
            Console.WriteLine(g.Count().ToString().PadLeft(5) + " " + g.Key);
        }
        return 0;
    }
}
