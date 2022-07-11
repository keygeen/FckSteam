using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using File = System.IO.File;

namespace SteamWorkaroundWPF
{
    /// <summary>
    /// A invisible Window that starts steam <br/>
    /// waits till the pile of shit closes its mainwindow to kill it.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// It does not cache the Steamprocess, <br/>
        /// It takes the fresh set of data from Process.GetProcess()<br/>
        /// otherwise we wont be able to detect the current state of the mainwindow!
        /// </summary>
        Process? Steam { get => Process.GetProcessesByName("Steam").FirstOrDefault();  }

        public MainWindow()
        {
            //ALWAYS put your stuff below InitializeComponent.
            //Even when you dont require component data.
            InitializeComponent();

            //Lets start Steam, so it can take its 2 decades of loading
            //while we preparing our data further below
            var steamProc = new Process()
            {
                //Startinfo describes things such as Filename, Argument and so on.
                StartInfo = new ProcessStartInfo()
                {
                    //define the path for steam.exe
                    FileName = LocateSteamExecutable(),
                }
            };            
            //Start this garbage
            steamProc.Start();
            //Use this sleep timer to delay if your pc is crappy
            //this ensures, we checking for steam not too early.
            //this tool has far less load and thus is quick af
            //Thread.Sleep(500);
            
            int TimeoutCounter = 0;
            //This defines the timeout.
            int TimeoutThreshold = 60;//seconds

            try
            {
                //We wait until timeout threshold as been exceeded 
                //OR MainWindow Shows up
                while (Steam!.MainWindowHandle == IntPtr.Zero && TimeoutCounter < TimeoutThreshold)
                {
                    //We dont have a window yet, increment
                    TimeoutCounter++;
                    //wait a sec.
                    Thread.Sleep(1000);
                }

                //we left the loop due to timeout, not due to appeared main window
                if (TimeoutThreshold < TimeoutCounter)
                {
                    //Inform the user about the inability to track the mainwindow handle
                    MessageBox.Show("Unable to track down MainWindowHandle of Steam!", "Critical Error!", MessageBoxButton.OK);
                    //and quit
                    Application.Current.Shutdown();
                }
                //We got out of the loop due to appeared main window, we can register our event handler
                RegisterEvent();
            }

            //This means, the steam process never went up and the while throwed it while
            //accessing the mainwindowhandle
            catch (NullReferenceException NULLREFEX)
            {
                //since we made the error messages, we can simply throw them into the msg box
                MessageBox.Show($"Steam could not be found!", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            //this happens if register or get steam path failed.            
            catch (InvalidOperationException INVAOPEX)
            {
                //since we made the error messages, we can simply throw them into the msg box
                MessageBox.Show($"{INVAOPEX.Message}\nThis Application will exit now!", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);

            }

            //If some cosmic radiation throw over a bit which results into a corrupt state of a ressource which is for some reason not detected by the error corrections alogrithms of hard and software
            catch (Exception ex)
            {
                //how i got here?!
                MessageBox.Show($"Unknown Error:\n{ex.Message}","Unknown Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Removes existing Events and creates a new one for the steam main window handle
        /// </summary>
        /// <exception cref="InvalidOperationException">Unable to use Steams Mainwindow Handle for Automation Elements</exception>
        private void RegisterEvent()
        {
            //First get rid of all eventhandlers, we dont need them anymore
            Automation.RemoveAllEventHandlers();
            //now we make a new one based on mainwindow handle of steam
            AutomationElement windowElement = AutomationElement.FromHandle(Steam.MainWindowHandle);
            //if its null, there is no sense in this application
            if (windowElement != null)
            {
                //i wish i could explain what i did here, but i cannot. Shame on me.
                Automation.AddStructureChangedEventHandler(windowElement, TreeScope.Element, CheckIfSteamShouldBeKilled);
            }
            else
            {
                //Throw up
                throw new InvalidOperationException("Unable to create Automation Element by Steams Main Window handle!");
            }
        }

        /// <summary>
        /// Reads "./steampath.config" and returns the pathstring within it.
        /// </summary>
        /// <returns>the Path defined in "./steampath.config"</returns>
        /// <exception cref="InvalidOperationException">the path in the steampath.config is wrong!</exception>
        private string LocateSteamExecutable()
        {
            //make a var out of the path, cuz im lazy
            const string configFilePath = "steampath.config";
            //Does the file exists?
            if (!File.Exists(configFilePath))
                //Nuh Uh? Okay then create the file with the default path
                File.WriteAllText(configFilePath, @"C:\Program Files (x86)\Steam\steam.exe",Encoding.UTF8);
            //Read the filecontent which should be ONLY the path of steam.exe!
            var steampath =  File.ReadAllText(configFilePath);

            //is the path legit?
            if (!File.Exists(steampath))
                //Nah? throw so the calling method can do whatever it needs.
                throw new InvalidOperationException("Steam.exe cannot be found!");
            
            //return the string, since its legit.
            return steampath;
        }

        /// <summary>
        /// Checks if the mainwindow is closed.<br/>
        /// if mainwindow is closed, it will call "killsteam()" and then stops this application<br/>
        /// otherwise it wont to anything.        /// 
        /// </summary>
        /// <param name="sender">not important, place null, a recipe of your grandma, whatever</param>
        /// <param name="e">Also unimportant, do whatever net core allows you to do here</param>
        private void CheckIfSteamShouldBeKilled(object sender, EventArgs e)
        {
            //if mainwindowhandle is Zero, main window has been closed
            if (Steam.MainWindowHandle == IntPtr.Zero)
            {                
                //its closed! giving gabe the middle finger
                KillSteam();
                //this application serves no purpose anymore, closing.
                Environment.Exit(0);
                //Zero is the returncode for the os.
                //if its a planned exit, it should be 0 otherwise less than 0 which represents a error code.
                //Search for application return code windows for further information
            }
            else
            {
                /* If the mainwindow handle is not zero, 
                 * but we reached here means the main window somehow got changed.
                 * 
                 * when you login, it opens a little window
                 * where it takes 2 decades to connect to steams
                 * potato servers.
                 * 
                 * if connection was successful,
                 * it closes the "mainwindow" and
                 * creates a new window
                 * 
                 * thats the reason we need to 
                 * take the mainwindowhandle from Process.GetProcessByName()
                 * instead of a variable. 
                 * in a variable the mainwindowhandle wont be refreshed. 
                */
                RegisterEvent();
            }
        }

        /// <summary>
        /// Kills Steam, if its currently running, otherwise it wont do anything
        /// </summary>
        private void KillSteam()
        {
            //Get SteamProcess
            var steamProc = Steam;
            //Do we have a running steamprocess?
            if(steamProc != default)
                //we had ;)
                steamProc.Kill();            
        }
    }
}
