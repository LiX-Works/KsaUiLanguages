using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;
using HarmonyLib;

namespace CompatibilityProbe;

internal sealed class TargetRecord
{
    public int Index { get; set; }
    public string? Signature { get; set; }
    public string? NormalizedIlSha256 { get; set; }
    public object? Body { get; set; }
    public string? Error { get; set; }
}
internal sealed class GroupRecord
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "pending";
    public string? Selector { get; set; }
    public string Installation { get; set; } = "not-attempted-resolve-only";
    public List<TargetRecord> Targets { get; set; } = [];
    public List<string> Errors { get; set; } = [];
}
internal sealed class Report
{
    public int SchemaVersion { get; set; } = 1;
    public string FingerprintFormat { get; set; } = "normalized-il-v1";
    public string Mode { get; set; } = "resolve-only";
    public string Verdict { get; set; } = "mechanical-precheck-incomplete";
    public string Limitation { get; set; } = "Target resolution and original IL comparison do not prove patch installation, transpiler match counts, runtime compatibility, UI coverage or player data safety. No plugin entry point, Runtime.Initialize or game entry point is called. Target selectors are executed in an isolated process after DocumentsFolderPath redirection.";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public object? Game { get; set; }
    public object? Plugin { get; set; }
    public object? Loader { get; set; }
    public string? FixtureDirectory { get; set; }
    public bool DocumentsRedirectionVerified { get; set; }
    public List<GroupRecord> Groups { get; set; } = [];
    public List<string> Errors { get; set; } = [];
    public List<object> LoadedAssemblies { get; set; } = [];
    public object? Summary { get; set; }
    public object? Comparison { get; set; }
}
internal static class Program
{
    private static string Fixture = "";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public static int Main(string[] args)
    {
        string? output = null;
        var report = new Report();
        try
        {
            var options = Parse(args);
            output = Path.GetFullPath(options["report"]);
            if (File.Exists(output)) throw new IOException("Report already exists; choose a new output path: " + output);
            string gameDir = Path.GetFullPath(options["game-dir"]);
            string pluginDir = Path.GetFullPath(options["plugin-dir"]);
            string loaderDir = Path.GetFullPath(options["loader-dir"]);
            Fixture = Path.GetFullPath(options["fixture-dir"]);
            if (Directory.Exists(Fixture)) throw new IOException("Fixture already exists; choose a new fixture path.");
            Directory.CreateDirectory(Fixture);
            report.FixtureDirectory = Fixture;
            report.Game = FileIdentity(Path.Combine(gameDir, "KSA.dll"));
            report.Plugin = FileIdentity(Path.Combine(pluginDir, "KsaUiLanguages.dll"));
            report.Loader = FileIdentity(Path.Combine(loaderDir, "0Harmony.dll"));
            // Candidate dependencies precede loader dependencies; plugin dependencies are last.
            // Explicitly bind Harmony from LoaderDir before entering any Harmony-referencing method.
            AssemblyLoadContext.Default.Resolving += (_, name) =>
            {
                foreach (string dir in new[] { gameDir, loaderDir, pluginDir })
                {
                    string path = Path.Combine(dir, name.Name + ".dll");
                    if (File.Exists(path)) return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
                }
                return null;
            };
            AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(loaderDir, "0Harmony.dll"));
            Probe(gameDir, pluginDir, report);
            if (options.TryGetValue("baseline-report", out string? baseline))
                report.Comparison = Compare(report, Path.GetFullPath(baseline));
        }
        catch (Exception error) { report.Errors.Add(Describe(error)); }
        report.Summary = new {
            groups = report.Groups.Count,
            failedGroups = report.Groups.Count(g => g.Status != "resolved"),
            targetReferences = report.Groups.Sum(g => g.Targets.Count),
            uniqueTargets = report.Groups.SelectMany(g => g.Targets).Where(t => t.Signature != null).Select(t => t.Signature).Distinct().Count(),
            fingerprintErrors = report.Groups.SelectMany(g => g.Targets).Count(t => t.Error != null)
        };
        bool passed = report.Errors.Count == 0 && report.DocumentsRedirectionVerified && report.Groups.Count > 0
            && report.Groups.All(g => g.Status == "resolved");
        report.Verdict = passed ? "mechanical-precheck-passed-runtime-unverified" : "mechanical-precheck-failed";
        // Refuse overwrites even after a race with another run.
        if (output != null && !File.Exists(output))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            JsonSerializer.Serialize(stream, report, JsonOptions);
        }
        Console.WriteLine(JsonSerializer.Serialize(new { report = output, report.Verdict, report.Summary, report.Errors }));
        return passed ? 0 : 2;
    }

    private static Dictionary<string, string> Parse(string[] args)
    {
        var allowed = new[] { "game-dir", "plugin-dir", "loader-dir", "report", "fixture-dir", "baseline-report" };
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int i = 0; i < args.Length; i += 2)
        {
            if (i + 1 == args.Length || !args[i].StartsWith("--") || !allowed.Contains(args[i][2..]))
                throw new ArgumentException("Arguments must be explicit --name value pairs.");
            result.Add(args[i][2..], args[i + 1]);
        }
        foreach (string required in allowed[..5])
            if (!result.ContainsKey(required)) throw new ArgumentException("Missing --" + required);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Probe(string gameDir, string pluginDir, Report report)
    {
        var game = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(gameDir, "KSA.dll"));
        // No candidate method or selector is executed until this reflection-resolved getter is redirected.
        var constants = game.GetType("KSA.Constants", throwOnError: true)!;
        var getter = constants.GetProperty("DocumentsFolderPath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetMethod
            ?? throw new MissingMemberException("Cannot isolate candidate KSA.Constants.DocumentsFolderPath; aborting.");
        var isolation = new Harmony("org.ksa.uilanguages.compatibilityprobe.isolation");
        isolation.Patch(getter, prefix: new HarmonyMethod(typeof(Program), nameof(UseFixture)));
        string? actual = getter.Invoke(null, null) as string;
        if (actual != Fixture) throw new InvalidOperationException("DocumentsFolderPath isolation verification failed; aborting.");
        report.DocumentsRedirectionVerified = true;
        try
        {
            var plugin = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(pluginDir, "KsaUiLanguages.dll"));
            Type[] types;
            try { types = plugin.GetTypes(); }
            catch (ReflectionTypeLoadException error)
            {
                report.Errors.AddRange(error.LoaderExceptions.OfType<Exception>().Select(Describe));
                types = error.Types.OfType<Type>().ToArray();
            }
            foreach (var type in types.OrderBy(t => t.FullName, StringComparer.Ordinal))
            {
                // CustomAttributeData identifies patch classes even if an attribute type cannot load.
                if (!type.GetCustomAttributesData().Any(a => a.AttributeType.FullName == typeof(HarmonyPatch).FullName)) continue;
                var group = new GroupRecord { Name = type.FullName! };
                report.Groups.Add(group);
                try
                {
                    var selector = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                        .Where(m => m.Name is "TargetMethods" or "TargetMethod" || m.GetCustomAttributesData()
                            .Any(a => a.AttributeType.FullName is "HarmonyLib.HarmonyTargetMethods" or "HarmonyLib.HarmonyTargetMethod"))
                        .SingleOrDefault();
                    IEnumerable targets;
                    if (selector != null)
                    {
                        group.Selector = IlFingerprint.MemberSignature(selector);
                        if (selector.GetParameters().Length != 0) throw new NotSupportedException("Selector parameters are unsupported; no target guessed.");
                        var value = selector.Invoke(null, null);
                        targets = value is MethodBase method ? new[] { method }
                            : value as IEnumerable ?? throw new MissingMethodException("Target selector returned null or unsupported result.");
                    }
                    else
                    {
                        group.Selector = "HarmonyPatch attributes";
                        var info = HarmonyMethod.Merge(type.GetCustomAttributes<HarmonyPatch>().Select(a => a.info).ToList());
                        if (info.declaringType == null) throw new MissingMemberException("Harmony target declaringType missing.");
                        MethodBase? target = info.methodType switch {
                            MethodType.Constructor => AccessTools.Constructor(info.declaringType, info.argumentTypes),
                            MethodType.StaticConstructor => info.declaringType.TypeInitializer,
                            MethodType.Getter => AccessTools.PropertyGetter(info.declaringType, info.methodName),
                            MethodType.Setter => AccessTools.PropertySetter(info.declaringType, info.methodName),
                            null or MethodType.Normal => AccessTools.Method(info.declaringType, info.methodName, info.argumentTypes),
                            _ => throw new NotSupportedException("Unsupported Harmony method type: " + info.methodType)
                        };
                        targets = new[] { target };
                    }
                    int index = 0;
                    // Enumerate inside the per-class try so a later iterator failure retains earlier targets.
                    foreach (object? value in targets)
                    {
                        var target = new TargetRecord { Index = index++ };
                        group.Targets.Add(target);
                        try
                        {
                            if (value is not MethodBase method) throw new MissingMethodException("Null or invalid resolved target at selector index " + target.Index);
                            target.Signature = IlFingerprint.MemberSignature(method);
                            var body = IlFingerprint.Read(method);
                            target.Body = body;
                            target.NormalizedIlSha256 = Sha(JsonSerializer.SerializeToUtf8Bytes(body));
                        }
                        catch (Exception error) { target.Error = Describe(error); }
                    }
                    if (group.Targets.Count == 0) group.Errors.Add("Selector resolved zero targets.");
                }
                catch (Exception error) { group.Errors.Add(Describe(error)); }
                group.Status = group.Errors.Count == 0 && group.Targets.All(t => t.Error == null) ? "resolved" : "failed";
            }
        }
        finally
        {
            report.LoadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .OrderBy(a => a.GetName().Name).Select(a => FileIdentity(a.Location)).ToList();
            // Isolation lasts until the process exits. Never invoke a candidate shutdown or unload hook.
        }
    }
    public static bool UseFixture(ref string __result) { __result = Fixture; return false; }
    internal static string Sha(byte[] data) => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
    private static object FileIdentity(string path) => new {
        path = Path.GetFullPath(path),
        assemblyVersion = AssemblyName.GetAssemblyName(path).Version?.ToString(),
        fileVersion = FileVersionInfo.GetVersionInfo(path).FileVersion,
        sha256 = Sha(File.ReadAllBytes(path))
    };
    internal static string Describe(Exception error)
    {
        while (error is TargetInvocationException { InnerException: not null }) error = error.InnerException;
        return error.ToString();
    }
    private static object Compare(Report candidate, string baselinePath)
    {
        using var baseline = JsonDocument.Parse(File.ReadAllText(baselinePath));
        if (baseline.RootElement.GetProperty("SchemaVersion").GetInt32() != candidate.SchemaVersion
            || baseline.RootElement.GetProperty("FingerprintFormat").GetString() != candidate.FingerprintFormat
            || baseline.RootElement.GetProperty("Verdict").GetString() != "mechanical-precheck-passed-runtime-unverified")
            throw new InvalidDataException("Baseline must be a successful precheck with the same schema and fingerprint format.");
        var previous = baseline.RootElement.GetProperty("Groups").EnumerateArray()
            .SelectMany(g => g.GetProperty("Targets").EnumerateArray().Where(t => t.GetProperty("Signature").ValueKind == JsonValueKind.String)
                .Select(t => (Key: g.GetProperty("Name").GetString() + "|" + t.GetProperty("Signature").GetString(), Hash: t.GetProperty("NormalizedIlSha256").GetString())))
            .GroupBy(t => t.Key).ToDictionary(g => g.Key, g => g.First().Hash);
        var current = candidate.Groups.SelectMany(g => g.Targets.Where(t => t.Signature != null)
            .Select(t => (Key: g.Name + "|" + t.Signature, Hash: t.NormalizedIlSha256)))
            .GroupBy(t => t.Key).ToDictionary(g => g.Key, g => g.First().Hash);
        return new {
            baselinePath, baselineSha256 = Sha(File.ReadAllBytes(baselinePath)),
            interpretation = "Missing includes targets unavailable due to selector failures. New targets can reflect plugin or game changes. Changes require review; equality does not establish compatibility.",
            baselinePlugin = baseline.RootElement.GetProperty("Plugin").Clone(), candidatePlugin = candidate.Plugin,
            changed = current.Keys.Intersect(previous.Keys).Where(k => current[k] != previous[k]).Order().ToArray(),
            missing = previous.Keys.Except(current.Keys).Order().ToArray(),
            @new = current.Keys.Except(previous.Keys).Order().ToArray(),
            unchanged = current.Keys.Intersect(previous.Keys).Count(k => current[k] == previous[k])
        };
    }
}
