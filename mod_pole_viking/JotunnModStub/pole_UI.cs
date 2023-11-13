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

namespace pole_UI
{

    class UIX
    {


        public static void ui_overlay()
        {
            ui_job_overlay();
        }
        public static void ui_job()
        {
            if (UI.job_selected == "")
            {
                if (job_manager_local.Count != 0)
                {
                    UI.job_selected = job_manager_local[0].jobname;
                    UI.job_overlay_xpquant = job_manager_local[0].xp;
                }
                else
                {
                    return;
                }
            }
            JobManager jobman = job_manager_local[0];
            Job thejob = jobman.job;
            int XP = jobman.xp;
            int LEVEL = thejob.levelbyxp(XP);
            int SCORELEVEL = jobman.levelscore;
            int numoflevels = thejob.xpcaps.Length;

            GUIStyle background = new GUIStyle(UI.skin1.GetStyle("Label"));
            GUIStyle buttons = new GUIStyle(UI.skin1.GetStyle("Button"));

            Color valorange = GUIManager.Instance.ValheimOrange;
            Color valyellow = GUIManager.Instance.ValheimYellow;
            buttons.normal.background = TEX_MyPNG["button01n"];
            buttons.normal.textColor = valorange;
            buttons.hover.background = TEX_MyPNG["button01h"];
            buttons.hover.textColor = valorange;
            buttons.active.background = TEX_MyPNG["button01c"];
            buttons.active.textColor = valorange;
            buttons.font = UI.skin1.font;
            buttons.fontSize = 24;

            background.font = UI.skin1.font;
            background.normal.textColor = valyellow;

            float width = UI.X * 2 / 3f;
            float height = 994f / 1334f * width;
            float rawwidth = 1338f;
            float rawheight = 994f;
            Vector2 slotrectxy = new Vector2(206f / rawwidth, 173f / rawheight);
            Vector2 slotrectwh = new Vector2(130f / rawwidth, 669f / rawheight);
            Rect arect = UI.RfromCenter(UI.X / 2, UI.Y / 2, width, height);
            Rect slotrect = new Rect(UI.RfromR(slotrectxy.x, slotrectxy.y, slotrectwh.x, slotrectwh.y, arect));


            int deltamouse = -(int)Math.Round(UnityInput.Current.mouseScrollDelta.y);
            if (deltamouse != 0)
            {
                if (UI.isMinR(slotrect))
                {
                    UI.job_bill_list_pos -= 0.01f * deltamouse;
                    UI.job_bill_list_pos = Math.Max(0, Math.Min(UI.job_bill_list_pos, 1 - 3f / numoflevels));
                }
            }
            float levelfloat = 0;
            if (LEVEL == thejob.xpcaps.Length)
            {
                levelfloat = 1;
            }
            else if (LEVEL == 0)
            {
                levelfloat = 0;
            }
            else
            {
                levelfloat = LEVEL + (1f * XP - thejob.xpcaps[LEVEL - 1]) / (1f * thejob.xpcaps[LEVEL] - thejob.xpcaps[LEVEL - 1]);
                levelfloat = levelfloat / numoflevels;
            }
            float levelscorefloat = (SCORELEVEL + 1) * 1f / numoflevels;


            float distancebetweenlevels = slotrect.height / 3f;
            float viewfloat = UI.job_bill_list_pos + 0.5f * 1 / numoflevels; //the view pos

            background.font = UI.skin1.font;
            background.normal.background = TEX_MyPNG["job_menu_back"];
            GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
            GUI.Label(arect, "", background);
            background.normal.background = TEX_MyPNG["job_menu_front"];
            GUI.Label(arect, "", background);
            GUI.backgroundColor = new Color(1, 0.5f, 0);
            background.normal.background = TEX_white;


            //XP BAR
            GUI.backgroundColor = new Color(0, 1, 1);
            if (levelscorefloat < levelfloat)
            {
                GUI.backgroundColor = new Color(1, 0, 0);
            }
            if (viewfloat + 3f / numoflevels < levelfloat)
            {
                GUI.Label(UI.RfromR(0.45f, 0, 0.1f, 1, slotrect), "", background);
            }
            if (viewfloat + 3f / numoflevels > levelfloat && viewfloat < levelfloat)
            {
                float sizeratio = (levelfloat - viewfloat) / (3f / numoflevels);
                GUI.Label(UI.RfromR(0.45f, 1 - sizeratio, 0.1f, sizeratio, slotrect), "", background);
            }
            if (levelscorefloat < levelfloat)
            {
                GUI.backgroundColor = new Color(0, 1, 1);
                if (viewfloat + 3f / numoflevels < levelscorefloat)
                {
                    GUI.Label(UI.RfromR(0.45f, 0, 0.1f, 1, slotrect), "", background);
                }
                if (viewfloat + 3f / numoflevels > levelscorefloat && viewfloat < levelscorefloat)
                {
                    float sizeratio = (levelscorefloat - viewfloat) / (3f / numoflevels);
                    GUI.Label(UI.RfromR(0.45f, 1 - sizeratio, 0.1f, sizeratio, slotrect), "", background);
                }
            }

            GUI.backgroundColor = new Color(1, 1, 1);
            TEX_MyPNG["runetube"].wrapMode = TextureWrapMode.Repeat;
            GUI.color = new Color(1, 0.5f, 0);
            GUI.DrawTextureWithTexCoords(UI.RfromR(0.45f, 0, 0.1f, 1, slotrect), TEX_MyPNG["runetube"], new Rect(0.0f, 0.0f - viewfloat, 1, 1));
            GUI.color = Color.white;

            for (int i = 0; i < numoflevels + 1; i++)
            {
                background.normal.background = TEX_dark;
                Rect levelrect = UI.RfromCenter(slotrect.x + slotrect.width / 2f, slotrect.y + slotrect.height - i * slotrect.height / 3f + (viewfloat) * 20 * distancebetweenlevels, slotrect.width / 2f, slotrect.width / 2f);
                if (arect.Contains(new Vector2(levelrect.x, levelrect.y - arect.height * 0.05f)) && arect.Contains(new Vector2(levelrect.x + levelrect.width, levelrect.y + levelrect.height + arect.height * 0.05f)))
                {
                    GUI.color = new Color(0.5f, 0.5f, 0.5f);
                    if (i == LEVEL + 1)
                    {
                        GUI.color = new Color(1, 1, 1);
                    }
                    if (i > LEVEL + 1)
                    {
                        GUI.color = new Color(1, 0.3f, 0.3f);
                        GUI.Label(levelrect, i.ToString(), buttons);
                        continue;
                    }
                    if (GUI.Button(levelrect, i.ToString(), buttons))
                    {
                        UI.job_level_selected = i;
                    }
                }
            }

            GUI.color = Color.white;

            background.normal.background = TEX_MyPNG["job_menu_front"];
            GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
            GUI.Label(arect, "", background);

            Rect mainrect = UI.RfromR(1.5f, 0, 6, 1, slotrect);

            if (UI.job_level_selected > 0)
            {
                background.normal.background = TEX_trans;
                background.alignment = TextAnchor.MiddleCenter;
                background.fontSize = 36;
                GUI.Label(UI.RfromR(0, 0, 1, 0.1f, mainrect), "Chapitre " + UI.job_level_selected + " de votre saga", background);
                background.alignment = TextAnchor.MiddleLeft;
                background.normal.background = TEX_dark;
                background.fontSize = 20;
                background.normal.textColor = valorange;
                background.padding.left = 20;
                background.padding.right = 20;
                GUI.Label(UI.RfromR(0, 0.3f, 1, 0.2f, mainrect), thejob.descriptions[UI.job_level_selected - 1].Replace("$$$", Player.m_localPlayer.GetHoverName()), background);
            }



        }
        public static void ui_job_overlay()
        {
            float deltatime = Time.time - UI.job_overlay_timestamp_show;
            float alpha = 0;
            if (deltatime < 0) { return; }
            else if (deltatime < 1) { float d = deltatime; alpha = deltatime; }
            else if (deltatime < 10) { alpha = 1; }
            else if (deltatime < 12) { float d = 1 - (deltatime - 10) / 2f; alpha = deltatime; }
            else { return; }
            GUI.color = new Color(1, 1, 1, alpha);
            if (UI.job_selected == "") { return; }
            JobManager jobman = job_manager_local[0];
            Job thejob = jobman.job;
            int XP = jobman.xp;
            int LEVEL = thejob.levelbyxp(XP);
            int SCORELEVEL = jobman.levelscore;
            int numoflevels = thejob.xpcaps.Length;

            if (LEVEL == thejob.xpcaps.Length) { return; }

            GUIStyle main = new GUIStyle(UI.skin1.GetStyle("Label"));
            main.normal.background = TEX_trans;

            Color valorange = GUIManager.Instance.ValheimOrange;
            Color valyellow = GUIManager.Instance.ValheimYellow;

            main.normal.textColor = valorange;
            main.font = UI.skin1.font;
            main.alignment = TextAnchor.MiddleCenter;
            main.fontSize = 16;


            GUI.DrawTexture(new Rect(0, 0, UI.X, UI.Y), TEX_MyPNG["job_xp_bar"]);

            Rect Label = UI.Rfrom1080p(1752, 1006, 160, 74);
            Rect BotLVL = UI.Rfrom1080p(1807, 957, 45, 45);
            Rect TopLVL = UI.Rfrom1080p(1807, 656, 45, 45);
            Rect XPSLOT = UI.Rfrom1080p(1816, 704, 28, 250);

            float xpperdt = 3 * (thejob.xpcaps[LEVEL] - thejob.xpcaps[LEVEL - 1]) / 100f;
            if (UI.job_overlay_xpquant < jobman.xp) { UI.job_overlay_xpquant += (int)Math.Ceiling(UI.dt * xpperdt); }
            UI.job_overlay_xpquant = Math.Min(UI.job_overlay_xpquant, jobman.xp);


            GUI.Label(BotLVL, LEVEL.ToString(), main);
            GUI.Label(TopLVL, (LEVEL + 1).ToString(), main);

            GUI.Label(UI.RfromR(0, 0, 1, 1 / 3f, Label), UI.job_selected, main);
            main.normal.textColor = Color.green;
            if (UI.job_overlay_xpquant < jobman.xp)
            {
                GUI.Label(UI.RfromR(0, 1 / 3f, 1, 1 / 3f, Label), "+" + (jobman.xp - UI.job_overlay_xpquant).ToString() + " xp", main);
            }
            main.normal.textColor = valorange;
            GUI.Label(UI.RfromR(0, 2 / 3f, 1, 1 / 3f, Label), UI.job_overlay_xpquant + " / " + thejob.xpcaps[LEVEL], main);



            //XP BAR

            float sizeratio = (jobman.xp - thejob.xpcaps[LEVEL - 1]) / (1f * (thejob.xpcaps[LEVEL] - thejob.xpcaps[LEVEL - 1]));
            float sizeratioprogress = (UI.job_overlay_xpquant - thejob.xpcaps[LEVEL - 1]) / (1f * (thejob.xpcaps[LEVEL] - thejob.xpcaps[LEVEL - 1]));
            sizeratioprogress = Math.Max(sizeratioprogress, 0);


            main.normal.background = TEX_white;
            float widthratio = 1 / 3f;
            GUI.backgroundColor = new Color(1, 1, 0);
            GUI.Label(UI.RfromR(0.5f - widthratio / 2, 1 - sizeratio, widthratio, sizeratio, XPSLOT), "", main);
            GUI.backgroundColor = new Color(0, 1, 1);
            if (jobman.levelscore < jobman.levelXP)
            {
                GUI.backgroundColor = new Color(1, 0, 0);
            }
            GUI.Label(UI.RfromR(0.5f - widthratio / 2, 1 - sizeratioprogress, widthratio, sizeratioprogress, XPSLOT), "", main);
            GUI.backgroundColor = new Color(1, 1, 1);
            GUI.color = new Color(1, 0.5f, 0, alpha);
            GUI.DrawTexture(UI.RfromR(0.5f - widthratio / 2, 0, widthratio, 1, XPSLOT), TEX_MyPNG["runetube"]);


            GUI.backgroundColor = new Color(1, 1, 1);
            GUI.color = new Color(1, 1, 1);
        }
        public static void ui_money()
        {
            Rect arect = UI.RfromCenter(UI.X / 2, UI.Y / 2, UI.X / 6, UI.Y / 1.5f);
            GUIStyle thestyle = new GUIStyle(UI.skin1.GetStyle("Label"));
            thestyle.font = UI.skin1.font;
            thestyle.normal.background = TEX_dark;
            GUI.Label(arect, "", thestyle);
            thestyle.normal.background = TEX_trans;
            GUIStyle titlestyle = new GUIStyle(UI.skin1.GetStyle("Label"));
            titlestyle.font = UI.skin1.font;
            titlestyle.alignment = TextAnchor.MiddleCenter;
            titlestyle.normal.textColor = GUIManager.Instance.ValheimOrange;
            titlestyle.fontSize = 24;
            GUI.Label(UI.RfromR(0, 0, 1, 0.1f, arect), "Offrandes aux dieux", titlestyle);


            int listsize = 20;
            for (int i = 0; i < listsize; i++)
            {
                int fonts = 16;
                Color col = GUIManager.Instance.ValheimOrange;
                string text = (i + 1).ToString() + ". ";
                if (i < UI.money_user_list_order.Count)
                {
                    User us = UI.money_user_list_order[i];
                    text += us.name + " : " + us.coin;
                    if (us.steamid == user_local.steamid)
                    {
                        //fonts = 22;
                        col = GUIManager.Instance.ValheimYellow;
                    }
                }
                else
                {
                    text += "...";
                }
                GUI.Label(UI.RfromR(0, 0.1f + i * 0.8f / listsize, 1, 0.8f / listsize, arect), text, thestyle);
            }

            if (GUI.Button(UI.RfromR(0, 0.9f, 1, 0.1f, arect), "Confier ses piastres à Njörd", UI.skin1.button))
            {
                MeltCoins();
            }
        }
        public static void ui_market()
        {
            Rect arect = UI.RfromCenter(UI.X / 2, UI.Y / 2, UI.X * 2 / 3f, UI.Y / 1.5f);
            GUIStyle background = new GUIStyle(UI.skin1.GetStyle("Label"));
            GUIStyle buttons = new GUIStyle(UI.skin1.GetStyle("Button"));
            background.font = UI.skin1.font;
            GUI.Label(arect, "", background);
            if (GUI.Button(UI.RfromR(0, 0, 0.2f, 0.1f, arect), "Buy / Sell", buttons))
            {
                UI.market_clean();
                UI.market_window = 0;
            }
            if (GUI.Button(UI.RfromR(0.2f, 0, 0.2f, 0.1f, arect), "Create Sell Bill", buttons))
            {
                UI.market_clean();
                UI.market_window = 1;
            }
            if (GUI.Button(UI.RfromR(0.4f, 0, 0.2f, 0.1f, arect), "Create Buy Bill", buttons))
            {
                UI.market_clean();
                UI.market_window = 2;
            }
            if (UI.market_window == 0)
            {
                Rect listrect = UI.RfromR(0, 0.1f, 1, 0.8f, arect);
                ui_market_billlist(listrect, list_bills);
            }
            if (UI.market_window == 1)
            {
                Rect listrect = UI.RfromR(0, 0.1f, 1 / 3f, 0.8f, arect);
                Rect listrectright = UI.RfromR(1 / 3f, 0.1f, 2 / 3f, 0.8f, arect);
                UI.market_item_field0 = GUI.TextField(UI.RfromR(0, 1, 1, 0.1f, listrect), UI.market_item_field0, background);
                List<string> listitems = new List<string>();
                foreach (ItemDrop.ItemData item in Player.m_localPlayer.GetInventory().GetAllItems())
                {
                    string partfixedname = item.TokenName().Replace("$item_", "");
                    string fixedname = char.ToUpper(partfixedname[0]) + partfixedname.Substring(1);
                    if (!listitems.Contains(fixedname) && fixedname.ToLower().Contains(UI.market_item_field0.ToLower()))
                    {
                        listitems.Add(fixedname);
                    }
                }
                ui_market_itemlist(listrect, listitems);
                if (UI.market_item_selected != "")
                {
                    ui_market_create_bill(listrectright);
                }
            }
            if (UI.market_window == 2)
            {
                Rect listrect = UI.RfromR(0, 0.1f, 1 / 3f, 0.8f, arect);
                Rect listrectright = UI.RfromR(1 / 3f, 0.1f, 2 / 3f, 0.8f, arect);
                UI.market_item_field0 = GUI.TextField(UI.RfromR(0, 1, 1, 0.1f, listrect), UI.market_item_field0, background);

                List<string> listitems = new List<string>();
                foreach (string astring in TEX_Icons.Keys)
                {
                    string fixedname = astring.Replace(".png", "");
                    if (!listitems.Contains(fixedname) && fixedname.ToLower().Contains(UI.market_item_field0.ToLower()))
                    {
                        listitems.Add(fixedname);
                    }
                }
                ui_market_itemlist(listrect, listitems);
                if (UI.market_item_selected != "")
                {
                    ui_market_create_bill(listrectright);
                }
            }
        }
        public static void ui_market_create_bill(Rect frame)
        {
            GUIStyle butstyle = new GUIStyle(UI.skin1.button);
            GUIStyle main = new GUIStyle(UI.skin1.GetStyle("Label"));
            string itemname = UI.market_item_selected;
            main.alignment = TextAnchor.MiddleCenter;

            string text = "Selling: ";
            if (UI.market_window == 2)
            {
                text = "Buying: ";
            }
            Bill fakebill = new Bill(UI.market_fakebill);
            string ownID = user_local.steamid;
            int billID = -1;
            string itemID = UI.market_item_selected;
            int quantity = fakebill.quantity;
            float price = fakebill.price;
            string meta = fakebill.metadata;

            if (UI.market_window == 2)
            {
                text = "Buying: ";
            }
            GUI.Label(UI.RfromR(0, 0, 1, 1 / 6f, frame), text + itemname, main);
            GUI.Label(UI.RfromR(0, 1 / 6f, 1 / 2f, 1 / 6f, frame), "QUANTITY: ", main);
            int.TryParse(GUI.TextField(UI.RfromR(1 / 2f, 1 / 6f, 1 / 2f, 1 / 6f, frame), quantity.ToString(), main), out quantity);
            quantity = Math.Abs(quantity);
            GUI.Label(UI.RfromR(0, 2 / 6f, 1 / 2f, 1 / 6f, frame), "Price per U: ", main);
            float.TryParse(GUI.TextField(UI.RfromR(1 / 2f, 2 / 6f, 1 / 2f, 1 / 6f, frame), price.ToString(), main), NumberStyles.Float, floatinfo, out price);
            price = Math.Abs(price);

            int pricetot = (int)Math.Ceiling(quantity * price);
            string summary = "Selling " + quantity.ToString() + " " + itemID + " for a total of " + pricetot + " coins";
            if (UI.market_window == 2)
            {
                summary = "Buying up to " + quantity.ToString() + " " + itemID + " for up to " + pricetot + " coins";
            }

            Inventory inv = Player.m_localPlayer.GetInventory();
            string invitemname = "$item_" + itemID.ToLower();
            int itemcount = inv.CountItems(invitemname);

            bool canbuyorsell = (UI.market_window == 1 && quantity <= itemcount) | (UI.market_window == 2 && user_local.coin >= pricetot);
            if (!canbuyorsell)
            {
                if (UI.market_window == 1)
                {
                    summary = "You only have " + itemcount.ToString() + " " + itemID;
                }
                if (UI.market_window == 2)
                {
                    summary += ". You can't afford the entire bill!";
                }
            }



            if (GUI.Button(UI.RfromR(0, 3 / 6f, 1, 1 / 6f, frame), summary, butstyle) && canbuyorsell)
            {
                int ID = 0;
                while (Bill.GetBill(ownID, ID, list_bills) != null) { ID++; }

                ownID = user_local.steamid;
                billID = ID;
                itemID = UI.market_item_selected;

                if (UI.market_window == 1)
                {
                    inv.RemoveItem(invitemname, quantity);
                }
                if (UI.market_window == 2)
                {
                    price = -price;
                    string data = user_local.steamid.ToString();
                    data += '\n';
                    data += (-pricetot).ToString();
                    MyRPCSend("request_useraddcoin", data);
                }


                MyRPCSend("request_modbill", new Bill(ownID, billID, itemID, quantity, price, meta).ToString() + "|0");


                return;
            }




            UI.market_fakebill = new Bill(ownID, billID, itemID, quantity, price, meta).ToString();
        }
        public static void ui_market_itemlist(Rect frame, List<string> itemlist)
        {
            int numofslots = 15;

            GUIStyle butstyle = new GUIStyle(UI.skin1.button);
            GUIStyle main = new GUIStyle(UI.skin1.GetStyle("Label"));
            main.font = UI.skin1.font;
            main.alignment = TextAnchor.MiddleCenter;
            main.normal.background = TEX_dark;

            int deltamouse = -(int)Math.Round(UnityInput.Current.mouseScrollDelta.y);
            if (deltamouse != 0)
            {
                if (UI.isMinR(frame))
                {
                    UI.market_bill_list_pos += deltamouse;
                    UI.market_bill_list_pos = Math.Min(UI.market_bill_list_pos, itemlist.Count - numofslots);
                    UI.market_bill_list_pos = Math.Max(0, UI.market_bill_list_pos);
                }
            }
            for (int i = 0; i < numofslots; i++)
            {
                int currentid = i + UI.market_bill_list_pos;
                if (currentid >= itemlist.Count) { return; }
                Rect arect = UI.RfromR(0, i * 1f / numofslots, 1, 1f / numofslots, frame);
                string itemname = itemlist[currentid];
                main.normal.background = TEX_Icons[itemname.ToLower().Replace("_", "")];;
                GUI.Label(new Rect(arect.x, arect.y, arect.height, arect.height), "", main);
                Rect mainright = new Rect(arect.x + arect.height, arect.y, arect.width - arect.height, arect.height);
                if (GUI.Button(mainright, itemname, butstyle))
                {
                    UI.market_item_selected = itemname;
                }
            }
        }
        public static void ui_market_billlist(Rect frame, List<Bill> alist)
        {
            int numofslots = 15;
            while (UI.market_bill_field0.Count < alist.Count)
            {
                UI.market_bill_field0.Add(0);
            }

            GUIStyle main = new GUIStyle(UI.skin1.GetStyle("Label"));
            main.font = UI.skin1.font;
            main.alignment = TextAnchor.MiddleCenter;
            main.normal.background = TEX_dark;


            int deltamouse = -(int)Math.Round(UnityInput.Current.mouseScrollDelta.y);
            if (deltamouse != 0)
            {
                if (UI.isMinR(frame))
                {
                    UI.market_bill_list_pos += deltamouse;
                    UI.market_bill_list_pos = Math.Min(UI.market_bill_list_pos, alist.Count - numofslots);
                    UI.market_bill_list_pos = Math.Max(0, UI.market_bill_list_pos);
                }
            }


            for (int i = 0; i < numofslots; i++)
            {
                int currentid = i + UI.market_bill_list_pos;
                if (currentid >= alist.Count) { return; }
                Rect arect = UI.RfromR(0, i * 1f / numofslots, 1, 1f / numofslots, frame);
                Bill bill = alist[currentid];
                if (bill.ownerID == user_local.steamid)
                {
                    ui_market_bill_owner(arect, bill, main, currentid);
                }
                else
                {
                    ui_market_bill(arect, bill, main, currentid);
                }
            }

        }
        public static void ui_market_bill(Rect frame, Bill abill, GUIStyle astyle, int index)
        {
            GUIStyle butstyle = new GUIStyle(UI.skin1.button);
            butstyle.fontSize = 14;
            butstyle.font = UI.skin1.font;
            astyle.font = UI.skin1.font;
            astyle.alignment = TextAnchor.MiddleCenter;
            astyle.normal.background = TEX_Icons[abill.itemID.ToLower().Replace("_","")];

            GUI.Label(new Rect(frame.x, frame.y, frame.height, frame.height), "", astyle);
            Rect mainright = new Rect(frame.x + frame.height, frame.y, frame.width, frame.height);
            astyle.normal.background = TEX_dark;
            GUI.Label(UI.RfromR(0, 0, 0.1f, 1, mainright), abill.itemID, astyle);
            GUI.Label(UI.RfromR(0.1f, 0, 0.1f, 1, mainright), User.IDtoName(abill.ownerID, list_users) + " (" + abill.quantity.ToString() + ")", astyle);
            GUI.Label(UI.RfromR(0.2f, 0, 0.1f, 1, mainright), "P/U: " + abill.price.ToString(), astyle);
            string rawtext = GUI.TextField(UI.RfromR(0.3f, 0, 0.1f, 1, mainright), UI.market_bill_field0[index].ToString(), astyle);
            string numeric = Regex.Replace(rawtext, "[^0-9]", "");
            int val = 0;
            if (!int.TryParse(numeric, out val)) { val = 0; }
            val = Math.Max(Math.Min(val, abill.quantity), 0);

            if (abill.price > 0)
            {
                int buyprice = (int)Math.Ceiling(val * abill.price);
                if (buyprice > 0)
                {
                    if (GUI.Button(UI.RfromR(0.4f, 0, 0.15f, 1, mainright), "Buy: " + buyprice.ToString() + "p", butstyle) && buyprice <= user_local.coin)
                    {
                        //add item
                        Imanip.spawnItemMaxStack(abill.itemID, val);
                        //remove money
                        string data = user_local.steamid.ToString();
                        data += '\n';
                        data += ((int)(buyprice)).ToString();
                        MyRPCSend("request_useraddcoin", data);
                        //modbill
                        MyRPCSend("request_modbill", abill.ToString() + "|" + (-val).ToString());
                    }
                }
            }
            else if (abill.price < 0)
            {
                Inventory inv = Player.m_localPlayer.GetInventory();
                string itemname = abill.itemID.ToString();
                string invitemname = "$item_" + itemname.ToLower();
                int itemcount = inv.CountItems(invitemname);
                val = Math.Min(val, itemcount);
                if (val > 0)
                {
                    if (GUI.Button(UI.RfromR(0.4f, 0, 0.15f, 1, mainright), "Sell: " + val.ToString() + " " + abill.itemID, butstyle))
                    {
                        //remove item
                        inv.RemoveItem(invitemname, val);
                        // get paid
                        string data = user_local.steamid.ToString();
                        data += '\n';
                        data += ((int)(val * Math.Ceiling(abill.price * -1))).ToString();
                        MyRPCSend("request_useraddcoin", data);
                        MyRPCSend("request_modbill", abill.ToString() + "|" + (-val).ToString());
                        //CHECK IF create new bill or amend old one
                        foreach (Bill checkbill in list_bills)
                        {
                            if (checkbill.ownerID == abill.ownerID && checkbill.itemID == abill.itemID && checkbill.price == 0)
                            {
                                MyRPCSend("request_modbill", checkbill.ToString() + "|" + val.ToString());
                                return;
                            }
                        }

                        int ID = 0;

                        while (Bill.GetBill(abill.ownerID, ID, list_bills) != null) { ID++; }


                        Bill newbill = new Bill(abill.ownerID, ID, abill.itemID, val, 0, abill.metadata);
                        MyRPCSend("request_modbill", newbill.ToString() + "|0");
                        return;
                    }
                }
            }
            UI.market_bill_field0[index] = val;

        }
        public static void ui_market_bill_owner(Rect frame, Bill abill, GUIStyle astyle, int index)
        {
            GUIStyle butstyle = new GUIStyle(UI.skin1.button);
            butstyle.fontSize = 14;
            butstyle.font = UI.skin1.font;
            astyle.font = UI.skin1.font;
            astyle.alignment = TextAnchor.MiddleCenter;
            astyle.normal.background = TEX_Icons[abill.itemID.ToLower().Replace("_", "")];

            GUI.Label(new Rect(frame.x, frame.y, frame.height, frame.height), "", astyle);
            Rect mainright = new Rect(frame.x + frame.height, frame.y, frame.width, frame.height);
            astyle.normal.background = TEX_dark;
            GUI.Label(UI.RfromR(0, 0, 0.1f, 1, mainright), abill.itemID, astyle);
            GUI.Label(UI.RfromR(0.1f, 0, 0.1f, 1, mainright), User.IDtoName(abill.ownerID, list_users) + " (" + abill.quantity.ToString() + ")", astyle);
            GUI.Label(UI.RfromR(0.2f, 0, 0.1f, 1, mainright), "P/U: " + abill.price.ToString(), astyle);
            string rawtext = GUI.TextField(UI.RfromR(0.3f, 0, 0.1f, 1, mainright), UI.market_bill_field0[index].ToString(), astyle);
            string numeric = Regex.Replace(rawtext, "[^0-9]", "");
            int val = 0;
            if (!int.TryParse(numeric, out val)) { val = 0; }
            UI.market_bill_field0[index] = val;
            string Quant = val.ToString() + " ";


            Inventory inv = Player.m_localPlayer.GetInventory();
            string invitemname = "$item_" + abill.itemID.ToLower();
            int itemcount = inv.CountItems(invitemname);
            int totprice = (int)Math.Abs(Math.Ceiling(abill.price * val));
            if (abill.price < 0)
            {
                // BUY bill you want to recup your money or increase offer
                bool canbuy = totprice <= user_local.coin;
                if (GUI.Button(UI.RfromR(0.4f, 0, 0.15f, 1, mainright), "Remove " + Quant + " " + abill.itemID + " from offer", butstyle))
                {
                    totprice = (int)Math.Abs(Math.Ceiling(abill.price * Math.Min(val, abill.quantity)));
                    string data = user_local.steamid.ToString();
                    data += '\n';
                    data += (totprice).ToString();
                    MyRPCSend("request_useraddcoin", data);
                    MyRPCSend("request_modbill", abill.ToString() + "|" + (-val).ToString());
                }
                string textadd = "Add " + Quant + " " + abill.itemID;
                if (!canbuy) { textadd = totprice + " is more than you can afford!"; }
                if (GUI.Button(UI.RfromR(0.55f, 0, 0.15f, 1, mainright), textadd, butstyle) && canbuy)
                {
                    string data = user_local.steamid.ToString();
                    data += '\n';
                    data += (-totprice).ToString();
                    MyRPCSend("request_useraddcoin", data);
                    MyRPCSend("request_modbill", abill.ToString() + "|" + (val).ToString());
                }
            }
            if (abill.price == 0)
            {
                // RECOVER bill you are trying to recover items
                if (GUI.Button(UI.RfromR(0.4f, 0, 0.15f, 1, mainright), "Remove " + Quant + " " + abill.itemID, butstyle))
                {
                    Imanip.spawnItemMaxStack(abill.itemID, Math.Min(abill.quantity, val));
                    MyRPCSend("request_modbill", abill.ToString() + "|" + (-val).ToString());
                }
            }
            if (abill.price > 0)
            {
                // SELL bill you are trying to recover or add items
                bool canadd = val <= itemcount;
                if (GUI.Button(UI.RfromR(0.4f, 0, 0.15f, 1, mainright), "Remove " + Quant + abill.itemID, butstyle))
                {
                    Imanip.spawnItemMaxStack(abill.itemID, Math.Min(abill.quantity, val));
                    MyRPCSend("request_modbill", abill.ToString() + "|" + (-val).ToString());
                }
                string textadd = "Add " + Quant + " " + abill.itemID;
                if (!canadd) { textadd = "You only have " + itemcount + " " + abill.itemID; }
                if (GUI.Button(UI.RfromR(0.55f, 0, 0.15f, 1, mainright), textadd, butstyle) && canadd)
                {
                    inv.RemoveItem(invitemname, val);
                    MyRPCSend("request_modbill", abill.ToString() + "|" + (val).ToString());
                }

            }
        }
        public static void ui_cheat()
        {

            Rect arect = UI.RfromCenter(UI.X / 2, UI.Y / 2, UI.X / 6, UI.Y / 1.5f);
            GUIStyle thestyle = new GUIStyle(UI.skin1.GetStyle("Label"));
            thestyle.font = UI.skin1.font;
            thestyle.normal.background = TEX_dark;
            GUI.Label(arect, "", thestyle);
            thestyle.normal.background = TEX_trans;
            GUI.Label(UI.RfromR(0, 0, 1, 0.1f, arect), "Offrandes aux dieux", thestyle);
            if (GUI.Button(UI.RfromR(0, 0, 0.1f, 0.1f, arect), "100 PIASTRES", UI.skin1.button))
            {
                Player.m_localPlayer.GetInventory().AddItem(PrefabManager.Instance.GetPrefab("Coins"), 100);
            }
            if (GUI.Button(UI.RfromR(0.1f, 0, 0.1f, 0.1f, arect), "100 WOOD", UI.skin1.button))
            {
                Player.m_localPlayer.GetInventory().AddItem(PrefabManager.Instance.GetPrefab("Wood"), 100);
            }
            if (GUI.Button(UI.RfromR(0.2f, 0, 0.1f, 0.1f, arect), "100 STONE", UI.skin1.button))
            {
                Player.m_localPlayer.GetInventory().AddItem(PrefabManager.Instance.GetPrefab("Stone"), 100);
            }
            if (GUI.Button(UI.RfromR(0.3f, 0, 0.1f, 0.1f, arect), "100 IRON", UI.skin1.button))
            {
                Player.m_localPlayer.GetInventory().AddItem(PrefabManager.Instance.GetPrefab("Iron"), 100);
            }
            if (GUI.Button(UI.RfromR(0.4f, 0, 0.1f, 0.1f, arect), "cheat", UI.skin1.button))
            {
                Imanip.spawnItemMaxStack("Wood", 105);
            }

        }



    }
    public class UI
    {
        public static float dtstamp = 0;
        public static bool inmenu = false;
        public static float X = Screen.width;
        public static float Y = Screen.height;
        public static GUISkin skin1;

