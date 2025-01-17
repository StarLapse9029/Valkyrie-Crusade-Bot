﻿using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using SharpAdbClient;
using SharpAdbClient.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Management;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Windows.Forms;
using System.Reflection;
using System.Net.Sockets;
using System.Security.Principal;
using System.IO.Compression;

namespace BotFramework
{
    /// <summary>
    /// All the android emulator controllers are here!
    /// </summary>
    public class BotCore
    {
        /// <summary>
        /// The path to bot.ini
        /// </summary>
        public static string profilePath = Variables.Instance;
        /// <summary>
        /// Adb Server
        /// </summary>
        public static readonly AdbServer server = new AdbServer();
        static string pcimagepath = "", androidimagepath = "";
        static readonly AdbClient client = new AdbClient();
        static IAdbSocket socket;
        static bool StartingEmulator; //To confirm only start the emulator once in the same time!
        /// <summary>
        /// Minitouch port number
        /// </summary>
        public static int minitouchPort = 1111;
        /// <summary>
        /// minitouch Tcp socket. Default null, use connectEmulator to gain a socket connection!
        /// </summary>
        public static TcpSocket minitouchSocket;
        static readonly ConsoleOutputReceiver receiver = new ConsoleOutputReceiver();
        static bool JustStarted = true;
        /// <summary>
        /// Read emulators dll
        /// </summary>
        public static void LoadEmulatorInterface([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            {
                MessageBox.Show("Admin needed to run this framework!");
                Environment.Exit(0);
            }
            List<EmulatorInterface> emulators = new List<EmulatorInterface>();
            if (!Directory.Exists("Emulators"))
            {
                Directory.CreateDirectory("Emulators");
            }
            var dlls = Directory.GetFiles("Emulators", "*.dll");
            if (dlls != null)
            {
                foreach (var dll in dlls)
                {
                    Assembly a = Assembly.LoadFrom(dll);
                    foreach (var t in a.GetTypes())
                    {
                        if (t.GetInterface("EmulatorInterface") != null)
                        {
                            emulators.Add(Activator.CreateInstance(t) as EmulatorInterface);
                        }
                    }
                }
            }
            bool[] installed = new bool[emulators.Count];
            for (int x = 0; x < emulators.Count; x++)
            {
                installed[x] = emulators[x].LoadEmulatorSettings();
                if (installed[x])
                {
                    Variables.AdvanceLog("Detected emulator " + emulators[x].EmulatorName(), lineNumber, caller);
                    EmuSelection_Resource.emu.Add(emulators[x]);
                }
            }
            Variables.AdbIpPort = "";
            Variables.AndroidSharedPath = "";
            Variables.SharedPath = "";
            Variables.VBoxManagerPath = "";
            Emulator:
            if (EmuSelection_Resource.emu.Count() > 1) //More than one installed
            {
                if (!File.Exists("Emulators\\Use_Emulator.ini"))
                {
                    Emulator_Selection em = new Emulator_Selection();
                    em.ShowDialog();
                    if (em.DialogResult == DialogResult.OK)
                    {
                        foreach (var e in emulators)
                        {
                            if (e.EmulatorName() == EmuSelection_Resource.selected)
                            {
                                Variables.emulator = e;
                                e.LoadEmulatorSettings();
                                File.WriteAllText("Emulators\\Use_Emulator.ini", "use=" + e.EmulatorName());
                                return;
                            }
                        }
                    }
                    else //No selection
                    {
                        for (int x = 0; x < emulators.Count(); x++)
                        {
                            if (installed[x])
                            {
                                Variables.emulator = emulators[x];
                                emulators[x].LoadEmulatorSettings();
                                File.WriteAllText("Emulators\\Use_Emulator.ini","use="+emulators[x].EmulatorName());
                                return;
                            }
                        }
                    }
                }
                else
                {
                    var line = File.ReadAllLines("Emulators\\Use_Emulator.ini")[0].Replace("use=","");
                    foreach(var e in emulators)
                    {
                        if(e.EmulatorName() == line)
                        {
                            Variables.emulator = e;
                            e.LoadEmulatorSettings();
                            return;
                        }
                    }
                    if(Variables.emulator == null)
                    {
                        File.Delete("Emulators\\Use_Emulator.ini");
                        goto Emulator;
                    }
                }
            }
            else if (EmuSelection_Resource.emu.Count() < 1) //No installed
            {
                MessageBox.Show("Please install any supported emulator first or install extentions to support your current installed emulator!","No supported emulator found!");
                Environment.Exit(0);
            }
            else
            {
                Variables.emulator = EmuSelection_Resource.emu[0];
                Variables.emulator.LoadEmulatorSettings();
            }

        }
        /// <summary>
        /// Compress image into byte array to avoid conflict while multiple function trying to access the image
        /// </summary>
        /// <param name="image">The image for compress</param>
        /// <returns></returns>
        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public static byte[] Compress(Image image)
        {
            try
            {
                ImageConverter _imageConverter = new ImageConverter();
                byte[] xByte = (byte[])_imageConverter.ConvertTo(image, typeof(byte[]));
                return xByte;
            }
            catch
            {
                return null;
            }

        }
        /// <summary>
        /// Decompress the byte array back to image for other usage
        /// </summary>
        /// <param name="buffer">the byte array of image compressed by Compress(Image image)</param>
        /// <returns>Image</returns>
        public static Bitmap Decompress(byte[] buffer)
        {
            try
            {
                using (var ms = new MemoryStream(buffer))
                {
                    return Image.FromStream(ms) as Bitmap;
                }
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// Install an apk to selected device from path
        /// </summary>
        public static void InstallAPK(string path)
        {
            if (!ScriptRun.Run)
            {
                return;
            }
            try
            {
                
                client.ExecuteRemoteCommand("adb install " + path, (Variables.Controlled_Device as DeviceData), receiver);
            }
            catch (InvalidOperationException)
            {

            }
            catch (ArgumentOutOfRangeException)
            {

            }
        }
        /// <summary>
        /// Close the emulator by using vBox command lines
        /// </summary>
        public static void CloseEmulator()
        {
            if (!ScriptRun.Run)
            {
                return;
            }
            EjectSockets();
            Variables.emulator.CloseEmulator();
            Variables.Proc = null;
            Variables.Controlled_Device = null;
            Variables.ScriptLog("Emulator Closed",Color.Red);
        }
        /// <summary>
        /// Restart emulator
        /// </summary>
        public static void RestartEmulator()
        {
            if (!ScriptRun.Run)
            {
                return;
            }
            CloseEmulator();
            Variables.ScriptLog("Restarting Emulator...",Color.Red);
            Thread.Sleep(1000);
            StartEmulator();
            JustStarted = true;
        }
        /// <summary>
        /// Method for resizing emulators using Variables.EmulatorWidth, Variables.EmulatorHeight, Variables.EmulatorDpi
        /// </summary>
        public static void ResizeEmulator()
        {
            if (!ScriptRun.Run)
            {
                return;
            }
            CloseEmulator();
            Delay(3000);
            Variables.emulator.SetResolution(Variables.EmulatorWidth, Variables.EmulatorHeight, Variables.EmulatorDpi);
            Variables.ScriptLog("Restarting Emulator after setting size", Color.Lime);
            StartEmulator();
        }
        /// <summary>
        /// Refresh (Variables.Controlled_Device as DeviceData), Variables.Proc and BotCore.Handle, following with connection with Minitouch. 
        /// Remember to run EjectSockets before exit program to avoid errors!
        /// </summary>
        public static void ConnectAndroidEmulator([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            int error = 0;
            Connect:
            StartAdb();
            if (!ScriptRun.Run)
            {
                return;
            }
            Variables.AdvanceLog("Connecting emulator...", lineNumber, caller);
            if(Variables.Proc == null)
            {
                if (!ScriptRun.Run)
                {
                    return;
                }
                Variables.emulator.ConnectEmulator();
                Variables.AdvanceLog("Emulator not connected, retrying in 2 second...", lineNumber, caller);
                Thread.Sleep(1000);
                error++;
                if(error > 15) //We had await for 15 seconds
                {
                    Variables.ScriptLog("Unable to connect to emulator! Emulator refused to load! Restarting it now!",Color.Red);
                    RestartEmulator();
                    Thread.Sleep(10000);
                    error = 0;
                }
                if(error % 5 == 0)
                {
                    Variables.ScriptLog("Emulator is still not running...",Color.Red);
                }
                goto Connect;
            }
            else
            {
                if (Variables.Proc.HasExited)
                {
                    StartEmulator();
                }
            }
            try
            {
                socket = new AdbSocket(new IPEndPoint(IPAddress.Parse("127.0.0.1"), Adb.CurrentPort));
                client.Connect(new DnsEndPoint("127.0.0.1", Convert.ToInt32(Variables.AdbIpPort.Split(':').Last())));
                Variables.AdvanceLog("Connecting Adb EndPoint " + Variables.AdbIpPort, lineNumber, caller);
                do
                {
                    foreach (var device in client.GetDevices())
                    {
                        Variables.AdvanceLog("Detected "+device.ToString(), lineNumber, caller);
                        if (device.ToString() == Variables.AdbIpPort)
                        {
                            Variables.Controlled_Device = device;
                            Variables.DeviceChanged = true;
                            if (error % 5 == 0)
                            {
                                Variables.AdvanceLog("Device found, connection establish on " + Variables.AdbIpPort, lineNumber, caller);
                            }
                            client.SetDevice(socket, device);
                            //await emulator start
                            receiver.Flush();
                            do
                            {
                                client.ExecuteRemoteCommand("getprop sys.boot_completed", (Variables.Controlled_Device as DeviceData),receiver);
                                if (receiver.ToString().Contains("1"))
                                {
                                    break;
                                }
                                else
                                {
                                    Delay(100);
                                }
                            }
                            while (true);
                            error = 0;
                            break;
                        }
                    }
                    Thread.Sleep(2000);
                    error++;
                } while (!Variables.DeviceChanged  && error < 30);
                if (error > 20 && !Variables.DeviceChanged) //We had await for 1 minute
                {
                    Variables.ScriptLog("Unable to connect to emulator! Emulator refused to load! Restarting it now!", Color.Red);
                    RestartEmulator();
                    Thread.Sleep(10000);
                    error = 0;
                }
                //Ok, so now we have no device change
                Variables.DeviceChanged = false;
            }
            catch(Exception ex)
            {
                Variables.AdvanceLog(ex.ToString(), lineNumber, caller);
                Thread.Sleep(2000);
                goto Connect;
            }
            if (Variables.Controlled_Device == null)
            {
                //Unable to connect device
                Variables.AdvanceLog("Unable to connect to device " + Variables.AdbIpPort, lineNumber, caller);
                Variables.ScriptLog("Emulator refused to connect or start, restarting...", Color.Red);
                RestartEmulator();
                return;
            }
            client.ExecuteRemoteCommand("settings put system font_scale 1.0", (Variables.Controlled_Device as DeviceData), receiver);
            ConnectMinitouch();
        }
        /// <summary>
        /// Warning!!Must run this before exit program, else all sockets records will continue in the PC even when restarted!!
        /// </summary>
        public static void EjectSockets()
        {
            try
            {
                if(minitouchSocket != null)
                {
                    minitouchSocket.Dispose();
                    minitouchSocket = new TcpSocket();
                }
            }
            catch
            {

            }
            receiver.Flush();
            var path = Path.GetTempPath() + "minitouch";
            if (File.Exists(path))
            {
                //Remove current socket port from record as we had dispose it!
                var ports = File.ReadAllLines(path);
                int x = 0;
                string [] newports = new string[ports.Length];
                foreach(var port in ports)
                {
                    if(port.Length > 0)
                    {
                        if (Convert.ToInt32(port) != minitouchPort)
                        {
                            newports[x] = port;
                            x++;
                        }
                    }
                }
                File.WriteAllLines(path, newports.Where(y => !string.IsNullOrEmpty(y)).ToArray());
            }
            var files = Directory.GetFiles(Variables.SharedPath, "*.raw");
            foreach(var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {

                }
            }
        }
        /// <summary>
        /// Connect minitouch
        /// </summary>
        private static void ConnectMinitouch([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            try
            {
                if (minitouchSocket != null)
                {
                    minitouchSocket = null;
                }
            }
            catch
            {

            }
            Thread.Sleep(100);
            try
            {
                var path = Path.GetTempPath() + "minitouch";
                if (File.Exists(path))
                {
                    var ports = File.ReadAllLines(path);
                    foreach (var tmp in ports)
                    {
                        foreach (var port in ports)
                        {
                            if (minitouchPort == Convert.ToInt32(port))
                            {
                                minitouchPort++;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
                using (var stream = File.AppendText(path))
                {
                    stream.WriteLine(minitouchPort); //Let other instance know that this socket is in use!
                }
            }
            catch
            {

            }
            if (!ScriptRun.Run)
            {
                return;
            }
            Variables.AdvanceLog("Connecting Minitouch", lineNumber, caller);
            Push(Environment.CurrentDirectory + "//adb//minitouch", "/data/local/tmp/minitouch", 777);
            try
            {
                int error = 0;
                while(Variables.Controlled_Device == null && ScriptRun.Run)
                {
                    error++;
                    Thread.Sleep(1000);
                    foreach (var device in client.GetDevices())
                    {
                        Variables.AdvanceLog(device.ToString(), lineNumber, caller);
                        if (device.ToString() == Variables.AdbIpPort)
                        {
                            Variables.Controlled_Device = device;
                            Variables.DeviceChanged = true;
                            Variables.AdvanceLog("Device found, connection establish on " + Variables.AdbIpPort, lineNumber, caller);
                            break;
                        }
                    }
                    if (error > 30)
                    {
                        RestartEmulator();
                        error = 0;
                    }
                }
                if (!ScriptRun.Run)
                {
                    return;
                }
                Cm:
                
                client.ExecuteRemoteCommandAsync("/data/local/tmp/minitouch", (Variables.Controlled_Device as DeviceData), receiver, CancellationToken.None, int.MaxValue);
                client.CreateForward((Variables.Controlled_Device as DeviceData), ForwardSpec.Parse($"tcp:{minitouchPort}"), ForwardSpec.Parse("localabstract:minitouch"), true);
                minitouchSocket = new TcpSocket();
                minitouchSocket.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"),minitouchPort));
                if (minitouchSocket.Connected)
                {
                    try
                    {
                        string cmd = "d 0 0 0 100\nc\nu 0\nc\n";
                        byte[] bytes = AdbClient.Encoding.GetBytes(cmd);
                        minitouchSocket.Send(bytes, 0, bytes.Length, SocketFlags.None);
                    }
                    catch
                    {
                        Variables.AdvanceLog("Socket disconnected, retrying...", lineNumber, caller);
                        goto Cm;
                    }
                    Variables.AdvanceLog("Minitouch connected on Port number " + minitouchPort, lineNumber, caller);
                    JustStarted = false;
                }
                else
                {
                    Variables.AdvanceLog("Socket disconnected, retrying...", lineNumber, caller);
                    EjectSockets();
                    minitouchPort++;
                    goto Cm;
                }
            }
            catch(Exception ex)
            {
                if(ex is AdbException)
                {
                    server.RestartServer();
                    RestartEmulator();
                    minitouchPort = 1111;
                    return;
                }
                Variables.AdvanceLog(ex.ToString(), lineNumber, caller);
                EjectSockets();
                minitouchPort++;
                ConnectMinitouch();
            }

        }
        /// <summary>
        /// Check Game is foreground and return a bool
        /// </summary>
        /// <param name="packagename">Applications Name in Android, such as com.supercell.clashofclans</param>
        public static bool GameIsForeground(string packagename)
        {
            if (!ScriptRun.Run)
            {
                return false;
            }
            try
            {
                
                if(Variables.Controlled_Device == null)
                {
                    return false;
                }
                var receiver = new ConsoleOutputReceiver();
                client.ExecuteRemoteCommand("dumpsys window windows | grep -E 'mCurrentFocus'", (Variables.Controlled_Device as DeviceData), receiver);
                if (receiver.ToString().Contains(packagename))
                {
                    return true;
                }
            }
            catch (InvalidOperationException)
            {

            }
            catch (ArgumentOutOfRangeException)
            {

            }
            return false;
        }
        /// <summary>
        /// Send Adb command to android
        /// </summary>
        public static string AdbCommand(string command,[CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if(Variables.Controlled_Device == null)
            {
                throw new DeviceNotFoundException("There is no connected device!");
            }
            receiver.Flush();
            client.ExecuteRemoteCommand(command, (Variables.Controlled_Device as DeviceData), receiver);
            return receiver.ToString();
        }

        private static void StartAdb([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (!ScriptRun.Run)
            {
                return;
            }
            string adbname = Environment.CurrentDirectory + "\\adb\\adb.exe";
            {
                if (!File.Exists(adbname))
                {
                    if(Variables.FindConfig("General","AdbPath", out var path))
                    {
                        path = path.Remove(path.LastIndexOf('\\'));
                        IEnumerable<string> exe = Directory.EnumerateFiles(path, "*.exe");
                        foreach (var e in exe)
                        {
                            if (e.Contains("adb"))
                            {
                                adbname = e;
                                break;
                            }
                        }
                    }
                    else
                    {
                        File.WriteAllBytes("adb.zip", AdbResource.adb);
                        ZipFile.ExtractToDirectory("adb.zip", Environment.CurrentDirectory);
                        File.Delete("adb.zip");
                    }
                }
            }
            Start:
            try
            {
                server.StartServer(adbname, false);
            }
            catch
            {
                goto Start;
            }

            Variables.AdvanceLog("Adb server started", lineNumber, caller);
            return;
        }
        /// <summary>
        /// Start Game by using CustomImg\Icon.png
        /// </summary>
        public static bool StartGame(Bitmap icon, byte[] img)
        {
            if (!ScriptRun.Run)
            {
                return false;
            }
            try
            {
                if(Variables.Controlled_Device == null)
                {
                    return false;
                }
                    

                    client.ExecuteRemoteCommand("input keyevent KEYCODE_HOME", (Variables.Controlled_Device as DeviceData), receiver);
                    Thread.Sleep(1000);
                    var ico = FindImage(img, icon, true);
                    if (ico != null)
                    {
                        SendTap(ico.Value);
                        Thread.Sleep(1000);
                        return true;
                    }
            }
            catch (InvalidOperationException)
            {

            }
            catch (ArgumentOutOfRangeException)
            {
                
            }
            return false;
        }
        /// <summary>
        /// Start game using game package name
        /// </summary>
        /// <param name="packagename"></param>
        /// <returns></returns>
        public static bool StartGame(string packagename)
        {
            if (!ScriptRun.Run)
            {
                return false;
            }
            try
            {
                if (Variables.Controlled_Device == null)
                {
                    return false;
                }
                
                client.ExecuteRemoteCommand("input keyevent KEYCODE_HOME", (Variables.Controlled_Device as DeviceData), receiver);
                Thread.Sleep(1000);
                client.ExecuteRemoteCommand("am start -n " + packagename, (Variables.Controlled_Device as DeviceData), receiver);
                Thread.Sleep(1000);
                return GameIsForeground(packagename);
            }
            catch (InvalidOperationException)
            {

            }
            catch (ArgumentOutOfRangeException)
            {

            }
            return false;
        }
        /// <summary>
        /// Close the game with package name
        /// </summary>
        /// <param name="packagename"></param>
        public static void KillGame(string packagename)
        {
            if (!ScriptRun.Run)
            {
                return;
            }
            try
            {
                if (Variables.Controlled_Device == null)
                {
                    return;
                }
                client.ExecuteRemoteCommand("input keyevent KEYCODE_HOME", (Variables.Controlled_Device as DeviceData), receiver);
                Thread.Sleep(1000);
                client.ExecuteRemoteCommand("am force-stop " + packagename, (Variables.Controlled_Device as DeviceData), receiver);
            }
            catch (InvalidOperationException)
            {

            }
            catch (ArgumentOutOfRangeException)
            {

            }
        }
        /// <summary>
        /// Kill All process tree
        /// </summary>
        /// <param name="pid"></param>
        public static void KillProcessAndChildren(int pid)
        {
            // Cannot close 'system idle process'.
            if (pid == 0)
            {
                return;
            }
            ManagementObjectSearcher searcher = new ManagementObjectSearcher ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }
        static bool captureerror = false;
        private static byte[] ImageCapture(IntPtr hWnd, Point cropstart, Point cropend, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            try
            {
                Stopwatch s = Stopwatch.StartNew();
                Rectangle rc = new Rectangle();
                DllImport.GetWindowRect(hWnd, ref rc);
                Bitmap bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
                Graphics gfxBmp = Graphics.FromImage(bmp);
                IntPtr hdcBitmap = gfxBmp.GetHdc();
                DllImport.PrintWindow(hWnd, hdcBitmap, 0);
                gfxBmp.ReleaseHdc(hdcBitmap);
                gfxBmp.Dispose();
                Variables.AdvanceLog("Screenshot saved to memory used " + s.ElapsedMilliseconds + " ms", lineNumber, caller);
                s.Stop();
                bmp = CropImage(bmp, cropstart, cropend, lineNumber, caller);
                if (Variables.ImageDebug)
                {
                    bmp.Save("Profiles\\Logs\\" + Encryption.SHA256(DateTime.Now.ToString()) + ".bmp");
                }
                return Compress(bmp);
            }
            catch
            {
                captureerror = true;
                return ImageCapture();
            }
        }
        /// <summary>
        /// Fast Capturing screen and return the image, uses WinAPI capture if Variables.Background is false.
        /// </summary>
        public static byte[] ImageCapture([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (!ScriptRun.Run)
            {
                return null;
            }
            if (Variables.WinApiCapt && !captureerror)
            {
                if(Variables.ProchWnd != IntPtr.Zero)
                {
                    return ImageCapture(Variables.ProchWnd, Variables.WinApiCaptCropStart, Variables.WinApiCaptCropEnd, lineNumber, caller);
                }
                else
                {
                    return ImageCapture(Variables.Proc.MainWindowHandle, Variables.WinApiCaptCropStart, Variables.WinApiCaptCropEnd, lineNumber, caller);
                }
            }
            captureerror = false;
            try
            {
                Stopwatch s = Stopwatch.StartNew();
                if (!Directory.Exists(Variables.SharedPath))
                {
                    Variables.ScriptLog("Warning, unable to find shared folder! Trying to use WinAPI!", Color.Red);
                    Variables.WinApiCapt = true;
                    return ImageCapture(Variables.Proc.MainWindowHandle, Variables.WinApiCaptCropStart, Variables.WinApiCaptCropEnd, lineNumber, caller);
                }
                if (pcimagepath == "" || androidimagepath == "")
                {
                    var tempname = Encryption.SHA256(DateTime.Now.ToString());
                    pcimagepath = (Variables.SharedPath + "\\" + tempname + ".rgba").Replace("\\\\","\\");
                    androidimagepath = (Variables.AndroidSharedPath + tempname + ".rgba");
                }
                byte[] raw = null;
                
                if (Variables.Controlled_Device == null)
                {
                    Variables.AdvanceLog("No device connected!", lineNumber, caller);
                    ConnectAndroidEmulator();
                    return null;
                }
                if ((Variables.Controlled_Device as DeviceData).State == DeviceState.Offline || !ScriptRun.Run)
                {
                    return null;
                }
                client.ExecuteRemoteCommand("screencap " + androidimagepath, (Variables.Controlled_Device as DeviceData), receiver);
                if (Variables.NeedPull)
                {
                    if (File.Exists(pcimagepath))
                    {
                        File.Delete(pcimagepath);
                    }
                    Pull(androidimagepath, pcimagepath);
                }
                if (!File.Exists(pcimagepath))
                {
                    Variables.AdvanceLog("Unable to read rgba file because of file not exist!", lineNumber, caller);
                    return null;
                }
                raw = File.ReadAllBytes(pcimagepath);
                int expectedsize = (Variables.EmulatorHeight * Variables.EmulatorWidth * 4) + 12;
                if (raw.Length != expectedsize || raw.Length > int.MaxValue || raw.Length < 1)
                {
                    //Image is not in same size, resize emulator
                    ResizeEmulator();
                    return null;
                }
                byte[] img = new byte[raw.Length - 12]; //remove header
                Array.Copy(raw,12, img,0, img.Length);
                Image<Rgba, byte> image = new Image<Rgba, byte>(Variables.EmulatorWidth,Variables.EmulatorHeight);
                image.Bytes = img;
                if (Variables.ImageDebug)
                {
                    image.Save("Profiles\\Logs\\" + Encryption.SHA256(DateTime.Now.ToString()) + ".bmp");
                }
                Variables.AdvanceLog("Screenshot saved to memory used " + s.ElapsedMilliseconds + " ms",lineNumber,caller);
                s.Stop();
                return Compress(image.Bitmap);
            }
            catch (IOException)
            {

            }
            catch (ArgumentOutOfRangeException)
            {

            }
            return null;
        }
        /// <summary>
        /// Tap at location
        /// </summary>
        /// <param name="point">The location</param>
        public static void SendTap(Point point)
        {
            if (!ScriptRun.Run)
            {
                return;
            }
            SendTap(point.X, point.Y);
        }
        /// <summary>
        /// Swipe the screen
        /// </summary>
        /// <param name="start">Swiping start position</param>
        /// <param name="end">Swiping end position</param>
        /// <param name="usedTime">The time used for swiping, milliseconds</param>
        public static void SendSwipe(Point start, Point end, int usedTime, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (!ScriptRun.Run)
            {
                return;
            }
            
            int x = start.X;
            int y = start.Y;
            int ex = end.X;
            int ey = end.Y;
            if (Variables.Controlled_Device == null)
            {
                return;
            }
            receiver.Flush();
            client.ExecuteRemoteCommand("input touchscreen swipe " + x + " " + y + " " + ex + " " + ey + " " + usedTime, (Variables.Controlled_Device as DeviceData), receiver);
            if (receiver.ToString().Contains("Error"))
            {
                Variables.AdvanceLog(receiver.ToString(), lineNumber, caller);
            }
        }
        /// <summary>
        /// Left click adb command on the point for generating background click in emulators
        /// </summary>
        /// <param name="x">X Posiition for clicking</param>
        /// <param name="y">Y Position for clicking</param>
        public static void SendTap(int x, int y)
        {
            if (!ScriptRun.Run)
            {
                return;
            }
            if (Variables.Controlled_Device == null)
            {
                return;
            }
            string cmd = $"d 0 {x} {y} 300\nc\nu 0\nc\n";
            Minitouch(cmd);
        }
        /// <summary>
        /// Send minitouch command to device
        /// </summary>
        /// <param name="command">Minitouch command such as d, c, u, m</param>
        public static void Minitouch(string command)
        {
            if (!ScriptRun.Run)
            {
                return;
            }
            try
            {
                Stopwatch s = Stopwatch.StartNew();
                if(minitouchSocket == null)
                {
                    ConnectMinitouch();
                }
                byte[] bytes = AdbClient.Encoding.GetBytes(command);
                minitouchSocket.Send(bytes, 0, bytes.Length, SocketFlags.None);
                s.Stop();
                Variables.AdvanceLog("Minitouch command "+ command.Replace("\n","\\n") + " sended. Used time: " + s.ElapsedMilliseconds + "ms");
            }
            catch(SocketException)
            {
                minitouchPort++;
                ConnectMinitouch();
                Minitouch(command);
            }
        }
        /// <summary>
        /// Swipe the screen
        /// </summary>
        /// <param name="startX">Swiping start position</param>
        /// <param name="startY">Swiping start postion</param>
        /// <param name="endX">Swiping end position</param>
        /// <param name="endY">Swiping end position</param>
        /// <param name="usedTime">The time used for swiping, milliseconds</param>
        public static void SendSwipe(int startX, int startY, int endX, int endY, int usedTime)
        {
            if (!ScriptRun.Run)
            {
                return;
            }

            if (Variables.Controlled_Device == null)
            {
                return;
            }
            receiver.Flush();
            client.ExecuteRemoteCommand("input touchscreen swipe " + startX + " " + startY + " " + endX + " " + endY + " " + usedTime, (Variables.Controlled_Device as DeviceData), receiver);
            if (receiver.ToString().Contains("Error"))
            {
                Variables.AdvanceLog(receiver.ToString());
            }
        }
        /// <summary>
        /// Emulator supported by this dll
        /// </summary>
        public static bool Is64BitOperatingSystem()
        {
            // Check if this process is natively an x64 process. If it is, it will only run on x64 environments, thus, the environment must be x64.
            if (IntPtr.Size == 8)
                return true;
            // Check if this process is an x86 process running on an x64 environment.
            IntPtr moduleHandle = DllImport.GetModuleHandle("kernel32");
            if (moduleHandle != IntPtr.Zero)
            {
                IntPtr processAddress = DllImport.GetProcAddress(moduleHandle, "IsWow64Process");
                if (processAddress != IntPtr.Zero)
                {
                    if (DllImport.IsWow64Process(DllImport.GetCurrentProcess(), out bool result) && result)
                        return true;
                }
            }
            // The environment must be an x86 environment.
            return false;
        }
        /// <summary>
        /// Start Emulator according to EmulatorInterface
        /// </summary>
        public static void StartEmulator()
        {
            if (StartingEmulator)
            {
                return;
            }
            StartingEmulator = true;
            if (!ScriptRun.Run)
            {
                return;
            }
            Variables.emulator.StartEmulator();
            Variables.ScriptLog("Starting Emulator...", Color.LimeGreen);
            StartingEmulator = false;
        }
        /// <summary>
        /// Get color of location in screenshots
        /// </summary>
        /// <param name="position">The position of image</param>
        /// <param name="rawimage">The image that need to return color</param>
        /// <returns>color</returns>
        public static Color GetPixel(Point position, byte[] rawimage)
        {
            if (!ScriptRun.Run)
            {
                return Color.Black;
            }
            return Decompress(rawimage).GetPixel(position.X, position.Y);
        }

        private static Color GetPixel(int x, int y, int step, int Width, int Depth, byte[] pixel)
        {
            if (!ScriptRun.Run)
            {
                return Color.Black;
            }
            Color clr = Color.Empty;
            int i = ((y * Width + x) * step);
            if (i > pixel.Length)
            {
                Variables.AdvanceLog("index of pixel array out of range at GetPixel");
                return clr;
            }
            if (Depth == 32 || Depth == 24)
            {
                byte b = pixel[i];
                byte g = pixel[i + 1];
                byte r = pixel[i + 2];
                clr = Color.FromArgb(r, g, b);
            }
            else if (Depth == 8)
            {
                byte b = pixel[i];
                clr = Color.FromArgb(b, b, b);
            }
            return clr;
        }
        /// <summary>
        /// Compare point RGB from image
        /// </summary>
        /// <param name="image">The image that need to return</param>
        /// <param name="point">The point to check for color</param>
        /// <param name="color">The color to check at point is true or false</param>
        /// <param name="tolerance">The tolerance on color, larger will more inaccurate</param>
        /// <returns>bool</returns>
        public static bool RGBComparer(byte[] image, Point point, Color color, int tolerance, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (!ScriptRun.Run)
            {
                return false;
            }
            int red = color.R;
            int blue = color.B;
            int green = color.G;
            return RGBComparer(image,point,red,green,blue,tolerance, lineNumber, caller);
        }

        /// <summary>
        /// Compare point RGB from image
        /// </summary>
        /// <param name="image">The image that need to return</param>
        /// <param name="point">The point to check for color</param>
        /// <param name="tolerance">Tolerance to the color RGB, example: red=120, Tolerance=20 Result=100~140 red will return true</param>
        /// <param name="blue">Blue value of color</param>
        /// <param name="green">Green value of color</param>
        /// <param name="red">Red value of color</param>
        /// <returns>bool</returns>
        public static bool RGBComparer(byte[] image, Point point, int red, int green, int blue, int tolerance, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (!ScriptRun.Run)
            {
                return false;
            }
            
            if (image == null)
            {
                return false;
            }
            Bitmap bmp = Decompress(image);
            int Width = bmp.Width;
            int Height = bmp.Height;
            int PixelCount = Width * Height;
            Rectangle rect = new Rectangle(0, 0, Width, Height);
            int Depth = Bitmap.GetPixelFormatSize(bmp.PixelFormat);
            if (Depth != 8 && Depth != 24 && Depth != 32)
            {
                Variables.AdvanceLog("Image bit per pixel format not supported", lineNumber, caller);
                return false;
            }
            BitmapData bd = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
            int step = Depth / 8;
            try
            {
                byte[] pixel = new byte[PixelCount * step];
                IntPtr ptr = bd.Scan0;
                Marshal.Copy(ptr, pixel, 0, pixel.Length);
                Color clr = GetPixel(point.X, point.Y, step, Width, Depth, pixel);
                if(clr.R >= (red - tolerance) && clr.R <= (red + tolerance))
                { 
                    if(clr.G >= (green - tolerance) && clr.G <= (green + tolerance))
                    {
                        if(clr.B >= (blue - tolerance) && clr.B <= (blue + tolerance))
                        {
                            bmp.UnlockBits(bd);
                            return true;
                        }
                    }
                }
                Variables.AdvanceLog("The point " + point.X + ", " + point.Y + " color is " + clr.R + ", " + clr.G + ", " + clr.B, lineNumber, caller);
            }
            catch
            {

            }
            bmp.UnlockBits(bd);
            return false;
        }
        /// <summary>
        /// Compare point RGB from image
        /// </summary>
        /// <returns>bool</returns>
        public static bool RGBComparer(byte[] rawimage, Color color)
        {
            if (!ScriptRun.Run)
            {
                return false;
            }
            if (rawimage == null)
            {
                return false;
            }
            Bitmap bmp = Decompress(rawimage);
            int Width = bmp.Width;
            int Height = bmp.Height;
            int PixelCount = Width * Height;
            Rectangle rect = new Rectangle(0, 0, Width, Height);
            int Depth = Bitmap.GetPixelFormatSize(bmp.PixelFormat);
            if (Depth != 8 && Depth != 24 && Depth != 32)
            {
                Variables.AdvanceLog("Image bit per pixel format not supported");
                return false;
            }
            BitmapData bd = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
            int step = Depth / 8;
            byte[] pixel = new byte[PixelCount * step];
            IntPtr ptr = bd.Scan0;
            Marshal.Copy(ptr, pixel, 0, pixel.Length);
            for (int i = 0; i < bmp.Height; i++)
            {
                for (int j = 0; j < bmp.Width; j++)
                {
                    //Get the color at each pixel
                    Color now_color = GetPixel(j, i, step, Width, Depth, pixel);

                    //Compare Pixel's Color ARGB property with the picked color's ARGB property 
                    if (now_color.ToArgb() == color.ToArgb())
                    {
                        bmp.UnlockBits(bd);
                        return true;
                    }
                }
            }
            bmp.UnlockBits(bd);
            return false;
        }
        /// <summary>
        /// Compare point RGB from image
        /// </summary>
        /// <returns>bool</returns>
        public static bool RGBComparer(byte[] rawimage, Color color, Point start, Point end, out Point? point, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (!ScriptRun.Run)
            {
                point = null;
                return false;
            }
            var image = CropImage(rawimage, start, end);
            if (image == null)
            {
                point = null;
                return false;
            }
            return RGBComparer(image, color, out point, lineNumber, caller);
        }

        /// <summary>
        /// Compare point RGB from image
        /// </summary>
        /// <returns>bool</returns>
        public static bool RGBComparer(byte[] rawimage, Color color, out Point? point, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (!ScriptRun.Run)
            {
                point = null;
                return false;
            }
            if (rawimage == null)
            {
                point = null;
                return false;
            }
            Bitmap bmp = Decompress(rawimage);
            int Width = bmp.Width;
            int Height = bmp.Height;
            int PixelCount = Width * Height;
            Rectangle rect = new Rectangle(0, 0, Width, Height);
            int Depth = Bitmap.GetPixelFormatSize(bmp.PixelFormat);
            if (Depth != 8 && Depth != 24 && Depth != 32)
            {
                Variables.AdvanceLog("Image bit per pixel format not supported");
                point = null;
                return false;
            }
            BitmapData bd = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
            int step = Depth / 8;
            byte[] pixel = new byte[PixelCount * step];
            IntPtr ptr = bd.Scan0;
            Marshal.Copy(ptr, pixel, 0, pixel.Length);
            for (int i = 0; i < bmp.Height; i++)
            {
                for (int j = 0; j < bmp.Width; j++)
                {
                    //Get the color at each pixel
                    Color now_color = GetPixel(j, i,  step, Width, Depth, pixel);

                    //Compare Pixel's Color ARGB property with the picked color's ARGB property 
                    if (now_color.ToArgb() == color.ToArgb())
                    {
                        point = new Point(j, i);
                        bmp.UnlockBits(bd);
                        return true;
                    }
                }
            }
            bmp.UnlockBits(bd);
            point = null;
            return false;
        }
        /// <summary>
        /// Return a Point location of the image in Variables.Image (will return null if not found)
        /// </summary>
        /// <param name="find">The smaller image for matching</param>
        /// <param name="original">Original image that need to get the point on it</param>
        /// <param name="GrayStyle">Convert the images to gray for faster detection</param>
        /// <param name="lineNumber"></param>
        /// <param name="caller"></param>
        /// <returns>Point or null</returns>
        /// <returns></returns>
        public static Point[] FindImages(byte[] original, Bitmap[] find, bool GrayStyle, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            Bitmap image = Decompress(original);
            return FindImages(image, find, GrayStyle, lineNumber, caller);
        }

        /// <summary>
        /// Return a Point location of the image in Variables.Image (will return null if not found)
        /// </summary>
        /// <param name="find">The smaller image for matching</param>
        /// <param name="original">Original image that need to get the point on it</param>
        /// <param name="GrayStyle">Convert the images to gray for faster detection</param>
        /// <param name="lineNumber"></param>
        /// <param name="caller"></param>
        /// <returns>Point or null</returns>
        /// <returns></returns>
        public static Point[] FindImages(Bitmap original, Bitmap[] find, bool GrayStyle, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (!ScriptRun.Run)
            {
                return null;
            }
            Stopwatch s = Stopwatch.StartNew();
            List<Point> matched = new List<Point>();
            try
            {
                if (GrayStyle)
                {
                    Image<Gray, byte> source = new Image<Gray, byte>(original);
                    foreach (var image in find)
                    {
                        Image<Gray, byte> template = new Image<Gray, byte>(image);
                        using (Image<Gray, float> result = source.MatchTemplate(template, TemplateMatchingType.CcoeffNormed))
                        {
                            result.MinMax(out double[] minValues, out double[] maxValues, out Point[] minLocations, out Point[] maxLocations);
                            for (int x = 0; x < maxValues.Length; x++)
                            {
                                if (maxValues[x] > 0.9 && !matched.Contains(maxLocations[x]))
                                {
                                    s.Stop();
                                    matched.Add(maxLocations[x]);
                                }
                            }
                        }
                    }
                }
                else
                {
                    Image<Bgr, byte> source = new Image<Bgr, byte>(original);
                    foreach (var image in find)
                    {
                        Image<Bgr, byte> template = new Image<Bgr, byte>(image);
                        using (Image<Gray, float> result = source.MatchTemplate(template, TemplateMatchingType.CcoeffNormed))
                        {
                            result.MinMax(out double[] minValues, out double[] maxValues, out Point[] minLocations, out Point[] maxLocations);
                            for (int x = 0; x < maxValues.Length; x++)
                            {
                                if (maxValues[x] > 0.9 && !matched.Contains(maxLocations[x]))
                                {
                                    s.Stop();
                                    matched.Add(maxLocations[x]);
                                }
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Variables.AdvanceLog(ex.ToString());
            }
            s.Stop();
            Variables.AdvanceLog("Image processed. Used time: " + s.ElapsedMilliseconds + " ms", lineNumber, caller);
            if (matched.Count < 1)
            {
                return null;
            }
            else
            {
                return matched.ToArray();
            }
        }
        /// <summary>
        /// Return a Point location of the image in Variables.Image (will return null if not found)
        /// </summary>
        /// <param name="find">The smaller image for matching</param>
        /// <param name="screencapture">Original image that need to get the point on it</param>
        /// <param name="GrayStyle">Convert the images to gray for faster detection</param>
        /// <returns>Point or null</returns>
        public static Point? FindImage(byte[] screencapture, Bitmap find, bool GrayStyle, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (!ScriptRun.Run)
            {
                return null;
            }
            if (screencapture == null)
            {
                Variables.AdvanceLog("Result return null because of null original image", lineNumber, caller);
                return null;
            }
            try
            {
                return FindImage(Decompress(screencapture), find, GrayStyle, lineNumber, caller);
            }
            catch (Exception ex)
            {
                Variables.AdvanceLog(ex.ToString(), lineNumber, caller);
                return null;
            }
        }
        /// <summary>
        /// Return a Point location of the image in Variables.Image (will return null if not found)
        /// </summary>
        /// <param name="find">The smaller image for matching</param>
        /// <param name="original">Original image that need to get the point on it</param>
        /// <param name="GrayStyle">Convert the images to gray for faster detection</param>
        /// <param name="lineNumber"></param>
        /// <param name="caller"></param>
        /// <returns>Point or null</returns>
        /// <returns></returns>
        public static Point? FindImage(Bitmap original, Bitmap find, bool GrayStyle, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (!ScriptRun.Run)
            {
                return null;
            }
            Stopwatch s = Stopwatch.StartNew();
            try
            {
                if (GrayStyle)
                {

                    Image<Gray, byte> source = new Image<Gray, byte>(original);
                    Image<Gray, byte> template = new Image<Gray, byte>(find);
                    using (Image<Gray, float> result = source.MatchTemplate(template, TemplateMatchingType.CcoeffNormed))
                    {
                        result.MinMax(out double[] minValues, out double[] maxValues, out Point[] minLocations, out Point[] maxLocations);

                        // You can try different values of the threshold. I guess somewhere between 0.75 and 0.95 would be good.
                        if (maxValues[0] > 0.9)
                        {
                            s.Stop();
                            Variables.AdvanceLog("Image matched. Used time: " + s.ElapsedMilliseconds + " ms", lineNumber,caller);
                            return maxLocations[0];
                        }
                    }
                }
                else
                {
                    Image<Bgr, byte> source = new Image<Bgr, byte>(original);
                    Image<Bgr, byte> template = new Image<Bgr, byte>(find);
                    using (Image<Gray, float> result = source.MatchTemplate(template, TemplateMatchingType.CcoeffNormed))
                    {
                        result.MinMax(out double[] minValues, out double[] maxValues, out Point[] minLocations, out Point[] maxLocations);

                        // You can try different values of the threshold. I guess somewhere between 0.75 and 0.95 would be good.
                        if (maxValues[0] > 0.9)
                        {
                            s.Stop();
                            Variables.AdvanceLog("Image matched. Used time: " + s.ElapsedMilliseconds + " ms",lineNumber,caller);
                            return maxLocations[0];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Variables.AdvanceLog(ex.ToString());
            }
            s.Stop();
            Variables.AdvanceLog("Image not matched. Used time: " + s.ElapsedMilliseconds + " ms", lineNumber,caller);
            return null;
        }
        /// <summary>
        /// Return a Point location of the image in Variables.Image (will return null if not found)
        /// </summary>
        /// <param name="findPath">The path of smaller image for matching</param>
        /// <param name="screencapture">Original image that need to get the point on it</param>
        /// <param name="GrayStyle">Convert the images to gray for faster detection</param>
        /// <param name="lineNumber"></param>
        /// <param name="caller"></param>
        /// <returns>Point or null</returns>
        public static Point? FindImage(byte[] screencapture, string findPath, bool GrayStyle, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (!ScriptRun.Run)
            {
                return null;
            }
            Stopwatch s = Stopwatch.StartNew();
            
            if (screencapture == null)
            {
                Variables.AdvanceLog("Result return null because of null original image",lineNumber,caller);
                return null;
            }
            Bitmap original = Decompress(screencapture);
            if (!File.Exists(findPath))
            {
                Variables.AdvanceLog("Unable to find image " + findPath.Split('\\').Last() + ", image path not valid", lineNumber,caller);
                return null;
            }
            try
            {
                if (GrayStyle)
                {
                    Image <Gray, byte> source = new Image<Gray, byte>(original);
                    Image<Gray, byte> template = new Image<Gray, byte>(findPath);
                    using (Image<Gray, float> result = source.MatchTemplate(template, TemplateMatchingType.CcoeffNormed))
                    {
                        result.MinMax(out double[] minValues, out double[] maxValues, out Point[] minLocations, out Point[] maxLocations);
                        // You can try different values of the threshold. I guess somewhere between 0.75 and 0.95 would be good.
                        if (maxValues[0] > 0.9)
                        {
                            s.Stop();
                            Variables.AdvanceLog("Image matched. Used time: " + s.ElapsedMilliseconds + " ms", lineNumber,caller);
                            return maxLocations[0];
                        }
                    }
                }
                else
                {
                    Image<Bgr, byte> source = new Image<Bgr, byte>(original);
                    Image<Bgr, byte> template = new Image<Bgr, byte>(findPath);
                    using (Image<Gray, float> result = source.MatchTemplate(template, TemplateMatchingType.CcoeffNormed))
                    {
                        result.MinMax(out double[] minValues, out double[] maxValues, out Point[] minLocations, out Point[] maxLocations);

                        // You can try different values of the threshold. I guess somewhere between 0.75 and 0.95 would be good.
                        if (maxValues[0] > 0.9)
                        {
                            s.Stop();
                            Variables.AdvanceLog("Image matched. Used time: " + s.ElapsedMilliseconds + " ms",lineNumber,caller);
                            return maxLocations[0];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Variables.AdvanceLog(ex.ToString());
            }
            s.Stop();
            Variables.AdvanceLog("Image not matched. Used time: " + s.ElapsedMilliseconds + " ms",lineNumber,caller);
            return null;
        }
        /// <summary>
        /// Return a Point location of the image in Variables.Image (will return null if not found)
        /// </summary>
        /// <param name="image">The smaller image for matching</param>
        /// <param name="screencapture">Original image that need to get the point on it</param>
        /// <param name="GrayStyle">Convert the images to gray for faster detection</param>
        /// <returns>Point or null</returns>
        public static Point? FindImage(byte[] screencapture, byte[] image, bool GrayStyle, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (!ScriptRun.Run)
            {
                return null;
            }
            if (screencapture == null)
            {
                Variables.AdvanceLog("Result return null because of null original image", lineNumber, caller);
                return null;
            }
            return FindImage(Decompress(screencapture), Decompress(image), GrayStyle);
        }
        /// <summary> 
        /// Crop the image and return the cropped image
        /// </summary>
        /// <param name="original">Image that need to be cropped</param>
        /// <param name="Start">Starting Point</param>
        /// <param name="End">Ending Point</param>
        /// <returns></returns>
        public static byte[] CropImage(byte[] original, Point Start, Point End, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (!ScriptRun.Run)
            {
                return null;
            }
            return Compress(CropImage(Decompress(original), Start, End, lineNumber, caller));
        }

        private static Bitmap CropImage(Bitmap original, Point start, Point End, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            Stopwatch s = Stopwatch.StartNew();
            if (original == null)
            {
                Variables.AdvanceLog("Result return null because of null original image");
                return null;
            }
            Image<Bgr, byte> imgInput = new Image<Bgr, byte>(original);
            Rectangle rect = new Rectangle
            {
                X = Math.Min(start.X, End.X),
                Y = Math.Min(start.Y, End.Y),
                Width = Math.Abs(start.X - End.X),
                Height = Math.Abs(start.Y - End.Y)
            };
            imgInput.ROI = rect;
            Image<Bgr, byte> temp = imgInput.CopyBlank();
            imgInput.CopyTo(temp);
            imgInput.Dispose();
            s.Stop();
            Variables.AdvanceLog("Image cropped. Used time: " + s.ElapsedMilliseconds + " ms", lineNumber, caller);
            return temp.Bitmap;
        }
        /// <summary>
        /// Force emulator keep potrait
        /// </summary>
        public static void StayPotrait()
        {
            
            if (Variables.Controlled_Device == null)
            {
                return;
            }
            if ((Variables.Controlled_Device as DeviceData).State == DeviceState.Online)
            {
                client.ExecuteRemoteCommand("content insert --uri content://settings/system --bind name:s:accelerometer_rotation --bind value:i:0", (Variables.Controlled_Device as DeviceData), receiver);
                client.ExecuteRemoteCommand("content insert --uri content://settings/system --bind name:s:user_rotation --bind value:i:0", (Variables.Controlled_Device as DeviceData), receiver);
                receiver.Flush();
            }
        }
        /// <summary>
        /// Rotate image
        /// </summary>
        /// <param name="image"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static Bitmap RotateImage(Image image, float angle)
        {
            if (!ScriptRun.Run)
            {
                return null;
            }
            if (image == null)
                throw new ArgumentNullException("image is not exist!");
            PointF offset = new PointF((float)image.Width / 2, (float)image.Height / 2);
            Bitmap rotatedBmp = new Bitmap(image.Width, image.Height);
            rotatedBmp.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            Graphics g = Graphics.FromImage(rotatedBmp);
            g.TranslateTransform(offset.X, offset.Y);
            g.RotateTransform(angle);
            g.TranslateTransform(-offset.X, -offset.Y);
            g.DrawImage(image, new PointF(0, 0));

            return rotatedBmp;
        }
        /// <summary>
        /// Pull file from emulator to PC
        /// </summary>
        /// <param name="from">path of file in android</param>
        /// <param name="to">path of file on PC</param>
        /// <returns></returns>
        public static bool Pull(string from, string to)
        {
            if (!ScriptRun.Run)
            {
                return false;
            }
            if (Variables.Controlled_Device == null)
            {
                return false;
            }
            socket = new AdbSocket(new IPEndPoint(IPAddress.Parse("127.0.0.1"), Adb.CurrentPort));
            using (SyncService service = new SyncService(socket, (Variables.Controlled_Device as DeviceData)))
            using (Stream stream = File.OpenWrite(to))
            {
                service.Pull(from, stream, null, CancellationToken.None);
            }
            if (File.Exists(to))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Push file from PC to emulator
        /// </summary>
        /// <param name="from">path of file on PC</param>
        /// <param name="to">path of file in android</param>
        /// <param name="permission">Permission of file</param>
        public static void Push(string from, string to, int permission)
        {
            if (!ScriptRun.Run)
            {
                return;
            }
            socket = new AdbSocket(new IPEndPoint(IPAddress.Parse("127.0.0.1"), Adb.CurrentPort));
            using (SyncService service = new SyncService(socket, (Variables.Controlled_Device as DeviceData)))
            {
                using (Stream stream = File.OpenRead(from))
                {
                    service.Push(stream, to, permission, DateTime.Now, null, CancellationToken.None);
                }
            }
        }

        private static readonly Random rnd = new Random();
        /// <summary>
        /// Add delays on script with human like randomize
        /// </summary>
        /// <param name="mintime">Minimum time to delay</param>
        /// <param name="maxtime">Maximum time to delay</param>
        public static void Delay(int mintime, int maxtime)
        {
            if (!ScriptRun.Run)
            {
                return;
            }
            if (maxtime < mintime)
            {
                //swap back
                int temp = maxtime;
                maxtime = mintime;
                mintime = temp;
            }
            if(mintime < 1)
                mintime = 1;
            if (maxtime < 1)
                maxtime = 1;
            Thread.Sleep(rnd.Next(mintime, maxtime));
        }
        /// <summary>
        /// Add delays on script with human like randomize
        /// </summary>
        /// <param name="randomtime">A actual time for delay</param>
        /// <param name="accurate">Real accurate or random about 200 miliseconds?</param>
        public static void Delay(int randomtime, bool accurate)
        {
            if (!ScriptRun.Run)
            {
                return;
            }
            if (accurate)
            {
                Delay(randomtime, randomtime);
            }
            else
            {
                Delay(randomtime - 100, randomtime + 100);
            }
        }
        /// <summary>
        /// Delay for specific time
        /// </summary>
        /// <param name="time">miliseconds</param>
        public static void Delay(int time)
        {
            Delay(time, true);
        }
        /// <summary>
        /// Delay for specific time
        /// </summary>
        /// <param name="time">time</param>
        public static void Delay(TimeSpan time)
        {
            Delay(Convert.ToInt32(time.TotalMilliseconds));
        }
        /// <summary>
        /// Send text to android emulator, can be used for typing but only ENGLISH!!
        /// </summary>
        /// <param name="text">The text needed to be sent</param>
        public static void SendText(string text)
        {
            if (!ScriptRun.Run)
            {
                return;
            }
            try
            {
                
                client.ExecuteRemoteCommand("input text \"" + text.Replace(" ", "%s") + "\"", (Variables.Controlled_Device as DeviceData), receiver);
            }
            catch (InvalidOperationException)
            {

            }
            catch (ArgumentOutOfRangeException)
            {

            }
        }
       /// <summary>
       /// Send keyevent via Adb to controlled device
       /// </summary>
       /// <param name="keycode">keycode</param>
        public static void SendEvent(int keycode)
        {
            AdbCommand("input keyevent " + keycode.ToString());
        }
        /// <summary>
        /// Send Adb command without binding with Variables.Device
        /// </summary>
        /// <param name="command"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        public static string AdbCommand(string command, object device)
        {
            receiver.Flush();
            client.ExecuteRemoteCommand(command, (device as DeviceData), receiver);
            return receiver.ToString();
        }
        /// <summary>
        /// Return objects of connected devices
        /// </summary>
        /// <returns></returns>
        public static object[] GetDevices(out string[] names)
        {
            List<object> devices = new List<object>();
            List<string> name = new List<string>();
            foreach (var device in client.GetDevices())
            {
                devices.Add(device);
                name.Add(device.Name);
            }
            names = name.ToArray();
            return devices.ToArray();
        }
    }
}