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

using pole_ReadWrite;

using pole_jobs;
using static Player;

namespace pole_StatManager
{
    public class U
    {
        public static void awake()
        {

        }
        public static void update()
        {

        }
    }

    public class Bonus
    {

        public static int maxlevel = 30;
        public static int maxhealth = 200;
        public static int maxstamina = 200;
        public static int maxeitr = 200;
        public static float maxspeed = 0.5f;
        public static int maxpods = 200;

        private int Level;
        private int Health_bonus;
        private int Endurance_bonus;
        private int Eiktr_bonus;
        private int Speed_bonus;
        private int Carriable_weight_bonus;
        public int _Level
        {
            get { return Level; }
            set { Level = value; }
        }
        public int _Health_bonus
        {
            get { return Health_bonus; }
            set { Health_bonus = value; }
        }
        public int _Endurance_bonus
        {
            get { return Endurance_bonus; }
            set { Endurance_bonus = value; }
        }
        public int _Eiktr_bonus
        {
            get { return Eiktr_bonus; }
            set { Eiktr_bonus = value; }
        }
        public int _Speed_bonus
        {
            get { return Speed_bonus; }
            set { Speed_bonus = value; }
        }
        public int _Carriable_weight_bonus
        {
            get { return Carriable_weight_bonus; }
            set { Carriable_weight_bonus = value; }
        }
        public Bonus(int level = 0, int healthbonus = 0, int endurancebonus = 0, int eiktrbonus = 0, int speedbonus = 0, int carriableweightbonus = 0)
        {
            Level = level;
            Health_bonus = healthbonus;
            Endurance_bonus = endurancebonus;
            Eiktr_bonus = eiktrbonus;
            Speed_bonus = speedbonus;
            Carriable_weight_bonus = carriableweightbonus;
        }
        public int TotalPoints()
        {
            return Level * 3;
        }
        public int UsedPoints()
        {
            int result = 0;
            List<int> collection = new List<int>() { Health_bonus, Endurance_bonus, Eiktr_bonus, Speed_bonus, Carriable_weight_bonus };

            foreach (int item in collection)
            {
                if (item > 0 && item <= 10)
                {
                    result += item;
                }
                if (item > 10 && item <= 20)
                {
                    result += 10 + 2 * (item - 10);
                }
                if (item > 20 && item <= 30)
                {
                    result += 10 + 20 + 3 * (item - 20);
                }
            }
            return result;
        }
        public int RemainingPoints()
        {
            return TotalPoints() - UsedPoints();
        }
        public string StringIt()
        {
            return $"bonus:{Level}:{Health_bonus}:{Endurance_bonus}:{Eiktr_bonus}:{Speed_bonus}:{Carriable_weight_bonus}";
        }
        public List<int> UpgradesInList()
        {
            return new List<int>() { Health_bonus, Endurance_bonus, Eiktr_bonus, Speed_bonus, Carriable_weight_bonus };
        }
        public static int NextStep(int niveau)
        {
            if (niveau >= 0 && niveau < 10)
            {
                return 1;
            }
            if (niveau >= 10 && niveau < 20)
            {
                return 2;
            }
            if (niveau >= 20 && niveau <= 30)
            {
                return 3;
            }
            return 0;
        }
        public static void Save(Bonus bonus)
        {
            string data = bonus.StringIt();
            string filepath = Application.dataPath + "/bonus.dat";
            File.WriteAllText(filepath, CYPHER.rail_cypher(data));
        }
        public static Bonus Load()
        {
            string def = $"qsdf0q4545dfh6fg168hdsoisdfqsdf,{new Bonus().StringIt()},dfqdf1651fdqf1gqs446sdg1s6g7sgsd665sd1f6q";
            string filepath = Application.dataPath + "/bonus.dat";
            if (!System.IO.File.Exists(filepath))
            {
                string defaultdata = def;
                string cdefaultdata = CYPHER.rail_cypher(defaultdata);
                File.WriteAllText(filepath, cdefaultdata);
            }

            string readdata = File.ReadAllText(filepath);
            string decrypteddata = CYPHER.rail_read(readdata);

            Bonus result = new Bonus();

            int.TryParse(decrypteddata.Split(',')[1].Split(':')[1], out result.Level);
            int.TryParse(decrypteddata.Split(',')[1].Split(':')[2], out result.Health_bonus);
            int.TryParse(decrypteddata.Split(',')[1].Split(':')[3], out result.Endurance_bonus);
            int.TryParse(decrypteddata.Split(',')[1].Split(':')[4], out result.Eiktr_bonus);
            int.TryParse(decrypteddata.Split(',')[1].Split(':')[5], out result.Speed_bonus);
            int.TryParse(decrypteddata.Split(',')[1].Split(':')[6], out result.Carriable_weight_bonus);

            return result;
        }
        public static Color ToColor(Color color0, Color color30, int value)
        {
            double calcul = (5 * Math.Sin(Math.PI * value / 5) * (1 / Math.PI)) + value;
            calcul *= 1 / 30f;
            return Color.Lerp(color0, color30, (float)calcul);
        }            //addfunction

    }


