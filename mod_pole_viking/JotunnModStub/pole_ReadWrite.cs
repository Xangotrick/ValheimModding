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

namespace pole_ReadWrite
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
    
    public class CYPHER
    {
        private static int n = 5;
        public static string rail_cypher(string input)
        {
            if (input == "") { return ""; }
            string returner = "";


            string[] rails = new string[n];
            for (int k = 0; k < input.Length; k++)
            {
                char c = input[k];

                rails[rail_index(k)] += c;
            }

            foreach (string astring in rails.ToList())
            {
                returner += astring;
            }


            return returner;
        }
        public static string rail_read(string input)
        {
            if (input == "") { return ""; }

            string[] returnarray = new string[input.Length];
            string wierdstring = "";

            for (int k = 0; k < input.Length; k++)
            {
                wierdstring += Convert.ToChar(k);
            }

            string mutatedstring = rail_cypher(wierdstring);


            for (int k = 0; k < input.Length; k++)
            {
                char c = input[k];
                int pos = Convert.ToInt16(mutatedstring[k]);
                returnarray[pos] += c;
            }

            string returner = "";

            for (int k = 0; k < returnarray.Length; k++)
            {
                returner += returnarray[k];
            }

            return returner;
        }
        private static int rail_index(int index)
        {
            int subrank = 2 * (n - 1);
            int subindex = index % subrank;
            if (subindex < n)
            {
                return subindex;
            }
            else
            {
                return (n - 1) - (subindex % (n - 1));
            }
        }
    }
    public class rw
    {

        public static string load_save_textfile(string rw, string filename, string data = "", string def = "")
        {
            string filepath = Application.dataPath + "/" + filename;
            if (rw == "w")
            {
                if (data == "") { return ""; }
                File.WriteAllText(filepath, CYPHER.rail_cypher(data));
            }
            if (rw == "r")
            {
                if (!System.IO.File.Exists(filepath))
                {
                    string defaultdata = def;
                    string cdefaultdata = CYPHER.rail_cypher(defaultdata);
                    File.WriteAllText(filepath, cdefaultdata);

                }

                string readdata = File.ReadAllText(filepath);
                return CYPHER.rail_read(readdata);
            }
            return "";
        }


        public static string dir_data = Application.dataPath + "/";

        public static void writefile(string data, string filename, string dir = "")
        {
            if (dir == "") { dir = dir_data; }

            File.WriteAllText(dir + filename, data);
        }

        public static string readfile(string filename, bool isroot = true)
        {
            string returner = "";

            string path = Application.dataPath + "/" + filename;
            if (!isroot) { path = filename; }

            try
            {
                returner = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Debug.LogError("ERROR: could not read file. does it exist ?");
            }

            return returner;
        }

    }

}