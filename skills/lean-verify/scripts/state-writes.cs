#:package Microsoft.CodeAnalysis.Workspaces.MSBuild@5.9.0
#:package Microsoft.CodeAnalysis.CSharp.Workspaces@5.9.0
#:property PublishAot=false
#:property Nullable=enable

// Roslyn inventory of every site that can write the given state fields/properties.
// Usage: dotnet run state-writes.cs -- <project.csproj|solution.sln[x]> <Namespace.Type.member>... [--expect cited.json]
// Output (stdout): deterministic JSON. Exit: 0 ok, 2 usage, 3 load/compile errors, 4 symbol not found,
//                  5 zero write sites (not evidence), 6 --expect mismatch (uncited, stale, or drifted).
// Newer SDK compilers (.NET 11 SDKs ship Roslyn 5.11): if exit 3 reports syntax errors in new language
// features, raise both #:package versions to match.
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Operations;

var argv = args.ToList();
string? expectPath = null;
int ei = argv.IndexOf("--expect");
if (ei >= 0) { if (ei + 1 >= argv.Count) return Usage(); expectPath = argv[ei + 1]; argv.RemoveRange(ei, 2); }
if (argv.Count < 2) return Usage();
string target = Path.GetFullPath(argv[0]);
string root = Path.GetDirectoryName(target)!;
var wanted = argv.Skip(1).ToList();

using var ws = MSBuildWorkspace.Create();
var loadFailures = new List<string>();
ws.RegisterWorkspaceFailedHandler(e => { if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure) loadFailures.Add(e.Diagnostic.Message); });
Solution solution = target.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
    ? (await ws.OpenProjectAsync(target)).Solution
    : await ws.OpenSolutionAsync(target);

// Evidence gate: a semantic model over a broken compilation resolves to error symbols.
var errors = new List<string>(loadFailures);
foreach (var p in solution.Projects)
{
    var c = await p.GetCompilationAsync();
    if (c is null) { errors.Add($"{p.Name}: no compilation"); continue; }
    errors.AddRange(c.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => $"{p.Name}: {d}"));
}
if (errors.Count > 0) { Console.Error.WriteLine("LOAD/COMPILE ERRORS (inventory would not be evidence):"); errors.Take(20).ToList().ForEach(Console.Error.WriteLine); return 3; }

var symbols = new List<ISymbol>();
foreach (var w in wanted)
{
    int dot = w.LastIndexOf('.');
    if (dot <= 0) { Console.Error.WriteLine($"Not Type.member: {w}"); return 2; }
    string typeName = w[..dot], member = w[(dot + 1)..];
    ISymbol? found = null;
    foreach (var p in solution.Projects)
    {
        var t = (await p.GetCompilationAsync())!.GetTypeByMetadataName(typeName);
        found = t?.GetMembers(member).FirstOrDefault(m => m is IFieldSymbol or IPropertySymbol);
        if (found is not null) break;
    }
    if (found is null) { Console.Error.WriteLine($"Symbol not found: {w}"); return 4; }
    symbols.Add(found);
}

var sites = new List<Site>();
foreach (var sym in symbols)
{
    string symName = sym.ToDisplayString();
    // Declaration initializers are writes too.
    foreach (var decl in sym.DeclaringSyntaxReferences.Select(r => r.GetSyntax()))
    {
        ExpressionSyntax? init = decl switch
        {
            VariableDeclaratorSyntax v => v.Initializer?.Value,
            PropertyDeclarationSyntax pd => pd.Initializer?.Value,
            _ => null
        };
        if (init is not null) sites.Add(MakeSite(symName, "initializer", decl, sym));
    }
    var refs = await SymbolFinder.FindReferencesAsync(sym, solution);
    foreach (var loc in refs.SelectMany(r => r.Locations))
    {
        var tree = loc.Location.SourceTree!;
        var model = await loc.Document.GetSemanticModelAsync();
        var node = (await tree.GetRootAsync()).FindNode(loc.Location.SourceSpan, getInnermostNodeForTie: true);
        IOperation? op = null;
        for (var n = node; n is not null && op is null; n = n.Parent) op = model!.GetOperation(n);
        while (op is not null && op is not (IFieldReferenceOperation or IPropertyReferenceOperation)) op = op.Parent;
        if (op is null) continue;
        string? kind = Classify(op);
        if (kind is not null) sites.Add(MakeSite(symName, kind, op.Syntax, null));
    }
}

// Stable ids: member doc-id | symbol | kind | ordinal within member (line numbers are for humans only).
var ordered = sites.OrderBy(s => s.file, StringComparer.Ordinal).ThenBy(s => s.line).ThenBy(s => s.column).ToList();
var withIds = ordered
    .GroupBy(s => (s.member, s.symbol, s.kind))
    .SelectMany(g => g.Select((s, i) => s with { id = $"{s.member}|{s.symbol}|{s.kind}|{i + 1}" }))
    .OrderBy(s => s.file, StringComparer.Ordinal).ThenBy(s => s.line).ThenBy(s => s.column).ToList();

