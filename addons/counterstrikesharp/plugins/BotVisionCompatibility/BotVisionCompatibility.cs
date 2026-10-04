using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace BotVisionCompatibility;

public sealed partial class BotVisionCompatibilityPlugin : BasePlugin
{
    public override string ModuleName => "BotVision Compatibility";
    public override string ModuleVersion => "0.1.4-fairplay.1";
    public override string ModuleAuthor => "CS2-Bot-Improver";
    public override string ModuleDescription => "Selects BotVision's tested fast smoke mode";

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnMetamodAllPluginsLoaded>(() => SelectFastSmokeMode("Metamod startup"));
        RegisterListener<Listeners.OnMapStart>(_ => SelectFastSmokeMode("map start"));
        SelectFastSmokeMode(hotReload ? "hot reload" : "plugin load");
    }

    private void SelectFastSmokeMode(string reason) => Server.NextFrame(() =>
    {
        Server.ExecuteCommand("bv_smoke_mode 1");
    });

}