        private static bool _market = false;
        private static bool _money = false;
        private static bool _cheat = false;
        private static bool _job = false;

        public static float dt
        {
            get
            {
                return Time.time - dtstamp;
            }
        }

        public static bool market
        {
            get { return _market; }
            set
            {
                _market = value;
                market_clean();
                UI.update_input_lock();
            }
        }
        public static bool money
        {
            get { return _money; }
            set
            {
                _money = value;
                UI.update_input_lock();
                if (_money) { MyRPCSend("request_userdata", "null"); }
            }
        }
        public static bool cheat
        {
            get { return _cheat; }
            set { _cheat = value; UI.update_input_lock(); }
        }
        public static bool job
        {
            get { return _job; }
            set { _job = value; UI.update_input_lock(); }
        }

        public static List<User> money_user_list_order = new List<User>();

        public static int market_window = 0;
        public static int market_bill_list_pos = 0;
        public static string market_item_field0 = "";
        public static string market_item_selected = "";
        public static string market_fakebill = new Bill("").ToString();
        public static List<int> market_bill_field0 = new List<int>();

        public static float job_bill_list_pos = 0;
        public static string job_selected = "";
        public static int job_level_selected = 0;
        public static float job_overlay_timestamp_show = -10000f;
        public static int job_overlay_xpquant;
        public static void market_clean()
        {
            market_fakebill = new Bill("").ToString();
            market_item_selected = "";
            market_item_field0 = "";
            market_bill_list_pos = 0;
            for (int k = 0; k < market_bill_field0.Count; k++) { market_bill_field0[k] = 0; }
        }