var json = new JsonSerializerOptions { WriteIndented = true };
Console.WriteLine(JsonSerializer.Serialize(new { target = Path.GetFileName(target), symbols = symbols.Select(s => s.ToDisplayString()), count = withIds.Count, sites = withIds }, json));
if (withIds.Count == 0) { Console.Error.WriteLine("Zero write sites found: not evidence of immutability; check symbol names and project scope."); return 5; }

if (expectPath is not null)
{
    var cited = JsonSerializer.Deserialize<List<Cited>>(File.ReadAllText(expectPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    var byId = withIds.ToDictionary(s => s.id);
    var uncited = withIds.Where(s => !cited.Any(c => c.Id == s.id)).Select(s => s.id).ToList();
    var stale = cited.Where(c => !byId.ContainsKey(c.Id)).Select(c => c.Id).ToList();
    var drifted = cited.Where(c => byId.TryGetValue(c.Id, out var s) && c.MemberHash is not null && c.MemberHash != s.memberHash).Select(c => c.Id).ToList();
    foreach (var (label, list) in new[] { ("UNCITED", uncited), ("STALE", stale), ("DRIFTED", drifted) })
        foreach (var id in list) Console.Error.WriteLine($"{label} {id}");
    if (uncited.Count + stale.Count + drifted.Count > 0) return 6;
    Console.Error.WriteLine($"OK: all {withIds.Count} write sites cited, none stale or drifted.");
}
return 0;

static string? Classify(IOperation reference)
{
    var parent = reference.Parent;
    return parent switch
    {
        ISimpleAssignmentOperation a when a.Target == reference => a.IsRef ? "ref-assign" : "assign",
        ICompoundAssignmentOperation c when c.Target == reference => "compound",
        ICoalesceAssignmentOperation c when c.Target == reference => "coalesce-assign",
        IIncrementOrDecrementOperation => "increment",
        IArgumentOperation { Parameter.RefKind: RefKind.Out } arg => $"out-arg:{Callee(arg)}",
        IArgumentOperation { Parameter.RefKind: RefKind.Ref } arg => $"ref-arg:{Callee(arg)}",
        IVariableInitializerOperation when reference.Syntax.Parent is RefExpressionSyntax => "ref-alias",
        IReturnOperation when reference.Syntax.Parent is RefExpressionSyntax => "ref-return",
        ITupleOperation t when t.Parent is IDeconstructionAssignmentOperation d && IsWithin(d.Target, reference) => "deconstruct",
        _ => reference.Parent is IAddressOfOperation ? "address-of" : null
    };
}
static bool IsWithin(IOperation root, IOperation op) { for (var o = op; o is not null; o = o.Parent) if (o == root) return true; return false; }
static string Callee(IArgumentOperation arg) => arg.Parent is IInvocationOperation inv ? $"{inv.TargetMethod.ContainingType.Name}.{inv.TargetMethod.Name}" : "?";

Site MakeSite(string symbol, string kind, SyntaxNode node, ISymbol? declared)
{
    var span = node.GetLocation().GetLineSpan();
    var memberNode = node.AncestorsAndSelf().FirstOrDefault(n => n is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or BasePropertyDeclarationSyntax or FieldDeclarationSyntax or LocalFunctionStatementSyntax)
                     ?? node;
    var model = solution.GetDocument(node.SyntaxTree)?.GetSemanticModelAsync().Result;
    var memberSym = declared ?? model?.GetDeclaredSymbol(memberNode is FieldDeclarationSyntax f ? f.Declaration.Variables[0] : memberNode);
    if (memberSym is IMethodSymbol { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet } acc) memberSym = acc;
    string member = memberSym?.GetDocumentationCommentId() ?? memberSym?.ToDisplayString() ?? "?";
    string tokens = string.Join(" ", memberNode.DescendantTokens().Select(t => t.Text));
    string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tokens)))[..16].ToLowerInvariant();
    string file = Path.GetRelativePath(root, span.Path).Replace('\\', '/');
    string text = node.ToString().ReplaceLineEndings(" ");
    return new Site("", symbol, kind, file, span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1, member, hash, text.Length > 120 ? text[..120] : text);
}

static int Usage() { Console.Error.WriteLine("usage: dotnet run state-writes.cs -- <project|solution> <Namespace.Type.member>... [--expect cited.json]"); return 2; }

record Site(string id, string symbol, string kind, string file, int line, int column, string member, string memberHash, string text);
record Cited(string Id, string? MemberHash);
