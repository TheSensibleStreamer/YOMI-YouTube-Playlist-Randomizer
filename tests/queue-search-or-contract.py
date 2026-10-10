#!/usr/bin/env python3
"""Compile YOMI's real Queue search methods and exercise comma-OR/negative regressions."""
from pathlib import Path
import subprocess
import tempfile

source = Path("payload/app/YomiControllerWpf.cs").read_text(encoding="utf-8-sig")

def production_method(signature: str) -> str:
    marker = "        " + signature
    start = source.index(marker)
    opening = source.index("{", start)
    depth = 0
    for pos in range(opening, len(source)):
        if source[pos] == "{":
            depth += 1
        elif source[pos] == "}":
            depth -= 1
            if depth == 0:
                return source[start:pos + 1]
    raise AssertionError("Unclosed production method: " + signature)

methods = [
    production_method("private static string NormalizeQueueSearchTerm("),
    production_method("private void AddQueueSearchTerm("),
    production_method("private static List<string> SplitQueueSearchClauses("),
    production_method("private void CompileQueueTextTerms("),
    production_method("private bool QueueTextFilterMatches("),
]
prefix = r"""
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
public class QueueRow { public string SearchText { get; set; } }
public class SearchHarness {
    private readonly List<List<string>> _queueQueryIncludeGroups = new List<List<string>>();
    private readonly List<string> _queueQueryExcludes = new List<string>();
    public void SetQuery(string query) {
        _queueQueryIncludeGroups.Clear();
        _queueQueryExcludes.Clear();
        CompileQueueTextTerms(query);
    }
    public bool Matches(string haystack) {
        return QueueTextFilterMatches(new QueueRow { SearchText = haystack });
    }
"""
suffix = r"""
}
public class Program {
    private static int checks = 0;
    private static void Check(string query, string track, bool expected) {
        var f = new SearchHarness();
        f.SetQuery(query);
        bool actual = f.Matches(track);
        if (actual != expected)
            throw new Exception($"FAIL search={query}, row={track}, expected={expected}, actual={actual}");
        checks++;
    }
    public static void Main() {
        Check("midnite, dezarie", "Midnite - Love The Life You Live", true);
        Check("midnite, dezarie", "Dezarie - Gracious Mama", true);
        Check("midnite, dezarie", "Dezarie with Midnite band", true);
        // Filtered Listen uses the WPF filtered row view in original queue order.
        // The OR engine must produce the union once, not duplicate collaborations.
        var rows = new[] {
            "Midnite - Love The Life You Live | Midnite",
            "Dezarie - Gracious Mama | Dezarie",
            "Dezarie with Midnite band | Midnite",
            "Steel Pulse - Your House | Steel Pulse",
            "MIDNITE - studio dub | Midnite",
            "Dezarie - Live recording | Dezarie"
        };
        var mixed = new SearchHarness();
        mixed.SetQuery("midnite, dezarie");
        var selected = rows.Where(mixed.Matches).ToArray();
        if (selected.Length != 5 ||
            !selected.SequenceEqual(new[] { rows[0], rows[1], rows[2], rows[4], rows[5] }))
            throw new Exception("Failed OR union, stable order, or one-row-per-occurrence filtering");
        checks++;
        mixed.SetQuery("midnite, dezarie, -live");
        selected = rows.Where(mixed.Matches).ToArray();
        // 'Live' in the first title also matches -live by design (substring
        // search), so both first and final rows are excluded consistently.
        if (selected.Length != 3 || selected.Contains(rows[0]) || selected.Contains(rows[5]) ||
            !selected.SequenceEqual(new[] { rows[1], rows[2], rows[4] }))
            throw new Exception("Global negative clause incorrectly crossed OR groups");
        checks++;
        Check("midnite, dezarie", "Steel Pulse - Your House", false);
        Check("MIDNITE, dezarie,", "dezarie - new track", true);
        Check("midnite, dezarie, -live", "Midnite - Studio", true);
        Check("midnite, dezarie, -live", "Dezarie - Studio", true);
        Check("midnite, dezarie, -live", "Midnite live recording", false);
        Check("midnite, dezarie, -live", "Dezarie LIVE RECORDING", false);
        Check("midnite, -live, dezarie", "Dezarie - Studio", true);
        Check("midnite, -live, dezarie", "Dezarie live", false);
        Check("midnite, -live", "Midnite - Studio", true);
        Check("midnite, -live", "Midnite live", false);
        Check("midnite, -live", "Dezarie - Studio", false);
        Check("-live", "Dezarie - Studio", true);
        Check("-live", "Dezarie - Live", false);
        Check("dub reggae", "dub reggae song", true);
        Check("dub reggae", "dub rock reggae", false);
        Check("dub reggae -live", "dub reggae studio", true);
        Check("dub reggae -live", "dub reggae live", false);
        Check("\"midnite, dezarie\"", "Midnite, Dezarie collaboration", true);
        Check("\"midnite, dezarie\"", "Midnite - solo", false);
        Check("midnite -live, dezarie -remix", "Midnite - Studio", true);
        Check("midnite -live, dezarie -remix", "Dezarie - Studio", true);
        Check("midnite -live, dezarie -remix", "Dezarie - Remix", false);
        Check("midnite -live, dezarie -remix", "Midnite live", false);
        Console.WriteLine($"PASS: {checks} actual C# Queue search cases");
    }
}
"""
with tempfile.TemporaryDirectory(prefix="yomi-queue-filter-") as folder:
    root = Path(folder)
    (root / "QueueFilter.csproj").write_text(
        '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
        '<TargetFramework>net8.0</TargetFramework><Nullable>disable</Nullable>'
        '</PropertyGroup></Project>', encoding="utf-8")
    (root / "Program.cs").write_text(prefix + "\n\n".join(methods) + suffix, encoding="utf-8")
    subprocess.run(["dotnet", "run", "--project", str(root / "QueueFilter.csproj"),
                    "-c", "Release"], check=True)
