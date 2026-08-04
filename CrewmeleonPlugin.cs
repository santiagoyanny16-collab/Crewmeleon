using BepInEx;
using BepInEx.Unity.IL2CPP;
using Crewmeleon.Components;
using FungleAPI.Api;
using FungleAPI.GameOptions.Lobby;
using FungleAPI.PluginLoading;
using HarmonyLib;

namespace TheOldUs
{
    [BepInProcess("Among Us.exe")]
    [BepInPlugin("obit508.crewmeleon", "Crewmeleon", "0.1.1")]
    [BepInDependency(FungleApiPlugin.ModId)]
    public class CrewmeleonPlugin : BasePlugin, IFungleBasePlugin
    {
        public static Harmony Harmony = new Harmony("obit508.crewmeleon");

        public static ModPlugin Plugin => FunglePlugin<CrewmeleonPlugin>.Plugin;
        public override void Load()
        {
            Harmony.PatchAll();
            AddComponent<MouseIconBehaviour>();
        }
        public void LoadTabs(ModPlugin modPlugin) { }
    }
}
