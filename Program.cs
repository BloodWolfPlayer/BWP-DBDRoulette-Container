using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

namespace BWPlayerTwitchMagic
{
    class Program
    {
        static string currentFolderPath = Path.GetFullPath(Directory.GetCurrentDirectory());
        static string settingsFilePath = Path.Combine(currentFolderPath, "settings.txt");
        static void Main(string[] args)
        {

            CheckAndCreateSettingsFile();
            var settings = ReadSettingsFromFile();
            // Hook into the ProcessExit event to clean up Electron processes
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            // Hook into the CancelKeyPress event to handle CTRL+C
            Console.CancelKeyPress += OnCancelKeyPress;

            // Twitch settings
            string username = settings["username"];
            string oauthToken = settings["oauthToken"];
            string channel = settings["channel"];
            string Command = settings["command"];

            // Spinner settings
            int survivors = int.Parse(settings["survivors"]);
            bool killer = bool.Parse(settings["killers"]);
            int screenSelection = int.Parse(settings["screenSelection"]);
            int cornerLoc = int.Parse(settings["cornerLoc"]);

            // Extract slot machine links
            string survivor1 = settings["survivor1"];
            string survivor2 = settings["survivor2"];
            string survivor3 = settings["survivor3"];
            string survivor4 = settings["survivor4"];
            string killerRoll = settings["killerRoll"];



            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(oauthToken) || string.IsNullOrWhiteSpace(channel))
            {
                Console.WriteLine("Please fill out the settings file with your Twitch information!\nOtherwise the program cant read chat!");
                Console.WriteLine("Press any key to close the program.");
                Console.ReadKey();
                Environment.Exit(0);
            }

            // Create threads for Electron app and TwitchReader
            Thread electronThread = new Thread(() => StartElectronApp(survivors, killer, screenSelection, cornerLoc, survivor1, survivor2, survivor3, survivor4, killerRoll));
            Thread twitchReaderThread = new Thread(() =>
            {
                var twitchReader = new TwitchReader(username, oauthToken, channel, Command);
                twitchReader.ConnectAndReadChat();
            });

            // Start both threads
            electronThread.Start();
            twitchReaderThread.Start();

            if (!oauthToken.StartsWith("oauth:"))
            {
                oauthToken = "oauth:" + oauthToken;
            }
        }

        private static void StartElectronApp(int survivors, bool killer, int screenSelection, int cornerLoc, string Survivor1 = null, string Survivor2 = null, string Survivor3 = null, string Survivor4 = null, string KillerRoll = null)
        {
            try
            {
                // Kill any existing Electron processes
                KillElectronProcesses();
        
                // Full path to npx
                //TODO: Make Dynamic, so it can find npx via system PATH variable.
                string command = @"C:\Program Files\nodejs\npx.cmd";
                string arguments = $"electron DBDElectronGambler.js {survivors} {killer.ToString().ToLower()} {screenSelection} {cornerLoc} {Survivor1} {Survivor2} {Survivor3} {Survivor4} {KillerRoll}";

                // Figure out where DBDElectronGambler.js is located.
                string[] commonFolders = {
                    currentFolderPath,
                    Path.Combine(currentFolderPath, @".."),
                    Path.Combine(currentFolderPath, @"JavaScripts\"),
                    Path.Combine(currentFolderPath, @"..\..\.."),
                    @"C:\\Users\\Sebastian\\Desktop\\Twitch Code\\BWPlayer TwitchMagic"
                    
                };
                string foundFolder = null;
                foreach (var folder in commonFolders)
                {
                    string fullPath = Path.Combine(folder, "DBDElectronGambler.js");
                    if (File.Exists(fullPath))
                    {
                        foundFolder = folder;
                        Console.WriteLine($"Found DBDElectronGambler.js in {folder}");
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"DBDElectronGambler.js not found in {folder}");
                    }
                }

                if (foundFolder == null)
                {
                    Console.WriteLine("DBDElectronGambler.js not found in any common folders.");
                    Console.WriteLine("Make sure you got DbDElectronGambler.js in either the same folder as this program or in the 'JavaScripts' folder.");
                    Console.WriteLine("If you dont have the folder, just create it! ;) ");
                    Console.WriteLine("Press any key to close the program.");
                    Console.ReadKey();
                    return;
                }
                bool launched = TryLaunchElectronApp(command, arguments, foundFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting DBDElectronGambler.js with npx: {ex.Message}");
            }
        }
        
        private static bool TryLaunchElectronApp(string command, string arguments, string workingDirectory, int timeoutMs = 0)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
        
                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        Console.WriteLine("Failed to start the Electron process.");
                        KillElectronProcesses();
                        return false;
                    }
        
