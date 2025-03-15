using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace AutoBuild
{
    [BepInPlugin("kumor.plugin.AutoBuild", "DSP_Kumor_AutoBuild", "1.0.4.0")]
    [BepInProcess("DSPGAME.exe")]
    public class AutoBuildPlugin : BaseUnityPlugin
    {
        public const string GUID = "kumor.plugin.AutoBuild";
        public const string NAME = "DSP_Kumor_AutoBuild";
        public const string VERSION = "1.0.4.0";
        public const string GAME_VERSION = "0.8.23.9832";
        public const string GAME_PROCESS = "DSPGAME.exe";
        public static ManualLogSource logger;
        public static Player player;
        public static PlayerAction_Build actionBuild;
        public static ConfigEntry<bool> AutoBuild;
        public static bool AutoBuildFlag;
        public static int PowerFlag;
        public static ConfigEntry<float> PowerPer;
        public static ConfigEntry<bool> AutoPowerCharger;
        public static ConfigEntry<float> PowerChargerDt;
        public static ConfigEntry<bool> droneEject;
        public static ConfigEntry<bool> WalkBuild;

        private void Awake()
        {
            AutoBuildPlugin.logger = this.Logger;
            new Harmony("kumor.plugin.AutoBuild").PatchAll(typeof(AutoBuildPatch));
            AutoBuildPlugin.AutoBuild = this.Config.Bind<bool>("自动建造", "是否开启自动建造", true, new ConfigDescription("Whether to enable automatic construction", (AcceptableValueBase)null, new object[0]));
            AutoBuildPlugin.AutoPowerCharger = this.Config.Bind<bool>("自动建造", "是否开启自动充电", true, new ConfigDescription("Whether to enable automatic charging", (AcceptableValueBase)null, new object[0]));
            AutoBuildPlugin.PowerPer = this.Config.Bind<float>("自动建造", "充电下限百分比", 0.3f, new ConfigDescription("the lower limit percentage of charging when automatic charging is enabled", (AcceptableValueBase)new AcceptableValueRange<float>(0.1f, 0.99f), new object[0]));
            AutoBuildPlugin.PowerChargerDt = this.Config.Bind<float>("自动建造", "充电站充电距离", 5.5f, new ConfigDescription("Charging station charging distance", (AcceptableValueBase)new AcceptableValueRange<float>(3f, 30f), new object[0]));
            AutoBuildPlugin.droneEject = this.Config.Bind<bool>("自动建造", "建造无人机", true, new ConfigDescription("Turn construction drone on and off", (AcceptableValueBase)null, new object[0]));
            AutoBuildPlugin.WalkBuild = this.Config.Bind<bool>("自动建造", "步行建造模式", false, new ConfigDescription("Limit construction to walking only", (AcceptableValueBase)null, new object[0]));
        }

        private void Start()
        {
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.N))
            {
                return;
            }

            if (!AutoBuildPlugin.AutoBuild.Value)
            {
                AutoBuildPlugin.logger.LogInfo("Key pressed, but Autobuild == false");
                return;
            }
            if (AutoBuildPlugin.player == null)
            {
                AutoBuildPlugin.logger.LogInfo("Key pressed, but Player == null: false);");
                return;
            }

            AutoBuildPlugin.logger.LogInfo("Toggling autobuild");
            AutoBuildPlugin.AutoBuildFlag = !AutoBuildPlugin.AutoBuildFlag;
            if (!AutoBuildPlugin.AutoBuildFlag)
                AutoBuildPatch.ExitAutoBuild();
            else if (!AutoBuildPlugin.WalkBuild.Value)
                AutoBuildPatch.Fly();
            AutoBuildPlugin.logger.LogInfo((object)("AutoBuildFlag: " + AutoBuildPlugin.AutoBuildFlag.ToString() + "  KeyCode.N"));
        }

        private void OnDestroy()
        {
        }
    }
}
