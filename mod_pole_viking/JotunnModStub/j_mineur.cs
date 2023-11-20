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
using System.Linq.Expressions;
using static Interpolate;
using System.Xml.Linq;
using pole_Data;

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
        internal class ManageMonolith
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

        [HarmonyPatch(typeof(Destructible), "RPC_Damage")]
        internal class SmallDrop
        {
            /*private static void Prefix(Destructible __instance, HitData hit)
            {
                if (hit.m_attacker.ID == Player.m_localPlayer.GetZDOID().ID)
                {
                    Mineur.H_fixbetterdrops(__instance.m_dropItems, __instance.name);
                }
            }*/
            private static void Postfix(Destructible __instance, HitData hit)
            {
                bool instanceview = (bool)Traverse.Create(__instance).Field("m_destroyed").GetValue();
                if (instanceview && hit.m_attacker.ID == Player.m_localPlayer.GetZDOID().ID)
                {
                    Mineur.H_on_ore_destroy(__instance.name);
                }
            }
        }

        [HarmonyPatch(typeof(MineRock5), "DamageArea")]
        internal class BigDrop
        {
            private static void Prefix(MineRock5 __instance, HitData hit)
            {
                if (hit.m_attacker.ID == Player.m_localPlayer.GetZDOID().ID)
                {
                    Mineur.H_fixbetterdrops(__instance);
                }
            }
            private static void Postfix(MineRock5 __instance, int hitAreaIndex, HitData hit)
            {
                object hitArea = typeof(MineRock5).GetMethod("GetHitArea", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { hitAreaIndex });
                typeof(MineRock5).GetMethod("LoadHealth", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);

                float H = (float)(hitArea.GetType().GetField("m_health").GetValue(hitArea));
                if (H < 0 && hit.m_attacker.ID == Player.m_localPlayer.GetZDOID().ID)
                {
                    Mineur.H_on_ore_destroy(__instance.name);
                    Mineur.H_geode_opportunity();

                }
            }
        }
        [HarmonyPatch(typeof(Character), "RPC_Damage")]
        internal class FightPickaxe
        {
            private static void Prefix(Character __instance, HitData hit, bool __state)
            {
                __state = false;
                if (hit.m_attacker.ID == Player.m_localPlayer.GetZDOID().ID && hit.m_damage.m_pickaxe > 0)
                {
                    hit.m_damage.m_pierce = hit.m_damage.m_pierce * Mineur.H_pickaxebonus();
                    __state = true;
                }
            }

            private static void Postfix(Character __instance, HitData hit, bool __state)
            {
                if (__state && __instance.GetHealth()<= 0)
                {
                    if(Mineur.getMineur().level >= 5)
                    {
                        Mineur.H_geode_opportunity();
                    }
                }
            }
        }

    }

    public class Mineur
    {
        public static string[] vanillaores = new string[] { "TinOre", "SilverOre", "IronScrap", "CopperOre", "BlackMetalScrap" };

        public static JobManager getMineur()
        {
            return getjob("Mineur");
        }
        public static bool isMineur()
        {
            if(getMineur() == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public static float moonoreP()
        {
            if (getMineur() == null) { return -1; }
            if(EnvMan.instance.IsNight() && getMineur().level >= 2) { return 0.2f; }
            return -1;
        }
        public static void H_on_ore_destroy(string instancename)
        {
            foreach (string orename in Mineur.vanillaores)
            {
                string fixedorename = orename.Replace("Ore", "");
                if (instancename.ToLower().Contains(fixedorename.ToLower()))
                {
                    float randval = UnityEngine.Random.Range(0f, 1f);
                    if (randval < Mineur.moonoreP())
                    {
                        Imanip.spawnItemMaxStack(fixedorename + "OreMoon", 1);
                    }
                }
            }
        }
        public static void H_geode_opportunity()
        {
            int mineurlevel = getMineur().level;
            float minspawnthesh = 0.05f;
            float maxspawnthesh = 0.01f;
            float spawnthreshold = minspawnthesh + (maxspawnthesh - minspawnthesh) * (1f * mineurlevel) / 20f;
            float rand_doesgeodespawn = UnityEngine.Random.Range(0f, 1f);
            
            if(rand_doesgeodespawn < spawnthreshold)
            {
                string[] plusnumber = new string[] { "+++++", "++++", "+++", "++", "+" };
                float[] typeofgeodethresholds = new float[] { 0.07f,0.13f,0.2f,0.27f,0.33f};
                int[] levelthreshold = new int[] {19,15,11,7, 3};
                float rand_typeofgeode = UnityEngine.Random.Range(0f, 1f);
                float type_t = 0f;
                for(int i = 0; i < levelthreshold.Length; i++)
                {
                    type_t += typeofgeodethresholds[i];
                    int level_t = levelthreshold[i];
                    if(rand_typeofgeode < type_t)
                    {
                        if (mineurlevel < level_t) { continue; }

                        Imanip.spawnItemMaxStack("Geode"+ plusnumber[i], 1);
                        return;
                    }
                }

            }
        }
        public static float H_pickaxebonus()
        {
            int mineurlevel = getMineur().level;
            if(mineurlevel >=  14) { return  2f; }
            if (mineurlevel >= 9) { return 1.6f; }
            if (mineurlevel >= 4) { return 1.3f; }
            return 1f;
        }
        public static void H_fixbetterdrops(MineRock5 arock)
        {
            string nameofinstance = arock.name;


            string theorename = "";
            string nameofprefab1 = "";
            string nameofprefab2 = "";
            foreach (string orename in Mineur.vanillaores)
            {
                string fixedorename = orename.Replace("Ore", "");
                if (nameofinstance.ToLower().Contains(fixedorename.ToLower()))
                {
                    theorename = orename;
                    nameofprefab1 = orename + "+";
                    nameofprefab2 = orename + "++";
                    break;
                }
            }
            if(nameofprefab1 == "" && nameofprefab2 == "") { return; }
            bool has1 = false;
            bool has2 = false;
            foreach (GameObject ADATA in arock.m_dropItems.GetDropList())
            {
                string name = ADATA.transform.name;
                Debug.LogError(name);
                if (name == theorename)
                {
                }

                if (name == nameofprefab1) {  has1 = true; }
                if (name == nameofprefab2) { has2 = true; }
            }
            if (!has1)
            {
                DropTable.DropData oredata1 = new DropTable.DropData();
                oredata1.m_weight = 1;
                oredata1.m_stackMin = 2;
                oredata1.m_stackMax = 3;
                oredata1.m_item = PrefabManager.Instance.GetPrefab(nameofprefab1);
                arock.m_dropItems.m_drops.Add(oredata1);
            }
            if (!has2)
            {
                DropTable.DropData oredata2 = new DropTable.DropData();
                oredata2.m_weight = 1;
                oredata2.m_stackMin = 2;
                oredata2.m_stackMax = 3;
                oredata2.m_item = PrefabManager.Instance.GetPrefab(nameofprefab2);
                arock.m_dropItems.m_drops.Add(oredata2);
            }
        }


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
            foreach(string normalor in vanillaores)
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

            ItemManager.Instance.GetItem("Geode+").ItemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_maxStackSize = 10;
            ItemManager.Instance.GetItem("Geode+").ItemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_weight = 10;
            ItemManager.Instance.GetItem("Geode++").ItemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_maxStackSize = 10;
            ItemManager.Instance.GetItem("Geode++").ItemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_weight = 10;
            ItemManager.Instance.GetItem("Geode+++").ItemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_maxStackSize = 10;
            ItemManager.Instance.GetItem("Geode+++").ItemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_weight = 10;
            ItemManager.Instance.GetItem("Geode++++").ItemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_maxStackSize = 10;
            ItemManager.Instance.GetItem("Geode++++").ItemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_weight = 10;
            ItemManager.Instance.GetItem("Geode+++++").ItemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_maxStackSize = 10;
            ItemManager.Instance.GetItem("Geode+++++").ItemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_weight = 10;



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