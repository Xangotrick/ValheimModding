using BepInEx;
using HarmonyLib;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using static ClutterSystem;
using static MeleeWeaponTrail;
using static MonoMod.InlineRT.MonoModRule;
using static pole_viking.pole_viking;
using static Skills;
using static TextViewer;
using Debug = UnityEngine.Debug;
using pole_viking;
using pole_UI;
using static pole_UI.UIX;
using pole_ReadWrite;
using pole_StatManager;
using pole_jobs;
using static pole_Data.Data;
using System.Media;
using System.CodeDom;

namespace j_mineur
{
    internal class __job: BaseUnityPlugin
    {
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.CanConsumeItem))]
        internal class GeodeConsume
        {
            private static bool Prefix(ref bool __result, Humanoid __instance, ItemDrop.ItemData item)
            {
                if(item.m_shared.m_name.ToLower().Contains("geode") && Player.m_localPlayer.transform == __instance.transform)
                {
                    Debug.Log(item.m_shared.m_name);
                    Player.m_localPlayer.transform.GetComponent<AudioSource>().Play();
                    int count = item.m_shared.m_name.Split('+').Length - 1;
                    string randdrop = Job.ProbabilityMatrixCalculation(count, Mineur.Matrix_GemNames, Mineur.Matrix_GemProbability);
                    Imanip.spawnItemMaxStack(randdrop, 1);
                    __result = true;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(WearNTear), "RPC_Damage")]
        internal class FindMonoDamage
        {
            private static void Prefix(WearNTear __instance, ref float hit)
            {
                Debug.LogError(__instance.name);
                Debug.LogError("test");
                if (__instance.name.ToLower().Contains("monolith"))
                {
                    Imanip.spawnItemMaxStack("Stone", 10);
                }
            }
        }


        [HarmonyPatch(typeof(MineRock5), "RPC_SetAreaHealth")]
        internal class  SetMiningFlag
        {
            private static void Prefix(MineRock5 __instance, ref float health)
            {
                Debug.LogError(__instance.name);
                Debug.LogError(health);
                if(__instance.name.ToLower().Contains("copper") && health < 0 && job_manager_local[0].level >= 2 && EnvMan.instance.IsNight())
                {
                    Imanip.spawnItemMaxStack("CopperOreMoon", 1, -1) ;
                }
                if (__instance.name.ToLower().Contains("rock") && health < 0)
                {
                    Imanip.spawnItemMaxStack("CopperOreMoon", 1, 5);
                    Imanip.spawnItemMaxStack("CopperOreMoon", 1, 2);
                    Imanip.spawnItemMaxStack("Rock", 1, 1);
                }
            }
        }
         
    }

    public class Mineur
    {

        public static string[,] Matrix_GemNames = 
        {
            {"Quartz", "Unakite","Agate","Magnetite","Salt"},
            {"Labradorite", "LapisLazuli","Hematite","Tourmaline","FalconEye"},
            {"Amethyst", "Jasper","Citrine","Carnelian","TigerEye"},
            {"Diamond", "BlackOpal","Sapphire","Emerald","Jade"},
            {"BloodDiamond", "ChaosOpal","StarSapphire","FrozenEmerald","AbsoluteJade"},
        };
        public static float[,] Matrix_GemProbability =
        {
            {0.16f,0.18f,0.2f,0.22f,0.24f},
            {0.12f,0.15f,0.19f,0.24f,0.30f},
            {0.09f,0.12f,0.18f,0.25f,0.36f},
            {0.06f,0.09f,0.16f,0.26f,0.43f},
            {0.03f,0.06f,0.13f,0.26f,0.52f}
        };
        public static void load_assets()
        {
            string[] normalores = new string[] { "TinOre", "SilverOre", "IronScrap", "CopperOre", "BlackMetalScrap" };
            foreach(string normalor in normalores)
            {
                clone_item( normalor + "Moon", normalor, 5);
                clone_item(normalor, normalor, 2, "", false);
            }

            clone_item("Geode", "Stone", 5, "", false);

            ItemManager.Instance.GetItem("Geode+").ItemDrop.m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Consumable;
            ItemManager.Instance.GetItem("Geode++").ItemDrop.m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Consumable;
            ItemManager.Instance.GetItem("Geode+++").ItemDrop.m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Consumable;
            ItemManager.Instance.GetItem("Geode++++").ItemDrop.m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Consumable;
            ItemManager.Instance.GetItem("Geode+++++").ItemDrop.m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Consumable;




            AssetBundle bundle_geode = AssetUtils.LoadAssetBundle("pole_viking_assets/bundle_gems");

            for (int line = 0; line < 5; line++)
            {
                for (int column = 0; column < 5; column++)
                {
                    string name = Matrix_GemNames[line, column];
                    clone_item(name, "Stone", 0, bundle: bundle_geode, bundleitem: "G"+(1+line).ToString()+name);
                }
            }



        }

        public static Job build_job()
        {

            Job returner = new Job("Mineur");
            returner.descriptions[0] = "";
            returner.descriptions[1] = "";

            returner.levelstatmods[2] = new StatMod[] { new StatMod("max_health", 22) };
            returner.levelstatmods[3] = new StatMod[] { new StatMod("max_stamina", 22), new StatMod("max_pod", 200) };

            returner.levelscorecaps[3] = new string[] { "A;100", "B;20", "C;30" };
            returner.levelscorecaps[4] = new string[] { "B;50", "D;100" };
            returner.levelscorecaps[5] = new string[] { "F;20", "G;20", "H;20" };


            return returner;
        }
    }

}