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
        Require(tooltip.ToString().Contains("RCS 推进器"), "Gauge tooltip can be explained in Chinese");
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
        Require(KsaUiLanguages.StartupUiPatch.RegionText("Select System").ToString() == "选择天体系统###Select System", "Startup region headings keep a stable ID");
        Require(KsaUiLanguages.StartupUiPatch.RegionText("System").ToString() == "System", "Startup field source labels remain unchanged");
        KsaUiLanguages.Runtime.StartupUiDepth = 1;
        Require(KsaUiLanguages.EditorFieldCaptionPatch.DisplayCaption("Location").ToString() == "起始地点", "Startup captions use their own context");
        Require(KsaUiLanguages.Runtime.Literal("START KSA") == "启动 KSA", "Start button caption is translated without changing its action");
        KsaUiLanguages.Runtime.StartupUiDepth = 0;
        var startupLabels = new[] { "Solar System (Dense)", "Unlimited", "Testing", "Always Show", "Select System" };
        var chineseStartupIds = startupLabels.ToDictionary(s => s, KsaUiLanguages.Runtime.StartupMenuText);
        foreach (var helper in KsaUiLanguages.StartupFieldPatch.TargetMethods())
        {
            var startupInstructions = KsaUiLanguages.StartupFieldPatch.Translate(PatchProcessor.GetOriginalInstructions(helper).Select(i => new CodeInstruction(i)), helper).ToList();
            Require(startupInstructions.Any(i => i.operand is MethodInfo m && m.Name == nameof(KsaUiLanguages.EditorFieldCaptionPatch.DisplayCaption)), "Startup helper caption conversion reaches the actual method: " + helper);
            if (helper.GetGenericArguments()[0] == typeof(KSA.ConfigOnStartPopup.VehicleObject))
                Require(!startupInstructions.Any(i => i.operand is MethodInfo m && m.DeclaringType == typeof(KsaUiLanguages.StartupFieldPatch)), "Vehicle names and keys are kept unchanged in startup");
        }
        results.Add("PASS: Startup captions, options and original vehicle identifiers");
        const string capsuleId = "CoreCommandA_Prefab_MediumCapsuleVariantA";
        var capsuleTemplate = new KSA.PartTemplate { Id = capsuleId, DisplayName = capsuleId };
        Require(KsaUiLanguages.Runtime.CurrentPack.Parts.ContainsKey(capsuleId), "Confirmed capsule has a Chinese display mapping");
        Require(KsaUiLanguages.Runtime.PartName(capsuleTemplate).Contains("乘员舱"), "Part tooltip uses the Chinese display name");
        Require(capsuleTemplate.Id == capsuleId && capsuleTemplate.DisplayName == capsuleId, "The original template ID and name are not mutated");
        var unknownTemplate = new KSA.PartTemplate { Id = "Unknown_TestPart", DisplayName = "Original Custom Part" };
        Require(KsaUiLanguages.Runtime.PartName(unknownTemplate) == "Original Custom Part", "Unknown parts retain their original names");
        var customPart = (KSA.Part)RuntimeHelpers.GetUninitializedObject(typeof(KSA.Part));
        customPart.Template = capsuleTemplate;
        typeof(KSA.Part).GetProperty(nameof(KSA.Part.DisplayName))!.SetValue(customPart, "My Capsule");
        Require(KsaUiLanguages.Runtime.PartCaption(customPart) == "My Capsule", "Player supplied instance names are retained");
        var tooltipNameMethod = AccessTools.Method(typeof(KSA.PartArchetypes), nameof(KSA.PartArchetypes.AppendTooltip));
        var tooltipNameInstructions = KsaUiLanguages.PartTooltipNamePatch.Translate(PatchProcessor.GetOriginalInstructions(tooltipNameMethod).Select(i => new CodeInstruction(i))).ToList();
        Require(tooltipNameInstructions.Count(i => i.operand is MethodInfo m && m.Name == nameof(KsaUiLanguages.Runtime.PartName)) == 1,
            "Exactly the actual tooltip display-name read is replaced");
        results.Add("PASS: Part names are display-only and preserve template / custom identifiers");
        CheckAuxiliaryCaptions(results);
        CheckFlightMenus(results);
        CheckControlCaptions(results);
        CheckHudWindowCaptions(results);
        CheckHudContextAndLayouts(results);
        CheckPlanningAndTracking(results);
        CheckFlightAndPartContexts(results);
        CheckResourceAndTimeCaptions(results);
        CheckSaveAndManifestCaptions(results);
        KsaUiLanguages.Runtime.ApplyLanguage("en-US");
        Require(KsaUiLanguages.Runtime.PartName(capsuleTemplate) == capsuleId, "English restores the original part name");
        results.Add("PASS: Part-name English fallback");
        foreach (var label in startupLabels)
            Require(HashGuiId(KsaUiLanguages.Runtime.StartupMenuText(label), 0x735A1913) == HashGuiId(chineseStartupIds[label], 0x735A1913), "Startup option IDs are stable across languages");
        Require(KsaUiLanguages.Runtime.StartupText("Testing") == "Testing", "Startup option falls back to English");
        results.Add("PASS: Startup region and option IDs across languages");
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
        string[] patchedDisplayMethods=Array.Empty<string>();
        try
        {
            KsaUiLanguages.Runtime.ApplyLanguage("zh-CN");
            patchSmoke.PatchAll(typeof(KsaUiLanguages.LanguagePlugin).Assembly);
            var sharedCheckbox=KsaUiLanguages.PartContextFieldCaptionPatch.TargetMethods().Single(m=>!m.IsGenericMethod);
            var finalCheckbox=PatchProcessor.GetCurrentInstructions(sharedCheckbox);
            Require(finalCheckbox.Count(i=>i.operand is MethodInfo m && m.DeclaringType==typeof(KsaUiLanguages.PartContextFieldCaptionPatch)
                && m.Name==nameof(KsaUiLanguages.PartContextFieldCaptionPatch.NativeText))==1, "Final shared-checkbox IL has exactly one composed display wrapper");
            KsaUiLanguages.FlightPlanUiScopePatch.Enter(out int checkboxScope);
            try { Require(KsaUiLanguages.PartContextFieldCaptionPatch.CombinedCaption("Hide Orbit").ToString()=="隐藏轨道", "Final composed checkbox lookup includes flight-plan scope"); }
            finally { KsaUiLanguages.FlightPlanUiScopePatch.Leave(null,checkboxScope); }
            results.Add("PASS: Final shared-checkbox patch contains the composed flight and resource display lookup");
            Require(Harmony.GetAllPatchedMethods().Any(m => Harmony.GetPatchInfo(m)?.Owners.Contains(patchSmoke.Id) == true), "Plugin Harmony patches can be installed against the actual game");
            patchedDisplayMethods=Harmony.GetAllPatchedMethods().Where(m=>Harmony.GetPatchInfo(m)?.Owners.Contains(patchSmoke.Id)==true)
                .Select(m=>m.DeclaringType?.FullName+"::"+m.Name+"("+string.Join(",",m.GetParameters().Select(p=>p.ParameterType.ToString()))+")").Order().ToArray();
            results.Add("PASS: All plugin patches install against game build 5541");
        }
        finally
        {
            patchSmoke.UnpatchAll(patchSmoke.Id);
            KsaUiLanguages.Runtime.RestoreProvider();
        }
        harmony.UnpatchAll("org.ksa.uilanguages.headlesschecks");
        var report = new { gameVersion = typeof(KSA.Constants).Assembly.GetName().Version?.ToString(),
            results, patchedDisplayMethods, scope = "Actual game localization objects; no GUI or button-operation validation" };
        File.WriteAllText(Path.Combine(Root, @"work\run-logs\language-checks.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(JsonSerializer.Serialize(report));
    }

    public static bool UseCheckDirectory(ref string __result)
    {
        __result = Path.Combine(Root, @"work\headless-language-checks");
        return false;
    }

    private static void CheckAuxiliaryCaptions(List<string> results)
    {
        string[] originalTabs = { "ROSTER", "MEMORIAL" };
        ReadOnlySpan<string> tabs = originalTabs;
        KsaUiLanguages.RosterTabsPatch.Localize("KittenRosterTabs", ref tabs);
        Require(tabs.SequenceEqual(new[] { "名单", "纪念" }) && originalTabs.SequenceEqual(new[] { "ROSTER", "MEMORIAL" }),
            "Roster draw captions change without mutating its shared tab array");
        ReadOnlySpan<string> foreignTabs = originalTabs;
        KsaUiLanguages.RosterTabsPatch.Localize("PlayerCustomTabs", ref foreignTabs);
        Require(foreignTabs.SequenceEqual(originalTabs), "Other segmented controls retain their captions");
        ReadOnlySpan<char> rosterTitle = "Kitten Roster";
        KsaUiLanguages.RosterTitlePatch.Localize("KSA-ROS", ref rosterTitle);
        Require(rosterTitle.ToString() == "乘员名单", "Roster title is translated at drawing time");
        ReadOnlySpan<char> otherTitle = "Kitten Roster";
        KsaUiLanguages.RosterTitlePatch.Localize("PlayerWindow", ref otherTitle);
        Require(otherTitle.ToString() == "Kitten Roster", "Unrelated window signatures are untouched");
        Require(KsaUiLanguages.RosterCaptionPatch.DisplayCaption("kittens").ToString() == "kittens",
            "Roster table IDs are untouched");
        Require(KsaUiLanguages.RosterCaptionPatch.DisplayCaption("Unassigned").ToString() == "未分配",
            "Plain roster status text contains no hidden widget ID suffix");
        results.Add("PASS: Roster captions, immutable tab data and window scope");

        var rosterType = typeof(KSA.KittenRosterWindow).GetNestedType("Window", BindingFlags.NonPublic)!;
        var rosterMethod = AccessTools.Method(rosterType, "Draw");
        var rosterInstructions = KsaUiLanguages.RosterCaptionPatch.Translate(PatchProcessor.GetOriginalInstructions(rosterMethod)
            .Select(i => new CodeInstruction(i))).ToList();
        var dynamicConversion = typeof(ImString).GetMethods().Single(m => m.Name == "op_Implicit" && m.GetParameters()[0].ParameterType == typeof(string));
        var dynamicInstructions = new[] { new CodeInstruction(System.Reflection.Emit.OpCodes.Call, dynamicConversion) };
        Require(KsaUiLanguages.RosterCaptionPatch.Translate(dynamicInstructions).Count() == 1,
            "Player names and vehicle string conversions are not localized");
        Require(rosterInstructions.Any(i => i.opcode == System.Reflection.Emit.OpCodes.Ldstr && Equals(i.operand, "EVA")),
            "Roster EVA assignment identifier is retained in the actual method");
        var celestialMethod = AccessTools.Method(typeof(KSA.Celestial), nameof(KSA.Celestial.DrawCelestialWindowData));
        var celestialInstructions = KsaUiLanguages.CelestialCaptionPatch.Translate(PatchProcessor.GetOriginalInstructions(celestialMethod)
            .Select(i => new CodeInstruction(i))).ToList();
        Require(celestialInstructions.Count(i => i.operand is MethodInfo m && m.DeclaringType == typeof(KsaUiLanguages.CelestialCaptionPatch)) == 6,
            "Exactly the four celestial readout captions and two yes/no values are translated");
        Require(KsaUiLanguages.CelestialCaptionPatch.ReadoutText("LAN") == "LAN"
            && KsaUiLanguages.CelestialCaptionPatch.ReadoutText("N7") == "N7"
            && KsaUiLanguages.CelestialCaptionPatch.DisplayCaption("X").ToString() == "X",
            "Orbital abbreviations, numeric formats and axis identifiers are retained");
        results.Add("PASS: Actual roster and celestial instructions preserve names and numeric readouts");

        var shadowTooltips = (Brutal.Localization.LString[])typeof(KSA.LStrings).GetField("SettingsGraphicsShadowsFilterTooltips")!.GetValue(null)!;
        Require(shadowTooltips.Length == 3 && shadowTooltips.All(t => KsaUiLanguages.Runtime.TooltipText(t.Template) != t.Template),
            "All three actual shadow-filter array templates have translated tooltip mappings");
        results.Add("PASS: Actual shadow-filter tooltip array coverage");

        var headerKeys = new[] { "Name", "Assigned Vehicle", "Missions", "Current Mission Elapsed", "Total Mission Elapsed", "Distance Travelled", "Fastest Speed" };
        var treeKeys = new[] { "LocalPosition", "OrbitVelocity", "Rotation", "Draw Axes", "Show SOI" };
        var chineseHeaderIds = headerKeys.ToDictionary(k => k, k => KsaUiLanguages.RosterCaptionPatch.DisplayCaption(k).ToString());
        var chineseTreeIds = treeKeys.ToDictionary(k => k, k => KsaUiLanguages.CelestialCaptionPatch.DisplayCaption(k).ToString());
        KsaUiLanguages.Runtime.ApplyLanguage("en-US");
        foreach (string key in headerKeys)
            Require(HashGuiId(KsaUiLanguages.RosterCaptionPatch.DisplayCaption(key).ToString(), 0x735A1913) == HashGuiId(chineseHeaderIds[key], 0x735A1913),
                "Roster column widget IDs are stable across languages: " + key);
        foreach (string key in treeKeys)
            Require(HashGuiId(KsaUiLanguages.CelestialCaptionPatch.DisplayCaption(key).ToString(), 0x735A1913) == HashGuiId(chineseTreeIds[key], 0x735A1913),
                "Celestial tree and checkbox IDs are stable across languages: " + key);
        tabs = originalTabs;
        KsaUiLanguages.RosterTabsPatch.Localize("KittenRosterTabs", ref tabs);
        Require(tabs.SequenceEqual(originalTabs) && KsaUiLanguages.CelestialCaptionPatch.ReadoutText("MASS") == "MASS",
            "Auxiliary window captions fall back to English");
        rosterTitle = "Kitten Roster";
        KsaUiLanguages.RosterTitlePatch.Localize("KSA-ROS", ref rosterTitle);
        Require(rosterTitle.ToString() == "Kitten Roster", "An existing roster's source title can render in English again");
        KsaUiLanguages.Runtime.ApplyLanguage("zh-CN");
        results.Add("PASS: Auxiliary window ID hashes and English round trip");
    }

    private delegate int FormatMenuCount(ReadOnlySpan<char> label, int count, Span<byte> buffer);

    private static void CheckFlightMenus(List<string> results)
    {
        var keys = new[] { "Auto Warp", "Vessels", "Bodies", "Manifest", "Kitten Roster", "Transfer Planner", "Flight Plan",
            "Ground Track", "Target Track", "Staging", "Threads", "Profiler", "Orbit Camera", "Free Camera", "Map Camera",
            "Add Camera", "Follow Terrain", "Show Orbit Markers", "Flight Plans", "Celestial Names", "Debug" };
        var chinese = keys.ToDictionary(k => k, k => KsaUiLanguages.MenuTextPatch.LocalizeStaticLabel(k).ToString());
        Require(chinese.All(p => p.Value.Split("###")[0] != p.Key), "All screenshot flight-menu labels have Chinese captions");
        Require(KsaUiLanguages.MenuTextPatch.LocalizeStaticLabel("Hold alt to multiselect").ToString() == "按住 Alt 可多选",
            "Plain menu instructions contain no hidden ID suffix");
        foreach (var method in KsaUiLanguages.MenuTextPatch.TargetMethods().Where(m => m.Name is "DrawMenuBar" or "DrawMenuItems"))
        {
            var original = PatchProcessor.GetOriginalInstructions(method).Select(i => new CodeInstruction(i)).ToList();
            var patched = KsaUiLanguages.MenuTextPatch.TranslateStaticMenus(original, method).ToList();
            int conversions = original.Count(i => i.operand is MethodInfo m && m.DeclaringType == typeof(ImString)
                && m.Name == "op_Implicit" && m.GetParameters()[0].ParameterType == typeof(ReadOnlySpan<byte>));
            int wrappers = patched.Count(i => i.operand is MethodInfo m && m.Name == nameof(KsaUiLanguages.MenuTextPatch.LocalizeStaticLabel));
            Require(conversions > 0 && wrappers == conversions, "All actual static menu conversion sites are covered: " + method);
        }
        var categoryMethod = AccessTools.Method(typeof(KSA.Program), "DrawMoveCameraToSubMenu");
        var categoryInstructions = KsaUiLanguages.MenuTextPatch.TranslateStaticMenus(PatchProcessor.GetOriginalInstructions(categoryMethod)
            .Select(i => new CodeInstruction(i)), categoryMethod).ToList();
        Require(categoryInstructions.Count(i => i.operand is MethodInfo m && m.Name == nameof(KsaUiLanguages.MenuTextPatch.FormatCameraCategoryWithCount)) == 1,
            "Exactly the category formatter is replaced in the actual camera submenu");
        var originalFormatter = AccessTools.Method(typeof(KSA.Program), "FormatLabelWithCount").CreateDelegate<FormatMenuCount>();
        var categoryChinese = new Dictionary<string, string>();
        Span<byte> buffer = stackalloc byte[256];
        Span<byte> originalBuffer = stackalloc byte[256];
        foreach (string key in new[] { "Minor Bodies", "Asteroids", "Comets" })
            foreach (int count in new[] { 0, 2, 123 })
            {
                int length = KsaUiLanguages.MenuTextPatch.FormatCameraCategoryWithCount(key, count, buffer);
                string caption = Encoding.UTF8.GetString(buffer[..length]);
                Require(buffer[length] == 0, "Category caption keeps its UTF-8 terminator");
                Require(caption.Split("###")[0] == KsaUiLanguages.Runtime.UiText(key) + (count > 0 ? $" ({count})" : ""),
                    "Camera category count remains visible before the hidden ID");
                categoryChinese[key + ":" + count] = caption;
            }
        foreach (int count in new[] { 0, 2, 123 })
        {
            int length = KsaUiLanguages.MenuTextPatch.FormatCameraCategoryWithCount("Player Category", count, buffer);
            int baselineLength = originalFormatter("Player Category", count, originalBuffer);
            Require(length == baselineLength && buffer[..(length + 1)].SequenceEqual(originalBuffer[..(baselineLength + 1)]),
                "Unknown camera categories retain the exact original formatter output");
        }
        KsaUiLanguages.Runtime.ApplyLanguage("en-US");
        foreach (string key in keys)
            foreach (uint seed in new uint[] { 0, 0x735A1913 })
                Require(HashGuiId(KsaUiLanguages.MenuTextPatch.LocalizeStaticLabel(key).ToString(), seed) == HashGuiId(chinese[key], seed),
                    "Flight-menu IDs remain stable across languages: " + key);
        foreach (string key in new[] { "Minor Bodies", "Asteroids", "Comets" })
            foreach (int count in new[] { 0, 2, 123 })
            {
                int length = KsaUiLanguages.MenuTextPatch.FormatCameraCategoryWithCount(key, count, buffer);
                Require(HashGuiId(Encoding.UTF8.GetString(buffer[..length]), 0x735A1913) == HashGuiId(categoryChinese[key + ":" + count], 0x735A1913),
                    "Camera category IDs retain source names and counts across languages");
            }
        KsaUiLanguages.Runtime.ApplyLanguage("zh-CN");
        results.Add("PASS: Actual flight-menu sites, visible category counts and stable ID hashes");
    }

    private static void CheckControlCaptions(List<string> results)
    {
        var bindings = (System.Collections.IDictionary)typeof(KSA.Input).GetMethod("GenerateDefault")!.Invoke(null, null)!;
        var originalValues = bindings.Keys.Cast<object>().ToDictionary(k => k, k => bindings[k]);
        var collection = typeof(KSA.EnumCollections).GetField("InputActions")!.GetValue(null)!;
        var getName = collection.GetType().GetMethod("GetName", new[] { typeof(KSA.InputAction) })!;
        var labels = bindings.Keys.Cast<object>().Select(action => (string)getName.Invoke(collection, new[] { action })!).ToArray();
        Require(labels.Length == 101 && labels.All(KsaUiLanguages.Runtime.CurrentPack.Controls.ContainsKey),
            "All 101 actual default action display names have controls mappings");
        Require(KsaUiLanguages.Runtime.ControlText("Camera Mode") == "切换相机模式" && KsaUiLanguages.Runtime.UiText("Camera Mode") == "相机模式",
            "Control actions and menu names use separate semantic dictionaries");
        Require(KsaUiLanguages.Runtime.ControlText("Player Action") == "Player Action", "Unknown control names fall back unchanged");
        var rowMethod = KsaUiLanguages.ControlsKeyCaptionPatch.TargetMethod();
        var instructions = KsaUiLanguages.ControlsKeyCaptionPatch.Translate(PatchProcessor.GetOriginalInstructions(rowMethod)
            .Select(i => new CodeInstruction(i))).ToList();
        Require(instructions.Count(i => i.operand is MethodInfo m && m.DeclaringType == typeof(KsaUiLanguages.Runtime)
            && m.Name == nameof(KsaUiLanguages.Runtime.ControlText)) == 1,
            "Exactly the action display-name read is localized in the actual assignment row");
        Require(instructions.Any(i => i.operand is MethodInfo m && m.Name == "AppendKeyBinding")
            && instructions.Any(i => i.opcode == System.Reflection.Emit.OpCodes.Ldstr && Equals(i.operand, "SettingsControlsKey")),
            "Key-value formatting and binding-button IDs remain in the original method");
        Require(KsaUiLanguages.KeyAssignmentPopupTitlePatch.LocalizeTitle("Roll Left") == "向左滚转", "Binding-popup action title uses its caption dictionary");
        var popupMethod = KsaUiLanguages.KeyAssignmentPopupTitlePatch.TargetMethod();
        var popupInstructions = KsaUiLanguages.KeyAssignmentPopupTitlePatch.Translate(PatchProcessor.GetOriginalInstructions(popupMethod)
            .Select(i => new CodeInstruction(i))).ToList();
        Require(popupInstructions.Count(i => i.operand is MethodInfo m && m.DeclaringType == typeof(KsaUiLanguages.KeyAssignmentPopupTitlePatch)) == 1,
            "Binding-popup constructor replaces only the visible uppercase title conversion");
        Require(bindings.Keys.Cast<object>().All(key => Equals(bindings[key], originalValues[key])),
            "Binding values and enum keys are unchanged after caption lookups");
        results.Add("PASS: All actual control captions, separate meanings and original binding paths");
        KsaUiLanguages.Runtime.ApplyLanguage("en-US");
        Require(labels.All(label => KsaUiLanguages.Runtime.ControlText(label) == label)
            && KsaUiLanguages.KeyAssignmentPopupTitlePatch.LocalizeTitle("Roll Left") == "ROLL LEFT",
            "Control page and newly created binding-popup titles restore original English");
        KsaUiLanguages.Runtime.ApplyLanguage("zh-CN");
        results.Add("PASS: Control-caption English fallback and restored Chinese");
    }

    private static void CheckHudWindowCaptions(List<string> results)
    {
        var contextWindowType = typeof(KSA.GaugeContextAssignmentWindow).GetNestedType("Window", BindingFlags.NonPublic)!;
        var contextWindow = (KSA.ImGuiWindow)RuntimeHelpers.GetUninitializedObject(contextWindowType);
        var foreignWindowType = typeof(KSA.CrewAssignmentWindow).GetNestedType("Window", BindingFlags.NonPublic)!;
        var foreignWindow = (KSA.ImGuiWindow)RuntimeHelpers.GetUninitializedObject(foreignWindowType);
        const string originalTitle = "Context Assignments###context-stable-window";
        string chineseTitle = KsaUiLanguages.HudWindowTitlePatch.DisplayTitle(originalTitle, contextWindow);
        Require(chineseTitle == "仪表显示条件###context-stable-window", "HUD window title changes only the visible caption");
        Require(KsaUiLanguages.HudWindowTitlePatch.DisplayTitle(originalTitle, foreignWindow) == originalTitle,
            "An unrelated window with a matching title is left unchanged");
        var titleField = AccessTools.Field(typeof(KSA.ImGuiWindow), "_windowTitle");
        titleField.SetValue(contextWindow, originalTitle);
        _ = KsaUiLanguages.HudWindowTitlePatch.DisplayTitle(originalTitle, contextWindow);
        Require(Equals(titleField.GetValue(contextWindow), originalTitle), "Original window-title storage and hidden ID are not changed");
        var method = AccessTools.Method(typeof(KSA.ImGuiWindow), nameof(KSA.ImGuiWindow.OnDrawUi));
        var instructions = KsaUiLanguages.HudWindowTitlePatch.Translate(PatchProcessor.GetOriginalInstructions(method)
            .Select(i => new CodeInstruction(i))).ToList();
        Require(instructions.Count(i => i.operand is MethodInfo m && m.DeclaringType == typeof(KsaUiLanguages.HudWindowTitlePatch)) == 1,
            "Exactly the actual window-title read is wrapped");
        var chineseView = KsaUiLanguages.HudWindowViewMenuPatch.DisplayCaption("View", contextWindow).ToString();
        Require(chineseView == "视图###View" && KsaUiLanguages.HudWindowViewMenuPatch.DisplayCaption("View", foreignWindow).ToString() == "View",
            "Inherited HUD view menus are limited to known window types");
        ReadOnlySpan<char> layoutTitle = "LAYOUTS";
        KsaUiLanguages.HudConsoleTitlePatch.Localize("KSA-LAY", ref layoutTitle);
        Require(layoutTitle.ToString() == "HUD 布局", "Layout chrome uses its scoped caption");
        Require(KsaUiLanguages.Runtime.HudText("Name") == "名称" && KsaUiLanguages.Runtime.UiText("Name") == "姓名",
            "Layout labels and crew labels keep their different meanings");
        results.Add("PASS: Actual HUD window title read, type scope and separate label meanings");

        var popup = (KSA.StringInputPopup)RuntimeHelpers.GetUninitializedObject(typeof(KSA.StringInputPopup));
        var popupTitle = AccessTools.Field(typeof(KSA.StringInputPopup), "_title");
        var input = AccessTools.Field(typeof(KSA.StringInputPopup), "_input");
        var sanitized = AccessTools.Field(typeof(KSA.StringInputPopup), "_sanitized");
        popupTitle.SetValue(popup, "SAVE LAYOUT");
        input.SetValue(popup, "My Custom Layout");
        sanitized.SetValue(popup, "My Custom Layout");
        KsaUiLanguages.HudSavePopupScopePatch.Enter(popup, out var previousScope);
        Require(KsaUiLanguages.HudSavePopupScopePatch.IsLayout
            && KsaUiLanguages.HudSavePopupCaptionPatch.DisplayTitle("SAVE LAYOUT") == "保存 HUD 布局"
            && KsaUiLanguages.HudSavePopupCaptionPatch.DisplayPrompt("Please enter a filename to continue.") == "请输入布局名称。",
            "Layout name-input captions are translated in their scope");
        KsaUiLanguages.HudSavePopupScopePatch.Leave(previousScope);
        Require(!KsaUiLanguages.HudSavePopupScopePatch.IsLayout
            && KsaUiLanguages.HudSavePopupCaptionPatch.DisplayPrompt("Please enter a filename to continue.") == "Please enter a filename to continue.",
            "HUD save prompts do not affect unrelated name-input dialogs");
        Require(Equals(popupTitle.GetValue(popup), "SAVE LAYOUT") && Equals(input.GetValue(popup), "My Custom Layout")
            && Equals(sanitized.GetValue(popup), "My Custom Layout"), "Popup title storage, typed name and sanitized filename are unchanged");
        Require(KsaUiLanguages.Runtime.HudText("Will be saved as \"").EndsWith("\"", StringComparison.Ordinal),
            "Sanitized-name hint retains its quote delimiter");
        results.Add("PASS: Layout save-popup scope and preserved user filename values");

        KsaUiLanguages.Runtime.ApplyLanguage("en-US");
        string englishTitle = KsaUiLanguages.HudWindowTitlePatch.DisplayTitle(originalTitle, contextWindow);
        Require(englishTitle == originalTitle && HashGuiId(chineseTitle, 0x735A1913) == HashGuiId(englishTitle, 0x735A1913),
            "Existing HUD title retains its original explicit ID across languages");
        Require(HashGuiId(chineseView, 0x735A1913) == HashGuiId(KsaUiLanguages.HudWindowViewMenuPatch.DisplayCaption("View", contextWindow).ToString(), 0x735A1913),
            "Inherited view-menu IDs remain stable across languages");
        Require(KsaUiLanguages.HudSavePopupCaptionPatch.DisplayTitle("SAVE LAYOUT") == "SAVE LAYOUT",
            "An existing layout save popup can render its original English title");
        KsaUiLanguages.Runtime.ApplyLanguage("zh-CN");
        results.Add("PASS: Existing HUD window and popup English round trip");
    }

    private static void CheckHudContextAndLayouts(List<string> results)
    {
        var source = System.Xml.Linq.XDocument.Load(Path.Combine(GameDir, "Content", "Core", "Gauges.xml"));
        var canvases = source.Descendants().Where(e => e.Name.LocalName == "GaugeCanvas" && e.Attribute("Id") is not null)
            .ToDictionary(e => e.Attribute("Id")!.Value, e => e.Elements().Single(x => x.Name.LocalName == "DisplayName").Value);
        Require(canvases.Count == 14, "Confirmed default Core canvas set contains 14 IDs");
        var chineseCanvasNames = canvases.Keys.ToDictionary(k => k, KsaUiLanguages.HudContextAssignmentDisplayPatch.CanvasCaption);
        Require(chineseCanvasNames.All(p => p.Key != p.Value), "Every actual Core canvas has a Chinese context-table display alias");
        Require(KsaUiLanguages.HudContextAssignmentDisplayPatch.CanvasCaption("Burn") == "机动编辑器"
            && KsaUiLanguages.HudContextAssignmentDisplayPatch.CanvasCaption("BurnControl") == "机动控制"
            && KsaUiLanguages.HudContextAssignmentDisplayPatch.ColumnCaption("Burn").StartsWith("有机动###", StringComparison.Ordinal),
            "Burn canvas, burn-control canvas and burn visibility condition remain separate meanings");
        Require(KsaUiLanguages.HudContextAssignmentDisplayPatch.CanvasCaption("Sequence") == "分级序列"
            && KsaUiLanguages.HudContextAssignmentDisplayPatch.ColumnCaption("Sequence").StartsWith("有分级###", StringComparison.Ordinal),
            "Sequence canvas and sequence visibility condition use their own captions");
        Require(KsaUiLanguages.HudContextAssignmentDisplayPatch.CanvasCaption("PlayerCustomCanvas") == "PlayerCustomCanvas",
            "Unknown canvas IDs are not treated as display aliases");
        var contextMethod = KsaUiLanguages.HudContextAssignmentDisplayPatch.TargetMethod();
        var contextOriginal = PatchProcessor.GetOriginalInstructions(contextMethod).Select(i => new CodeInstruction(i)).ToList();
        var contextPatched = KsaUiLanguages.HudContextAssignmentDisplayPatch.Translate(contextOriginal).ToList();
        Require(contextPatched.Count(i => i.operand is MethodInfo m && m.DeclaringType == typeof(KsaUiLanguages.HudContextAssignmentDisplayPatch)) == 4,
            "Exactly two column calls, one canvas display and one instruction are wrapped");
        Require(contextPatched.Any(i => i.operand is MethodInfo m && m.Name == "SetFlag")
            && contextPatched.Any(i => i.operand is MethodInfo m && m.DeclaringType == typeof(ImGui) && m.Name == "PushID")
            && contextPatched.Any(i => i.operand is MethodInfo m && m.DeclaringType == typeof(ImGui) && m.Name == "Checkbox"),
            "Context checkbox IDs and original settings update call remain unchanged");
        results.Add("PASS: Actual Core canvas aliases, context meanings and unchanged settings calls");

        string[] defaults = { "Default", "Default (default)" };
        var chineseDefaults = defaults.ToDictionary(s => s, KsaUiLanguages.HudLayoutPatches.BuiltinDefaultLabel);
        Require(chineseDefaults["Default (default)"].StartsWith("默认 （默认）###", StringComparison.Ordinal),
            "Builtin default layout and fixed marker render in Chinese");
        string[] customLabels = { "Default (default)", "Resources* (default) [Ctrl+1]", "My (default) name (default) [F9]" };
        var chineseCustom = customLabels.ToDictionary(s => s, s => KsaUiLanguages.HudLayoutPatches.CustomLayoutMenuLabel(s, true));
        Require(chineseCustom[customLabels[0]].StartsWith("Default （默认）###", StringComparison.Ordinal),
            "A custom layout named Default is not renamed as the builtin label");
        Require(chineseCustom[customLabels[1]].StartsWith("Resources* （默认） [Ctrl+1]###", StringComparison.Ordinal)
            && chineseCustom[customLabels[2]].StartsWith("My (default) name （默认） [F9]###", StringComparison.Ordinal),
            "Custom names, embedded default text, dirty marker and hotkeys are preserved");
        foreach (string label in customLabels)
            Require(KsaUiLanguages.HudLayoutPatches.CustomLayoutMenuLabel(label, false) == label,
                "Nondefault user layouts are unchanged regardless of their names");
        Require(KsaUiLanguages.HudLayoutPatches.DefaultLayoutRowLabel("Resources (default)", true)
            .StartsWith("Resources （默认）###", StringComparison.Ordinal)
            && KsaUiLanguages.HudLayoutPatches.DefaultLayoutRowLabel("Resources", false) == "Resources",
            "Layout table rows localize only the fixed marker");
        Require(KsaUiLanguages.HudLayoutPatches.LayoutFooterUnit(" LAYOUT").ToString() == " 个布局"
            && KsaUiLanguages.HudLayoutPatches.LayoutFooterUnit(" LAYOUTS").ToString() == " 个布局",
            "Layout count fragments preserve their leading spacing and avoid the window-title meaning");
        var menuMethod = AccessTools.Method(typeof(KSA.LayoutSaves), nameof(KSA.LayoutSaves.DrawMenu));
        var layoutMenu = KsaUiLanguages.HudLayoutPatches.Translate(PatchProcessor.GetOriginalInstructions(menuMethod)
            .Select(i => new CodeInstruction(i)), menuMethod).ToList();
        Require(layoutMenu.Count(i => i.operand is MethodInfo m && m.Name == nameof(KsaUiLanguages.HudLayoutPatches.BuiltinDefaultMenuItem)) == 1
            && layoutMenu.Count(i => i.operand is MethodInfo m && m.Name == nameof(KsaUiLanguages.HudLayoutPatches.CustomLayoutMenuItem)) == 1,
            "Actual layout menu distinguishes the builtin item from the loop's custom items");
        var rowMethod = AccessTools.Method(typeof(KSA.LayoutSave), nameof(KSA.LayoutSave.DrawInTable));
        var layoutRow = KsaUiLanguages.HudLayoutPatches.Translate(PatchProcessor.GetOriginalInstructions(rowMethod)
            .Select(i => new CodeInstruction(i)), rowMethod).ToList();
        Require(layoutRow.Count(i => i.operand is MethodInfo m && m.Name == nameof(KsaUiLanguages.HudLayoutPatches.DefaultLayoutRow)) == 1,
            "Exactly the actual user layout-row selectable is wrapped");
        var bufferMethod = KsaUiLanguages.HudLayoutMenuBufferPatch.TargetMethod();
        var bufferInstructions = KsaUiLanguages.HudLayoutMenuBufferPatch.Translate(PatchProcessor.GetOriginalInstructions(bufferMethod)
            .Select(i => new CodeInstruction(i))).ToList();
        Require(bufferInstructions.Count(i => i.operand is MethodInfo m && m.DeclaringType == typeof(KsaUiLanguages.HudLayoutMenuBufferPatch)) == 2,
            "Both the actual menu byte allocation and matching char span capacity are scoped together");
        string longOriginal = new string('A', 100) + " (default)";
        string longChinese = KsaUiLanguages.HudLayoutPatches.CustomLayoutMenuLabel(longOriginal, true);
        int previousCapacity = KsaUiLanguages.HudLayoutMenuBufferPatch.RequiredCharacters;
        KsaUiLanguages.HudLayoutMenuBufferPatch.RequiredCharacters = longChinese.Length + 3;
        Require(KsaUiLanguages.HudLayoutMenuBufferPatch.BufferCharacters() >= longChinese.Length + 3
            && KsaUiLanguages.HudLayoutMenuBufferPatch.BufferBytes() == KsaUiLanguages.HudLayoutMenuBufferPatch.BufferCharacters() * sizeof(char),
            "Long layout names and hidden IDs fit a correctly sized byte and character buffer");
        KsaUiLanguages.HudLayoutMenuBufferPatch.RequiredCharacters = previousCapacity;
        Require(KsaUiLanguages.HudLayoutMenuBufferPatch.BufferCharacters() == 128 && KsaUiLanguages.HudLayoutMenuBufferPatch.BufferBytes() == 256,
            "Unrelated menu calls retain their original buffer capacity");
        results.Add("PASS: Layout menu and table preserve custom names, markers and hotkeys");

        const string layoutName = "Resources's {0} layout";
        string overwriteMessage = "Are you sure you want to overwrite layout '" + layoutName + "'?";
        string deleteMessage = "Are you sure you want to delete layout '" + layoutName + "'?";
        Require(KsaUiLanguages.HudConfirmPopupCaptionPatch.DisplayMessage(overwriteMessage, "OVERWRITE LAYOUT")
            == "确定要覆盖布局“" + layoutName + "”吗？"
            && KsaUiLanguages.HudConfirmPopupCaptionPatch.DisplayMessage(deleteMessage, "DELETE LAYOUT")
            == "确定要删除布局“" + layoutName + "”吗？",
            "Confirmation templates preserve embedded quotes and placeholder-like player names");
        Require(KsaUiLanguages.HudConfirmPopupCaptionPatch.DisplayMessage(deleteMessage, "FOREIGN POPUP") == deleteMessage,
            "Other confirmation dialogs retain their messages");
        var eva = source.Descendants().Single(e => e.Name.LocalName == "GaugeCanvas" && e.Attribute("Id")?.Value == "KittenFlightControl");
        var actualEvaTooltips = eva.Descendants().Where(e => e.Name.LocalName == "Tooltip").Select(e => e.Attribute("Value")!.Value).ToList();
        Require(actualEvaTooltips.Count == 5 && actualEvaTooltips.All(s => KsaUiLanguages.Runtime.TooltipText(s) != s),
            "All five actual EVA control tooltips have translated mappings");
        var languageMenu = PatchProcessor.GetOriginalInstructions(AccessTools.Method(typeof(KsaUiLanguages.LanguageMenuPatch), nameof(KsaUiLanguages.LanguageMenuPatch.Draw)));
        Require(languageMenu.Any(i => i.opcode == System.Reflection.Emit.OpCodes.Ldstr && Equals(i.operand, "Language"))
            && !languageMenu.Any(i => i.opcode == System.Reflection.Emit.OpCodes.Ldstr && Equals(i.operand, "Language / 语言")),
            "Language selector title follows the requested single English label");
        results.Add("PASS: Preserved layout confirmation names, actual EVA tooltips and Language title");

        var conditionNames = Enum.GetNames(typeof(KSA.GaugeVisibilityFlag));
        var chineseColumns = conditionNames.ToDictionary(s => s, KsaUiLanguages.HudContextAssignmentDisplayPatch.ColumnCaption);
        KsaUiLanguages.Runtime.ApplyLanguage("en-US");
        foreach (string canvasId in canvases.Keys)
            Require(KsaUiLanguages.HudContextAssignmentDisplayPatch.CanvasCaption(canvasId) == canvasId,
                "English context table restores the raw canvas ID");
        foreach (string condition in conditionNames)
            Require(HashGuiId(KsaUiLanguages.HudContextAssignmentDisplayPatch.ColumnCaption(condition), 0x735A1913)
                == HashGuiId(chineseColumns[condition], 0x735A1913), "Context column IDs remain stable across languages");
        foreach (string label in defaults)
            Require(HashGuiId(KsaUiLanguages.HudLayoutPatches.BuiltinDefaultLabel(label), 0x735A1913) == HashGuiId(chineseDefaults[label], 0x735A1913),
                "Builtin layout menu IDs remain stable across languages");
        foreach (string label in customLabels)
            Require(HashGuiId(KsaUiLanguages.HudLayoutPatches.CustomLayoutMenuLabel(label, true), 0x735A1913) == HashGuiId(chineseCustom[label], 0x735A1913),
                "Custom layout menu IDs remain stable across languages");
        Require(KsaUiLanguages.HudConfirmPopupCaptionPatch.DisplayMessage(overwriteMessage, "OVERWRITE LAYOUT") == overwriteMessage,
            "Existing layout confirmation restores its original English message");
        KsaUiLanguages.Runtime.ApplyLanguage("zh-CN");
        results.Add("PASS: HUD canvas, context-column and layout English fallback with stable IDs");
    }

    private static void CheckPlanningAndTracking(List<string> results)
    {
        var transferKeys = typeof(KSA.TransferPlanner).GetField("TransferTypes")!.GetValue(null) as System.Collections.IEnumerable;
        var typeRecords=transferKeys!.Cast<object>().Select(o=>new{instance=o,key=(string)o.GetType().GetMethod("GetKey")!.Invoke(o,null)!,name=(string)o.GetType().GetMethod("GetName")!.Invoke(o,null)!}).ToList();
        Require(typeRecords.Count==4 && typeRecords.All(t=>KsaUiLanguages.Runtime.CurrentPack.Planning.ContainsKey(t.name)), "All four actual transfer types have display translations");
        var chinesePlanOptions=typeRecords.ToDictionary(t=>t.name,t=>KsaUiLanguages.TransferPlannerComboPatch.OptionText(t.name,false));
        foreach(var item in typeRecords)
        {
            Require(!KsaUiLanguages.TransferPlannerComboPatch.PreviewText(item.name,false).Contains("###"), "Transfer preview is plain display text");
            Require((string)item.instance.GetType().GetMethod("GetKey")!.Invoke(item.instance,null)! == item.key
                && (string)item.instance.GetType().GetMethod("GetName")!.Invoke(item.instance,null)! == item.name, "Transfer objects retain their actual key and native name");
        }
        Require(KsaUiLanguages.TransferPlannerComboPatch.OptionText("Player Plan",false)=="Player Plan", "Unrecognized transfer option names are retained");
        foreach(var method in KsaUiLanguages.TransferPlannerCaptionPatch.TargetMethods())
        {
            var original=PatchProcessor.GetOriginalInstructions(method).Select(i=>new CodeInstruction(i)).ToList();
            var patched=KsaUiLanguages.TransferPlannerCaptionPatch.Translate(original,method).ToList();
            Require(patched.Any(i=>i.operand is MethodInfo m && m.DeclaringType==typeof(KsaUiLanguages.TransferPlannerCaptionPatch)), "Actual transfer renderer has translated call sites: "+method.Name);
            Require(patched.Where(i=>i.opcode==System.Reflection.Emit.OpCodes.Ldstr).Select(i=>i.operand)
                .SequenceEqual(original.Where(i=>i.opcode==System.Reflection.Emit.OpCodes.Ldstr).Select(i=>i.operand)), "Transfer rendering retains original string literals for lookup and ID construction");
        }
        foreach(var method in KsaUiLanguages.TransferPlannerComboPatch.TargetMethods())
        {
            var patched=KsaUiLanguages.TransferPlannerComboPatch.Translate(PatchProcessor.GetOriginalInstructions(method).Select(i=>new CodeInstruction(i)),method).ToList();
            Require(patched.Count(i=>i.operand is MethodInfo m && m.DeclaringType==typeof(KsaUiLanguages.TransferPlannerComboPatch))==2, "Closed value-type transfer combo contains exactly preview and option wrappers");
        }
        results.Add("PASS: Actual transfer types, display-only keys and rendering call sites");

        Require(KsaUiLanguages.TrackingWindowTitlePatch.DisplayTitle("My Ground Track | Ground Track","KSA-GND")=="My Ground Track | 地面轨迹", "Tracking title translates only the appended suffix");
        Require(KsaUiLanguages.TrackingWindowTitlePatch.DisplayTitle("My Ground Track | Ground Track","OtherWindow")=="My Ground Track | Ground Track", "Unrelated window titles are not changed");
        Require(KsaUiLanguages.TrackingCaptionPatch.StaticText("Reset##AltReset").ToString()=="重置###Reset##AltReset", "Tracking reset button retains the complete original ID");
        Require(KsaUiLanguages.TrackingCaptionPatch.StaticText("##GizmoXAlt").ToString()=="##GizmoXAlt", "Tracking gizmo input IDs are untouched");
        Require(KsaUiLanguages.TrackingCaptionPatch.PlainText("Scale: ")=="刻度间隔: " && KsaUiLanguages.TrackingCaptionPatch.PlainText("/s")=="/s", "Tracking display fragments and numeric units remain distinct");
        foreach(var method in KsaUiLanguages.TrackingCaptionPatch.TargetMethods())
        {
            var patched=KsaUiLanguages.TrackingCaptionPatch.Translate(PatchProcessor.GetOriginalInstructions(method).Select(i=>new CodeInstruction(i)),method).ToList();
            Require(patched.Any(i=>i.operand is MethodInfo m && m.DeclaringType==typeof(KsaUiLanguages.TrackingCaptionPatch)), "Actual tracking render method contains its scoped display lookup");
        }
        results.Add("PASS: Tracking titles, gizmo IDs and relative-axis meanings");

        string modMessage="A new mod with id 'Hohmann's {0}' has been found, do you want it enabled?";
        Require(KsaUiLanguages.CommonPopupCaptionPatch.ModMessage(modMessage)=="发现新模组“Hohmann's {0}”，要启用吗？", "Mod confirmation keeps the entire original mod ID");
        Require(KsaUiLanguages.CommonPopupCaptionPatch.ModMessage("Player note") == "Player note", "Unrecognized popup messages are retained");
        foreach(var method in KsaUiLanguages.CommonPopupCaptionPatch.TargetMethods())
        {
            var original=PatchProcessor.GetOriginalInstructions(method).Select(i=>new CodeInstruction(i)).ToList();
            var patched=KsaUiLanguages.CommonPopupCaptionPatch.Translate(original,method).ToList();
            Require(patched.Any(i=>i.operand is MethodInfo m && m.DeclaringType==typeof(KsaUiLanguages.CommonPopupCaptionPatch)), "Common popup includes actual display wrapper: "+method.DeclaringType!.Name);
            Require(patched.Count(i=>i.operand is MethodInfo m && m.Name=="DrawConsoleButtonRow") == original.Count(i=>i.operand is MethodInfo m && m.Name=="DrawConsoleButtonRow"), "Popup button/action path remains present");
        }
        results.Add("PASS: Common popup captions, dynamic mod IDs and original action calls");

        KsaUiLanguages.Runtime.ApplyLanguage("en-US");
        foreach(var item in typeRecords)
            Require(HashGuiId(KsaUiLanguages.TransferPlannerComboPatch.OptionText(item.name,false),0x735A1913)==HashGuiId(chinesePlanOptions[item.name],0x735A1913), "Transfer option IDs stay stable in English");
        Require(KsaUiLanguages.TrackingWindowTitlePatch.DisplayTitle("My Ground Track | Ground Track","KSA-GND")=="My Ground Track | Ground Track"
            && KsaUiLanguages.CommonPopupCaptionPatch.ModMessage(modMessage)==modMessage, "Tracking and common popups restore original English");
        KsaUiLanguages.Runtime.ApplyLanguage("zh-CN");
        results.Add("PASS: Planning and tracking English fallback and stable option hashes");
    }

    private static void CheckFlightAndPartContexts(List<string> results)
    {
        string[] burnMenus={"Create Burn [Manual]","Add 2nd Burn [Manual]","Create Burn [Prograde]"};
        var chineseMenus=burnMenus.ToDictionary(s=>s,KsaUiLanguages.FlightPlanCallsitePatch.PlanningBurnActionMenuText);
        Require(chineseMenus[burnMenus[1]].StartsWith("添加第 2 次机动 [手动]###",StringComparison.Ordinal), "Ordinal burn display uses its number without the English suffix");
        const string departure="Create Burn [Hohmann Departure]";
        string chineseDeparture=KsaUiLanguages.FlightPlanCallsitePatch.PlanningParentDepartureMenuText(departure);
        Require(chineseDeparture.Contains("Hohmann")&&!chineseDeparture.Contains("霍曼"), "Departure menu preserves its dynamic celestial name");
        string[] headers={"Patch '0'###FlightPlanPatch0","Closest Approach to Hohmann###Encounter-Hohmann"};
        var chineseHeaders=headers.ToDictionary(s=>s,KsaUiLanguages.FlightPlanCallsitePatch.PlanningCollapsingHeaderText);
        Require(chineseHeaders[headers[0]].StartsWith("轨迹段 '0'###",StringComparison.Ordinal), "Patch headers translate only the fixed prefix and keep their ID");
        var fieldLookup=AccessTools.Method(typeof(KsaUiLanguages.FlightPlanCallsitePatch),"PlanningFieldText");
        Require((string)fieldLookup.Invoke(null,new object[]{"Period"})! == "轨道周期"
            && KsaUiLanguages.Runtime.PlanningText("Period")=="显示周期数", "Orbit-period field uses a different semantic key from plot display periods");
        foreach(var transition in Enum.GetValues<KSA.PatchTransition>())
        {
            var copy=transition;
            _=KsaUiLanguages.FlightPlanCallsitePatch.PlanningTransitionText(ref copy);
            Require(copy.Equals(transition), "Patch transition enum values are unchanged by display lookups");
        }
        KsaUiLanguages.FlightPlanUiScopePatch.Enter(out int previousScope);
        Require(KsaUiLanguages.FlightPlanCheckboxCaptionPatch.DisplayCaption("Hide Orbit").ToString()=="隐藏轨道", "Flight-plan checkbox uses Chinese display inside its drawing scope");
        KsaUiLanguages.FlightPlanUiScopePatch.Leave(null,previousScope);
        Require(KsaUiLanguages.FlightPlanCheckboxCaptionPatch.DisplayCaption("Hide Orbit").ToString()=="Hide Orbit", "Shared checkbox caption is unchanged outside flight-plan drawing");
        foreach(var method in KsaUiLanguages.FlightPlanCallsitePatch.TargetMethods())
        {
            var patched=KsaUiLanguages.FlightPlanCallsitePatch.Translate(PatchProcessor.GetOriginalInstructions(method).Select(i=>new CodeInstruction(i)),method).ToList();
            Require(patched.Any(i=>i.operand is MethodInfo m && m.DeclaringType==typeof(KsaUiLanguages.FlightPlanCallsitePatch)), "Flight renderer has actual display call-site wrappers: "+method.Name);
        }
        results.Add("PASS: Flight-plan fields, dynamic burn menus and scoped checkbox captions");

        string[] controls={"Enter My Craft##seat1","Dock###dock","In###tankxferin","Set Target: Hohmann###target-port","Tank 'Resources'###tankResources"};
        var chineseControls=controls.ToDictionary(s=>s,KsaUiLanguages.PartContextCaptionPatch.InteractiveText);
        Require(chineseControls[controls[0]].StartsWith("进入 My Craft###",StringComparison.Ordinal), "Part menu keeps the dynamic craft name");
        Require(chineseControls[controls[3]].StartsWith("设置目标: Hohmann###",StringComparison.Ordinal), "Docking menu does not interpret a vehicle name as a translation key");
        Require(KsaUiLanguages.PartContextCaptionPatch.InteractiveText("Player Custom") == "Player Custom", "Unknown part-menu labels retain their original text");
        int previousDepth=KsaUiLanguages.PartContextFieldScopePatch.Depth;
        KsaUiLanguages.PartContextFieldScopePatch.Depth=1;
        var flows=Enum.GetNames<KSA.FlowRule>();
        var sourceFlows=flows.Where(KsaUiLanguages.PartContextFieldCaptionPatch.FlowOptionKeys.Contains).ToArray();
        Require(sourceFlows.Length==4 && sourceFlows.All(s=>KsaUiLanguages.PartContextFieldCaptionPatch.FlowPreviewText(s)!=s), "All four actual flow-rule options have plain display translations");
        var chineseFlows=sourceFlows.ToDictionary(s=>s,KsaUiLanguages.PartContextFieldCaptionPatch.FlowOptionText);
        Require(KsaUiLanguages.PartContextFieldCaptionPatch.DisplayVariable("Fuel Flow").ToString()=="推进剂供给方式", "Part field label is translated separately from its source ID");
        KsaUiLanguages.PartContextFieldScopePatch.Depth=0;
        Require(sourceFlows.All(s=>KsaUiLanguages.PartContextFieldCaptionPatch.FlowOptionText(s)==s), "Other flow-rule widgets remain unchanged outside part scope");
        KsaUiLanguages.PartContextFieldScopePatch.Depth=previousDepth;
        foreach(var method in KsaUiLanguages.PartContextCaptionPatch.TargetMethods())
            _=KsaUiLanguages.PartContextCaptionPatch.Translate(PatchProcessor.GetOriginalInstructions(method).Select(i=>new CodeInstruction(i)),method).ToList();
        results.Add("PASS: Part and docking names, original axes and scoped flow-rule captions");

        KsaUiLanguages.Runtime.ApplyLanguage("en-US");
        foreach(string menu in burnMenus)
            Require(HashGuiId(KsaUiLanguages.FlightPlanCallsitePatch.PlanningBurnActionMenuText(menu),0x735A1913)==HashGuiId(chineseMenus[menu],0x735A1913), "Dynamic burn-action ID is stable across languages");
        Require(HashGuiId(KsaUiLanguages.FlightPlanCallsitePatch.PlanningParentDepartureMenuText(departure),0x735A1913)==HashGuiId(chineseDeparture,0x735A1913), "Departure-action ID is stable across languages");
        foreach(string header in headers)
            Require(HashGuiId(KsaUiLanguages.FlightPlanCallsitePatch.PlanningCollapsingHeaderText(header),0x735A1913)==HashGuiId(chineseHeaders[header],0x735A1913), "Dynamic patch-header ID is stable across languages");
        foreach(string control in controls)
            foreach(uint seed in new uint[]{0,0x735A1913})
                Require(HashGuiId(KsaUiLanguages.PartContextCaptionPatch.InteractiveText(control),seed)==HashGuiId(chineseControls[control],seed), "Part/docking interactive IDs are stable across languages");
        KsaUiLanguages.PartContextFieldScopePatch.Depth=1;
        foreach(string flow in sourceFlows)
            Require(KsaUiLanguages.PartContextFieldCaptionPatch.FlowPreviewText(flow)==flow
                && HashGuiId(KsaUiLanguages.PartContextFieldCaptionPatch.FlowOptionText(flow),0x735A1913)==HashGuiId(chineseFlows[flow],0x735A1913), "Flow-rule English previews and option IDs are preserved");
        KsaUiLanguages.PartContextFieldScopePatch.Depth=previousDepth;
        KsaUiLanguages.Runtime.ApplyLanguage("zh-CN");
        results.Add("PASS: Native ImGui hashes for flight, part and flow-rule language round trips");
    }

    private static void CheckResourceAndTimeCaptions(List<string> results)
    {
        string[] captions={"Automatic","Engine Resources###EngineResources8","Fuel Lines###FuelLines123"};
        var chinese=captions.ToDictionary(s=>s,KsaUiLanguages.ResourceUtilityCaptionPatch.InteractiveText);
        Require(chinese[captions[1]]=="发动机资源###EngineResources8", "Resource header retains the original native suffix");
        Require(KsaUiLanguages.ResourceUtilityCaptionPatch.PlainText("Resources")=="Resources"
            && KsaUiLanguages.ResourceUtilityCaptionPatch.PlainText("KSA_STG_ORDER")=="KSA_STG_ORDER", "Resource window IDs and drag payloads remain byte-exact");
        Require(KsaUiLanguages.ResourceUtilityCaptionPatch.InteractiveText("My Generator###custom")=="My Generator###custom", "Unknown device and part names remain unchanged");
        string group=KsaUiLanguages.ResourceUtilityCaptionPatch.GroupText("Group 7",true);
        Require(group=="分组 7###Group 7" && KsaUiLanguages.ResourceUtilityCaptionPatch.GroupText("Group 7",false)=="分组 7", "Resource group caption and drag preview use separate display forms");
        var depth=AccessTools.Field(typeof(KsaUiLanguages.ResourceUtilityCaptionPatch),"_fieldDepth");
        int previous=(int)depth.GetValue(null)!;
        try
        {
            depth.SetValue(null,1);
            Require(KsaUiLanguages.PartContextFieldCaptionPatch.DisplayVariable("Number Tanks").ToString()=="显示储箱编号", "Resource checkbox display composes with the existing helper patch");
            Require(KsaUiLanguages.PartContextFieldCaptionPatch.DisplayVariable("Unknown Field").ToString()=="Unknown Field", "Scoped resource helper does not alter unknown fields");
            depth.SetValue(null,0);
            Require(KsaUiLanguages.PartContextFieldCaptionPatch.DisplayVariable("Number Tanks").ToString()=="Number Tanks", "Resource field labels stay unchanged outside their scope");
        }
        finally { depth.SetValue(null,previous); }
        foreach(var method in KsaUiLanguages.ResourceUtilityCaptionPatch.TargetMethods())
        {
            var original=PatchProcessor.GetOriginalInstructions(method).Select(i=>new CodeInstruction(i)).ToList();
            var patched=KsaUiLanguages.ResourceUtilityCaptionPatch.Translate(original,method).ToList();
            Require(patched.Any(i=>i.operand is MethodInfo m && m.DeclaringType==typeof(KsaUiLanguages.ResourceUtilityCaptionPatch)), "Actual resource renderer has scoped display wrappers");
            Require(patched.Where(i=>i.opcode==System.Reflection.Emit.OpCodes.Ldstr).Select(i=>i.operand)
                .SequenceEqual(original.Where(i=>i.opcode==System.Reflection.Emit.OpCodes.Ldstr).Select(i=>i.operand)), "Resource rendering keeps all original literal inputs");
            if(method.DeclaringType==typeof(KSA.Tank))
                Require(patched.Count(i=>i.operand is MethodInfo m && m.DeclaringType==typeof(ImGui) && m.Name==nameof(ImGui.CollapsingHeader))
                    ==original.Count(i=>i.operand is MethodInfo m && m.DeclaringType==typeof(ImGui) && m.Name==nameof(ImGui.CollapsingHeader)), "Tank substance names retain their original native header call");
        }
        results.Add("PASS: Resource captions, original payloads, device names and shared checkbox scopes");
        Require(KsaUiLanguages.FlightPlanCallsitePatch.PlanningTimeValue("4.89 minutes")=="4.89 分钟"
            && KsaUiLanguages.FlightPlanCallsitePatch.PlanningTimeValue(" 1,234.50 seconds ")==" 1,234.50 秒 ", "Numeric time readouts keep exact number formatting and spacing");
        foreach(string unknown in new[]{"Earth","42 min","1,2 seconds","My 42 minutes","0.4 km/s"})
            Require(KsaUiLanguages.FlightPlanCallsitePatch.PlanningTimeValue(unknown)==unknown, "Time caption parser preserves nonmatching names, units and values");
        results.Add("PASS: Time-value unit translation keeps numeric formatting and unknown text");
        KsaUiLanguages.Runtime.ApplyLanguage("en-US");
        foreach(string caption in captions)
            foreach(uint seed in new uint[]{0,0x735A1913})
                Require(HashGuiId(KsaUiLanguages.ResourceUtilityCaptionPatch.InteractiveText(caption),seed)==HashGuiId(chinese[caption],seed), "Resource control ID hashes remain stable in both languages");
        Require(HashGuiId(KsaUiLanguages.ResourceUtilityCaptionPatch.GroupText("Group 7",true),0x735A1913)==HashGuiId(group,0x735A1913), "Resource group ID remains stable across language switches");
        Require(KsaUiLanguages.FlightPlanCallsitePatch.PlanningTimeValue("4.89 minutes")=="4.89 minutes", "Time readout restores English exactly");
        KsaUiLanguages.Runtime.ApplyLanguage("zh-CN");
        results.Add("PASS: Resource native ID hashes and time units restore English safely");
    }

    private static void CheckSaveAndManifestCaptions(List<string> results)
    {
        string[] labels={"Load","Refresh","Apoapsis","Planets"};
        var chinese=labels.ToDictionary(s=>s,s=>KsaUiLanguages.UtilityWindowPatches.MenuCaption(s).ToString());
        Require(chinese["Apoapsis"].StartsWith("远点高度###",StringComparison.Ordinal), "Manifest orbit column reports altitude rather than radius");
        Require(KsaUiLanguages.UtilityWindowPatches.PlainCaption("Vehicle").ToString()=="飞船"
            && !KsaUiLanguages.UtilityWindowPatches.PlainCaption("Vehicle").ToString().Contains("###"), "Manifest type readout is plain text");
        var menuKeys=(HashSet<string>)AccessTools.Field(typeof(KsaUiLanguages.UtilityWindowPatches),"MenuLabels").GetValue(null)!;
        Require(menuKeys.All(KsaUiLanguages.Runtime.CurrentPack.Utility.ContainsKey), "All save/manifest fixed menu and column labels have dictionary entries");
        Require(KsaUiLanguages.UtilityWindowPatches.PlainCaption("FixedStar").ToString()=="恒星"
            && KsaUiLanguages.UtilityWindowPatches.PlainCaption("PlanetaryBody").ToString()=="行星类天体"
            && KsaUiLanguages.UtilityWindowPatches.PlainCaption("AtmosphericBody").ToString()=="有大气天体", "Observed Core body implementation types have scoped readable names");
        Require(KsaUiLanguages.UtilityWindowPatches.MenuCaption("Player Custom Save").ToString()=="Player Custom Save", "Unknown save labels are not changed");
        var message=AccessTools.Method(typeof(KsaUiLanguages.UtilityWindowPatches),"TranslateConfirmMessage");
        const string custom="Earth's {0}";
        string source="Are you sure you want to delete save '"+custom+"'?";
        string translated=(string)message.Invoke(null,new object[]{"DELETE SAVE",source})!;
        Require(translated=="确定要删除游戏存档“"+custom+"”吗？", "Save confirmation preserves quotes and placeholder-like text in the actual filename");
        Require((string)message.Invoke(null,new object[]{"Unknown","Player Custom Message"})! == "Player Custom Message", "Unknown confirmation text remains unchanged");
        foreach(var method in KsaUiLanguages.UtilityWindowPatches.TargetMethods())
        {
            var original=PatchProcessor.GetOriginalInstructions(method).Select(i=>new CodeInstruction(i)).ToList();
            var patched=KsaUiLanguages.UtilityWindowPatches.Translate(original,method).ToList();
            Require(patched.Where(i=>i.opcode==System.Reflection.Emit.OpCodes.Ldstr).Select(i=>i.operand)
                .SequenceEqual(original.Where(i=>i.opcode==System.Reflection.Emit.OpCodes.Ldstr).Select(i=>i.operand)), "Save/manifest method retains source names, formats and IDs");
            Require(patched.Count(i=>i.operand is MethodInfo m && m.DeclaringType==typeof(KSA.StringInputPopup) && m.Name=="Create")
                ==original.Count(i=>i.operand is MethodInfo m && m.DeclaringType==typeof(KSA.StringInputPopup) && m.Name=="Create"), "Input popup creation and callbacks use the original native call");
        }
        results.Add("PASS: Save and manifest captions, dynamic confirmation names and original input callbacks");
        var popup=(KSA.StringInputPopup)RuntimeHelpers.GetUninitializedObject(typeof(KSA.StringInputPopup));
        var title=AccessTools.Field(typeof(KSA.StringInputPopup),"_title");
        title.SetValue(popup,"SAVE GAME");
        KsaUiLanguages.HudSavePopupScopePatch.Enter(popup,out var previous);
        try
        {
            Require(KsaUiLanguages.HudSavePopupCaptionPatch.DisplayTitle("SAVE GAME")=="保存游戏"
                && KsaUiLanguages.HudSavePopupCaptionPatch.DisplayPrompt("Please enter a filename to continue.")=="请输入存档名称。", "Game-save input popup has scoped Chinese title and filename hint");
            Require((string)title.GetValue(popup)! == "SAVE GAME", "Save popup keeps its original stored title");
        }
        finally { KsaUiLanguages.HudSavePopupScopePatch.Leave(previous); }
        Require(KsaUiLanguages.HudSavePopupCaptionPatch.DisplayPrompt("Please enter a filename to continue.")=="Please enter a filename to continue.", "Unknown filename popups remain outside save scope");
        KsaUiLanguages.Runtime.ApplyLanguage("en-US");
        foreach(string label in labels)
            Require(HashGuiId(KsaUiLanguages.UtilityWindowPatches.MenuCaption(label).ToString(),0x735A1913)==HashGuiId(chinese[label],0x735A1913), "Save/manifest control ID remains stable across languages");
        Require((string)message.Invoke(null,new object[]{"DELETE SAVE",source})! ==source
            && KsaUiLanguages.HudSavePopupCaptionPatch.DisplayTitle("SAVE GAME")=="SAVE GAME", "Save confirmation and title restore exact English");
        KsaUiLanguages.Runtime.ApplyLanguage("zh-CN");
        results.Add("PASS: Save popup display scope, unchanged title field and native menu ID round trip");
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
