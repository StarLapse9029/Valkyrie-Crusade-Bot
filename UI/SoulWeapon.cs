﻿using BotFramework;
using ImgXml;
using System;
using System.Drawing;
using System.IO;

namespace UI
{
    class SoulWeapon
    {
        static bool UnhandledException;

        static int error = 0;
        public static void SoulWeaponEnter()
        {
            if (UnhandledException)
            {
                Variables.ScriptLog("Unhandled exception had found in SoulWeapon event UI. Skip it! ",Color.Red);
                return;
            }
            do
            {
                VCBotScript.LocateMainScreen();
            }
            while (!PrivateVariable.InMainScreen);
            BotCore.SendTap(170, 630);
            BotCore.Delay(5000, false);
            for (int x = 0; x < 5; x++)
            {
                VCBotScript.image = BotCore.ImageCapture();
                Point? located = BotCore.FindImage(VCBotScript.image, Environment.CurrentDirectory + "\\Img\\LocateEventSwitch.png", true);
                if (located == null)
                {
                    x -= 1;
                    BotCore.Delay(1000, false);
                    if (error > 10)
                    {
                        ScriptErrorHandler.Reset("Unable to locate Event Switch screen! Returning main screen!");
                        error = 0;
                        return;
                    }
                    error++;
                    ScriptErrorHandler.ErrorHandle();
                    continue;
                }
                else
                {
                    break;
                }
            }
            if (File.Exists("Img\\WeaponEvent.png"))
            {
                var point = BotCore.FindImage(VCBotScript.image, "Img\\WeaponEvent.png", false);
                if (point != null)
                {
                    BotCore.SendTap(point.Value);
                    //Enter event
                    SwitchStage();
                }
                else
                {
                    Variables.ScriptLog("Unable to find WeaponEvent.png at event page. Exiting function! ", Color.Red);
                }
            }
            else
            {
                Variables.ScriptLog("WeaponEvent.png not found! Exiting function! ", Color.Red);
            }
        }
        public static void SwitchStage()
        {
            //Check do we still can get into the stage
            do
            {
                BotCore.Delay(1000, 1200);
                VCBotScript.image = BotCore.ImageCapture();
                if (BotCore.FindImage(BotCore.CropImage(VCBotScript.image, new Point(634, 374), new Point(694, 421)), Img.Archwitch_Rec, false) != null)
                {
                    Attack();
                    return;
                }
            }
            while (BotCore.FindImage(VCBotScript.image, Img.SoulWeapon, true) == null);
            int error = 0;
            //Check if we didnt need to switch stage
            do
            {
                var crop = BotCore.CropImage(VCBotScript.image, new Point(73, 83), new Point(112, 102));
                var tempstring = OCR.OcrImage(crop, "eng");
                var leftTimes = tempstring.Split('/')[0];
                if (leftTimes == "0")
                {
                    Variables.ScriptLog("Today had already attacked 5 times. Exiting...", Color.Lime);
                    return;
                }
                else
                {
                    //Back to 1-1 first before we continue
                    BotCore.Delay(1000);
                    //BotCore.Minitouch("d 0 290 340 150\nc\nm 0 340 340 150\nc\nm 0 390 340 150\nc\nm 0 440 340 150\nc\nm 0 490 340 150\nc\nm 0 540 340 150\nc\nm 0 590 340 150\nc\nm 0 640 340 150\nc\nm 0 740 340 150\nc\nm 0 840 340 150\nc\nm 0 940 340 150\nc\nu 0\nc\n");
                    BotCore.SendSwipe(290, 340, 990, 360, 3000);//Why it can't swipe????
                    BotCore.Delay(1000);

                    switch (VCBotScript.Weapon_Stage)
                    {
                        case 1.1:
                            BotCore.SendTap(449, 532);
                            break;
                        case 1.2:
                            BotCore.SendTap(577, 422);
                            break;
                        case 1.3:
                            BotCore.SendTap(692, 277);
                            break;
                        case 2.1:
                            BotCore.SendTap(820, 423);
                            break;
                        case 2.2:
                            BotCore.SendTap(944, 306);
                            break;
                        case 2.3:
                            BotCore.SendTap(1053, 210);
                            break;
                        case 3.1:
                            BotCore.SendTap(1191, 310);
                            break;
                        //next page
                        case 3.2:
                            BotCore.SendSwipe(1191, 310, 670, 310, 3000);
                            BotCore.Delay(1000);
                            BotCore.SendTap(315, 427);
                            break;
                    }
                    BotCore.Delay(1000, 1200);
                    VCBotScript.image = BotCore.ImageCapture();
                    if (BotCore.FindImage(VCBotScript.image, Img.GreenButton, false) != null)
                    {
                        BotCore.SendTap(776, 524);
                        BotCore.Delay(1000, 2000);
                        Attack();
                    }
                    else
                    {
                        Variables.ScriptLog("Unexpected error found! Unable to get into stage! Exiting for now!", Color.Red);
                        return;
                    }
                }
                error++;
            }
            while (error != 10);
            if(error == 10)
            {
                Variables.ScriptLog("Something error happens. Unable to get detect expcted UI",Color.Red);
            }
        }

