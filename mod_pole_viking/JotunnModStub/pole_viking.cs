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
        public const string PluginVersion = "0.0.01";

        /// VARIABLE CREATION
        private readonly Harmony harmony = new Harmony(PluginGUID);
        public static List<User> list_users = new List<User>();
        public static User user_local;
        public static List<Bill> list_bills = new List<Bill>();
        public static List<JobManager> job_manager_local;

        /// GAME OBJECTS 


        //RESSOURCES
        public static Texture2D TEX_dark;
        public static Texture2D TEX_white;
        public static Texture2D TEX_black;
        public static Texture2D TEX_trans;
        public static Dictionary<string, Texture2D> TEX_Icons = new Dictionary<string, Texture2D>();
        public static Dictionary<string, Texture2D> TEX_MyPNG = new Dictionary<string, Texture2D>();
        private AssetBundle BUNDLE_UI;
        public static NumberFormatInfo floatinfo = new CultureInfo("en-US").NumberFormat;
        public static CustomLocalization Localizations = LocalizationManager.Instance.GetLocalization();



        Material trashmaterial = null;
        #endregion
        private void Awake()
        {
            isserver = (System.IO.File.Exists(dir_root + file_server));
            MyRPC = NetworkManager.Instance.AddRPC("MyRPC", pole_RPC.RPC.MyRPCServerReceive, pole_RPC.RPC.MyRPCClientReceive);
            Job.build_jobs();

            //LOAD RESSOURCES
            load_assets();
            PrefabManager.OnVanillaPrefabsAvailable += load_miner_assets;

            if (isserver)
            {
            }
            else
            {

                string jobstring = rw.load_save_textfile("r", "job.dat", "", JobManager.defaultsavevalue);
                Debug.Log(jobstring);
                List<string> jobstringlist = jobstring.Split(',').ToList();
                job_manager_local = new List<JobManager>();
                foreach(string astring in jobstringlist) { job_manager_local.Add(new JobManager(astring)); }
                StatManager._bonus = new Bonus(5,5,5,5,5,5);
                StatManager._jobmanager_list = job_manager_local;
                StatManager.BuildList();

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


            AssetBundle pieceBundle = AssetUtils.LoadAssetBundle("pole_viking_assets/monolith01");

            PieceConfig cylinder = new PieceConfig();
            cylinder.Name = "$cylinder_display_name";
            cylinder.Description = "$cylinder_description";
            cylinder.PieceTable = PieceTables.Hammer;
            cylinder.CraftingStation = CraftingStations.Workbench;
            cylinder.Category = PieceCategories.Misc;
            cylinder.AddRequirement(new RequirementConfig("Wood", 2, 0, true));

            PieceManager.Instance.AddPiece(new CustomPiece(pieceBundle, "monolith01", fixReference: false, cylinder));
        }
        private void load_assets()
        {

            ///TEXTURES
            TEX_dark = AssetUtils.LoadTexture("pole_viking_assets/dark.png");
            TEX_trans = AssetUtils.LoadTexture("pole_viking_assets/trans.png");
            TEX_black = AssetUtils.LoadTexture("pole_viking_assets/black.png");
            TEX_white = AssetUtils.LoadTexture("pole_viking_assets/white.png");

            GameObject game = new GameObject();

            DirectoryInfo icondir = new DirectoryInfo("BepInEx/plugins/pole_viking_assets/raw_data/icons/");
            FileInfo[] info = icondir.GetFiles("*.png");
            foreach (FileInfo infofile in info)
            {
                TEX_Icons.Add(infofile.Name.ToLower().Replace(".png", ""), AssetUtils.LoadTexture(infofile.FullName));
            }
            DirectoryInfo customicondir = new DirectoryInfo("BepInEx/plugins/pole_viking_assets/raw_data/custom_icons/");
            info = customicondir.GetFiles("*.png");
            foreach (FileInfo infofile in info)
            {
                TEX_Icons.Add(infofile.Name.ToLower().Replace(".png", ""), AssetUtils.LoadTexture(infofile.FullName));
            }

            DirectoryInfo mypngdir = new DirectoryInfo("BepInEx/plugins/pole_viking_assets/PNG/");
            info = mypngdir.GetFiles("*.png");
            foreach (FileInfo infofile in info)
            {
                Debug.Log(infofile.Name.ToLower().Replace(".png", ""));
                TEX_MyPNG.Add(infofile.Name.ToLower().Replace(".png",""), AssetUtils.LoadTexture(infofile.FullName));
            }


            BUNDLE_UI = AssetUtils.LoadAssetBundle("pole_viking_assets/tutskinbundle");
            //TEX_job_front = AssetUtils.LoadTexture("pole_viking_assets/job_menu_front.png");
            UI.skin1 = BUNDLE_UI.LoadAsset<GUISkin>("skintest");
            UI.skin1.label.normal.background = TEX_dark;
            ///OTHER
            //UI.skin1 = AssetUtils.loadpref ("pole_viking_assets/dark.png");
        }
        
        public static void load_miner_assets()
        {// Use the vanilla beech tree prefab to render our icon from
            
            Debug.LogError("testfuckme");
            Texture2D tex = TEX_Icons["copperoremoon"];
            Sprite icon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));



            // Create the custom item with the rendered icon
            ItemConfig itemconfig = new ItemConfig();
            itemconfig.Name = "$item_copperoremoon";
            itemconfig.Description = "$item_copperoremoon";
            itemconfig.Icons = new Sprite[] { icon };

            CustomItem copperoremoon = new CustomItem("CopperOreMoon", "CopperOre", itemconfig);

            ItemManager.Instance.AddItem(copperoremoon);

            // You want that to run only once, Jotunn has the item cached for the game session

            PrefabManager.OnVanillaPrefabsAvailable -= load_miner_assets;
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
            UI.dtstamp = Time.time;
        }
        private void Update()
        {
            pole_StatManager.StatManager.Update();
            if (isserver)
            {
                Update_Server();
            }
            else
            {
                Update_Client();
            }

            if (UnityInput.Current.GetKeyDown(KeyCode.T))
            {
                job_manager_local[0].xp += 500;
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
            if (UnityInput.Current.GetKeyDown(KeyCode.U))
            {
                if(trashmaterial == null)
                {
                    GameObject cop = PrefabManager.Instance.GetPrefab("rock4_copper");
                    MeshRenderer meshrend = cop.transform.Find("model").GetComponent<MeshRenderer>();
                    trashmaterial = meshrend.sharedMaterials[0];
                    return;
                }
                trashmaterial.SetColor("_EmissionColor", 3 * new Vector4(UnityEngine.Random.Range(0f,1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f),1f));
                Debug.Log((3 * new Vector4(UnityEngine.Random.Range(0, 1), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), 1)).ToString());
                return;
                

                //DynamicGI.UpdateEnvironment();

                /*
                int a = 0;
                foreach (var gameObj in FindObjectsOfType(typeof(GameObject)) as GameObject[])
                {
                    if (gameObj.name.ToLower().Contains("copper"))
                    {
                        foreach (Transform trans in gameObj.transform)
                        {
                            Debug.Log(trans.name);
                        }
                        a += 1;
                    }
                    else
                    {
                        //Debug.Log(gameObj.name);
                    }
                }*/

                GameObject troll = new GameObject();

                PrefabManager.Instance.GetPrefab("Neck").GetComponent<Humanoid>().m_health = 1;
                GameObject neck = CreatureManager.Instance.GetCreaturePrefab("Neck");
                Humanoid huma = neck.GetComponent<Humanoid>();
                huma.m_health = 1;
                var newneckconfig = new CreatureConfig();
                newneckconfig.Name = "$creature_neck";
                newneckconfig.Faction = Character.Faction.ForestMonsters;
                newneckconfig.UseCumulativeLevelEffects = true;
                newneckconfig.AddSpawnConfig(new SpawnConfig
                {
                    SpawnChance = 20000,
                    SpawnInterval = 1,
                    SpawnDistance = 1,
                    Biome = Heightmap.Biome.Meadows,
                    MinLevel = 4,
                    MaxLevel = 10
                }
                );
                var necker = new CustomCreature("NeckTest", "Neck", newneckconfig);
                CreatureManager.Instance.AddCreature(necker);

                CreatureManager.OnVanillaCreaturesAvailable -= u_temp_Update;

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
        

        public class Imanip
        {
            public static void spawnItemMaxStack(string name, int quantity)
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
                }
                if(numoffull > 0)
                {
                    for (int i = 0; i < numoffull; i++)
                    {
                        GameObject obj2 = UnityEngine.Object.Instantiate(obj, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f + Vector3.up + vector, Quaternion.identity);
                        obj2.GetComponent<ItemDrop>().SetStack(maxstack);
                    }
                }
            }
        }
        
    }
}