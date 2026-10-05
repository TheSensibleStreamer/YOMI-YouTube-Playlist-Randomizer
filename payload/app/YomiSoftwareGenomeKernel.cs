using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Yomi.ProductShell
{
    internal sealed class GenomeScanOptions
    {
        public int MaxFiles { get; set; }
        public long MaxFileBytes { get; set; }
        public int MaxSymbols { get; set; }
        public int MaxEdges { get; set; }
        public int MaxFindings { get; set; }
        public bool IncludeGenerated { get; set; }
        public GenomeScanOptions() { MaxFiles = 12000; MaxFileBytes = 3L * 1024L * 1024L; MaxSymbols = 50000; MaxEdges = 120000; MaxFindings = 12000; }
    }

    internal sealed class GenomeFile
    {
        public string Id { get; set; }
        public string RelativePath { get; set; }
        public string Language { get; set; }
        public string Sha256 { get; set; }
        public long Bytes { get; set; }
        public int Lines { get; set; }
        public int NonBlankLines { get; set; }
        public int SymbolCount { get; set; }
        public int FanIn { get; set; }
        public int FanOut { get; set; }
        public int ComplexityProxy { get; set; }
        public int MutationSites { get; set; }
        public int ExternalBoundarySites { get; set; }
        public int HistoryTouches { get; set; }
        public string PrimaryOwner { get; set; }
        public string Layer { get; set; }
        public bool IsTest { get; set; }
        public bool TruncatedForAnalysis { get; set; }
        public List<string> Imports { get; set; }
        public List<string> Tags { get; set; }
        public GenomeFile() { Imports = new List<string>(); Tags = new List<string>(); }
        public override string ToString() { return (RelativePath ?? Id ?? "file") + "   ·   " + (Language ?? "TEXT") + "   ·   " + Lines.ToString("N0", CultureInfo.InvariantCulture) + " lines   ·   symbols " + SymbolCount.ToString(CultureInfo.InvariantCulture) + "   ·   in/out " + FanIn + "/" + FanOut + "   ·   cx " + ComplexityProxy + (IsTest ? "   ·   TEST" : ""); }
    }

    internal sealed class GenomeSymbol
    {
        public string Id { get; set; }
        public string FileId { get; set; }
        public string FilePath { get; set; }
        public string Name { get; set; }
        public string QualifiedName { get; set; }
        public string Kind { get; set; }
        public string Visibility { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public int Lines { get; set; }
        public int ComplexityProxy { get; set; }
        public int FanIn { get; set; }
        public int FanOut { get; set; }
        public int ReadSites { get; set; }
        public int WriteSites { get; set; }
        public string Signature { get; set; }
        public string BodyFingerprint { get; set; }
        public List<string> Attributes { get; set; }
        public GenomeSymbol() { Attributes = new List<string>(); }
        public override string ToString() { return (Kind ?? "SYMBOL") + "   ·   " + (QualifiedName ?? Name ?? Id) + "   ·   " + (FilePath ?? "") + ":" + StartLine + "   ·   in/out " + FanIn + "/" + FanOut + "   ·   cx " + ComplexityProxy; }
    }

    internal sealed class GenomeEdge
    {
        public string Id { get; set; }
        public string FromId { get; set; }
        public string ToId { get; set; }
        public string FromLabel { get; set; }
        public string ToLabel { get; set; }
        public string Kind { get; set; }
        public string Evidence { get; set; }
        public int Line { get; set; }
        public double Confidence { get; set; }
        public string CertificateHash { get; set; }
        public override string ToString() { return (Kind ?? "EDGE") + "   ·   " + (FromLabel ?? FromId) + " → " + (ToLabel ?? ToId) + "   ·   " + Confidence.ToString("P0", CultureInfo.InvariantCulture) + (Line > 0 ? "   ·   line " + Line : ""); }
    }

    internal sealed class GenomeDataFlow
    {
        public string Id { get; set; }
        public string FileId { get; set; }
        public string SymbolId { get; set; }
        public string Variable { get; set; }
        public string Access { get; set; }
        public int Line { get; set; }
        public string Evidence { get; set; }
        public string Classification { get; set; }
        public override string ToString() { return (Access ?? "USE") + "   ·   " + (Variable ?? "state") + "   ·   " + (Classification ?? "LOCAL") + "   ·   " + (SymbolId ?? FileId ?? "") + "   ·   line " + Line; }
    }

    internal sealed class GenomeFinding
    {
        public string Id { get; set; }
        public string Severity { get; set; }
        public string Kind { get; set; }
        public string SubjectId { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public string Evidence { get; set; }
        public string CertificateHash { get; set; }
        public GenomeFinding() { Severity = "INFO"; }
        public override string ToString() { return (Severity ?? "INFO") + "   ·   " + (Kind ?? "FINDING") + "   ·   " + (Subject ?? SubjectId ?? "") + "   ·   " + (Message ?? ""); }
    }

    internal sealed class GenomeCycle
    {
        public string Id { get; set; }
        public List<string> Members { get; set; }
        public int InternalEdges { get; set; }
        public string CertificateHash { get; set; }
        public GenomeCycle() { Members = new List<string>(); }
        public override string ToString() { return "SCC   ·   " + Members.Count + " members   ·   " + InternalEdges + " internal edges   ·   " + String.Join(" → ", Members.Take(5).ToArray()) + (Members.Count > 5 ? " …" : ""); }
    }

    internal sealed class GenomeHistoryCommit
    {
        public string Sha { get; set; }
        public string Author { get; set; }
        public string Email { get; set; }
        public long Unix { get; set; }
        public string Utc { get; set; }
        public List<string> Files { get; set; }
        public GenomeHistoryCommit() { Files = new List<string>(); }
        public override string ToString() { return ShortSha(Sha) + "   ·   " + (Utc ?? "") + "   ·   " + (Author ?? "unknown") + "   ·   " + Files.Count + " files"; }
        private static string ShortSha(string s) { return String.IsNullOrWhiteSpace(s) ? "∅" : s.Substring(0, Math.Min(10, s.Length)); }
    }

    internal sealed class GenomeOwnership
    {
        public string FilePath { get; set; }
        public string Owner { get; set; }
        public int Touches { get; set; }
        public double Share { get; set; }
        public string Source { get; set; }
        public override string ToString() { return (FilePath ?? "") + "   ·   " + (Owner ?? "unowned") + "   ·   " + Touches + " touches   ·   " + Share.ToString("P0", CultureInfo.InvariantCulture) + "   ·   " + (Source ?? "HISTORY"); }
    }

    internal sealed class GenomeCoChange
    {
        public string Left { get; set; }
        public string Right { get; set; }
        public int CommitsTogether { get; set; }
        public double Jaccard { get; set; }
        public override string ToString() { return (Left ?? "") + " ⇄ " + (Right ?? "") + "   ·   " + CommitsTogether + " commits   ·   J=" + Jaccard.ToString("0.000", CultureInfo.InvariantCulture); }
    }

    internal sealed class GenomeImpactReport
    {
        public string Id { get; set; }
        public string SnapshotId { get; set; }
        public string SubjectId { get; set; }
        public string Subject { get; set; }
        public string ChangeKind { get; set; }
        public string CreatedUtc { get; set; }
        public int DirectDependents { get; set; }
        public int TransitiveDependents { get; set; }
        public int AffectedTests { get; set; }
        public int AffectedCycles { get; set; }
        public int ExternalBoundaryTouches { get; set; }
        public double RiskScore { get; set; }
        public List<string> Impacted { get; set; }
        public List<string> Reasons { get; set; }
        public string CertificateHash { get; set; }
        public GenomeImpactReport() { CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); Impacted = new List<string>(); Reasons = new List<string>(); }
        public override string ToString() { return (ChangeKind ?? "CHANGE") + "   ·   " + (Subject ?? SubjectId ?? "") + "   ·   direct " + DirectDependents + "   ·   transitive " + TransitiveDependents + "   ·   tests " + AffectedTests + "   ·   risk " + RiskScore.ToString("0.0", CultureInfo.InvariantCulture) + "/100"; }
    }

    internal sealed class GenomeSnapshot
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string RootPath { get; set; }
        public string CreatedUtc { get; set; }
        public string RootFingerprint { get; set; }
        public string AnalyzerVersion { get; set; }
        public bool FileCeilingReached { get; set; }
        public bool SymbolCeilingReached { get; set; }
        public bool EdgeCeilingReached { get; set; }
        public int SkippedBinaryFiles { get; set; }
        public int SkippedOversizeFiles { get; set; }
        public List<GenomeFile> Files { get; set; }
        public List<GenomeSymbol> Symbols { get; set; }
        public List<GenomeEdge> Edges { get; set; }
        public List<GenomeDataFlow> DataFlows { get; set; }
        public List<GenomeFinding> Findings { get; set; }
        public List<GenomeCycle> Cycles { get; set; }
        public List<GenomeHistoryCommit> History { get; set; }
        public List<GenomeOwnership> Ownership { get; set; }
        public List<GenomeCoChange> CoChanges { get; set; }
        public GenomeSnapshot()
        {
            CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); AnalyzerVersion = "DEV13.37.25";
            Files = new List<GenomeFile>(); Symbols = new List<GenomeSymbol>(); Edges = new List<GenomeEdge>(); DataFlows = new List<GenomeDataFlow>(); Findings = new List<GenomeFinding>(); Cycles = new List<GenomeCycle>(); History = new List<GenomeHistoryCommit>(); Ownership = new List<GenomeOwnership>(); CoChanges = new List<GenomeCoChange>();
        }
        public override string ToString() { return (Name ?? Id ?? "snapshot") + "   ·   " + Files.Count.ToString("N0", CultureInfo.InvariantCulture) + " files   ·   " + Symbols.Count.ToString("N0", CultureInfo.InvariantCulture) + " symbols   ·   " + Edges.Count.ToString("N0", CultureInfo.InvariantCulture) + " edges   ·   " + Cycles.Count + " cycles   ·   " + SoftwareGenomeKernel.Short(RootFingerprint); }
    }

    internal static class SoftwareGenomeKernel
    {
        private static readonly Regex CsType = new Regex(@"\b(?:(public|private|protected|internal)\s+)?(?:(?:static|sealed|abstract|partial)\s+)*(class|struct|interface|enum|record)\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
        private static readonly Regex CsMethod = new Regex(@"\b(public|private|protected|internal)\s+(?:(?:static|virtual|override|abstract|async|sealed|new|partial)\s+)*(?:[A-Za-z_][A-Za-z0-9_<>,\.\[\]\? ]*\s+)+([A-Za-z_][A-Za-z0-9_]*)\s*\(([^;{}]*)\)\s*(?:where\b[^\{]+)?\{", RegexOptions.Compiled);
        private static readonly Regex PsFunction = new Regex(@"(?im)^\s*function\s+([A-Za-z_][A-Za-z0-9_\-:]*)\s*(?:\([^\)]*\))?\s*\{", RegexOptions.Compiled);
        private static readonly Regex LuaFunction = new Regex(@"(?im)^\s*(?:local\s+)?function\s+([A-Za-z_][A-Za-z0-9_\.:]*)\s*\(", RegexOptions.Compiled);
        private static readonly Regex JsFunction = new Regex(@"(?im)^\s*(?:export\s+)?(?:async\s+)?function\s+([A-Za-z_$][A-Za-z0-9_$]*)\s*\(|^\s*(?:export\s+)?class\s+([A-Za-z_$][A-Za-z0-9_$]*)\b|\b(?:const|let|var)\s+([A-Za-z_$][A-Za-z0-9_$]*)\s*=\s*(?:async\s*)?\([^\)]*\)\s*=>", RegexOptions.Compiled);
        private static readonly Regex PySymbol = new Regex(@"(?im)^\s*(class|def|async\s+def)\s+([A-Za-z_][A-Za-z0-9_]*)\s*[\(:]", RegexOptions.Compiled);
        private static readonly Regex IdentifierCall = new Regex(@"\b([A-Za-z_][A-Za-z0-9_]*)\s*\(", RegexOptions.Compiled);
        private static readonly Regex Assignment = new Regex(@"(?m)(?<![=!<>])\b([A-Za-z_][A-Za-z0-9_\.]*)\s*(?:\+\+|--|[+\-*/%&|\^]?=)(?!=)", RegexOptions.Compiled);
        private static readonly Regex Identifier = new Regex(@"\b[A-Za-z_][A-Za-z0-9_]*\b", RegexOptions.Compiled);
        private static readonly HashSet<string> CallStop = new HashSet<string>(new[] { "if", "for", "foreach", "while", "switch", "catch", "using", "lock", "return", "new", "typeof", "nameof", "sizeof", "checked", "unchecked", "default", "delegate", "function" }, StringComparer.OrdinalIgnoreCase);
        private static readonly string[] DefaultExtensions = { ".cs", ".ps1", ".psm1", ".lua", ".js", ".jsx", ".ts", ".tsx", ".py", ".java", ".kt", ".kts", ".cpp", ".c", ".h", ".hpp", ".go", ".rs", ".rb", ".php", ".swift", ".scala", ".fs", ".fsx", ".vb", ".xaml", ".xml", ".json", ".jsonl", ".yaml", ".yml", ".toml", ".ini", ".config", ".props", ".targets", ".csproj", ".sln", ".md", ".txt", ".sql", ".sh", ".bat", ".cmd" };
        private static readonly HashSet<string> ExcludedDirectories = new HashSet<string>(new[] { ".git", ".svn", ".hg", "bin", "obj", "node_modules", ".vs", ".idea", ".vscode", "dist", "build", "packages", "vendor", "coverage", ".next", ".nuxt", "target" }, StringComparer.OrdinalIgnoreCase);

        public static string NewId(string prefix) { return (prefix ?? "GEN") + "-" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant(); }
        public static string Clip(string s, int n) { if (String.IsNullOrEmpty(s)) return ""; return s.Length <= n ? s : s.Substring(0, Math.Max(0, n - 1)) + "…"; }
        public static string Short(string s) { return String.IsNullOrWhiteSpace(s) ? "∅" : s.Substring(0, Math.Min(12, s.Length)); }
        public static string HashText(string text) { using (SHA256 sha = SHA256.Create()) return Hex(sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? ""))); }
        public static string HashFile(string path) { using (SHA256 sha = SHA256.Create()) using (FileStream fs = File.OpenRead(path)) return Hex(sha.ComputeHash(fs)); }
        private static string Hex(byte[] bytes) { StringBuilder b = new StringBuilder(bytes.Length * 2); foreach (byte x in bytes) b.Append(x.ToString("x2", CultureInfo.InvariantCulture)); return b.ToString(); }

        public static GenomeSnapshot Scan(string root, string name, GenomeScanOptions options)
        {
            if (options == null) options = new GenomeScanOptions();
            string full = Path.GetFullPath(root ?? "");
            if (!Directory.Exists(full)) throw new DirectoryNotFoundException("Codebase root not found: " + full);
            GenomeSnapshot s = new GenomeSnapshot { Id = NewId("GENOME"), Name = String.IsNullOrWhiteSpace(name) ? new DirectoryInfo(full).Name : name.Trim(), RootPath = full };
            Dictionary<string, string> texts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            List<string> paths = EnumerateSourceFiles(full, options.MaxFiles + 1).ToList();
            if (paths.Count > options.MaxFiles) { paths = paths.Take(options.MaxFiles).ToList(); s.FileCeilingReached = true; }
            foreach (string path in paths)
            {
                FileInfo fi;
                try { fi = new FileInfo(path); } catch { continue; }
                if (fi.Length > options.MaxFileBytes) { s.SkippedOversizeFiles++; continue; }
                string rel = Relative(full, path); string ext = Path.GetExtension(path).ToLowerInvariant();
                if (!DefaultExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase)) { s.SkippedBinaryFiles++; continue; }
                string text;
                try { text = ReadText(path, options.MaxFileBytes); } catch { s.SkippedBinaryFiles++; continue; }
                if (LooksBinary(text)) { s.SkippedBinaryFiles++; continue; }
                GenomeFile gf = new GenomeFile { Id = "FILE-" + HashText(rel.ToLowerInvariant()).Substring(0, 16).ToUpperInvariant(), RelativePath = rel, Language = Language(ext), Sha256 = HashFile(path), Bytes = fi.Length, Lines = CountLines(text), NonBlankLines = text.Split('\n').Count(x => !String.IsNullOrWhiteSpace(x)), IsTest = IsTestPath(rel), Layer = InferLayer(rel) };
                gf.ComplexityProxy = Complexity(text); gf.MutationSites = Assignment.Matches(text).Count; gf.ExternalBoundarySites = BoundarySites(text); gf.Imports.AddRange(Imports(text, gf.Language));
                if (GeneratedHint(rel, text)) gf.Tags.Add("GENERATED?");
                if (gf.ExternalBoundarySites > 0) gf.Tags.Add("EXTERNAL_BOUNDARY");
                if (gf.MutationSites > 20) gf.Tags.Add("STATE_DENSE");
                s.Files.Add(gf); texts[gf.Id] = text;
            }
            ExtractSymbols(s, texts, options);
            ExtractDependenciesAndCalls(s, texts, options);
            ExtractDataFlows(s, texts, options);
            BuildCycles(s);
            BuildFindings(s, texts, options);
            CalculateMetrics(s);
            s.RootFingerprint = Fingerprint(s);
            return s;
        }

        private static IEnumerable<string> EnumerateSourceFiles(string root, int ceiling)
        {
            Stack<string> stack = new Stack<string>(); stack.Push(root); int yielded = 0;
            while (stack.Count > 0 && yielded < ceiling)
            {
                string dir = stack.Pop();
                string[] subs = new string[0]; string[] files = new string[0];
                try { subs = Directory.GetDirectories(dir); files = Directory.GetFiles(dir); } catch { continue; }
                foreach (string sub in subs.OrderByDescending(x => x, StringComparer.OrdinalIgnoreCase)) { string n = Path.GetFileName(sub); if (!ExcludedDirectories.Contains(n)) stack.Push(sub); }
                foreach (string file in files.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) { yield return file; yielded++; if (yielded >= ceiling) yield break; }
            }
        }

        private static string ReadText(string path, long maxBytes) { using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) using (StreamReader sr = new StreamReader(fs, true)) return sr.ReadToEnd(); }
        private static bool LooksBinary(string t) { if (String.IsNullOrEmpty(t)) return false; int n = Math.Min(t.Length, 4096), weird = 0; for (int i = 0; i < n; i++) { char c = t[i]; if (c == '\0') return true; if (c < 8 || (c > 13 && c < 32)) weird++; } return weird > n / 20; }
        private static int CountLines(string text) { if (String.IsNullOrEmpty(text)) return 0; int n = 1; for (int i = 0; i < text.Length; i++) if (text[i] == '\n') n++; return n; }
        private static string Relative(string root, string path) { Uri r = new Uri(AppendSlash(root)); Uri p = new Uri(path); return Uri.UnescapeDataString(r.MakeRelativeUri(p).ToString()).Replace('/', Path.DirectorySeparatorChar); }
        private static string AppendSlash(string p) { return p.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ? p : p + Path.DirectorySeparatorChar; }
        private static string Language(string ext) { switch (ext) { case ".cs": return "C#"; case ".ps1": case ".psm1": return "POWERSHELL"; case ".lua": return "LUA"; case ".js": case ".jsx": return "JAVASCRIPT"; case ".ts": case ".tsx": return "TYPESCRIPT"; case ".py": return "PYTHON"; case ".xaml": return "XAML"; case ".json": case ".jsonl": return "JSON"; case ".yaml": case ".yml": return "YAML"; case ".sql": return "SQL"; case ".xml": case ".config": case ".props": case ".targets": case ".csproj": return "XML"; default: return ext.TrimStart('.').ToUpperInvariant(); } }
        private static bool IsTestPath(string rel) { string x = rel.Replace('\\', '/').ToLowerInvariant(); string n = Path.GetFileNameWithoutExtension(rel).ToLowerInvariant(); return x.Contains("/test/") || x.Contains("/tests/") || x.Contains("/spec/") || n.EndsWith("test") || n.EndsWith("tests") || n.EndsWith("spec") || n.Contains(".test") || n.Contains(".spec"); }
        private static bool GeneratedHint(string rel, string text) { string x = rel.ToLowerInvariant(); return x.Contains("generated") || x.Contains("designer.") || (text != null && text.IndexOf("<auto-generated", StringComparison.OrdinalIgnoreCase) >= 0); }
        private static string InferLayer(string rel) { string[] parts = rel.Replace('\\', '/').Split('/'); if (parts.Length <= 1) return "ROOT"; string p = parts[0].Trim(); return String.IsNullOrWhiteSpace(p) ? "ROOT" : p.ToUpperInvariant(); }
        private static int Complexity(string text) { if (String.IsNullOrWhiteSpace(text)) return 0; int n = 1; string[] tokens = { " if ", " for ", " foreach ", " while ", " case ", " catch ", "&&", "||", "?", " match ", " when " }; string padded = " " + text.ToLowerInvariant() + " "; foreach (string t in tokens) n += CountOccurrences(padded, t); return n; }
        private static int CountOccurrences(string s, string term) { int n = 0, p = 0; while ((p = s.IndexOf(term, p, StringComparison.Ordinal)) >= 0) { n++; p += term.Length; } return n; }
        private static int BoundarySites(string text) { string[] terms = { "Process.Start", "Start-Process", "Invoke-WebRequest", "Invoke-RestMethod", "HttpClient", "WebClient", "TcpClient", "UdpClient", "File.Write", "File.Delete", "Directory.Delete", "Registry.", "Environment.SetEnvironmentVariable", "SqlConnection", "Npgsql", "MySqlConnection", "socket(", "fetch(", "XMLHttpRequest", "requests.", "subprocess.", "os.system" }; int n = 0; foreach (string t in terms) n += CountOccurrences(text ?? "", t); return n; }

        private static void ExtractSymbols(GenomeSnapshot s, Dictionary<string, string> texts, GenomeScanOptions options)
        {
            foreach (GenomeFile f in s.Files)
            {
                string text; if (!texts.TryGetValue(f.Id, out text)) continue; List<GenomeSymbol> found = new List<GenomeSymbol>();
                if (f.Language == "C#")
                {
                    foreach (Match m in CsType.Matches(text)) found.Add(Symbol(f, text, m, m.Groups[3].Value, m.Groups[2].Value.ToUpperInvariant(), m.Groups[1].Value));
                    foreach (Match m in CsMethod.Matches(text)) found.Add(Symbol(f, text, m, m.Groups[2].Value, "METHOD", m.Groups[1].Value, Clip(m.Value.Replace("\r", " ").Replace("\n", " "), 240)));
                }
                else if (f.Language == "POWERSHELL") foreach (Match m in PsFunction.Matches(text)) found.Add(Symbol(f, text, m, m.Groups[1].Value, "FUNCTION", ""));
                else if (f.Language == "LUA") foreach (Match m in LuaFunction.Matches(text)) found.Add(Symbol(f, text, m, m.Groups[1].Value, "FUNCTION", ""));
                else if (f.Language == "JAVASCRIPT" || f.Language == "TYPESCRIPT") foreach (Match m in JsFunction.Matches(text)) { string n = FirstNonBlank(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value); if (!String.IsNullOrWhiteSpace(n)) found.Add(Symbol(f, text, m, n, m.Groups[2].Success ? "CLASS" : "FUNCTION", "")); }
                else if (f.Language == "PYTHON") foreach (Match m in PySymbol.Matches(text)) found.Add(Symbol(f, text, m, m.Groups[2].Value, m.Groups[1].Value.ToUpperInvariant().Replace(" ", "_"), ""));
                found = found.OrderBy(x => x.StartLine).ThenBy(x => x.Kind).ToList();
                for (int i = 0; i < found.Count; i++) { int next = i + 1 < found.Count ? found[i + 1].StartLine - 1 : f.Lines; found[i].EndLine = Math.Max(found[i].StartLine, next); found[i].Lines = Math.Max(1, found[i].EndLine - found[i].StartLine + 1); found[i].QualifiedName = f.RelativePath + "::" + found[i].Name; found[i].BodyFingerprint = HashSymbolBody(text, found[i].StartLine, found[i].EndLine); found[i].ComplexityProxy = SymbolComplexity(text, found[i].StartLine, found[i].EndLine); }
                foreach (GenomeSymbol sym in found) { if (s.Symbols.Count >= options.MaxSymbols) { s.SymbolCeilingReached = true; break; } s.Symbols.Add(sym); }
                f.SymbolCount = found.Count;
                if (s.SymbolCeilingReached) break;
            }
        }
        private static GenomeSymbol Symbol(GenomeFile f, string text, Match m, string name, string kind, string visibility, string signature = null) { int line = 1; int lim = Math.Min(m.Index, (text ?? "").Length); for (int i = 0; i < lim; i++) if (text[i] == '\n') line++; return new GenomeSymbol { Id = "SYM-" + HashText(f.Id + "|" + name + "|" + line).Substring(0, 16).ToUpperInvariant(), FileId = f.Id, FilePath = f.RelativePath, Name = name, Kind = kind, Visibility = visibility, StartLine = line, Signature = signature ?? Clip(m.Value.Replace("\r", " ").Replace("\n", " "), 240) }; }
        private static string FirstNonBlank(params string[] items) { foreach (string x in items) if (!String.IsNullOrWhiteSpace(x)) return x; return ""; }
        private static string HashSymbolBody(string text, int start, int end) { string body = Lines(text, start, end); body = Regex.Replace(body, @"\s+", " ").Trim(); return HashText(body); }
        private static int SymbolComplexity(string text, int start, int end) { return Complexity(Lines(text, start, end)); }
        private static string Lines(string text, int start, int end) { string[] a = (text ?? "").Replace("\r", "").Split('\n'); int s = Math.Max(0, start - 1), e = Math.Min(a.Length - 1, Math.Max(s, end - 1)); return String.Join("\n", a.Skip(s).Take(e - s + 1).ToArray()); }

        private static void ExtractDependenciesAndCalls(GenomeSnapshot s, Dictionary<string, string> texts, GenomeScanOptions options)
        {
            Dictionary<string, GenomeFile> byPath = s.Files.ToDictionary(x => NormalizePath(x.RelativePath), x => x, StringComparer.OrdinalIgnoreCase);
            Dictionary<string, List<GenomeSymbol>> symbolsByName = s.Symbols.GroupBy(x => x.Name, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
            Dictionary<string, GenomeFile> stemUnique = s.Files.GroupBy(x => Path.GetFileNameWithoutExtension(x.RelativePath), StringComparer.OrdinalIgnoreCase).Where(g => g.Count() == 1).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            foreach (GenomeFile f in s.Files)
            {
                string text; if (!texts.TryGetValue(f.Id, out text)) continue;
                HashSet<string> depTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string import in f.Imports)
                {
                    GenomeFile target;
                    string last = import.Split(new[] { '.', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                    if (!String.IsNullOrWhiteSpace(last) && stemUnique.TryGetValue(last, out target) && target.Id != f.Id) depTargets.Add(target.Id);
                }
                HashSet<string> typeNames = new HashSet<string>(Identifier.Matches(text).Cast<Match>().Select(m => m.Value), StringComparer.Ordinal);
                foreach (string token in typeNames)
                {
                    List<GenomeSymbol> cands;
                    if (!symbolsByName.TryGetValue(token, out cands) || cands.Count != 1) continue;
                    GenomeSymbol targetSym = cands[0]; if (targetSym.FileId == f.Id) continue;
                    if (targetSym.Kind == "CLASS" || targetSym.Kind == "STRUCT" || targetSym.Kind == "INTERFACE" || targetSym.Kind == "ENUM" || targetSym.Kind == "RECORD") depTargets.Add(targetSym.FileId);
                }
                foreach (string to in depTargets) { if (s.Edges.Count >= options.MaxEdges) { s.EdgeCeilingReached = true; return; } GenomeFile tf = s.Files.FirstOrDefault(x => x.Id == to); AddEdge(s, f.Id, to, f.RelativePath, tf == null ? to : tf.RelativePath, "FILE_DEPENDS_ON", "import/type reference", 0, 0.78); }
                string[] lines = text.Replace("\r", "").Split('\n'); List<GenomeSymbol> inFile = s.Symbols.Where(x => x.FileId == f.Id).OrderBy(x => x.StartLine).ToList();
                for (int li = 0; li < lines.Length; li++)
                {
                    GenomeSymbol caller = FindContaining(inFile, li + 1); if (caller == null) continue;
                    foreach (Match cm in IdentifierCall.Matches(lines[li]))
                    {
                        string name = cm.Groups[1].Value; if (CallStop.Contains(name)) continue; List<GenomeSymbol> candidates;
                        if (!symbolsByName.TryGetValue(name, out candidates) || candidates.Count != 1) continue; GenomeSymbol callee = candidates[0]; if (callee.Id == caller.Id) continue;
                        if (s.Edges.Count >= options.MaxEdges) { s.EdgeCeilingReached = true; return; }
                        AddEdge(s, caller.Id, callee.Id, caller.QualifiedName, callee.QualifiedName, "CALLS", Clip(lines[li].Trim(), 180), li + 1, 0.72);
                    }
                }
            }
        }
        private static GenomeSymbol FindContaining(List<GenomeSymbol> list, int line) { GenomeSymbol best = null; foreach (GenomeSymbol s in list) if (s.StartLine <= line && s.EndLine >= line) { if (best == null || s.StartLine >= best.StartLine) best = s; } return best; }
        private static void AddEdge(GenomeSnapshot s, string from, string to, string fl, string tl, string kind, string evidence, int line, double confidence) { string payload = kind + "|" + from + "|" + to + "|" + line + "|" + evidence; s.Edges.Add(new GenomeEdge { Id = "EDGE-" + HashText(payload).Substring(0, 16).ToUpperInvariant(), FromId = from, ToId = to, FromLabel = fl, ToLabel = tl, Kind = kind, Evidence = evidence, Line = line, Confidence = confidence, CertificateHash = HashText(payload) }); }

        private static void ExtractDataFlows(GenomeSnapshot s, Dictionary<string, string> texts, GenomeScanOptions options)
        {
            int ceiling = Math.Min(options.MaxEdges, 80000);
            foreach (GenomeFile f in s.Files)
            {
                string text; if (!texts.TryGetValue(f.Id, out text)) continue; string[] lines = text.Replace("\r", "").Split('\n'); List<GenomeSymbol> inFile = s.Symbols.Where(x => x.FileId == f.Id).OrderBy(x => x.StartLine).ToList();
                for (int i = 0; i < lines.Length && s.DataFlows.Count < ceiling; i++)
                {
                    foreach (Match m in Assignment.Matches(lines[i]))
                    {
                        string v = m.Groups[1].Value; string c = ClassifyVariable(v, lines[i]); GenomeSymbol owner = FindContaining(inFile, i + 1);
                        s.DataFlows.Add(new GenomeDataFlow { Id = NewId("FLOW"), FileId = f.Id, SymbolId = owner == null ? null : owner.Id, Variable = v, Access = "WRITE", Line = i + 1, Evidence = Clip(lines[i].Trim(), 180), Classification = c });
                    }
                    if (lines[i].IndexOf("return ", StringComparison.OrdinalIgnoreCase) >= 0) { GenomeSymbol owner = FindContaining(inFile, i + 1); s.DataFlows.Add(new GenomeDataFlow { Id = NewId("FLOW"), FileId = f.Id, SymbolId = owner == null ? null : owner.Id, Variable = "return", Access = "EMIT", Line = i + 1, Evidence = Clip(lines[i].Trim(), 180), Classification = "OUTPUT" }); }
                }
            }
        }
        private static string ClassifyVariable(string v, string line) { if (v.Contains(".")) return "MEMBER_STATE"; string l = line.ToLowerInvariant(); if (l.Contains("config") || l.Contains("setting")) return "CONFIG"; if (l.Contains("state") || l.Contains("store") || l.Contains("cache")) return "PERSISTENT_OR_SHARED"; return "LOCAL_OR_SCOPED"; }

        private static void BuildCycles(GenomeSnapshot s)
        {
            Dictionary<string, List<string>> g = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (GenomeFile f in s.Files) g[f.Id] = new List<string>();
            foreach (GenomeEdge e in s.Edges.Where(x => x.Kind == "FILE_DEPENDS_ON")) { List<string> a; if (g.TryGetValue(e.FromId, out a) && g.ContainsKey(e.ToId)) a.Add(e.ToId); }
            Dictionary<string, int> index = new Dictionary<string, int>(); Dictionary<string, int> low = new Dictionary<string, int>(); HashSet<string> on = new HashSet<string>(); Stack<string> st = new Stack<string>(); int next = 0;
            Action<string> visit = null; visit = delegate(string v) { index[v] = next; low[v] = next; next++; st.Push(v); on.Add(v); foreach (string w in g[v].Distinct()) { if (!index.ContainsKey(w)) { visit(w); low[v] = Math.Min(low[v], low[w]); } else if (on.Contains(w)) low[v] = Math.Min(low[v], index[w]); } if (low[v] == index[v]) { List<string> comp = new List<string>(); string w; do { w = st.Pop(); on.Remove(w); comp.Add(w); } while (w != v); if (comp.Count > 1) { List<string> labels = comp.Select(id => s.Files.First(x => x.Id == id).RelativePath).OrderBy(x => x).ToList(); int edges = s.Edges.Count(e => e.Kind == "FILE_DEPENDS_ON" && comp.Contains(e.FromId) && comp.Contains(e.ToId)); s.Cycles.Add(new GenomeCycle { Id = NewId("SCC"), Members = labels, InternalEdges = edges, CertificateHash = HashText(String.Join("|", labels.ToArray())) }); } } };
            foreach (string v in g.Keys.OrderBy(x => x)) if (!index.ContainsKey(v)) visit(v);
        }

        private static void BuildFindings(GenomeSnapshot s, Dictionary<string, string> texts, GenomeScanOptions options)
        {
            foreach (GenomeFile f in s.Files)
            {
                if (s.Findings.Count >= options.MaxFindings) break;
                if (f.ComplexityProxy >= 180) Finding(s, "WARN", "FILE_COMPLEXITY", f.Id, f.RelativePath, "Very high branch/decision proxy " + f.ComplexityProxy + ".", "lexical complexity proxy");
                if (f.MutationSites >= 80) Finding(s, "WARN", "MUTATION_DENSITY", f.Id, f.RelativePath, f.MutationSites + " assignment/mutation sites.", "assignment scanner");
                if (f.ExternalBoundarySites > 0) Finding(s, "INFO", "EXTERNAL_BOUNDARY", f.Id, f.RelativePath, f.ExternalBoundarySites + " process/network/filesystem/database boundary signatures.", "fixed lexical boundary vocabulary");
            }
            foreach (GenomeCycle c in s.Cycles.Take(512)) Finding(s, "WARN", "DEPENDENCY_CYCLE", c.Id, String.Join(" → ", c.Members.Take(4).ToArray()), c.Members.Count + " files form a dependency SCC.", c.CertificateHash);
            foreach (var grp in s.Symbols.Where(x => x.Lines >= 5).GroupBy(x => x.BodyFingerprint).Where(g => g.Count() > 1).Take(512)) { string names = String.Join(" | ", grp.Take(4).Select(x => x.QualifiedName).ToArray()); Finding(s, "INFO", "EXACT_BODY_CLONE", grp.Key, names, grp.Count() + " symbols normalize to the same body fingerprint.", grp.Key); }
            foreach (GenomeSymbol sym in s.Symbols.Where(x => x.ComplexityProxy >= 45).OrderByDescending(x => x.ComplexityProxy).Take(1000)) Finding(s, sym.ComplexityProxy >= 90 ? "WARN" : "INFO", "SYMBOL_COMPLEXITY", sym.Id, sym.QualifiedName, "Complexity proxy " + sym.ComplexityProxy + " across " + sym.Lines + " lines.", sym.BodyFingerprint);
        }
        private static void Finding(GenomeSnapshot s, string sev, string kind, string id, string subject, string message, string evidence) { string p = sev + "|" + kind + "|" + id + "|" + message + "|" + evidence; s.Findings.Add(new GenomeFinding { Id = "FIND-" + HashText(p).Substring(0, 16).ToUpperInvariant(), Severity = sev, Kind = kind, SubjectId = id, Subject = subject, Message = message, Evidence = evidence, CertificateHash = HashText(p) }); }

        private static void CalculateMetrics(GenomeSnapshot s)
        {
            Dictionary<string, GenomeFile> files = s.Files.ToDictionary(x => x.Id);
            Dictionary<string, GenomeSymbol> syms = s.Symbols.ToDictionary(x => x.Id);
            foreach (GenomeEdge e in s.Edges)
            {
                GenomeFile ff, tf; GenomeSymbol fs, ts;
                if (files.TryGetValue(e.FromId, out ff)) ff.FanOut++; if (files.TryGetValue(e.ToId, out tf)) tf.FanIn++;
                if (syms.TryGetValue(e.FromId, out fs)) fs.FanOut++; if (syms.TryGetValue(e.ToId, out ts)) ts.FanIn++;
            }
            foreach (GenomeDataFlow flow in s.DataFlows) { GenomeSymbol sym; if (flow.SymbolId != null && syms.TryGetValue(flow.SymbolId, out sym)) { if (flow.Access == "WRITE") sym.WriteSites++; else sym.ReadSites++; } }
        }

        public static GenomeImpactReport Impact(GenomeSnapshot s, string subjectId, string changeKind)
        {
            if (s == null) return null; GenomeFile file = s.Files.FirstOrDefault(x => x.Id == subjectId); GenomeSymbol sym = s.Symbols.FirstOrDefault(x => x.Id == subjectId); string seedFile = file != null ? file.Id : sym != null ? sym.FileId : subjectId; string label = file != null ? file.RelativePath : sym != null ? sym.QualifiedName : subjectId;
            HashSet<string> direct = new HashSet<string>(s.Edges.Where(e => e.ToId == subjectId || e.ToId == seedFile).Select(e => e.FromId));
            HashSet<string> seen = new HashSet<string>(); Queue<string> q = new Queue<string>(); q.Enqueue(seedFile); seen.Add(seedFile);
            while (q.Count > 0 && seen.Count < 5000) { string cur = q.Dequeue(); foreach (string parent in s.Edges.Where(e => e.Kind == "FILE_DEPENDS_ON" && e.ToId == cur).Select(e => e.FromId).Distinct()) if (seen.Add(parent)) q.Enqueue(parent); }
            seen.Remove(seedFile); List<GenomeFile> impactedFiles = s.Files.Where(x => seen.Contains(x.Id)).ToList(); int tests = impactedFiles.Count(x => x.IsTest); int cycles = s.Cycles.Count(c => c.Members.Any(p => impactedFiles.Any(f => f.RelativePath == p)) || c.Members.Contains(file == null ? "" : file.RelativePath)); int boundaries = impactedFiles.Sum(x => x.ExternalBoundarySites) + (file == null ? 0 : file.ExternalBoundarySites);
            double risk = Math.Min(100.0, direct.Count * 4.0 + seen.Count * 1.2 + tests * 1.5 + cycles * 8.0 + boundaries * 2.0 + (sym == null ? 0 : sym.ComplexityProxy * 0.25));
            GenomeImpactReport r = new GenomeImpactReport { Id = NewId("IMPACT"), SnapshotId = s.Id, SubjectId = subjectId, Subject = label, ChangeKind = changeKind ?? "MODIFY", DirectDependents = direct.Count, TransitiveDependents = seen.Count, AffectedTests = tests, AffectedCycles = cycles, ExternalBoundaryTouches = boundaries, RiskScore = risk };
            r.Impacted.AddRange(impactedFiles.OrderByDescending(x => x.FanIn + x.FanOut).Take(250).Select(x => x.RelativePath)); if (direct.Count > 0) r.Reasons.Add("Reverse dependency graph reaches " + direct.Count + " direct dependents."); if (cycles > 0) r.Reasons.Add(cycles + " dependency SCC(s) intersect the impact cone."); if (boundaries > 0) r.Reasons.Add(boundaries + " external-boundary sites lie in the impact cone."); if (tests == 0) r.Reasons.Add("No test-shaped files were found in the transitive impact cone."); r.CertificateHash = HashText(r.SnapshotId + "|" + r.SubjectId + "|" + r.ChangeKind + "|" + String.Join("|", r.Impacted.ToArray())); return r;
        }

        public static void HarvestGitHistory(GenomeSnapshot s, int maxCommits)
        {
            if (s == null || String.IsNullOrWhiteSpace(s.RootPath)) throw new InvalidOperationException("No repository root is retained.");
            string gitDir = Path.Combine(s.RootPath, ".git"); if (!Directory.Exists(gitDir) && !File.Exists(gitDir)) throw new InvalidOperationException("No .git metadata is visible under the retained root.");
            ProcessStartInfo psi = new ProcessStartInfo { FileName = "git.exe", UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true, WorkingDirectory = s.RootPath };
            psi.Arguments = "-c core.hooksPath=NUL -C " + Quote(s.RootPath) + " log -n " + Math.Max(1, Math.Min(1000, maxCommits)).ToString(CultureInfo.InvariantCulture) + " --date=unix --pretty=format:@@@%H%x1f%an%x1f%ae%x1f%at --name-only --no-renames -- .";
            Process p = Process.Start(psi); string stdout = p.StandardOutput.ReadToEnd(); string stderr = p.StandardError.ReadToEnd(); p.WaitForExit(); if (p.ExitCode != 0) throw new InvalidOperationException("Read-only git history harvest failed: " + Clip(stderr, 500));
            List<GenomeHistoryCommit> commits = ParseHistory(stdout); s.History = commits; BuildOwnershipAndCoChange(s); foreach (GenomeFile f in s.Files) { List<GenomeOwnership> own = s.Ownership.Where(x => String.Equals(NormalizePath(x.FilePath), NormalizePath(f.RelativePath), StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.Touches).ToList(); f.HistoryTouches = own.Sum(x => x.Touches); f.PrimaryOwner = own.Count == 0 ? null : own[0].Owner; }
        }
        private static string Quote(string x) { return "\"" + (x ?? "").Replace("\"", "\\\"") + "\""; }
        private static List<GenomeHistoryCommit> ParseHistory(string text)
        {
            List<GenomeHistoryCommit> result = new List<GenomeHistoryCommit>(); GenomeHistoryCommit cur = null;
            foreach (string raw in (text ?? "").Replace("\r", "").Split('\n')) { string line = raw.Trim(); if (line.StartsWith("@@@", StringComparison.Ordinal)) { string[] p = line.Substring(3).Split(new[] { '\x1f' }); long unix = 0; if (p.Length > 3) Int64.TryParse(p[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out unix); cur = new GenomeHistoryCommit { Sha = p.Length > 0 ? p[0] : "", Author = p.Length > 1 ? p[1] : "", Email = p.Length > 2 ? p[2] : "", Unix = unix, Utc = unix > 0 ? new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unix).ToString("o", CultureInfo.InvariantCulture) : "" }; result.Add(cur); } else if (cur != null && !String.IsNullOrWhiteSpace(line)) cur.Files.Add(line.Replace('/', Path.DirectorySeparatorChar)); }
            return result;
        }
        private static void BuildOwnershipAndCoChange(GenomeSnapshot s)
        {
            List<GenomeOwnership> owns = new List<GenomeOwnership>();
            foreach (var fg in s.History.SelectMany(c => c.Files.Distinct(StringComparer.OrdinalIgnoreCase).Select(f => new { File = f, Commit = c })).GroupBy(x => NormalizePath(x.File), StringComparer.OrdinalIgnoreCase)) { int total = fg.Count(); foreach (var ag in fg.GroupBy(x => String.IsNullOrWhiteSpace(x.Commit.Author) ? x.Commit.Email : x.Commit.Author, StringComparer.OrdinalIgnoreCase)) owns.Add(new GenomeOwnership { FilePath = fg.First().File, Owner = ag.Key, Touches = ag.Count(), Share = total == 0 ? 0 : (double)ag.Count() / total, Source = "GIT_LOG" }); }
            s.Ownership = owns.OrderBy(x => x.FilePath).ThenByDescending(x => x.Touches).ToList();
            Dictionary<string, int> fileTouches = s.History.SelectMany(x => x.Files.Distinct(StringComparer.OrdinalIgnoreCase)).GroupBy(NormalizePath, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase); Dictionary<string, int> pairs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (GenomeHistoryCommit c in s.History) { string[] files = c.Files.Distinct(StringComparer.OrdinalIgnoreCase).Take(80).Select(NormalizePath).OrderBy(x => x).ToArray(); for (int i = 0; i < files.Length; i++) for (int j = i + 1; j < files.Length; j++) { string k = files[i] + "\x1f" + files[j]; int n; pairs.TryGetValue(k, out n); pairs[k] = n + 1; } }
            s.CoChanges = pairs.OrderByDescending(x => x.Value).Take(5000).Select(kv => { string[] p = kv.Key.Split('\x1f'); int a = fileTouches.ContainsKey(p[0]) ? fileTouches[p[0]] : kv.Value, b = fileTouches.ContainsKey(p[1]) ? fileTouches[p[1]] : kv.Value; return new GenomeCoChange { Left = p[0], Right = p[1], CommitsTogether = kv.Value, Jaccard = (double)kv.Value / Math.Max(1, a + b - kv.Value) }; }).ToList();
        }

        public static List<string> CodeOwners(GenomeSnapshot s)
        {
            List<string> outp = new List<string>(); if (s == null || String.IsNullOrWhiteSpace(s.RootPath)) return outp; string[] candidates = { Path.Combine(s.RootPath, ".github", "CODEOWNERS"), Path.Combine(s.RootPath, "CODEOWNERS"), Path.Combine(s.RootPath, "docs", "CODEOWNERS") }; string path = candidates.FirstOrDefault(File.Exists); if (path == null) return outp; foreach (string raw in File.ReadAllLines(path)) { string line = raw.Trim(); if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue; outp.Add(line); } return outp;
        }

        public static List<string> ArchitectureLayers(GenomeSnapshot s)
        {
            if (s == null) return new List<string>(); Dictionary<string, int> edges = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); foreach (GenomeEdge e in s.Edges.Where(x => x.Kind == "FILE_DEPENDS_ON")) { GenomeFile a = s.Files.FirstOrDefault(x => x.Id == e.FromId), b = s.Files.FirstOrDefault(x => x.Id == e.ToId); if (a == null || b == null) continue; string k = a.Layer + " → " + b.Layer; int n; edges.TryGetValue(k, out n); edges[k] = n + 1; } return edges.OrderByDescending(x => x.Value).Select(x => x.Key + "   ·   " + x.Value + " dependency edges").ToList();
        }
        public static List<string> TestMap(GenomeSnapshot s)
        {
            List<string> result = new List<string>(); if (s == null) return result; foreach (GenomeFile test in s.Files.Where(x => x.IsTest)) { List<string> targets = s.Edges.Where(e => e.Kind == "FILE_DEPENDS_ON" && e.FromId == test.Id).Select(e => s.Files.FirstOrDefault(f => f.Id == e.ToId)).Where(f => f != null && !f.IsTest).Select(f => f.RelativePath).Distinct().Take(12).ToList(); result.Add(test.RelativePath + "   →   " + (targets.Count == 0 ? "no statically resolved production dependency" : String.Join(" | ", targets.ToArray()))); } return result; }
        public static List<string> OrphanCandidates(GenomeSnapshot s) { if (s == null) return new List<string>(); return s.Symbols.Where(x => x.FanIn == 0 && (x.Kind == "METHOD" || x.Kind == "FUNCTION") && !x.Name.StartsWith("Main", StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.Lines).Select(x => x.QualifiedName + "   ·   no resolved callers   ·   " + x.Lines + " lines   ·   heuristic only").ToList(); }
        public static List<string> BoundaryMap(GenomeSnapshot s) { if (s == null) return new List<string>(); return s.Files.Where(x => x.ExternalBoundarySites > 0).OrderByDescending(x => x.ExternalBoundarySites).Select(x => x.RelativePath + "   ·   " + x.ExternalBoundarySites + " boundary sites   ·   " + String.Join(", ", x.Tags.ToArray())).ToList(); }
        public static List<string> MutationMap(GenomeSnapshot s) { if (s == null) return new List<string>(); return s.DataFlows.Where(x => x.Access == "WRITE").OrderBy(x => x.FileId).ThenBy(x => x.Line).Select(x => x.ToString() + "   ·   " + Clip(x.Evidence, 120)).ToList(); }
        public static List<string> Hotspots(GenomeSnapshot s) { if (s == null) return new List<string>(); return s.Files.OrderByDescending(x => x.ComplexityProxy + x.FanIn * 5 + x.FanOut * 3 + x.HistoryTouches * 2 + x.MutationSites).Select(x => x.RelativePath + "   ·   score " + (x.ComplexityProxy + x.FanIn * 5 + x.FanOut * 3 + x.HistoryTouches * 2 + x.MutationSites) + "   ·   cx " + x.ComplexityProxy + "   ·   in/out " + x.FanIn + "/" + x.FanOut + "   ·   history " + x.HistoryTouches + "   ·   writes " + x.MutationSites).ToList(); }

        public static string Fingerprint(GenomeSnapshot s)
        {
            if (s == null) return HashText(""); StringBuilder b = new StringBuilder(); b.Append(s.AnalyzerVersion).Append('|').Append(s.Name).Append('|'); foreach (GenomeFile f in s.Files.OrderBy(x => x.RelativePath, StringComparer.OrdinalIgnoreCase)) b.Append(f.RelativePath).Append(':').Append(f.Sha256).Append(':').Append(f.Lines).Append('|'); foreach (GenomeSymbol sym in s.Symbols.OrderBy(x => x.Id)) b.Append(sym.Id).Append(':').Append(sym.BodyFingerprint).Append('|'); foreach (GenomeEdge e in s.Edges.OrderBy(x => x.Id)) b.Append(e.Id).Append('|'); return HashText(b.ToString());
        }
        private static string NormalizePath(string p) { return (p ?? "").Replace('/', '\\').TrimStart('.','\\').ToLowerInvariant(); }
        private static List<string> Imports(string text, string language)
        {
            List<string> r = new List<string>(); MatchCollection m;
            if (language == "C#") m = Regex.Matches(text ?? "", @"(?m)^\s*using\s+(?:static\s+)?([A-Za-z_][A-Za-z0-9_\.]*)\s*;");
            else if (language == "PYTHON") m = Regex.Matches(text ?? "", @"(?m)^\s*(?:from\s+([A-Za-z0-9_\.]+)\s+import|import\s+([A-Za-z0-9_\.]+))");
            else if (language == "JAVASCRIPT" || language == "TYPESCRIPT") m = Regex.Matches(text ?? "", "(?m)(?:from\\s+|require\\s*\\()\\s*[\'\"]([^\'\"]+)[\'\"]");
            else if (language == "LUA") m = Regex.Matches(text ?? "", "require\\s*\\(?\\s*[\'\"]([^\'\"]+)[\'\"]");
            else if (language == "POWERSHELL") m = Regex.Matches(text ?? "", "(?im)^\\s*(?:Import-Module\\s+|\\.\\s+[\'\"]?)([^\'\"\\s]+)");
            else return r;
            foreach (Match x in m) { string v = ""; for (int i = 1; i < x.Groups.Count; i++) if (x.Groups[i].Success && !String.IsNullOrWhiteSpace(x.Groups[i].Value)) { v = x.Groups[i].Value; break; } if (!String.IsNullOrWhiteSpace(v) && !r.Contains(v, StringComparer.OrdinalIgnoreCase)) r.Add(v); }
            return r.Take(256).ToList();
        }
    }
}
