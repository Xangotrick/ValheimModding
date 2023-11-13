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

namespace pole_User
{
    public class User
    {
        string _steamid;
        int _coin;
        string _name;

        public User(string saveddata)
        {
            string[] dataarray = saveddata.Split(',');
            _steamid = dataarray[1];
            _coin = int.Parse(dataarray[2]);
            _name = dataarray[3];
        }

        public string steamid
        {
            get { return _steamid; }
        }
        public int coin
        {
            get { return _coin; }
            set { _coin = value; SaveUser(this); }
        }
        public string name
        {
            get
            {
                if (_name == "")
                {
                    return "NONAME" + _steamid;
                }
                else
                {
                    return _name;
                }
            }
            set { _name = value; SaveUser(this); }
        }

        public override string ToString()
        {
            string returner = "";

            returner += "u,";
            returner += _steamid + ",";
            returner += _coin.ToString() + ",";
            returner += _name.ToString() + ",";

            return returner;
        }

        public static List<User> list_user()
        {
            List<User> returner = new List<User>();
            List<string> data = rw.readfile(dir_root + file_server, false).Split('\n').ToList();
            foreach (string line in data)
            {
                if (line[0] == 'u')
                {
                    returner.Add(new User(line));
                }
            }
            return returner;

        }

        public static void SaveUser(User user)
        {
            if (!isserver) { return; }

            string[] data = rw.readfile(dir_root + file_server, false).Split('\n');

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].Contains(user.steamid))
                {
                    data[i] = user.ToString();
                    string datatosave = string.Join("\n", data);
                    rw.writefile(datatosave, file_server, dir_root);
                    return;
                }
            }

            Debug.LogError("ERROR: no user found in data. Data was not saved");
        }
        public static void AddUser(User user)
        {
            if (!isserver) { return; }
            User check = User.GetUser(user.steamid);
            if (check != null) { Debug.LogError("ERROR: Client already in database"); return; }


            List<string> data = rw.readfile(dir_root + file_server, false).Split('\n').ToList();
            data.Add(user.ToString());
            string datatosave = string.Join("\n", data);
            rw.writefile(datatosave, file_server, dir_root);
        }

        public static string IDtoName(string steamid, List<User> users)
        {
            foreach (User user in users)
            {
                if (user.steamid == steamid)
                {
                    return user.name;
                }
            }


            Debug.LogError("ERROR: user " + steamid + " not found in data");

            return "";
        }
        public static User GetUser(string steamid)
        {
            foreach (User user in User.list_user())
            {
                if (user.steamid == steamid)
                {
                    return user;
                }
            }


            Debug.LogError("ERROR: user " + steamid + " not found in data");

            return null;
        }
    }

}