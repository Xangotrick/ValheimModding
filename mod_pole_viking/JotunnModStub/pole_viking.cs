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

using pole_jobs;
using pole_UI;
using static pole_UI.UIX;
using pole_User;
using pole_Bill;
using pole_ReadWrite;
using pole_StatManager;
using pole_Data;

using static pole_RPC.RPC;


namespace pole_viking
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class pole_viking : BaseUnityPlugin
    {


        #region VAR statements

        public static bool isinserver = false;
        public static bool isserver = false;
        public static string file_server = "_serverinfo.txt";
        public static string dir_root = Application.dataPath+ "/" ;

        ///MOD DETAILS
        public const string PluginGUID = "com.jotunn.pole_viking";
        public const string PluginName = "Pole Viking";
        public const string PluginVersion = "0.0.02";

        /// VARIABLE CREATION
        private readonly Harmony harmony = new Harmony(PluginGUID);
        public static List<User> list_users = new List<User>();
        public static User user_local;
        public static List<Bill> list_bills = new List<Bill>();
        public static List<JobManager> job_manager_local;

        /// GAME OBJECTS 
        static GameObject objj;

        //RESSOURCES
        public static NumberFormatInfo floatinfo = new CultureInfo("en-US").NumberFormat;
        public static CustomLocalization Localizations = LocalizationManager.Instance.GetLocalization();





        Material trashmaterial = null;
        #endregion
        private void Awake()
        {
            isserver = (System.IO.File.Exists(dir_root + file_server));

            pole_RPC.U.awake();
            pole_jobs.U.awake();
            pole_Data.U.awake();
            //LOAD RESSOURCES

            PrefabManager.OnVanillaPrefabsAvailable += load_assets;

            if (isserver)
            {
            }
            else
            {

            }


            Assembly assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);

            //var monobundle = AssetUtils.LoadAssetBundle("pole_viking_assets/monolith01");
            /*var magicmono = monobundle.LoadAsset<GameObject>("monolith01");
            PrefabManager.Instance.AddPrefab(magicmono);

            GameObject tablePrefab = monobundle.LoadAsset<GameObject>("monolith01");
            CustomPiece CPT = new CustomPiece(tablePrefab);
            PieceManager.Instance.AddPiece(CPT);

            PieceConfig makeConfig = new PieceConfig();
            makeConfig.PieceTable = "_BlueprintTestTable";
            var makePiece = new CustomPiece(monobundle, "monolith01", fixReference: false, makeConfig);
            PieceManager.Instance.AddPiece(makePiece);*/


        }

        private void load_assets()
        {
            pole_Data.Data.load_assets();

            PrefabManager.OnVanillaPrefabsAvailable -= load_assets;
        }
        


        [HarmonyPatch(typeof(Game), "SpawnPlayer")]
        class SetAuras
        {
            static void Postfix(Player __result)
            {
                //GameObject.Instantiate(objj, __result.transform.position, Quaternion.identity, __result.transform);
            }
        }


        private void OnGUI()
        {
            UI.update_screen();

            ui_overlay();

            if(UI.market)
            {
                ui_market();
            }
            if(UI.money)
            {
                ui_money();
            }
            if(UI.cheat)
            {
                ui_cheat();
            }
            if(UI.job)
            {
                ui_job();
            }
            if(UI.bonus)
            {
                ui_upgrade();
            }
            UI.dtstamp = Time.time;
        }
        private void Update()
        {
            if (isserver)
            {
                Update_Server();
            }
            else
            {
                Update_Client();
            }

        }

        private void Update_Server()
        {

        }
        private void Update_Client()
        {
            u_Login_Check();
            u_temp_Update();
        }
        private void u_Login_Check()
        {
            if (ZNet.instance == null) { isinserver = false; return; }
            if (ZNet.instance.GetServerPeer() == null) { isinserver = false; return; }
            if (!isinserver && Player.m_localPlayer) { isinserver = true; On_Login(); }
        }
        //Only removable content, for testing
        private void u_temp_Update()
        {
            if(UI.inmenu)
            {
                if (UnityInput.Current.GetKeyDown(KeyCode.Escape))
                {
                    UI.cheat = false;
                    UI.money = false;
                    UI.market = false;
                    UI.job = false;
                    UI.bonus = false;
                }
                return;
            }


            if (UnityInput.Current.GetKeyDown(KeyCode.L))
            {
                UI.money = !UI.money;
            }
            if (UnityInput.Current.GetKeyDown(KeyCode.O))
            {
                UI.cheat = !UI.cheat;
            }
            if (UnityInput.Current.GetKeyDown(KeyCode.K))
            {
                UI.market_clean();
                UI.market = !UI.market;
            }
            if (UnityInput.Current.GetKeyDown(KeyCode.N))
            {
                UI.job = !UI.job;
            }
            if (UnityInput.Current.GetKeyDown(KeyCode.T))
            {
                job_manager_local[0].xp += 500;
                StatManager._bonus._Health_bonus += 1;
                StatManager.BuildList();
            }
            if (UnityInput.Current.GetKeyDown(KeyCode.R))
            {
                StatManager.BuildList();
                GameObject objeeee = null;
                foreach (Transform t in Player.m_localPlayer.transform)
                {
                    if (t.gameObject.name.Contains("testparticle"))
                    {
                        objeeee = t.gameObject;
                        break;
                    }
                }
            }
            if (UnityInput.Current.GetKeyDown(KeyCode.P))
            {
                UI.bonus = !UI.bonus;
            }
        }
        private void temp_On_Login()
        {
            Player.m_localPlayer.SetGodMode(true);

        }


        private void On_Login()
        {
            temp_On_Login();
            //if(isserver) { return; }
            string datatosend = Steamworks.SteamUser.GetSteamID().ToString() + "|" + Player.m_localPlayer.GetHoverName();
            Debug.LogError(datatosend);
            MyRPCSend("request_login", datatosend);
        }


        public static JobManager getjob(string name)
        {
            foreach(JobManager ajob in job_manager_local)
            {
                if (ajob.jobname == name)
                {
                    return ajob;
                }
            }
            return null;
        }
        public class Imanip
        {
            public static void spawnItemMaxStack(string name, int quantity, int quality = -1)
            {
                int count = 1;
                Vector3 vector = UnityEngine.Random.insideUnitSphere * ((count == 1) ? 0f : 0.5f);

                GameObject obj = PrefabManager.Instance.GetPrefab(name);
                ItemDrop itemDrop = obj.GetComponent<ItemDrop>();
                int maxstack = itemDrop.m_itemData.m_shared.m_maxStackSize;
                int numoffull = quantity / maxstack;
                int remainder = quantity % maxstack;
                if(remainder > 0)
                {
                    GameObject obj2 = UnityEngine.Object.Instantiate(obj, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f + Vector3.up + vector, Quaternion.identity);
                    obj2.GetComponent<ItemDrop>().SetStack(remainder);
                    obj2.GetComponent<ItemDrop>().m_itemData.m_quality = quality;
                }
                if(numoffull > 0)
                {
                    for (int i = 0; i < numoffull; i++)
                    {
                        GameObject obj2 = UnityEngine.Object.Instantiate(obj, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f + Vector3.up + vector, Quaternion.identity);
                        obj2.GetComponent<ItemDrop>().SetStack(maxstack);
                        obj2.GetComponent<ItemDrop>().m_itemData.m_quality = quality;
                    }
                }
            }
        }
        
    }
}