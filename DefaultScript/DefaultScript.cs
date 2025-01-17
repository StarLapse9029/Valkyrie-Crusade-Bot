﻿using System;
using System.Collections.Generic;
using System.Linq;
using UI;
using System.Windows.Forms;
using BotFramework;
using System.Drawing;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;

namespace DefaultScript
{
    public class defaultScript : BattleScript
    {
        public static List<ComboBox> toolParameterComboBoxes = new List<ComboBox>();
        private static bool KOChance;
        public void Attack()
        {
            Random rnd = new Random();
            Bitmap bmp = (Bitmap)Image.FromFile("Img\\KO_Chance\\KO.png");
            var points = BotCore.FindImage(VCBotScript.image, new Bitmap[] { bmp }, true);
            if(points != null)
            {
                //Ko Chance
                KOChance = true;
            }
            else
            {
                KOChance = false;
            }
            foreach (var cb in toolParameterComboBoxes)
            {
                int index = 0;
                cb.Invoke((MethodInvoker)delegate { index = cb.SelectedIndex; });
                switch (index)
                {
                    case 0:
                        if (!cboxchecked[index] || PrivateVariable.VCevent != PrivateVariable.EventType.Tower)
                        {
                            byte[] crop = BotCore.CropImage(VCBotScript.image, new Point(176, 356), new Point(330, 611));
                            foreach (var f in PrivateVariable.Skills)
                            {
                                if (!ScriptRun.Run)
                                {
                                    return;
                                }
                                try
                                {
                                    Thread.Sleep(10);
                                    Point? p = BotCore.FindImage(crop, f, false);
                                    if (p != null)
                                    {
                                        Variables.ScriptLog("Skill actived", Color.Blue);
                                        BotCore.SendSwipe(new Point(263, 473), new Point(264, 474), 1200);
                                        for (int x = 0; x < 6; x++)
                                        {
                                            Thread.Sleep(100);
                                            BotCore.SendTap(rnd.Next(5, 15), rnd.Next(5, 15));
                                        }
                                        break;
                                    }
                                }
                                catch
                                {

                                }
                            }
                        }
                        else if(KOChance)
                        {
                            byte[] crop = BotCore.CropImage(VCBotScript.image, new Point(176, 356), new Point(330, 611));
                            foreach (var f in PrivateVariable.Skills)
                            {
                                if (!ScriptRun.Run)
                                {
                                    return;
                                }
                                try
                                {
                                    Thread.Sleep(10);
                                    Point? p = BotCore.FindImage(crop, f, false);
                                    if (p != null)
                                    {
                                        Variables.ScriptLog("Skill actived", Color.Blue);
                                        BotCore.SendSwipe(new Point(263, 473), new Point(264, 474), 1200);
                                        for (int x = 0; x < 6; x++)
                                        {
                                            Thread.Sleep(100);
                                            BotCore.SendTap(rnd.Next(5, 15), rnd.Next(5, 15));
                                        }
                                        break;
                                    }
                                }
                                catch
                                {

                                }
                            }
                        }
                        break;
                    case 1:
                        if (!cboxchecked[index] || PrivateVariable.VCevent != PrivateVariable.EventType.ArchWitch)
                        {
                            byte[] crop = BotCore.CropImage(VCBotScript.image, new Point(357, 356), new Point(543, 610));
                            foreach (var f in PrivateVariable.Skills)
                            {
                                if (!ScriptRun.Run)
                                {
                                    return;
                                }
                                try
                                {
                                    Thread.Sleep(10);
                                    Point? p = BotCore.FindImage(crop, f, false);
                                    if (p != null)
                                    {
                                        Variables.ScriptLog("Skill actived", Color.Blue);
                                        BotCore.SendSwipe(new Point(448, 492), new Point(449, 493), 1200);
                                        for (int x = 0; x < 6; x++)
                                        {
                                            Thread.Sleep(100);
                                            BotCore.SendTap(rnd.Next(5, 15), rnd.Next(5, 15));
                                        }
                                        break;
                                    }
                                }
                                catch
                                {

                                }
                            }
                        }
                        else
                        {
                            byte[] crop = BotCore.CropImage(VCBotScript.image, new Point(500, 0), new Point(800, 50));
                            if (BotCore.FindImage(crop, "Img\\KO_Chance\\KO.png", false) != null)//Ko Chance, active skill
                            {
                                crop = BotCore.CropImage(VCBotScript.image, new Point(357, 356), new Point(543, 610));
                                foreach (var f in PrivateVariable.Skills)
                                {
                                    if (!ScriptRun.Run)
                                    {
                                        return;
                                    }
                                    try
                                    {
                                        Thread.Sleep(10);
                                        Point? p = BotCore.FindImage(crop, f, false);
                                        if (p != null)
                                        {
                                            Variables.ScriptLog("Skill actived", Color.Blue);
                                            BotCore.SendSwipe(new Point(263, 473), new Point(264, 474), 1200);
                                            for (int x = 0; x < 6; x++)
                                            {
                                                Thread.Sleep(100);
                                                BotCore.SendTap(rnd.Next(5, 15), rnd.Next(5, 15));
                                            }
                                            break;
                                        }
                                    }
                                    catch
                                    {

                                    }
                                }
                            }
                        }
                        break;
                    case 2:
                        if (!cboxchecked[index] || PrivateVariable.VCevent != PrivateVariable.EventType.ArchWitch)
                        {
                            byte[] crop = BotCore.CropImage(VCBotScript.image, new Point(546, 376), new Point(724, 597));
                            foreach (var f in PrivateVariable.Skills)
                            {
                                if (!ScriptRun.Run)
                                {
                                    return;
                                }
                                try
                                {
                                    Thread.Sleep(10);
                                    Point? p = BotCore.FindImage(crop, f, false);
                                    if (p != null)
                                    {
                                        Variables.ScriptLog("Skill actived", Color.Blue);
                                        BotCore.SendSwipe(new Point(641, 473), new Point(642, 474), 1200);
                                        for (int x = 0; x < 6; x++)
                                        {
                                            Thread.Sleep(100);
                                            BotCore.SendTap(rnd.Next(5, 15), rnd.Next(5, 15));
                                        }
                                        break;
                                    }
                                }
                                catch
                                {

                                }
                            }
                        }
                        else
                        {
                            byte[] crop = BotCore.CropImage(VCBotScript.image, new Point(500, 0), new Point(800, 50));
                            if (BotCore.FindImage(crop, "Img\\KO_Chance\\KO.png", false) != null)//Ko Chance, active skill
                            {
                                crop = BotCore.CropImage(VCBotScript.image, new Point(546, 376), new Point(724, 597));
                                foreach (var f in PrivateVariable.Skills)
                                {
                                    if (!ScriptRun.Run)
                                    {
                                        return;
                                    }
                                    try
                                    {
                                        Thread.Sleep(10);
                                        Point? p = BotCore.FindImage(crop, f, false);
                                        if (p != null)
                                        {
                                            Variables.ScriptLog("Skill actived", Color.Blue);
                                            BotCore.SendSwipe(new Point(263, 473), new Point(264, 474), 1200);
                                            for (int x = 0; x < 6; x++)
                                            {
                                                Thread.Sleep(100);
                                                BotCore.SendTap(rnd.Next(5, 15), rnd.Next(5, 15));
                                            }
                                            break;
                                        }
                                    }
                                    catch
                                    {

                                    }
                                }
                            }
                        }
                        break;
                    case 3:
                        if (!cboxchecked[index] || PrivateVariable.VCevent != PrivateVariable.EventType.ArchWitch)
                        {
                            byte[] crop = BotCore.CropImage(VCBotScript.image, new Point(761, 356), new Point(921, 613));
                            foreach (var f in PrivateVariable.Skills)
                            {
                                if (!ScriptRun.Run)
                                {
                                    return;
                                }
                                try
                                {
                                    Thread.Sleep(10);
                                    Point? p = BotCore.FindImage(crop, f, false);
                                    if (p != null)
                                    {
                                        Variables.ScriptLog("Skill actived", Color.Blue);
                                        BotCore.SendSwipe(new Point(834, 483), new Point(835, 484), 1200);
                                        for (int x = 0; x < 6; x++)
                                        {
                                            Thread.Sleep(100);
                                            BotCore.SendTap(rnd.Next(5, 15), rnd.Next(5, 15));
                                        }
                                        break;
                                    }
                                }
                                catch
                                {

                                }
                            }
                        }
                        else
                        {
                            byte[] crop = BotCore.CropImage(VCBotScript.image, new Point(500, 0), new Point(800, 50));
                            if (BotCore.FindImage(crop, "Img\\KO_Chance\\KO.png", false) != null)//Ko Chance, active skill
                            {
                                crop = BotCore.CropImage(VCBotScript.image, new Point(761, 356), new Point(921, 613));
                                foreach (var f in PrivateVariable.Skills)
                                {
                                    if (!ScriptRun.Run)
                                    {
                                        return;
                                    }
                                    try
                                    {
                                        Thread.Sleep(10);
                                        Point? p = BotCore.FindImage(crop, f, false);
                                        if (p != null)
                                        {
                                            Variables.ScriptLog("Skill actived", Color.Blue);
                                            BotCore.SendSwipe(new Point(263, 473), new Point(264, 474), 1200);
                                            for (int x = 0; x < 6; x++)
                                            {
                                                Thread.Sleep(100);
                                                BotCore.SendTap(rnd.Next(5, 15), rnd.Next(5, 15));
                                            }
                                            break;
                                        }
                                    }
                                    catch
                                    {

                                    }
                                }
                            }
                        }
                        break;
                    case 4:
                        if (!cboxchecked[index] || PrivateVariable.VCevent != PrivateVariable.EventType.ArchWitch)
                        {
                            byte[] crop = BotCore.CropImage(VCBotScript.image, new Point(934, 356), new Point(1090, 578));
                            foreach (var f in PrivateVariable.Skills)
                            {
                                if (!ScriptRun.Run)
                                {
                                    return;
                                }
                                try
                                {
                                    Thread.Sleep(10);
                                    Point? p = BotCore.FindImage(crop, f, false);
                                    if (p != null)
                                    {
                                        Variables.ScriptLog("Skill actived", Color.Blue);
                                        BotCore.SendSwipe(new Point(1017, 470), new Point(1018, 471), 1200);
                                        for (int x = 0; x < 6; x++)
                                        {
                                            Thread.Sleep(100);
                                            BotCore.SendTap(rnd.Next(5, 15), rnd.Next(5, 15));
                                        }
                                        break;
                                    }
                                }
                                catch
                                {

                                }
                            }
                        }
                        else
                        {
                            byte[] crop = BotCore.CropImage(VCBotScript.image, new Point(500, 0), new Point(800, 50));
                            if (BotCore.FindImage(crop, "Img\\KO_Chance\\KO.png", false) != null)//Ko Chance, active skill
                            {
                                crop = BotCore.CropImage(VCBotScript.image, new Point(934, 356), new Point(1090, 578));
                                foreach (var f in PrivateVariable.Skills)
                                {
                                    if (!ScriptRun.Run)
                                    {
                                        return;
                                    }
                                    try
                                    {
                                        Thread.Sleep(10);
                                        Point? p = BotCore.FindImage(crop, f, false);
                                        if (p != null)
                                        {
                                            Variables.ScriptLog("Skill actived", Color.Blue);
                                            BotCore.SendSwipe(new Point(263, 473), new Point(264, 474), 1200);
                                            for (int x = 0; x < 6; x++)
                                            {
                                                Thread.Sleep(100);
                                                BotCore.SendTap(rnd.Next(5, 15), rnd.Next(5, 15));
                                            }
                                            break;
                                        }
                                    }
                                    catch
                                    {

                                    }
                                }
                            }
                        }
                        break;
                }
            }
        }
        public static string cboxselected ="";
        public static bool[] cboxchecked = new bool[5];
        public Control[] CreateUI()
        {
            List<Control> thingstoreturn = new List<Control>();
            UI ui = new UI();
            var text = cboxselected.ToCharArray();
            toolParameterComboBoxes = ui.Controls.OfType<ComboBox>().OrderBy(combo => combo.Name).ToList<ComboBox>();
            foreach (Control control in ui.Controls)
            {
                if (control is ComboBox)
                {
                    var index = Convert.ToInt32(control.Name.Replace("comboBox", "")) - 1;
                    if(text.Count() == 5)
                    {
                        toolParameterComboBoxes[toolParameterComboBoxes.IndexOf(control as ComboBox)].SelectedIndex = Convert.ToInt32(text[index].ToString());
                    }
                }
                if(control is CheckBox)
                {
                    var index = Convert.ToInt32(control.Name.Replace("checkBox", "")) - 1;
                    (control as CheckBox).Checked = cboxchecked[index];
                }
                thingstoreturn.Add(control);
            }
            ui.LoadCompleted = true;
            Variables.ScriptLog("Default script loaded",Color.Cyan);
            return thingstoreturn.ToArray();
        }

        public void ReadConfig()
        {
            Variables.FindConfig("DefaultScript", "Skill_Acd", out cboxselected);
            if(cboxselected == null)
            {
                cboxselected = "";
                Variables.ModifyConfig("DefaultScript", "Skill_Acd", "01234");
            }
            Variables.FindConfig("DefaultScript", "KO_Chance", out string check);
            if(check != null)
            {
                int x = 0;
                foreach(var c in check.Split('|'))
                {
                    cboxchecked[x] = bool.Parse(c);
                    x++;
                }
            }
            else
            {
                cboxchecked = new bool[] { false, false, false, false, false };
            }
        }
        public string ScriptName()
        {
            return "Default Script";
        }
    }
}
