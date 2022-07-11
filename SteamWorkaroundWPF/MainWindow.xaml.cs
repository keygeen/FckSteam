using IWshRuntimeLibrary;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using File = System.IO.File;

namespace SteamWorkaroundWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        Process Steam { get => Process.GetProcessesByName("Steam").First();  }
        public MainWindow()
        {
            InitializeComponent();
            var steamProc = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = LocateSteamExecutable(),
                }
            };            
            steamProc.Start();
            //Thread.Sleep(12000);
            int TimeoutCounter = 0;
            int TimeoutThreshold = 60;
            while (Steam.MainWindowHandle == IntPtr.Zero && TimeoutCounter < TimeoutThreshold)
            {
                TimeoutCounter++;
                Thread.Sleep(1000);
            }            
            if (TimeoutThreshold < TimeoutCounter)
            {
                MessageBox.Show("Unable to track down MainWindowHandle of Steam!", "Critical Error!", MessageBoxButton.OK);
                Application.Current.Shutdown();
            }
            RegisterEvent();
        }
        private void RegisterEvent()
        {
            Automation.RemoveAllEventHandlers();
            AutomationElement windowElement = AutomationElement.FromHandle(Steam.MainWindowHandle);
            if (windowElement != null)
            {
                //Automation.AddAutomationFocusChangedEventHandler(CheckIfSteamShouldBeKilled);
                Automation.AddStructureChangedEventHandler(windowElement, TreeScope.Element, CheckIfSteamShouldBeKilled);
            }
        }
        private string LocateSteamExecutable()
        {
            string configFilePath = "steampath.config";
            if (!File.Exists(configFilePath))
                File.WriteAllText(configFilePath, @"C:\Program Files (x86)\Steam\steam.exe",Encoding.UTF8);
            return File.ReadAllText(configFilePath);
        }
        //private string LocateSteamExecutable()
        //{
        //    string displayName;
        //    string InstallPath;
        //    string registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

        //    //64 bits computer
        //    RegistryKey key64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        //    RegistryKey key = key64.OpenSubKey(registryKey);

        //    if (key != null)
        //    {
        //        foreach (RegistryKey subkey in key.GetSubKeyNames().Select(keyName => key.OpenSubKey(keyName)))
        //        {
        //            displayName = subkey.GetValue("DisplayName") as string;
        //            if (displayName != null && displayName.Contains("Steam"))
        //            {

        //                InstallPath = subkey.GetValue("InstallLocation").ToString();

        //                return InstallPath; //or displayName

        //            }
        //        }
        //        key.Close();
        //    }

        //    MessageBox.Show("Unable to track down shithole Steam!", "Critical Error!", MessageBoxButton.OK);
        //    Application.Current.Shutdown();
        //    return null;
        //}

        private void CheckIfSteamShouldBeKilled(object sender, EventArgs e)
        {
            if (Steam.MainWindowHandle == IntPtr.Zero)
            {                
                KillSteam();
                Environment.Exit(0);
            }
            else
            {
                RegisterEvent();
            }
        }
        private void KillSteam()
        {
            var steamProc = Process.GetProcessesByName("Steam").FirstOrDefault();
            if(steamProc != default)
                steamProc.Kill();            
        }
    }
}
