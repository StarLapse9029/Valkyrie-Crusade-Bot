﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using BotFramework;
using Microsoft.Win32;

namespace MEmu
{
    public class MEmu : EmulatorInterface
    {
        public void CloseEmulator()
        {
            ProcessStartInfo close = new ProcessStartInfo();
            close.FileName = Variables.VBoxManagerPath;

            if (Variables.Instance.Length > 0)
            {
                close.Arguments = "controlvm " + Variables.Instance + " poweroff";
            }
            else
            {
                close.Arguments = "controlvm MEmu poweroff";
            }
            close.CreateNoWindow = true;
            close.WindowStyle = ProcessWindowStyle.Hidden;
            try
            {
                if (Variables.Proc != null)
                {
                    Variables.Proc.Kill();
                }
            }
            catch
            {

            }
            Process p = Process.Start(close);
        }

        public void ConnectEmulator()
        {
            string[] MEmu = { "MEmu", "逍遥模拟器" };
            foreach (var p in Process.GetProcessesByName("MEmu"))
            {
                Debug_.WriteLine(p.MainWindowTitle);
                if (MEmu.Contains(p.MainWindowTitle))
                {
                    IntPtr handle = DllImport.FindWindowEx(p.MainWindowHandle, IntPtr.Zero, string.Empty, string.Empty);
                    Variables.Proc = p;
                    Variables.ScriptLog("Emulator ID: " + p.Id, Color.DarkGreen);
                    break;
                }
            }
        }

        public string EmulatorName()
        {
            return "MEmu";
        }

        public bool LoadEmulatorSettings()
        {
            RegistryKey reg = Registry.LocalMachine;
            try
            {
                RegistryKey r;
                if (BotCore.Is64BitOperatingSystem())
                {
                    r = reg.OpenSubKey("SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\MEmu");
                }
                else
                {
                    r = reg.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\MEmu");
                }
                var location = r.GetValue("InstallLocation");
                if (location != null)
                {
                    var path = location.ToString().Replace("\0", "");
                    if (Directory.Exists(path))
                    {
                        Variables.VBoxManagerPath = path + @"\MEmuHyperv\MEmuManage.exe";
                        ProcessStartInfo fetch = new ProcessStartInfo(Variables.VBoxManagerPath);
                        if (Variables.Instance.Length > 0)
                        {
                            fetch.Arguments = "showvminfo " + Variables.Instance;
                        }
                        else
                        {
                            fetch.Arguments = "showvminfo MEmu";
                        }
                        fetch.CreateNoWindow = true;
                        fetch.RedirectStandardOutput = true;
                        fetch.UseShellExecute = false;
                        do
                        {
                            Process fetching = Process.Start(fetch);
                            string result = fetching.StandardOutput.ReadToEnd();
                            string[] splitted = result.Split('\n');
                            foreach (var s in splitted)
                            {
                                if (s.Contains("Name: 'download', Host path:"))
                                {
                                    string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
                                    Regex re = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
                                    path = s.ToLower().Replace("name: 'download', host path: '", "").Replace("' (machine mapping), writable", "");
                                    path = re.Replace(path, "\\");
                                    path = path.Replace("\\\\", ":\\");
                                    Variables.SharedPath = path;
                                    if (!Directory.Exists(Variables.SharedPath))
                                    {
                                        MessageBox.Show("共享路径包含不允许字符，请重新设置模拟器的共享路径！");
                                        Environment.Exit(0);
                                    }
                                }
                                else if (s.Contains("name = ADB"))
                                {
                                    var port = s.Substring(s.IndexOf("port = ") + 7, 5).Replace(" ", "");
                                    Variables.AdbIpPort = "127.0.0.1:" + port;
                                }
                            }
                        }
                        while (Variables.SharedPath == null || Variables.AdbIpPort == null);
                        Variables.AndroidSharedPath = "/sdcard/Download/";
                        return true;
                    }
                }
            }
            catch
            {

            }
            return false;
        }

        public void SetResolution(int x, int y, int dpi)
        {
            ProcessStartInfo s = new ProcessStartInfo(Variables.VBoxManagerPath);
            s.Arguments = "guestproperty set MEmu resolution_height " + y;
            Process.Start(s);
            s.Arguments = "guestproperty set MEmu resolution_width " + x;
            Process.Start(s);
            s.Arguments = "guestproperty set MEmu vbox_dpi " + dpi;
            Process.Start(s);
        }

        public void StartEmulator()
        {
            try
            {
                if (!File.Exists(Variables.VBoxManagerPath))
                {
                    MessageBox.Show("Unable to locate path of emulator!");
                    Process.Start("Profiles\\" + BotCore.profilePath + "\\bot.ini");
                }
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = Variables.VBoxManagerPath.Replace(@"\MEmuHyperv\MEmuManage.exe", @"\MEmu\MEmuConsole.exe");
                if (Variables.Instance.Length > 0)
                {
                    info.Arguments = Variables.Instance;
                }
                else
                {
                    info.Arguments = "MEmu";
                }
                info.RedirectStandardOutput = true;
                info.UseShellExecute = false;
                info.CreateNoWindow = true;
                Process.Start(info);
            }
            catch (SocketException)
            {

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while starting emulator! Error message: " + ex.Message);
                Process.Start("Profiles\\" + BotCore.profilePath + "\\bot.ini");
                Environment.Exit(0);
            }
        }
    }
}
