using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace ShiroUtils;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 5; // Increment version

    // Module Settings will be added here
    public bool EnableMobHunt { get; set; } = true;
    public uint MobHuntMarkerIconId { get; set; } = 60434;

    public bool EnableGatherMap { get; set; } = true;
    public uint MiningPrimaryIconId { get; set; } = 60438;
    public uint MiningSecondaryIconId { get; set; } = 60437;
    
    public uint BotanyPrimaryIconId { get; set; } = 60433;
    public uint BotanySecondaryIconId { get; set; } = 60432;

    public bool EnableQuickTryOn { get; set; } = true;
    public int QuickTryOnCooldownMs { get; set; } = 500;

    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
        
        // Migration logic
        if (Version < 5)
        {
            MiningPrimaryIconId = 60438;
            MiningSecondaryIconId = 60437;
            BotanyPrimaryIconId = 60433;
            BotanySecondaryIconId = 60432;
            Version = 5;
            Save();
        }
    }

    public void Save()
    {
        this.pluginInterface!.SavePluginConfig(this);
    }
}
