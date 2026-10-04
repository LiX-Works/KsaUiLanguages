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
            Environment.GetEnvironmentVariable("KSA_PLUGIN_DIR") ?? Path.Combine(Root, @"work\build\KsaUiLanguages"),
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
        if (Environment.GetEnvironmentVariable("KSA_DIAGNOSTICS") == "1")
        {
            foreach (var m in typeof(ImGui).GetMethods().Where(m => m.Name is "BeginCombo" or "Selectable"))
                Console.WriteLine("GUI-SIGNATURE: " + m);
            var comboDefinition = typeof(KSA.ImGuiHelper).GetMethods().First(m => m.Name == "DrawCombo" && m.IsGenericMethodDefinition
                && m.GetGenericArguments()[0].GetGenericParameterConstraints().Contains(typeof(KSA.IComboable))
                && m.GetParameters()[1].ParameterType == m.GetGenericArguments()[0].MakeByRefType());
            foreach (var i in PatchProcessor.GetOriginalInstructions(comboDefinition.MakeGenericMethod(typeof(KSA.CelestialObject))))
                if (i.operand is MethodInfo m && m.DeclaringType == typeof(ImGui)) Console.WriteLine("COMBO-CALL: " + m);
        }
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
        ReadOnlySpan<string> editorSegments = new[] { "PLACE", "TRANS", "ROTATE", "SCALE" };
        KsaUiLanguages.EditorSegmentsPatch.Localize("GizmoTool", ref editorSegments);
        Require(editorSegments.SequenceEqual(new[] { "放置", "平移", "旋转", "缩放" }), "Editor tool captions are translated");
        ReadOnlySpan<string> otherSegments = new[] { "PLACE" };
        KsaUiLanguages.EditorSegmentsPatch.Localize("PlayerCustomWidget", ref otherSegments);
        Require(otherSegments[0] == "PLACE", "Other segmented controls are untouched");
        Span<char> categoryBuffer = stackalloc char[128];
        int categoryLength = KsaUiLanguages.EditorCategoryPatch.LocalizedUpper("Fuel Tanks", categoryBuffer);
        Require(categoryBuffer[..categoryLength].ToString() == "推进剂箱", "Category display is translated");
        ReadOnlySpan<char> binLabel = "BIN";
        KsaUiLanguages.EditorBinPatch.Localize("SymmetryBin", ref binLabel);
        Require(binLabel.ToString() == "删除", "Delete zone caption is translated independently");
        results.Add("PASS: Scoped editor segments and category display");
        Require(KsaUiLanguages.Runtime.EditorLiteral("\nMass: ") == "\n质量: ", "Part tooltip retains ASCII colon and newline");
        Require(KsaUiLanguages.Runtime.EditorLiteral("\nDecoupler Force: ") == "\n分离力: ", "Decoupler readout delimiter remains valid");
        Require(KsaUiLanguages.Runtime.EditorLiteral(" s") == " s", "Units are unchanged");
        Require(KsaUiLanguages.EditorLiteralPatch.PropellantDisplay("Double-Base").ToString() == "双基推进剂", "Known propellant display name is translated");
        var propellantMethod = AccessTools.Method(typeof(KSA.PartArchetypes), "AppendPropellantBranch");
        var propellantInstructions = KsaUiLanguages.EditorLiteralPatch.Translate(PatchProcessor.GetOriginalInstructions(propellantMethod).Select(i => new CodeInstruction(i)), propellantMethod).ToList();
        Require(propellantInstructions.Any(i => i.operand is MethodInfo m && m.Name == nameof(KsaUiLanguages.EditorLiteralPatch.PropellantDisplay)), "Propellant display conversion reaches actual append instructions");
        results.Add("PASS: Part-tooltip separators and units");
        ImString fieldLabel = "Vehicle Name";
        KsaUiLanguages.Runtime.EditorUiDepth = 0;
        Require(KsaUiLanguages.EditorFieldCaptionPatch.DisplayCaption(fieldLabel).ToString() == "Vehicle Name", "Outside editor scope caption is unchanged");
        KsaUiLanguages.Runtime.EditorUiDepth = 1;
        Require(KsaUiLanguages.EditorFieldCaptionPatch.DisplayCaption(fieldLabel).ToString() == "飞船名称" && fieldLabel.ToString() == "Vehicle Name", "Only a temporary display caption changes");
        KsaUiLanguages.Runtime.EditorUiDepth = 0;
        var categoryMethod = AccessTools.Method(typeof(KSA.VehicleEditor), "DrawCategoryRow");
        var categoryOriginal = PatchProcessor.GetOriginalInstructions(categoryMethod);
        var categoryPatched = KsaUiLanguages.EditorCategoryPatch.Translate(categoryOriginal.Select(i => new CodeInstruction(i))).ToList();
        Require(categoryPatched.Count(i => i.operand is MethodInfo m && m.DeclaringType == typeof(KsaUiLanguages.EditorCategoryPatch)) == 1, "Exactly the category display conversion is replaced");
        Require(categoryPatched.Any(i => i.operand is MethodInfo m && m.Name == "InvisibleButton"), "Category button ID call remains present");
        var pickerType = typeof(KSA.VehicleEditor).GetNestedType("PartWindow", BindingFlags.Public | BindingFlags.NonPublic)!;
        var pickerMethod = AccessTools.Method(pickerType, "OnDrawUi");
        var pickerInstructions = KsaUiLanguages.EditorLiteralPatch.Translate(PatchProcessor.GetOriginalInstructions(pickerMethod).Select(i => new CodeInstruction(i)), pickerMethod).ToList();
        int allIndex = pickerInstructions.FindIndex(i => i.opcode == System.Reflection.Emit.OpCodes.Ldstr && Equals(i.operand, "All"));
        Require(allIndex >= 0 && !(pickerInstructions[allIndex + 1].operand is MethodInfo allMethod && allMethod.Name == nameof(KsaUiLanguages.Runtime.EditorLiteral)), "All category tag is untouched at its caller");
        var inputMethod = typeof(KSA.ImGuiHelper).GetMethods().First(m => m.Name == "DrawInput" && m.GetParameters()[0].ParameterType == typeof(ImString));
        var inputPatched = KsaUiLanguages.EditorFieldCaptionPatch.Translate(PatchProcessor.GetOriginalInstructions(inputMethod), inputMethod).ToList();
        Require(inputPatched.Count(i => i.operand is MethodInfo m && m.DeclaringType == typeof(KsaUiLanguages.EditorFieldCaptionPatch)) == 1, "One field display conversion is injected without rewriting the variable");
        foreach (var helper in KsaUiLanguages.EditorFieldCaptionPatch.TargetMethods().Where(m => m.IsGenericMethod))
        {
            var patched = KsaUiLanguages.EditorFieldCaptionPatch.Translate(PatchProcessor.GetOriginalInstructions(helper).Select(i => new CodeInstruction(i)), helper).ToList();
            Require(patched.Count(i => i.operand is MethodInfo m && m.DeclaringType == typeof(KsaUiLanguages.EditorFieldCaptionPatch)) == 3,
                "Native launch combo has display, preview and option wrappers at the actual call sites");
        }
        results.Add("PASS: Actual editor display instructions preserve ID construction");
        var editorLabels = new[] { "All Sizes", "Toggle Resource Display", "Earth", "At CCSFS LC-39A On Earth", "Orbit around Moon" };
        var chineseEditorIds = editorLabels.ToDictionary(s => s, s => s == "Earth" ? KsaUiLanguages.Runtime.EditorNativeSelectable(s, 1)
            : s.StartsWith("At ") || s.StartsWith("Orbit ") ? KsaUiLanguages.Runtime.EditorNativeSelectable(s, 2) : KsaUiLanguages.Runtime.EditorMenuText(s));
        Require(KsaUiLanguages.Runtime.EditorNativeText("Earth", 1) == "地球", "Launch-body preview is translated");
        Require(KsaUiLanguages.Runtime.EditorNativeText("At CCSFS LC-39A On Earth", 2) == "地球：CCSFS LC-39A", "Location formatting preserves the site identifier");
        KsaUiLanguages.Runtime.ApplyLanguage("en-US");
        foreach (var original in editorLabels)
        {
            string englishId = original == "Earth" ? KsaUiLanguages.Runtime.EditorNativeSelectable(original, 1)
                : original.StartsWith("At ") || original.StartsWith("Orbit ") ? KsaUiLanguages.Runtime.EditorNativeSelectable(original, 2) : KsaUiLanguages.Runtime.EditorMenuText(original);
            Require(HashGuiId(englishId, 0x735A1913) == HashGuiId(chineseEditorIds[original], 0x735A1913), "Editor selectable IDs are stable across languages");
        }
        Require(KsaUiLanguages.Runtime.EditorNativeText("Orbit around Moon", 2) == "Orbit around Moon", "English dynamic location formatting is restored");
        results.Add("PASS: Launch previews and stable option IDs across languages");
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
        var patchSmoke = new Harmony("org.ksa.uilanguages.patchsmoke");
        try
        {
            KsaUiLanguages.Runtime.ApplyLanguage("zh-CN");
            patchSmoke.PatchAll(typeof(KsaUiLanguages.LanguagePlugin).Assembly);
            Require(Harmony.GetAllPatchedMethods().Any(m => Harmony.GetPatchInfo(m)?.Owners.Contains(patchSmoke.Id) == true), "Plugin Harmony patches can be installed against the actual game");
            results.Add("PASS: All plugin patches install against game build 5541");
        }
        finally
        {
            patchSmoke.UnpatchAll(patchSmoke.Id);
            KsaUiLanguages.Runtime.RestoreProvider();
        }
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