        public static Rect RfromCenter(float x, float y, float w, float h)
        {
            return new Rect(x - w / 2f, y - h / 2f, w, h);
        }
        public static Rect RfromR(float rx, float ry, float rw, float rh, Rect arect)
        {
            return new Rect(arect.x + rx * arect.width, arect.y + ry * arect.height, arect.width * rw, arect.height * rh);
        }
        public static Rect RRatio(float ratio, Rect arect)
        {
            return RfromCenter(arect.x + arect.width / 2f, arect.y + arect.height / 2f, arect.width * ratio, arect.height * ratio);
        }
        public static Rect Rfrom1080p(float x, float y, float w, float h)
        {
            float rx = X / (1920f);
            float ry = Y / (1080f);
            return new Rect(x * rx, y * ry, w * rx, h * ry);
        }

        public static bool isMinR(Rect arect)
        {
            return arect.Contains(Input.mousePosition);
        }
        public static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];

            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();

            return result;
        }


        public static void update_screen()
        {
            UI.X = Screen.width;
            UI.Y = Screen.height;
        }
        public static void update_input_lock()
        {
            inmenu = money | market | cheat | job;
            if (inmenu)
            {
                GUIManager.BlockInput(inmenu);
            }
            else
            {
                GUIManager.BlockInput(inmenu);
                GUIManager.BlockInput(inmenu);
                GUIManager.BlockInput(inmenu);
            }
        }


    }

}