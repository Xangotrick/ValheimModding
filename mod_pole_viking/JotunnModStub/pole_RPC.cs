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
using pole_Bill;
using pole_UI;

using pole_User;
using pole_Bill;

namespace pole_RPC
{
    class RPC
    {
        public static CustomRPC MyRPC;

        public static void MeltCoins()
        {
            Player theplayer = Player.m_localPlayer;

            Inventory inv = theplayer.GetInventory();
            int coincount = inv.CountItems("$item_coins");
            if (coincount > 0)
            {
                inv.RemoveItem("$item_coins", coincount);
                string data = user_local.steamid.ToString();
                data += '\n';
                data += coincount;
                MyRPCSend("request_useraddcoin", data);
            }
        }

        public void BillServerRequest(Bill abill, int delta)
        {

            string data = "";
            data += abill.ToString() + "|";
            data += delta.ToString();
            MyRPCSend("request_modbill", data);
        }



        public static void MyRPCSend(string title, string data)
        {
            string datatosend = title + "$" + data;
            int L = datatosend.Length;
            if (L % 4 > 0)
            {
                datatosend = datatosend.PadRight(L + 4 - L % 4, '=');
            }


            ZPackage package = new ZPackage();
            package.Write(datatosend);

            ZNetPeer target = ZNet.instance.GetServerPeer();
            if (isserver)
            {
                Debug.Log("send to users");
                MyRPC.SendPackage(ZNet.instance.GetPeers(), package);
            }
            else
            {
                Debug.Log("send to server");
                MyRPC.SendPackage(ZNet.instance.GetServerPeer().m_uid, package);
            }
        }
        public static IEnumerator MyRPCServerReceive(long sender, ZPackage package)
        {
            yield return null;
            string data = package.ReadString();
            Debug.Log(data);
            data = data.Trim('=');
            Debug.LogError(data);
            string[] datasplit = data.Split('$');
            Debug.LogError(datasplit);
            ServerRPCManager(datasplit[0], datasplit[1]);
        }
        public static IEnumerator MyRPCClientReceive(long sender, ZPackage package)
        {
            yield return null;
            string data = package.ReadString();
            Debug.Log(data);
            Debug.Log(data.Trim('='));
            string[] datasplit = data.Trim('=').Split('$');
            ClientRPCManager(datasplit[0], datasplit[1]);
        }

        public static void ServerRPCManager(string title, string data)
        {
            switch (title)
            {
                case "request_userdata":
                    S_userdatasync();
                    break;
                case "request_billdata":
                    S_billdatasync();
                    break;
                case "request_useraddcoin":
                    S_addcointouser(data);
                    break;
                case "request_modbill":
                    S_modbill(data);
                    break;
                case "request_login":
                    S_request_login(data);
                    break;
            }
        }
        public static void ClientRPCManager(string title, string data)
        {
            switch (title)
            {
                case "sync_userdata":
                    C_userdatasync(data);
                    break;
                case "sync_billdata":
                    C_billdatasync(data);
                    break;
            }
        }

        public static void S_userdatasync()
        {
            string data = "";
            foreach (User user in User.list_user())
            {
                data += user.ToString() + '\n';
            }
            MyRPCSend("sync_userdata", data);
        }
        public static void S_billdatasync()
        {
            string data = "";
            foreach (Bill bill in Bill.list_bill())
            {
                data += bill.ToString() + '\n';
            }
            MyRPCSend("sync_billdata", data);
        }

        public static void S_request_login(string data)
        {
            string[] datasplit = data.Split('|');
            Debug.Log(data);
            string id = datasplit[0];
            string username = datasplit[1];

            User theuser = User.GetUser(id);
            if (theuser == null)
            {
                User newuser = new User("u," + id + ",0," + username + ',');
                User.AddUser(newuser);
            }
            else
            {
                if (theuser.name != username)
                {
                    theuser.name = username;
                }
            }
            S_userdatasync();
            S_billdatasync();
        }
        public static void S_addcointouser(string data)
        {
            string[] datasplit = data.Split('\n');
            User user = User.GetUser(datasplit[0]);
            int quantity = int.Parse(datasplit[1]);
            Debug.Log(user.steamid);
            Debug.Log(User.GetUser(user.steamid).coin);
            User.GetUser(user.steamid).coin += quantity;
            Debug.Log(User.GetUser(user.steamid).coin);
            S_userdatasync();
        }
        public static void S_modbill(string data)
        {
            string[] datum = data.Split('|');
            Bill thebill = new Bill(datum[0]);
            Bill referencebill = Bill.GetBill(thebill.ownerID, thebill.billID);
            int delta = int.Parse(datum[1]);
            if (referencebill == null)
            {
                bool nocopy = true;
                foreach (Bill abill in Bill.list_bill())
                {
                    if (abill.ownerID == thebill.ownerID && abill.metadata == thebill.metadata && abill.price == thebill.price && abill.itemID == thebill.itemID)
                    {
                        abill.quantity += thebill.quantity;
                        break;
                    }
                }
                if (nocopy) { Bill.AddBill(thebill); }
            }
            else if (referencebill.quantity + delta <= 0)
            {
                Bill.RemoveBill(referencebill);
            }
            else
            {
                referencebill.quantity += delta;
            }
            S_billdatasync();
        }
        public static void C_userdatasync(string data)
        {
            List<string> datalines = data.Split('\n').ToList();

            foreach (string astring in datalines)
            {
                if (astring.Length > 0)
                {
                    if (astring[0] == 'u')
                    {
                        User theuser = new User(astring);
                        if (theuser.steamid == Steamworks.SteamUser.GetSteamID().ToString())
                        {
                            user_local = theuser;
                        }
                        bool notyetthere = true;
                        foreach (User auser in list_users)
                        {
                            if (auser.steamid == theuser.steamid)
                            {
                                notyetthere = false;
                                auser.coin = theuser.coin;
                                break;
                            }
                        }
                        if (notyetthere) { list_users.Add(theuser); }
                    }
                }
            }

            foreach (User user in list_users)
            {
                Debug.Log(user.ToString());
            }
            UI.money_user_list_order = list_users.OrderByDescending(order => order.coin).ToList();
        }
        public static void C_billdatasync(string data)
        {
            List<string> datalines = data.Split('\n').ToList();
            list_bills = new List<Bill>();
            foreach (string astring in datalines)
            {
                if (astring.Length == 0) { return; }
                list_bills.Add(new Bill(astring));
            }
            foreach (Bill bill in list_bills)
            {
                Debug.Log(bill.ToString());
            }
            UI.market_clean();
        }

    }
}