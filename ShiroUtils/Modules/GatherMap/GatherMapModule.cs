using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;

namespace ShiroUtils.Modules.GatherMap;

public unsafe class GatherMapModule : IDisposable
{
    private readonly IClientState clientState;
    private readonly IDataManager dataManager;
    private readonly IPluginLog log;
    private readonly IAddonLifecycle addonLifecycle;
    private readonly IObjectTable objectTable;
    private readonly Configuration configuration;

    private const uint MinerJobId = 16;
    private const uint BotanistJobId = 17;

    private readonly Lumina.Excel.ExcelSheet<GatheringPoint>? gatheringPointSheet;
    private readonly Lumina.Excel.ExcelSheet<GatheringPointBase>? gatheringPointBaseSheet;

    private DateTime lastUpdateTime = DateTime.MinValue;
    private const double UpdateIntervalSeconds = 0.5;
    private bool wasEnabled = false;

    public GatherMapModule(
        IClientState clientState,
        IDataManager dataManager,
        IPluginLog log,
        IAddonLifecycle addonLifecycle,
        IObjectTable objectTable,
        Configuration configuration)
    {
        this.clientState = clientState;
        this.dataManager = dataManager;
        this.log = log;
        this.addonLifecycle = addonLifecycle;
        this.objectTable = objectTable;
        this.configuration = configuration;

        this.gatheringPointSheet = dataManager.GetExcelSheet<GatheringPoint>();
        this.gatheringPointBaseSheet = dataManager.GetExcelSheet<GatheringPointBase>();

        this.addonLifecycle.RegisterListener(AddonEvent.PostSetup, "AreaMap", OnMapRefresh);
        this.addonLifecycle.RegisterListener(AddonEvent.PostRefresh, "AreaMap", OnMapRefresh);
        this.addonLifecycle.RegisterListener(AddonEvent.PostUpdate, "AreaMap", OnMapRefresh);
        
        log.Information("GatherMapModule initialized");
    }

    private void OnMapRefresh(AddonEvent type, AddonArgs args)
    {
        // Check for state change from Enabled -> Disabled
        if (wasEnabled && !configuration.EnableGatherMap)
        {
            var agentMap = AgentMap.Instance();
            if (agentMap != null)
            {
                agentMap->ResetMapMarkers();
                log.Debug("GatherMap disabled, markers reset.");
            }
            wasEnabled = false;
            return;
        }

        wasEnabled = configuration.EnableGatherMap;
        if (!configuration.EnableGatherMap) return;

        RefreshMarkers();
    }

    private void RefreshMarkers()
    {
        if ((DateTime.Now - lastUpdateTime).TotalSeconds < UpdateIntervalSeconds)
        {
            return;
        }
        lastUpdateTime = DateTime.Now;

        var agentMap = AgentMap.Instance();
        if (agentMap == null) return;

        if (gatheringPointSheet == null || gatheringPointBaseSheet == null) return;

        var localPlayer = objectTable.LocalPlayer;
        if (localPlayer == null) return;

        var currentJobId = localPlayer.ClassJob.RowId;
        bool isMiner = currentJobId == MinerJobId;
        bool isBotanist = currentJobId == BotanistJobId;

        if (!isMiner && !isBotanist)
        {
            return;
        }

        var mapTitle = agentMap->MapTitleString.ToString();
        if (!string.IsNullOrEmpty(mapTitle))
        {
            var start = mapTitle.LastIndexOf('（');
            var end = mapTitle.LastIndexOf('）');
            if (start != -1 && end != -1 && end > start)
            {
                return; 
            }
            
            start = mapTitle.LastIndexOf('(');
            end = mapTitle.LastIndexOf(')');
            if (start != -1 && end != -1 && end > start)
            {
                return; 
            }
        }

        var markersToAdd = new List<(Vector3 position, uint iconId)>();

        foreach (var obj in objectTable)
        {
            if (obj.ObjectKind != ObjectKind.GatheringPoint) continue;

            if (!obj.IsTargetable) continue;

            if (!gatheringPointSheet.TryGetRow(obj.BaseId, out var gpRow)) continue;
            var baseId = gpRow.GatheringPointBase.RowId;
            if (!gatheringPointBaseSheet.TryGetRow(baseId, out var baseRow)) continue;

            var typeId = baseRow.GatheringType.RowId;
            
            bool isMiningNode = (typeId == 0 || typeId == 1);
            bool isBotanyNode = (typeId == 2 || typeId == 3);
            bool isPrimaryTool = (typeId == 0 || typeId == 2); 

            if (!isMiningNode && !isBotanyNode) continue;

            if (isMiner && !isMiningNode) continue;
            if (isBotanist && !isBotanyNode) continue;

            uint iconId;
            if (isMiningNode)
            {
                iconId = isPrimaryTool ? configuration.MiningPrimaryIconId : configuration.MiningSecondaryIconId;
            }
            else
            {
                iconId = isPrimaryTool ? configuration.BotanyPrimaryIconId : configuration.BotanySecondaryIconId;
            }

            markersToAdd.Add((obj.Position, iconId));
        }

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

        foreach (var (position, iconId) in markersToAdd)
        {
            var scale = sizeFactor / 100.0f;
            var mapX = ((position.X + offsetX) * scale / 50.0f) + 21.0f;
            var mapY = ((position.Z + offsetY) * scale / 50.0f) + 21.0f;
            
            var markerPos = MapCoordToWorld(mapX, mapY, sizeFactor, offsetX, offsetY);
            
            agentMap->AddMapMarker(markerPos, iconId, scale: 0);
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
