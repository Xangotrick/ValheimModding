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

namespace pole_jobs
{
    internal class __job: BaseUnityPlugin
    {
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyAttack))]
        private class IncreaseDamageDone
        {
            private static void Prefix(SEMan __instance, ref HitData hitData)
            {
                
                hitData.m_damage.m_pickaxe *= 1 + 20;
                hitData.m_damage.m_pierce *= 1;
                Debug.LogError(hitData.m_damage.m_pickaxe);
                Debug.LogError(hitData.ToString());
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
                    Imanip.spawnItemMaxStack("CopperOreMoon", 1);
                }
            }
        }
         
    }

    
    public class JobManager
    {
        public static string defaultsavevalue = "Mineur:0:A;0$B;0$C;0$D;0$";
        private string _jobname;
        private int _xp;
        private Dictionary<string, int> _scores;

        public string jobname
        {
            get { return _jobname; }
            set { _jobname = value; }
        }
        public int xp
        {
            get { return _xp; }
            set
            {
                if (Time.time - UI.job_overlay_timestamp_show < 12)
                {
                    UI.job_overlay_timestamp_show = Time.time - 1;
                }
                else
                {
                    UI.job_overlay_timestamp_show = Time.time;
                }
                StatManager.BuildList();
                _xp = value;
                Save();
            }
        }
        public Job job
        {
            get
            {
                return Job.jobbyname(_jobname);
            }
        }
        public int levelXP
        {
            get
            {
                return job.levelbyxp(_xp);
            }
        }
        public int levelscore
        {
            get
            {
                int returner = 0;
                int scoreid = 0;
                for (int i = 0; i < job.xpcaps.Length; i++)
                {
                    foreach (string astring in job.levelscorecaps[i].ToList())
                    {
                        string key = astring.Split(';')[0];
                        int value = int.Parse(astring.Split(';')[1]);
                        if (getscore(key) < value)
                        {
                            return i;
                        }
                        scoreid++;
                    }
                }
                return job.xpcaps.Length;
            }
        }
        public int level
        {
            get
            {
                return (int)(Math.Min(levelXP, levelscore));
            }
        }

        public JobManager(string arawstring)
        {
            List<string> splitdata = arawstring.Split(':').ToList();

            _jobname = splitdata[0];
            _xp = int.Parse(splitdata[1]);


            List<string> scoredatadata = splitdata[2].Split('$').ToList();
            _scores = new Dictionary<string, int>();
            foreach (string astring in scoredatadata)
            {
                if (astring == "") { continue; }

                string name = astring.Split(';')[0];
                int value = int.Parse(astring.Split(';')[1]);
                _scores[name] = value;
            }

        }

        public int getscore(string key)
        {
            if (!_scores.ContainsKey(key))
            {
                setscore(key, 0);
            }
            return _scores[key];
        }
        public void setscore(string index, int value)
        {
            _scores[index] = value;

            Save();
        }

        public override string ToString()
        {
            string returner = "";

            returner += jobname + ':';
            returner += xp.ToString() + ':';
            foreach (string key in _scores.Keys) { returner += key + ";" + _scores[key] + "$"; }

            return returner;
        }
        public void Save()
        {
            string jobstring = rw.load_save_textfile("r", "job.dat", "", defaultsavevalue);
            string[] jobstringarray = jobstring.Split(',');
            bool foundjob = false;
            for (int i = 0; i < jobstringarray.Length; i++)
            {
                string astring = jobstringarray[i];
                if (astring.Contains(jobname))
                {
                    jobstringarray[i] = this.ToString();
                    foundjob = true;
                    break;
                }
            }
            if (!foundjob)
            {
                List<string> jobstringlist = jobstringarray.ToList();
                jobstringlist.Add(this.ToString());
                jobstringarray = jobstringlist.ToArray();
            }
            string datatosave = string.Join(",", jobstringarray);
            rw.load_save_textfile("w", "job.dat", datatosave, "");



        }
    }
    public class Job
    {
        public static List<Job> jobs = new List<Job>();
        public string name;
        public int[] xpcaps = new int[] { 1, 1000, 3000, 6000, 10000, 15000, 21000, 28000, 36000, 45000, 55000, 66000, 78000, 91000, 105000, 120000, 136000, 153000, 171000, 190000 };
        public string[][] levelscorecaps = new string[20][];
        public StatMod[][] levelstatmods = new StatMod[20][];
        public string[] descriptions = new string[20];

        public Job(string aname)
        {
            name = aname;
            descriptions = new string[xpcaps.Length];
            levelscorecaps = new string[xpcaps.Length][];
            for (int i = 0; i < xpcaps.Length; i++)
            {
                levelscorecaps[i] = new string[] { ";0" };
                levelstatmods[i] = new StatMod[] { new StatMod("null", 0) };
            }
        }

        public int levelbyxp(int xp)
        {
            for (int i = 0; i < xpcaps.Length; i++)
            {
                if (xp < xpcaps[i])
                {
                    return i;
                }
            }
            return xpcaps.Length;
        }

        public static void build_jobs()
        {
            jobs = new List<Job>();

            Job Mineur = new Job("Mineur");
            Mineur.descriptions[0] = "La saga de $$$, Maître des Mines de Midgard commence! Loin de votre futures richesses à en rendre jaloux les nains, vos débuts sont humbles. Découvrez les richesses de la terre!";
            Mineur.descriptions[1] = "Après avoir bravé les effrois de la nuit, le fier $$$, Briseur de roches, fut récompensé de la plus rare des pierres, une Pierre de Lune!";

            Mineur.levelstatmods[2] = new StatMod[] { new StatMod("max_health", 22) };
            Mineur.levelstatmods[3] = new StatMod[] { new StatMod("max_stamina", 22), new StatMod("max_pod", 200) };

            Mineur.levelscorecaps[3] = new string[] { "A;100", "B;20", "C;30" };
            Mineur.levelscorecaps[4] = new string[] { "B;50", "D;100" };
            Mineur.levelscorecaps[5] = new string[] { "F;20", "G;20", "H;20" };


            jobs.Add(Mineur);
        }
        public static Job jobbyname(string aname)
        {
            foreach (Job ajob in jobs)
            {
                if (ajob.name == aname) { return ajob; }
            }
            return null;
        }
    }

}