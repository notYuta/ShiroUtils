using System;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;

namespace ShiroUtils.Modules.QuickTryOn;

public unsafe class QuickTryOnModule : IDisposable
{
    private readonly IGameGui gameGui;
    private readonly IKeyState keyState;
    private readonly IDataManager dataManager;
    private readonly IPluginLog log;
    private readonly Configuration configuration;

    private const int VK_SHIFT = 0x10;

    private DateTime lastTryOnTime = DateTime.MinValue;

    public QuickTryOnModule(
        IGameGui gameGui,
        IKeyState keyState, 
        IDataManager dataManager, 
        IPluginLog log, 
        Configuration configuration)
    {
        this.gameGui = gameGui;
        this.keyState = keyState;
        this.dataManager = dataManager;
        this.log = log;
        this.configuration = configuration;

        this.gameGui.HoveredItemChanged += OnHoveredItemChanged;
        
        log.Information("QuickTryOnModule initialized");
    }

    private void OnHoveredItemChanged(object? sender, ulong rawItemId)
    {
        if (!configuration.EnableQuickTryOn || rawItemId == 0 || !keyState[VK_SHIFT])
            return;

        // アイテムIDに関わらず、まずクールダウンをチェック
        if (!CanTryOn())
            return;

        var baseItemId = GetBaseItemId(rawItemId);
        if (!IsEquipmentItem(baseItemId))
            return;

        ExecuteTryOn(baseItemId);
        lastTryOnTime = DateTime.Now;
    }

    private uint GetBaseItemId(ulong rawItemId)
    {
        if (rawItemId >= 1000000)
            return (uint)(rawItemId - 1000000);
        if (rawItemId >= 500000)
            return (uint)(rawItemId - 500000);
        return (uint)rawItemId;
    }

    private bool IsEquipmentItem(uint itemId)
    {
        var itemSheet = dataManager.GetExcelSheet<Item>();
        if (itemSheet == null)
            return false;

        var item = itemSheet.GetRowOrDefault(itemId);
        if (item == null)
            return false;

        return item.Value.EquipSlotCategory.RowId > 0;
    }

    private bool CanTryOn()
    {
        var elapsed = (DateTime.Now - lastTryOnTime).TotalMilliseconds;
        return elapsed >= configuration.QuickTryOnCooldownMs;
    }

    private void ExecuteTryOn(uint itemId)
    {
        try
        {
            AgentTryon.TryOn(0, itemId, 0, 0, 0, false);
            log.Debug($"Tried on item: {itemId}");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Failed to try on item {itemId}");
        }
    }

    public void Dispose()
    {
        gameGui.HoveredItemChanged -= OnHoveredItemChanged;
    }
}
