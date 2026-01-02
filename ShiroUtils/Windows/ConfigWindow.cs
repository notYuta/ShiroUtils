using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

namespace ShiroUtils.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    private enum ConfigCategory
    {
        All,
        Map,
        Items
    }

    private ConfigCategory selectedCategory = ConfigCategory.All;

    // 各機能の設定が展開されているかどうかを保持する
    private bool showMobHuntSettings = false;
    private bool showGatherMapSettings = false;
    private bool showQuickTryOnSettings = false;
    
    // 検索文字列
    private string searchText = "";

    public ConfigWindow(Configuration configuration) : base("ShiroUtils Settings")
    {
        this.configuration = configuration;
        
        Size = new Vector2(700, 500);
        SizeCondition = ImGuiCond.FirstUseEver;
        Flags = ImGuiWindowFlags.None; 
    }

    public override void Draw()
    {
        // --- Header (Search) ---
        ImGui.SetNextItemWidth(200);
        ImGui.InputTextWithHint("##Search", "Search features...", ref searchText, 64);
        
        ImGui.Spacing();
        ImGui.Separator();

        // --- Layout ---
        ImGui.BeginChild("##LeftSidebar", new Vector2(150, 0), true);
        
        DrawCategoryItem("All Features", ConfigCategory.All);
        ImGui.Separator();
        DrawCategoryItem("Map & Travel", ConfigCategory.Map);
        DrawCategoryItem("Items & Inventory", ConfigCategory.Items);
        
        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("##RightContent", new Vector2(0, 0), false);

        // 機能リストの描画
        DrawFeatures();

        ImGui.EndChild();
    }

    private void DrawCategoryItem(string label, ConfigCategory category)
    {
        bool isSelected = selectedCategory == category;
        if (ImGui.Selectable(label, isSelected))
        {
            selectedCategory = category;
        }
    }

    private void DrawFeatures()
    {
        // 検索フィルター
        bool Filter(string name, ConfigCategory category)
        {
            if (selectedCategory != ConfigCategory.All && selectedCategory != category)
                return false;
            
            if (!string.IsNullOrEmpty(searchText) && !name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        // --- Map Category ---
        if (Filter("Mob Hunt Overlay", ConfigCategory.Map))
        {
            bool enabled = configuration.EnableMobHunt;
            DrawFeatureRow("Mob Hunt Overlay", "Displays markers for B-rank mobs on the map.", ref enabled, ref showMobHuntSettings, () =>
            {
                configuration.EnableMobHunt = enabled;
                configuration.Save();
            }, DrawMobHuntDetails);
        }

        if (Filter("Gather Map Overlay", ConfigCategory.Map))
        {
            bool enabled = configuration.EnableGatherMap;
            DrawFeatureRow("Gather Map Overlay", "Displays gathering nodes on the map based on your current job.", ref enabled, ref showGatherMapSettings, () =>
            {
                configuration.EnableGatherMap = enabled;
                configuration.Save();
            }, DrawGatherMapDetails);
        }

        // --- Items Category ---
        if (Filter("Quick Try On", ConfigCategory.Items))
        {
            bool enabled = configuration.EnableQuickTryOn;
            DrawFeatureRow("Quick Try On", "Shift + Hover over items to try them on.", ref enabled, ref showQuickTryOnSettings, () =>
            {
                configuration.EnableQuickTryOn = enabled;
                configuration.Save();
            }, DrawQuickTryOnDetails);
        }
    }

    private void DrawFeatureRow(string name, string description, ref bool enabled, ref bool showSettings, Action onToggle, Action? drawDetails)
    {
        ImGui.BeginGroup();

        if (ImGui.Checkbox($"##{name}_toggle", ref enabled))
        {
            onToggle();
        }

        ImGui.SameLine();

        bool showArrow = drawDetails != null && enabled;

        if (showArrow)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1, 1, 1, 0.1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(1, 1, 1, 0.2f));

            if (ImGui.ArrowButton($"##{name}_settings", showSettings ? ImGuiDir.Down : ImGuiDir.Right))
            {
                showSettings = !showSettings;
            }

            ImGui.PopStyleColor(3);
        }
        else
        {
            ImGui.Dummy(new Vector2(ImGui.GetFrameHeight(), ImGui.GetFrameHeight()));
            if (!enabled) showSettings = false;
        }
        
        ImGui.SameLine();
        ImGui.TextColored(enabled ? new Vector4(1, 1, 1, 1) : new Vector4(0.5f, 0.5f, 0.5f, 1), name);
        ImGui.SameLine();
        ImGui.TextDisabled($"- {description}");

        ImGui.EndGroup();

        if (drawDetails != null && showSettings)
        {
            ImGui.Indent(28.0f);
            ImGui.Dummy(new Vector2(0, 5));
            drawDetails();
            ImGui.Dummy(new Vector2(0, 5));
            ImGui.Unindent(28.0f);
        }
        
        ImGui.Separator();
    }

    // 共通UIパーツ: アイコン設定行
    private void DrawIconRow(string label, uint currentValue, Action<uint> updateAction)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text(label);
        ImGui.SameLine(160); // 固定幅で揃える
        
        var val = (int)currentValue;
        ImGui.SetNextItemWidth(100);
        if (ImGui.InputInt($"##{label}", ref val))
        {
            if (val < 0) val = 0;
            updateAction((uint)val);
            configuration.Save();
        }
    }

    private void DrawMobHuntDetails()
    {
        ImGui.Text("Icon Settings (Advanced)");
        ImGui.Spacing();

        DrawIconRow("B-Rank Mob", configuration.MobHuntMarkerIconId, v => configuration.MobHuntMarkerIconId = v);
        
        // ImGui.SameLine();
        // ImGui.TextDisabled("(Default: 60434)");

        ImGui.Spacing();

        if (ImGui.Button("Reset to Default##MobHunt"))
        {
            configuration.MobHuntMarkerIconId = 60434;
            configuration.Save();
        }
    }

    private void DrawGatherMapDetails()
    {
        ImGui.Text("Icon Settings (Advanced)");
        ImGui.Spacing();

        // Mining
        ImGui.TextDisabled("Mining");
        ImGui.Indent(10.0f);
        DrawIconRow("Quarrying", configuration.MiningSecondaryIconId, v => configuration.MiningSecondaryIconId = v);
        DrawIconRow("Mining", configuration.MiningPrimaryIconId, v => configuration.MiningPrimaryIconId = v);
        ImGui.Unindent(10.0f);

        ImGui.Dummy(new Vector2(0, 5));

        // Botany
        ImGui.TextDisabled("Botany");
        ImGui.Indent(10.0f);
        DrawIconRow("Harvesting", configuration.BotanySecondaryIconId, v => configuration.BotanySecondaryIconId = v);
        DrawIconRow("Logging", configuration.BotanyPrimaryIconId, v => configuration.BotanyPrimaryIconId = v);
        ImGui.Unindent(10.0f);
        
        ImGui.Spacing();

        if (ImGui.Button("Reset Icons to Defaults"))
        {
            configuration.MiningPrimaryIconId = 60438;
            configuration.MiningSecondaryIconId = 60437;

            configuration.BotanyPrimaryIconId = 60433;
            configuration.BotanySecondaryIconId = 60432;
            
            configuration.Save();
        }
    }

    private void DrawQuickTryOnDetails()
    {
        var cooldown = configuration.QuickTryOnCooldownMs;
        
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Cooldown (ms)");
        ImGui.SameLine(160);
        
        if (ImGui.SliderInt("##Cooldown", ref cooldown, 100, 2000))
        {
            configuration.QuickTryOnCooldownMs = cooldown;
            configuration.Save();
        }
        
        ImGui.Spacing();

        if (ImGui.Button("Reset to Default##QuickTryOn"))
        {
            configuration.QuickTryOnCooldownMs = 500;
            configuration.Save();
        }
    }

    public void Dispose()
    {
    }
}
