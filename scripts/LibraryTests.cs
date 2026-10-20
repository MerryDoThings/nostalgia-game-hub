using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using BirdsLibrary;

internal static class LibraryTests
{
    private static void Expect(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    public static int Main(string[] args)
    {
        try
        {
            var catalog = new JavaScriptSerializer().Deserialize<List<Game>>(File.ReadAllText(args[0]));
            var path = Path.Combine(args[1], "library.json");
            Expect(catalog.Select(g => g.id).Distinct().Count() == catalog.Count, "Catalog IDs must be unique");
            Expect(!LibraryStore.IsLaunchPath("https://example.com/a.exe"), "URLs must not launch as programs");
            Expect(!LibraryStore.IsLaunchPath("relative.exe"), "Relative executable paths must be rejected");
            Expect(!LibraryStore.IsLaunchPath(@"C:\Games\surprise.bat"), "Scripts must be rejected");
            Expect(LibraryStore.IsLaunchPath(@"C:\Games\Il mio gioco.lnk"), "Windows shortcuts must be accepted");
            Expect(LibraryStore.IsLaunchPath(@"C:\Games\Gioco.exe"), "Windows executables must be accepted");
            var first = LibraryStore.Load(path, catalog);
            Expect(first.enabled.Count == catalog.Count, "First launch should offer the complete catalog");
            var selection = new UserLibrary { enabled = new List<string> { "shellrazer" } };
            selection.paths["shellrazer"] = @"C:\Games\Tartaruga.lnk";
            LibraryStore.Save(path, selection);
            var saved = LibraryStore.Load(path, catalog);
            Expect(saved.enabled.SequenceEqual(new[] { "shellrazer" }), "Excluded games must stay excluded");
            Expect(saved.paths["shellrazer"] == selection.paths["shellrazer"], "The selected local shortcut must survive reload");
            saved.enabled.Clear();
            LibraryStore.Save(path, saved);
            Expect(File.Exists(path + ".bak"), "An update must preserve the preceding configuration");
            Expect(LibraryStore.Load(path, catalog).enabled.Count == 0, "An explicitly empty library must stay empty");
            saved.enabled.AddRange(new[] { "shellrazer", "shellrazer", "not-in-catalog" });
            saved.paths["classic"] = @"C:\Games\surprise.cmd";
            saved.paths["not-in-catalog"] = @"C:\Games\Other.exe";
            LibraryStore.Save(path, saved);
            var cleaned = LibraryStore.Load(path, catalog);
            Expect(cleaned.enabled.SequenceEqual(new[] { "shellrazer" }), "Unknown and duplicate selections must be removed");
            Expect(!cleaned.paths.ContainsKey("classic") && !cleaned.paths.ContainsKey("not-in-catalog"), "Invalid paths and unknown titles must not be loaded");
            Console.WriteLine("PASS: selection, exclusions, empty library, saved paths, backup, path restrictions and catalog filtering.");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e.Message); return 1; }
    }
}
