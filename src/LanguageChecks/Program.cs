using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.Json;
using HarmonyLib;
using Brutal.ImGuiApi;
using System.Runtime.InteropServices;
using System.Text;

namespace LanguageChecks;

internal static class Program
{
    private static readonly string Root = Path.GetFullPath(Environment.GetEnvironmentVariable("KSA_PROJECT_ROOT")
        ?? Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
    private static readonly string GameDir = Path.GetFullPath(Environment.GetEnvironmentVariable("KSA_GAME_DIR")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Kitten Space Agency"));
    private static readonly string LoaderDir = Path.GetFullPath(Environment.GetEnvironmentVariable("KSA_LOADER_DIR")
        ?? Path.Combine(Root, "tools", "StarMap-0.4.7"));
    public static void Main()
    {
        var directories = new[] {
            Path.Combine(Root, @"work\build\KsaUiLanguages"),
            LoaderDir,
            GameDir
        };
        AssemblyLoadContext.Default.Resolving += (_, name) => {
            foreach (var directory in directories) {
                var file = Path.Combine(directory, name.Name + ".dll");
                if (File.Exists(file)) return AssemblyLoadContext.Default.LoadFromAssemblyPath(file);
            }
            return null;
        };
        NativeLibrary.SetDllImportResolver(typeof(Program).Assembly, (name, assembly, searchPath) =>
            name == "KsaCheckImGui" ? NativeLibrary.Load(Path.Combine(GameDir, "imgui.dll")) : IntPtr.Zero);
        Directory.CreateDirectory(Path.Combine(Root, "work", "run-logs"));
        RunChecks();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunChecks()
    {
        var results = new List<string>();
        var harmony = new Harmony("org.ksa.uilanguages.headlesschecks");
        harmony.Patch(AccessTools.PropertyGetter(typeof(KSA.Constants), nameof(KSA.Constants.DocumentsFolderPath)),
            prefix: new HarmonyMethod(typeof(Program), nameof(UseCheckDirectory)));
        KsaUiLanguages.Runtime.Initialize();
        Require(KSA.LStrings.SettingsDisplayResolution.Localized == "分辨率", "Chinese native resource updates the actual game LString");
        results.Add("PASS: Chinese native text");
        Require(KsaUiLanguages.Runtime.Literal("SETTINGS") == "设置", "Chinese hardcoded UI text");
        Require(KsaUiLanguages.Runtime.Literal("Settings") == "Settings", "UI scope ID remains unchanged");
        Require(KsaUiLanguages.Runtime.Literal("SettingsDisplayModeCombo") == "SettingsDisplayModeCombo", "Control ID remains unchanged");
        results.Add("PASS: UI literals and machine IDs");
        ImString menuLabel = "File"u8;
        KsaUiLanguages.MenuTextPatch.LocalizeMenuLabel(ref menuLabel);
        Require(menuLabel.ToString() == "文件###File", "UTF-8 menu label preserves the original widget ID");
        Require(KsaUiLanguages.Runtime.MenuText("File##menu-main") == "文件###File##menu-main", "Original full ID is preserved for ## labels");
        var originals = new[] { "File", "File##menu-main", "File###menu-main", "High##quality-high", "Resources###hud-resources" };
        var chineseLabels = originals.ToDictionary(original => original, KsaUiLanguages.Runtime.MenuText);
        KsaUiLanguages.Runtime.ApplyLanguage("en-US");
        foreach (var original in originals)
            foreach (uint seed in new uint[] { 0, 0x735A1913 })
            {
                Require(HashGuiId(KsaUiLanguages.Runtime.MenuText(original), seed) == HashGuiId(chineseLabels[original], seed), "Actual ImGui ID remains stable across locales: " + original);
                if (original.Contains("###")) Require(HashGuiId(original, seed) == HashGuiId(chineseLabels[original], seed), "Existing explicit ImGui ID is retained: " + original);
            }
        KsaUiLanguages.Runtime.ApplyLanguage("zh-CN");
        ImString dynamicObjectName = "View";
        var staticMenuMethod = AccessTools.Method(typeof(KSA.Program), "DrawMenuBar");
        var stringConversion = typeof(ImString).GetMethods().Single(method => method.Name == "op_Implicit" && method.GetParameters()[0].ParameterType == typeof(string));
        var dynamicInstructions = new[] { new CodeInstruction(System.Reflection.Emit.OpCodes.Call, stringConversion) };
        Require(KsaUiLanguages.MenuTextPatch.TranslateStaticMenus(dynamicInstructions, staticMenuMethod).Count() == 1,
            "Dynamic object-name conversion receives no translation instruction");
        results.Add("PASS: Actual ImGui hashes and unmodified dynamic object names");
        Require(KsaUiLanguages.Runtime.MenuText("RCS") == "RCS", "RCS abbreviation is retained");
        Require(KsaUiLanguages.Runtime.MenuText("TWR") == "TWR", "TWR abbreviation is retained");
        Require(KsaUiLanguages.Runtime.MenuText("Δv") == "Δv", "Delta-V notation is retained");
        results.Add("PASS: UTF-8 menu translation and retained simulator terminology");
        ReadOnlySpan<char> preview = "Windowed".AsSpan();
        KsaUiLanguages.ComboPreviewPatch.LocalizePreview("SettingsDisplayModeCombo", ref preview);
        Require(preview.ToString() == "窗口", "Display-mode preview is translated");
        ImString option = "High##quality-high";
        KsaUiLanguages.Runtime.InSettingsCombo = false;
        KsaUiLanguages.ComboOptionPatch.LocalizeOption(ref option);
        Require(option.ToString() == "High##quality-high", "Non-settings selectable is unchanged");
        KsaUiLanguages.Runtime.InSettingsCombo = true;
        KsaUiLanguages.ComboOptionPatch.LocalizeOption(ref option);
        Require(option.ToString() == "高###High##quality-high", "Quality option preserves its full original ID");
        ReadOnlySpan<char> buttonCaption = "CLOSE";
        ReadOnlySpan<char> buttonId = buttonCaption;
        KsaUiLanguages.ButtonCaptionPatch.LocalizeCaption(ref buttonCaption);
        Require(buttonCaption.ToString() == "关闭" && buttonId.ToString() == "CLOSE", "Caption changes without changing the button ID");
        results.Add("PASS: Custom button caption and ID are independent");
        KsaUiLanguages.Runtime.InSettingsCombo = false;
        results.Add("PASS: Settings-only dropdown translation and stable option IDs");
        ReadOnlySpan<char> tooltip = "Toggles the RCS thrusters.".AsSpan();
        KsaUiLanguages.ConsoleTooltipPatch.LocalizeTooltip(ref tooltip);
        Require(tooltip.ToString().Contains("姿态控制推进器"), "Gauge tooltip can be explained in Chinese");
        results.Add("PASS: Chinese tooltip without changing the RCS label");
        KsaUiLanguages.Runtime.ApplyLanguage("en-US");
        Require(KSA.LStrings.SettingsDisplayResolution.Localized == KSA.LStrings.SettingsDisplayResolution.Template,
            "English restores the actual game template");
        Require(KsaUiLanguages.Runtime.Literal("SETTINGS") == "SETTINGS", "English UI literal fallback");
        Require(KsaUiLanguages.Runtime.MenuText("File") == "File###File", "English menu fallback retains the explicit stable ID");
        Require(KsaUiLanguages.Runtime.TooltipText("Toggles the RCS thrusters.") == "Toggles the RCS thrusters.", "English tooltip fallback");
        results.Add("PASS: English fallback");
        KsaUiLanguages.Runtime.ApplyLanguage("zh-CN");
        Require(KSA.LStrings.SettingsDisplayResolution.Localized == "分辨率", "Chinese can be restored");
        Require(KSA.LStrings.AlertTargetSet.Localized.Contains("{Target}"), "Named placeholder is retained");
        results.Add("PASS: Round trip and named placeholder");
        var invalid = new KsaUiLanguages.LanguagePack { Locale = "invalid" };
        invalid.Native.Add("AlertTargetSet", "Bad {Wrong}");
        KsaUiLanguages.Runtime.Packs.Add("invalid", invalid);
        var rejected = false;
        try { KsaUiLanguages.Runtime.ApplyLanguage("invalid"); }
        catch (InvalidDataException) { rejected = true; }
        Require(rejected && KsaUiLanguages.Runtime.SelectedLocale == "zh-CN", "Invalid placeholders are rejected before switching");
        results.Add("PASS: Invalid translation is rejected atomically");
        KsaUiLanguages.Runtime.RestoreProvider();
        Require(KSA.LStrings.SettingsDisplayResolution.Localized == KSA.LStrings.SettingsDisplayResolution.Template,
            "Removing the provider restores English");
        results.Add("PASS: Provider removal restores English");
        harmony.UnpatchAll("org.ksa.uilanguages.headlesschecks");
        var report = new { gameVersion = typeof(KSA.Constants).Assembly.GetName().Version?.ToString(),
            results, scope = "Actual game localization objects; no GUI or button-operation validation" };
        File.WriteAllText(Path.Combine(Root, @"work\run-logs\language-checks.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(JsonSerializer.Serialize(report));
    }

    public static bool UseCheckDirectory(ref string __result)
    {
        __result = Path.Combine(Root, @"work\headless-language-checks");
        return false;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("FAILED: " + message);
    }

    [DllImport("KsaCheckImGui", EntryPoint="?ImHashStr@@YAIPEBD_KI@Z", ExactSpelling=true, CallingConvention=CallingConvention.Cdecl)]
    private static extern unsafe uint NativeHash(byte* data, nuint size, uint seed);

    private static unsafe uint HashGuiId(string text, uint seed)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text + '\0');
        fixed (byte* pointer = bytes) return NativeHash(pointer, (nuint)(bytes.Length - 1), seed);
    }
}
