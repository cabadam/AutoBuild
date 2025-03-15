using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace AutoBuild
{
    internal class AutoBuildPatch
    {
        private static Text modText;

        public static void Fly()
        {
            if (AutoBuildPlugin.WalkBuild.Value || AutoBuildPlugin.player.controller.mecha.thrusterLevel <= 0 || AutoBuildPlugin.player.movementState != EMovementState.Walk)
                return;
            AutoBuildPlugin.player.movementState = EMovementState.Fly;
            AutoBuildPlugin.player.controller.actionWalk.jumpCoolTime = 0.3f;
            AutoBuildPlugin.player.controller.actionWalk.jumpedTime = 0.0f;
            AutoBuildPlugin.player.controller.actionWalk.flyUpChance = 0.0f;
            AutoBuildPlugin.player.controller.actionWalk.SwitchToFly();
        }

        public static void Walk()
        {
            if (AutoBuildPlugin.player.movementState != EMovementState.Fly)
                return;
            AutoBuildPlugin.player.movementState = EMovementState.Walk;
            AutoBuildPlugin.player.controller.movementStateInFrame = EMovementState.Walk;
            AutoBuildPlugin.player.controller.softLandingTime = 1.2f;
        }

        public static void ExitAutoBuild(string extraTip = null)
        {
            AutoBuildPlugin.AutoBuildFlag = false;
            AutoBuildPlugin.PowerFlag = 0;
            string text = "自动建造模式结束";
            if (extraTip != null)
                text = text + "-" + extraTip;
            UIRealtimeTip.Popup(text);
            if ((Object)AutoBuildPatch.modText != (Object)null && AutoBuildPatch.modText.IsActive())
            {
                AutoBuildPatch.modText.gameObject.SetActive(false);
                AutoBuildPatch.modText.text = string.Empty;
            }
            AutoBuildPlugin.logger.LogInfo((object)("ExitAutoBuild => " + text));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIGeneralTips), "_OnUpdate")]
        public static void UIGeneralTips_OnUpdate(UIGeneralTips __instance)
        {
            if (!AutoBuildPlugin.AutoBuild.Value || !AutoBuildPlugin.AutoBuildFlag)
                return;
            AutoBuildPatch.modText = Traverse.Create((object)__instance).Field("modeText").GetValue<Text>();
            AutoBuildPatch.modText.rectTransform.anchoredPosition = new Vector2(0.0f, 160f);
            AutoBuildPatch.modText.gameObject.SetActive(true);
            switch (AutoBuildPlugin.PowerFlag)
            {
                case 0:
                    AutoBuildPatch.modText.text = "自动建造模式";
                    break;
                case 1:
                    AutoBuildPatch.modText.text = "正在寻找充电站...";
                    break;
                case 2:
                    AutoBuildPatch.modText.text = "正在充电...";
                    break;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerController), "Init")]
        public static void PlayerControllerInit(PlayerController __instance)
        {
            AutoBuildPlugin.player = __instance.player;
            AutoBuildPlugin.actionBuild = __instance.actionBuild;
            AutoBuildPlugin.logger.LogInfo((object)"PlayerController Init OK");
        }

        private static bool PowerCharger(int mode)
        {
            bool flag = false;
            Vector3 vector3_1 = new Vector3(0.0f, 0.0f, 0.0f);
            float num1 = 999f;
            Vector3 vector3_2;
            foreach (PowerNodeComponent powerNodeComponent in GameMain.localPlanet.factory.powerSystem.nodePool)
            {
                if (powerNodeComponent.id != 0 && powerNodeComponent.isCharger && AutoBuildPlugin.player.planetData.factory.entityPool[powerNodeComponent.entityId].protoId == (short)2202)
                {
                    if ((double)vector3_1.magnitude == 0.0)
                    {
                        flag = true;
                        vector3_1 = powerNodeComponent.powerPoint;
                        vector3_2 = vector3_1 - AutoBuildPlugin.player.position;
                        num1 = vector3_2.magnitude;
                    }
                    else
                    {
                        double num2 = (double)num1;
                        vector3_2 = powerNodeComponent.powerPoint - AutoBuildPlugin.player.position;
                        double magnitude = (double)vector3_2.magnitude;
                        if (num2 > magnitude)
                        {
                            flag = true;
                            vector3_1 = powerNodeComponent.powerPoint;
                            vector3_2 = vector3_1 - AutoBuildPlugin.player.position;
                            num1 = vector3_2.magnitude;
                        }
                    }
                }
            }
            if (!flag)
                return false;
            switch (mode)
            {
                case 0:
                    AutoBuildPlugin.PowerFlag = 1;
                    AutoBuildPlugin.logger.LogInfo((object)("PowerNode Dt: " + num1.ToString()));
                    break;
                case 1:
                    AutoBuildPatch.Walk();
                    if ((double)num1 < (double)AutoBuildPlugin.PowerChargerDt.Value)
                        AutoBuildPlugin.PowerFlag = 2;
                    AutoBuildPlugin.logger.LogInfo((object)("PowerNode Dt: " + num1.ToString()));
                    break;
            }
            AutoBuildPlugin.player.Order(new OrderNode()
            {
                target = vector3_1,
                type = EOrderType.Move
            }, false);
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerController), "GetInput")]
        public static void PlayerControllerGetInput()
        {
            if (!AutoBuildPlugin.AutoBuild.Value || !AutoBuildPlugin.AutoBuildFlag)
                return;
            if ((bool)VFInput._moveForward || (bool)VFInput._moveBackward || (bool)VFInput._moveLeft || (bool)VFInput._moveRight || VFInput.alt)
                AutoBuildPatch.ExitAutoBuild("玩家移动");
            else if (AutoBuildPlugin.player.movementState != EMovementState.Fly && AutoBuildPlugin.PowerFlag == 0 && !AutoBuildPlugin.WalkBuild.Value || AutoBuildPlugin.player.movementState == EMovementState.Sail)
                AutoBuildPatch.ExitAutoBuild("退出飞行");
            else if (UIGame.viewMode == EViewMode.Build)
            {
                AutoBuildPatch.ExitAutoBuild("进入手动建造模式");
            }
            else
            {
                if (GameMain.localPlanet == null || AutoBuildPlugin.actionBuild == null || AutoBuildPlugin.actionBuild.clickTool == null || AutoBuildPlugin.actionBuild.clickTool.factory == null || AutoBuildPlugin.actionBuild.clickTool.factory.prebuildPool == null)
                    return;
                if (AutoBuildPlugin.player.currentOrder != null && !AutoBuildPlugin.player.currentOrder.targetReached)
                {
                    if ((double)(AutoBuildPlugin.player.position - AutoBuildPlugin.player.currentOrder.target).magnitude >= 20.0)
                        return;
                    AutoBuildPlugin.player.AchieveOrder();
                }
                else
                {
                    if (AutoBuildPlugin.AutoPowerCharger.Value)
                    {
                        double num = AutoBuildPlugin.player.mecha.coreEnergy / AutoBuildPlugin.player.mecha.coreEnergyCap;
                        if (AutoBuildPlugin.PowerFlag > 0)
                        {
                            if (num <= 0.99)
                            {
                                if (AutoBuildPlugin.PowerFlag == 1)
                                {
                                    AutoBuildPatch.PowerCharger(1);
                                    AutoBuildPlugin.logger.LogInfo((object)"PowerCharger(1)...");
                                    return;
                                }
                                int powerFlag = AutoBuildPlugin.PowerFlag;
                                return;
                            }
                            if (num > 0.99)
                            {
                                AutoBuildPlugin.PowerFlag = 0;
                                AutoBuildPatch.Fly();
                                AutoBuildPlugin.logger.LogInfo((object)"PowerCharger(exit)...");
                            }
                        }
                        else if (num < (double)AutoBuildPlugin.PowerPer.Value)
                        {
                            if (AutoBuildPatch.PowerCharger(0))
                            {
                                AutoBuildPlugin.logger.LogInfo((object)"PowerCharger(0)...");
                                return;
                            }
                            AutoBuildPatch.ExitAutoBuild("能量过低");
                            return;
                        }
                    }
                    bool flag = false;
                    Vector3 vector3 = new Vector3(0.0f, 0.0f, 0.0f);
                    float num1 = 999f;
                    foreach (PrebuildData prebuildData in AutoBuildPlugin.actionBuild.clickTool.factory.prebuildPool)
                    {
                        if (prebuildData.id != 0 && (prebuildData.itemRequired == 0 || prebuildData.itemRequired <= AutoBuildPlugin.player.package.GetItemCount((int)prebuildData.protoId)))
                        {
                            if ((double)vector3.magnitude == 0.0)
                            {
                                flag = true;
                                vector3 = prebuildData.pos;
                                num1 = (vector3 - AutoBuildPlugin.player.position).magnitude;
                            }
                            else if ((double)num1 > (double)(prebuildData.pos - AutoBuildPlugin.player.position).magnitude)
                            {
                                flag = true;
                                vector3 = prebuildData.pos;
                                num1 = (vector3 - AutoBuildPlugin.player.position).magnitude;
                            }
                        }
                    }
                    if (!flag)
                        return;
                    AutoBuildPlugin.player.Order(new OrderNode()
                    {
                        target = vector3,
                        type = EOrderType.Move
                    }, false);
                }
            }
        }

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(MechaDroneLogic), "UpdateTargets")]
        //public static bool MechaDroneLogicUpdateTargets() => AutoBuildPlugin.droneEject.Value;
    }
}
