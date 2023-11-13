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

namespace pole_Bill
{

    public class Bill
    {
        ///BEFORE ADDING PARAMETERS CHECK s_modbill
        private static string BillPath = dir_root + "hdv/titres_vente.csv";

        private string _ownerID;
        private int _billID;
        private string _itemID;
        private int _quantity;
        private float _price;
        private string _metadata;
        public Bill(string data)
        {
            if (data == "") { data = "b,-1,0,,1,0,empty\r\n"; }
            string[] datum = data.Split(',');
            _ownerID = datum[1];
            _billID = int.Parse(datum[2]);
            _itemID = datum[3];
            _quantity = int.Parse(datum[4]);
            _price = float.Parse(datum[5], floatinfo);
            _metadata = datum[6];
        }
        public Bill(string ownerID, int billID, string itemID, int quantity, float price, string metadata)
        {
            _ownerID = ownerID;
            _billID = billID;
            _itemID = itemID;
            _quantity = quantity;
            _price = price;
            _metadata = metadata;
        }

        public string ownerID
        {
            get { return _ownerID; }
        }
        public int billID
        {
            get { return _billID; }
        }
        public string itemID
        { get { return _itemID; } }
        public int quantity
        {
            get { return _quantity; }
            set { _quantity = value; SaveBill(this); }
        }
        public float price
        {
            get { return _price; }
        }
        public string metadata
        { get { return _metadata; } }

        public static List<Bill> list_bill()
        {
            List<Bill> returner = new List<Bill>();

            List<string> data = rw.readfile(BillPath, false).Split('\n').ToList();

            foreach (string astring in data)
            {
                if (astring.Length == 0) { continue; }
                if (astring[0] != 'b') { continue; }
                returner.Add(new Bill(astring));

            }

            return returner;
        }




        public static void SaveBill(Bill bill)
        {
            if (!isserver) { return; }
            if (bill == null) { return; }
            string[] data = rw.readfile(BillPath, false).Split('\n');
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].Contains(bill.ownerID + ',' + bill.billID))
                {
                    data[i] = bill.ToString();
                    string datatosave = string.Join("\n", data);
                    rw.writefile(datatosave, "hdv/titres_vente.csv", dir_root);
                    return;
                }
            }
        }
        public static void AddBill(Bill bill)
        {
            if (!isserver) { return; }
            if (bill == null) { return; }
            Bill check = Bill.GetBill(bill.ownerID, bill.billID);
            if (check != null) { Debug.LogError("ERROR: Bill already in database"); return; }

            List<string> data = rw.readfile(BillPath, false).Split('\n').ToList();
            data.Add(bill.ToString());
            string datatosave = string.Join("\n", data);
            rw.writefile(datatosave, "hdv/titres_vente.csv", dir_root);
        }
        public static void RemoveBill(Bill bill)
        {
            if (!isserver) { return; }
            if (bill == null) { return; }
            Bill check = Bill.GetBill(bill.ownerID, bill.billID);
            if (check == null) { Debug.LogError("ERROR: Bill not in database"); return; }
            string[] data = rw.readfile(BillPath, false).Split('\n');
            List<string> newdata = new List<string>();
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].Contains(bill.ownerID + ',' + bill.billID))
                {
                }
                else
                {
                    newdata.Add(data[i]);
                }
            }

            string datatosave = string.Join("\n", newdata);
            rw.writefile(datatosave, "hdv/titres_vente.csv", dir_root);


        }

        public override string ToString()
        {
            string returner = "";

            returner += "b,";
            returner += _ownerID + ",";
            returner += _billID.ToString() + ",";
            returner += _itemID + ",";
            returner += _quantity.ToString() + ",";
            returner += _price.ToString() + ",";
            returner += _metadata + ",";

            return returner;
        }




        public static Bill GetBill(string ownerID, int billID)
        {
            foreach (Bill bill in Bill.list_bill())
            {
                if (bill._billID == billID && bill._ownerID == ownerID)
                {
                    return bill;
                }
            }


            Debug.LogError("ERROR: bill not found in data");

            return null;
        }
        public static Bill GetBill(string ownerID, int billID, List<Bill> list)
        {
            foreach (Bill bill in list)
            {
                if (bill._billID == billID && bill._ownerID == ownerID)
                {
                    return bill;
                }
            }


            Debug.LogError("ERROR: bill not found in data");

            return null;
        }
    }

}