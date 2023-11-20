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
using pole_jobs;
using pole_User;
using pole_Bill;
using static pole_RPC.RPC;
using pole_UI;
using static System.Net.Mime.MediaTypeNames;

namespace pole_Data
{

    public class U
    {
        public static void awake()
        {
            Data.data_awake();
        }
        public static void update()
        {

        }
    }

    class Data
    {

        //RESSOURCES
        public static Texture2D TEX_dark;
        public static Texture2D TEX_white;
        public static Texture2D TEX_black;
        public static Texture2D TEX_trans;
        public static Texture2D TEX_1STAR;
        public static Texture2D TEX_2STAR;
        public static Texture2D TEX_3STAR;
        public static Texture2D TEX_4STAR;
        public static Texture2D TEX_5STAR;
        public static Dictionary<string, Texture2D> TEX_Icons = new Dictionary<string, Texture2D>();
        public static Dictionary<string, Texture2D> TEX_MyPNG = new Dictionary<string, Texture2D>();
        private static AssetBundle BUNDLE_UI;

        public static void data_awake()
        {
            load_UI();
        }

        public static void load_UI()
        {

            //TEXTURES
            TEX_dark = AssetUtils.LoadTexture("pole_viking_assets/dark.png");
            TEX_trans = AssetUtils.LoadTexture("pole_viking_assets/trans.png");
            TEX_black = AssetUtils.LoadTexture("pole_viking_assets/black.png");
            TEX_white = AssetUtils.LoadTexture("pole_viking_assets/white.png");
            TEX_1STAR = AssetUtils.LoadTexture("pole_viking_assets/1STAR.png");
            TEX_2STAR = AssetUtils.LoadTexture("pole_viking_assets/2STAR.png");
            TEX_3STAR = AssetUtils.LoadTexture("pole_viking_assets/3STAR.png");
            TEX_4STAR = AssetUtils.LoadTexture("pole_viking_assets/4STAR.png");
            TEX_5STAR = AssetUtils.LoadTexture("pole_viking_assets/5STAR.png");

            //FILL ICON DICTIONARY
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
                
                Texture2D tex = AssetUtils.LoadTexture(infofile.FullName);

                string key = infofile.Name.ToLower().Replace(".png", "");
                if (!TEX_Icons.ContainsKey(key)) { TEX_Icons.Add( key, tex); }
            }
            DirectoryInfo gemicondir = new DirectoryInfo("BepInEx/plugins/pole_viking_assets/raw_data/gem_icons/");
            info = gemicondir.GetFiles("*.png");
            foreach (FileInfo infofile in info)
            {

                Texture2D tex = AssetUtils.LoadTexture(infofile.FullName);
                string key = infofile.Name.ToLower().Replace(".png", "");
                if (!TEX_Icons.ContainsKey(key)) { TEX_Icons.Add(key, tex); }
            }


            //FILL MyPNG DICTIONARY
            DirectoryInfo mypngdir = new DirectoryInfo("BepInEx/plugins/pole_viking_assets/PNG/");
            info = mypngdir.GetFiles("*.png");
            foreach (FileInfo infofile in info)
            {
                Debug.Log(infofile.Name.ToLower().Replace(".png", ""));
                TEX_MyPNG.Add(infofile.Name.ToLower().Replace(".png", ""), AssetUtils.LoadTexture(infofile.FullName));
            }


            //LOAD SKINS
            BUNDLE_UI = AssetUtils.LoadAssetBundle("pole_viking_assets/tutskinbundle");
            UI.skin1 = BUNDLE_UI.LoadAsset<GUISkin>("skintest");
            UI.skin1.label.normal.background = TEX_dark;
        }