                    // Read the standard output and error streams asynchronously
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();
        
                    
                    if (process.ExitCode == -1)
                    {
                        Console.WriteLine($"Electron app closed via forced exit. Exit code: {process.ExitCode}");
                        KillElectronProcesses();
                        return false;
                    }
                    else if (process.ExitCode == 2)
                    {
                        Console.WriteLine("You need to have atleast one Survivor or the Killer enabled! Trying to roll 0 Slot machines defeats the purpose of using me!");
                        KillElectronProcesses();
                        return false;
                    }
                    else if (process.ExitCode == 0)
                    {
                        Console.WriteLine("Electron app launched successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"Electron app failed to launch. Exit code: {process.ExitCode}");
                        Console.WriteLine($"Error output: {error}");
                        KillElectronProcesses();
                        return false;
                    }
        
                    // Log the output for debugging purposes
                    Console.WriteLine($"Electron app output: {output}");
                    Console.WriteLine("Electron app launched successfully.... again....");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to launch Electron app from: {workingDirectory}. Error: {ex.Message}");
                KillElectronProcesses();
                return false;
            }
        }

        private static void KillElectronProcesses()
        {
            try
            {
                // Get all processes named "electron" will kill other Electron apps too...
                //TODO: Target only DBDGambler instances.
                var electronProcesses = Process.GetProcessesByName("electron");

                foreach (var process in electronProcesses)
                {
                    Console.WriteLine($"Killing Electron process with ID: {process.Id}");
                    process.Kill(); 
                    process.WaitForExit();
                }

                Console.WriteLine("All Electron processes terminated.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error terminating Electron processes: {ex.Message}");
            }
        }

        private static void OnProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine("Application is exiting. Cleaning up Electron processes so you dont have to do it.");
            KillElectronProcesses();
        }

        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("CTRL+C detected. Cleaning up Electron processes...");
            KillElectronProcesses();

            e.Cancel = false;
        }




        // Settings file handling
        private static void CheckAndCreateSettingsFile()
        {
            if (!File.Exists(settingsFilePath))
            {
                Console.WriteLine("Settings file not found. Creating a new one...");
                File.WriteAllText(settingsFilePath, @"
# Fill out Twitch information here!

# Fill out your Twitch username here, its case sensitive!
username = 

# Enter your Twitch CHANNEL name here! (its the Name that appears in the Link to your channel!)
channel = 

# Add your Access Token here!
oauthToken = 

# If you don't want to use !gamble, you can change the command here.
# !gambling will stay active, as a guaranteed fallback, and for the memes.
command = 


# Settings for the Slot Machines

# How many Survivors are present? (min 0 max 4)
survivors = 0

# Do you want to enable Killer Perks? (true/false)
killers = false

# Which screen do you want to use? (0 = Primary, 1 Secondary, 2 etc...)
# If you try to use a screen which does not exist, it will default to Primary.
screenSelection = 1

# Where on the screen do you want the Machines to appear? 
# 0 Top-Left , 1 Top-Right, 2 Bottom-Left, 3 Bottom-Right
cornerLoc = 0

# Slotmachine Links
# You can make them here: https://dpsm.3stadt.com
# If no link is provided, ALL perks will be activated. (Default)

# Survivor 1
survivor1 = 

# Survivor 2
survivor2 = 

# Survivor 3
survivor3 = 

# Survivor 4
survivor4 = 

# Killer
killerRoll = 

");
                Console.WriteLine("We made you a Settings.txt file at " + settingsFilePath);
                Console.WriteLine("If you need help with the Twitch setup, check out the README file!");
                Console.WriteLine("Press any key to close the program.");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }

        private static Dictionary<string, string> ReadSettingsFromFile()
        {
            var settings = new Dictionary<string, string>();
            foreach (var line in File.ReadAllLines(settingsFilePath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    settings[parts[0].Trim()] = parts[1].Trim();
                }
            }
            return settings;
        }

    }
}