        private static void Attack()
        {
            do
            {
                BotCore.Delay(1500);
                VCBotScript.image = BotCore.ImageCapture();
            }
            while (BotCore.RGBComparer(VCBotScript.image, new Point(400, 400), Color.Black, 10));
            Variables.ScriptLog("Running stage!", Color.Lime);
            int error = 0;
            do
            {
                Random rnd = new Random();
                VCBotScript.image = BotCore.ImageCapture();
                var crop = BotCore.CropImage(VCBotScript.image, new Point(420, 360), new Point(855, 430));
                Point? buttons = BotCore.FindImage(crop, Img.GreenButton, false);
                if (buttons != null)
                {
                    ArchwitchEvent.CheckWalkEnergy();
                    if (ArchwitchEvent.CurrentWalkEnergy < 15)
                    {
                        //No energy
                        Variables.ScriptLog("SoulWeapon Event have no energy. Exiting now! ", Color.Yellow);
                        return;
                    }
                    BotCore.SendTap(buttons.Value.X + rnd.Next(430, 845), buttons.Value.Y + rnd.Next(370, 420));
                    BotCore.Delay(2000, 3000);
                    continue;
                }
                buttons = BotCore.FindImage(VCBotScript.image, Img.Return, true);
                if(buttons != null)
                {
                    BotCore.SendTap(buttons.Value);
                    BotCore.Delay(1000, 1500);
                    continue;
                }
                buttons = BotCore.FindImage(VCBotScript.image, Img.Close2, true);
                if (buttons != null)
                {
                    BotCore.SendTap(buttons.Value);
                    BotCore.Delay(1000, 1500);
                    continue;
                }
                buttons = BotCore.FindImage(crop, Img.Red_Button, false);
                if (buttons != null)
                {
                    ArchwitchEvent.CheckBossEnergy();
                    if (ArchwitchEvent.CurrentBossEnergy == 0)
                    {
                        Variables.ScriptLog("SoulWeapon Event have no energy. Exiting now! ", Color.Yellow);
                        return;
                    }
                    BotCore.SendTap(buttons.Value.X + rnd.Next(430, 845), buttons.Value.Y + rnd.Next(370, 420));
                    BotCore.Delay(2000, 3000);
                    PrivateVariable.Battling = true;
                    VCBotScript.Battle();
                    continue;
                }
                buttons = BotCore.FindImage(VCBotScript.image, Img.ShopKeeper, true);
                if(buttons != null)
                {
                    BotCore.SendTap(530, 660);
                    BotCore.Delay(1500);
                    continue;
                }
                if(BotCore.FindImage(VCBotScript.image, Img.SoulWeapon, true) != null)
                {
                    //Stage completed
                    return;
                }
                error++;
                if(error > 30)
                {
                    BotCore.Decompress(VCBotScript.image).Save("Profiles\\Logs\\error.png");
                    UnhandledException = true;
                    Variables.ScriptLog("Unhandled exception. Contact PoH98 for fix!", Color.Red);
                    return;
                }
                else
                {
                    BotCore.Delay(1000,1500);
                }
            }
            while (true);
        }
    }
}