    internal class Harm : BaseUnityPlugin
    {
        [HarmonyPatch(typeof(Player), "GetTotalFoodValue")]
        class SetHES
        {
            static void Prefix(Player __instance)
            {
                __instance.m_baseHP = 25 + StatManager.Get("max_health");
                __instance.m_baseStamina = 50 + StatManager.Get("max_stamina");
            }
            static void Postfix(Player __instance, out float eitr)
            {
                eitr = StatManager.Get("max_eitr");
            }
        }
        [HarmonyPatch(typeof(Character), "UpdateWalking")]
        class SetSpeed
        {
            static void Prefix(Player __instance)
            {
                if(__instance.IsPlayer())
                {
                    if (!__instance.HaveRider())
                    {
                        __instance.m_runSpeed = 7 * (1+ StatManager.Get("move_speed"));
                    }
                }
            }
        }
        [HarmonyPatch(typeof(Player), nameof(Player.GetMaxCarryWeight))]
        class SetPods
        {
            static void Prefix(Player __instance)
            {
                __instance.m_maxCarryWeight = 300 + StatManager.Get("max_pod") ;
                
            }
        }
    }

    public class StatManager
    {
        public static StatManager mystatmanager;

        public static Bonus _bonus;
        public static List<JobManager> _jobmanager_list;

        private static List<StatMod> _modlist;

        public static void BuildList()
        {
            _modlist = new List<StatMod>();
            Add(new StatMod("max_health", (Bonus.maxhealth) * (_bonus._Health_bonus) / (1f * Bonus.maxlevel)));
            Add(new StatMod("max_stamina", (Bonus.maxstamina) * (_bonus._Endurance_bonus) / (1f * Bonus.maxlevel)));
            Add(new StatMod("max_eitr", (Bonus.maxeitr) * (_bonus._Eiktr_bonus) / (1f * Bonus.maxlevel)));
            Add(new StatMod("move_speed", (Bonus.maxspeed) * (_bonus._Speed_bonus) / (1f * Bonus.maxlevel)));
            Add(new StatMod("max_pod", (Bonus.maxpods) * (_bonus._Carriable_weight_bonus) / (1f * Bonus.maxlevel)));

            foreach( JobManager amanager in _jobmanager_list )
            {
                int reallevel = amanager.level;
                if(reallevel == 0) { break; }

                for(int i = 0; i < reallevel; i++ )
                {
                    foreach(StatMod mod in amanager.job.levelstatmods[i].ToList())
                    {
                        Add(mod);
                    }
                }
            }
            Print();
        }
        
        public static void Add(StatMod mod)
        {
            bool found = false;
            foreach(StatMod amod in _modlist)
            {
                if(amod._code == mod._code)
                {
                    amod._val += mod._val;
                    return;
                }
            }
            _modlist.Add(mod);
        }
        public static float Get(string id)
        {
            foreach( StatMod amod in _modlist)
            {
                if(amod._code == id)
                {
                    return amod._val;
                }
            }
            return 0;
        }


        public static void Awake()
        {

        }

        public static void Update()
        {
        }

        public static void Print()
        {
            foreach(StatMod amod in _modlist)
            {
                Debug.Log(amod.ToString());
            }
        }

    }
    public class StatMod
    {
        public string _code;
        public float _val;


        public StatMod(string code, float val)
        {
            _code = code;
            _val = val;
        }

        public override string ToString()
        {
            return _code + ":" + _val;
        }
    }

}