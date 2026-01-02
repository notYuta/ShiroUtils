using System;
using System.IO;
using System.Numerics;
using System.Text.Json;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using ShiroUtils.Modules.MobHunt.Models;

namespace ShiroUtils.Modules.MobHunt;

public unsafe class MobHuntModule : IDisposable
{
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly IClientState clientState;
    private readonly IDataManager dataManager;
    private readonly IPluginLog log;
    private readonly IAddonLifecycle addonLifecycle;
    private readonly Configuration configuration;

    public MobLocationData? MobLocationData { get; private set; }

    public MobHuntModule(
        IDalamudPluginInterface pluginInterface,
        IClientState clientState, 
        IDataManager dataManager, 
        IPluginLog log, 
        IAddonLifecycle addonLifecycle,
        Configuration configuration)
    {
        this.pluginInterface = pluginInterface;
        this.clientState = clientState;
        this.dataManager = dataManager;
        this.log = log;
        this.addonLifecycle = addonLifecycle;
        this.configuration = configuration;

        LoadMobLocationData();

        this.addonLifecycle.RegisterListener(AddonEvent.PostSetup, "AreaMap", OnMapRefresh);
        this.addonLifecycle.RegisterListener(AddonEvent.PostRefresh, "AreaMap", OnMapRefresh);
        this.addonLifecycle.RegisterListener(AddonEvent.PostUpdate, "AreaMap", OnMapRefresh);
        
        log.Information("MobHuntModule initialized");
    }

    private void OnMapRefresh(AddonEvent type, AddonArgs args)
    {
        if (!configuration.EnableMobHunt) return;
        RefreshMarkers();
    }

    private void LoadMobLocationData()
    {
        try
        {
            var jsonPath = Path.Combine(pluginInterface.AssemblyLocation.Directory?.FullName!, "Data", "MobLocations.json");
            if (!File.Exists(jsonPath))
            {
                // Try parent directory if running from dev env sometimes
                var parentPath = Path.Combine(pluginInterface.AssemblyLocation.Directory?.Parent?.FullName!, "Data", "MobLocations.json");
                if (File.Exists(parentPath))
                {
                    jsonPath = parentPath;
                }
                else
                {
                    log.Warning($"MobLocations.json not found at {jsonPath}");
                    return;
                }
            }

            var jsonContent = File.ReadAllText(jsonPath);
            var data = JsonSerializer.Deserialize<MobLocationData>(jsonContent);
            if (data != null)
            {
                MobLocationData = data;
                log.Information($"Loaded mob location data version {data.Version}");
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to load mob location data");
        }
    }

    private void RefreshMarkers()
    {
        var agentMap = AgentMap.Instance();
        if (agentMap == null) return;

        if (MobLocationData == null) return;

        var territoryId = (ushort)agentMap->SelectedTerritoryId;
        if (territoryId == 0)
        {
            territoryId = clientState.TerritoryType;
        }

        var territoryData = MobLocationData.Data.Find(t => t.TerritoryTypeId == territoryId);
        if (territoryData == null) return;

        var mapTitle = agentMap->MapTitleString.ToString();
        string? targetMobName = null;

        if (!string.IsNullOrEmpty(mapTitle))
        {
            var start = mapTitle.LastIndexOf('（');
            var end = mapTitle.LastIndexOf('）');
            
            if (start != -1 && end != -1 && end > start)
            {
                targetMobName = mapTitle.Substring(start + 1, end - start - 1).Trim();
            }
            else
            {
                start = mapTitle.LastIndexOf('(');
                end = mapTitle.LastIndexOf(')');
                
                if (start != -1 && end != -1 && end > start)
                {
                    targetMobName = mapTitle.Substring(start + 1, end - start - 1).Trim();
                }
            }
        }

        if (string.IsNullOrEmpty(targetMobName)) return;

        // Note: ResetMapMarkers clears ALL markers, including those from other plugins potentially.
        // However, this logic was in the original plugin.
        agentMap->ResetMapMarkers();

        ushort sizeFactor = 100;
        short offsetX = 0;
        short offsetY = 0;

        var mapId = agentMap->SelectedMapId > 0 ? agentMap->SelectedMapId : clientState.MapId;

        if (dataManager.GetExcelSheet<Map>().TryGetRow(mapId, out var mapRow))
        {
            sizeFactor = mapRow.SizeFactor;
            offsetX = mapRow.OffsetX;
            offsetY = mapRow.OffsetY;
        }

        var bnpcSheet = dataManager.GetExcelSheet<BNpcName>();

        foreach (var mob in territoryData.Mobs)
        {
            bool isMatch = false;

            if (bnpcSheet != null && bnpcSheet.TryGetRow(mob.BNpcNameId, out var bnpcRow))
            {
                var localName = bnpcRow.Singular.ToString();
                
                if (!string.IsNullOrEmpty(localName) && string.Equals(targetMobName, localName, StringComparison.OrdinalIgnoreCase))
                {
                    isMatch = true;
                }
                else if (!string.IsNullOrEmpty(localName) && localName.Contains(targetMobName, StringComparison.OrdinalIgnoreCase))
                {
                    isMatch = true;
                }
            }

            if (!isMatch && !string.IsNullOrEmpty(mob.MobName))
            {
                if (string.Equals(targetMobName, mob.MobName, StringComparison.OrdinalIgnoreCase))
                {
                    isMatch = true;
                }
            }
            
            if (!isMatch) continue;
            
            foreach (var location in mob.Locations)
            {
                var worldPos = MapCoordToWorld(location.X, location.Y, sizeFactor, offsetX, offsetY);
                agentMap->AddMapMarker(worldPos, configuration.MobHuntMarkerIconId, scale: 0);
            }
        }
    }

    private Vector3 MapCoordToWorld(float mapX, float mapY, ushort sizeFactor, short offsetX, short offsetY)
    {
        var scale = sizeFactor / 100.0f;
        var worldX = (mapX - 21.0f) * 50.0f / scale - offsetX;
        var worldZ = (mapY - 21.0f) * 50.0f / scale - offsetY;
        
        return new Vector3(worldX, 0, worldZ);
    }

    public void Dispose()
    {
        addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "AreaMap", OnMapRefresh);
        addonLifecycle.UnregisterListener(AddonEvent.PostRefresh, "AreaMap", OnMapRefresh);
        addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, "AreaMap", OnMapRefresh);
    }
}