        public static void load_assets()
        {



            temp_load_testdata();
        }

        
        public static void clone_item(string itemname, string baseprefab, int qualitymax = 0, string desc = "", bool createnormalobj = true, AssetBundle bundle = null, string bundleitem = "")
        {
            //Normal OBJ
            if (createnormalobj)
            {
                Texture2D tex = TEX_Icons[itemname.ToLower()];
                Sprite icon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                ItemConfig itemconfig = new ItemConfig();
                itemconfig.Name = "$item_" + itemname.ToLower();
                itemconfig.Description = desc;
                itemconfig.Icons = new Sprite[] { icon };
                CustomItem item = new CustomItem(itemname, baseprefab, itemconfig);
                if(bundle == null)
                {
                    ItemManager.Instance.AddItem(item);
                }
                else
                {
                    CustomItem bitem = new CustomItem(bundle, bundleitem, fixReference: false, new ItemConfig());
                    bitem.ItemDrop.m_itemData = item.ItemDrop.m_itemData;
                    bitem.ItemDrop.name = item.ItemDrop.name;
                    bitem.ItemPrefab.name = item.ItemPrefab.name;
                    ItemManager.Instance.AddItem(bitem);
                }
            }
            //Quality OBJ
            if(qualitymax > 0)
            {
                string stars = "";
                for(int k = 0; k < qualitymax; k++)
                {
                    stars += "+";
                    Texture2D texorigin = TEX_Icons[itemname.ToLower()];
                    switch(k)
                    {
                        case 0: TEX_Icons.Add(itemname.ToLower() + stars, AddWatermark(texorigin, TEX_1STAR, new Color(1, 1, 0))); break;
                        case 1: TEX_Icons.Add(itemname.ToLower() + stars, AddWatermark(texorigin, TEX_2STAR, new Color(1, 150f / 250f, 0))); break;
                        case 2: TEX_Icons.Add(itemname.ToLower() + stars, AddWatermark(texorigin, TEX_3STAR, new Color(1, 100f / 250f, 0))); break;
                        case 3: TEX_Icons.Add(itemname.ToLower() + stars, AddWatermark(texorigin, TEX_4STAR, new Color(1, 0, 0))); break;
                        case 4: TEX_Icons.Add(itemname.ToLower() + stars, AddWatermark(texorigin, TEX_5STAR, new Color(1, 0, 1))); break;
                    }

                    
                    Texture2D texq = TEX_Icons[itemname.ToLower()+ stars];
                    Sprite iconq = Sprite.Create(texq, new Rect(0, 0, texq.width, texq.height), new Vector2(0.5f, 0.5f));
                    ItemConfig itemconfigq = new ItemConfig();
                    itemconfigq.Name = "$item_" + itemname.ToLower()+stars;
                    itemconfigq.Description = desc;
                    itemconfigq.Icons = new Sprite[] { iconq };
                    CustomItem itemq = new CustomItem(itemname + stars, baseprefab, itemconfigq);

                    if (bundle == null)
                    {
                        ItemManager.Instance.AddItem(itemq);
                    }
                    else
                    {
                        CustomItem bitem = new CustomItem(bundle, bundleitem, fixReference: false, new ItemConfig());
                        bitem.ItemDrop.m_itemData = itemq.ItemDrop.m_itemData;
                        bitem.ItemDrop.name = itemq.ItemDrop.name;
                        bitem.ItemPrefab.name = itemq.ItemPrefab.name;
                        ItemManager.Instance.AddItem(bitem);
                    }
                }
            }
        }

        public static void temp_load_testdata()
        {
            j_mineur.Mineur.load_assets();

            // You want that to run only once, Jotunn has the item cached for the game session




            AssetBundle bundleparticle = AssetUtils.LoadAssetBundle("pole_viking_assets/testparticle");
            AssetBundle bundle_geode = AssetUtils.LoadAssetBundle("pole_viking_assets/sfx_pole_viking/geode_break");
            GameObject objj = bundleparticle.LoadAsset<GameObject>("testparticle");
            AudioClip clip = bundle_geode.LoadAsset<AudioClip>("geode_break.wav");

            GameObject necki = PrefabManager.Instance.GetPrefab("Player");
            necki.transform.localScale *= 1;
            necki.AddComponent<ParticleSystem>();
            ParticleSystem neckipart = necki.GetComponent<ParticleSystem>();
            neckipart.maxParticles = 2000;
            ParticleSystem.MainModule main = neckipart.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            necki.GetComponent<ParticleSystemRenderer>().material = objj.GetComponent<ParticleSystemRenderer>().material;
            necki.AddComponent<AudioSource>().playOnAwake = false;
            necki.GetComponent<AudioSource>().clip = clip;


            AssetBundle pieceBundle = AssetUtils.LoadAssetBundle("pole_viking_assets/monolith01");

            PieceConfig cylinder = new PieceConfig();
            cylinder.Name = "$cylinder_display_name";
            cylinder.Description = "$cylinder_description";
            cylinder.PieceTable = PieceTables.Hammer;
            cylinder.CraftingStation = CraftingStations.Workbench;
            cylinder.Category = PieceCategories.Misc;
            cylinder.AddRequirement(new RequirementConfig("Wood", 2, 0, true));


            PieceManager.Instance.AddPiece(new CustomPiece(pieceBundle, "monolith01", fixReference: false, cylinder));
            SmelterConversionConfig blastConfig = new SmelterConversionConfig();
            blastConfig.Station = "monolith01"; // Override the default "smelter" station of the SmelterConversionConfig
            blastConfig.FromItem = "Wood";
            blastConfig.ToItem = "Stone"; // This is our custom prefabs name we have loaded just above
            PieceManager.Instance.GetPiece("monolith01").PiecePrefab.GetComponent<Smelter>().m_fuelItem = PrefabManager.Instance.GetPrefab("Stone").GetComponent<ItemDrop>();
            ItemManager.Instance.AddItemConversion(new CustomItemConversion(blastConfig));

        }


        public static Texture2D AddWatermark(Texture2D background, Texture2D watermark, Color tcolor)
        {
            Texture2D returner = new Texture2D(background.width,background.height);

            int startX = 0;
            int startY = background.height - watermark.height;

            for (int x = startX; x < background.width; x++)
            {

                for (int y = startY; y < background.height; y++)
                {
                    Color bgColor = background.GetPixel(x, y);
                    Color wmColor = watermark.GetPixel(x - startX, y - startY);
                    wmColor = new Color(wmColor.r * tcolor.r, wmColor.g * tcolor.g, wmColor.b * tcolor.b, wmColor.a * tcolor.a);
                    wmColor = new Color(wmColor.r * wmColor.a, wmColor.g * wmColor.a, wmColor.b * wmColor.a, wmColor.a);

                    Color final_color = bgColor + wmColor;

                    returner.SetPixel(x, y, final_color);
                }
            }

            returner.Apply();
            return returner;
        }

    }
}