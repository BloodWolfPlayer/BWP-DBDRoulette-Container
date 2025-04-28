using System;
using System.IO;
using System.Diagnostics;

namespace BWPlayerTwitchMagic
{
    class Program
    {
        static string currentFolderPath = Path.GetFullPath(Directory.GetCurrentDirectory());
        static void Main(string[] args)
        {
            // Hook into the ProcessExit event to clean up Electron processes
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            // Hook into the CancelKeyPress event to handle CTRL+C
            Console.CancelKeyPress += OnCancelKeyPress;

            // Start the Electron app (DBDElectronGambler.js)
            StartElectronApp();

            string username = "BloodWolfPlayer";
            string oauthToken = "oauth:rn7fi3fscntdh5jie7ctzw25rgq15l";
            string channel = "bloodwolfplayer";

            var twitchReader = new TwitchReader(username, oauthToken, channel);
            twitchReader.ConnectAndReadChat();
        }

        private static void StartElectronApp()
        {
            try
            {
                // Kill any existing Electron processes
                KillElectronProcesses();
        
                // Full path to npx
                string command = @"C:\Program Files\nodejs\npx.cmd";
                string arguments = "electron DBDElectronGambler.js";
        

                // Figure out where DBDElectronGambler.js is located.
                string[] commonFolders = {
                    currentFolderPath,
                    Path.Combine(currentFolderPath, @".."),
                    Path.Combine(currentFolderPath, @"JavaScripts\"),
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
        
                Process process = Process.Start(startInfo);
        
                if (timeoutMs > 0)
                {
                    // Wait for the process to start or timeout
                    if (!process.WaitForExit(timeoutMs))
                    {
                        Console.WriteLine($"Timeout reached while launching Electron app from: {workingDirectory}");
                        process.Kill(); // Kill the process if it exceeds the timeout
                        return false;
                    }
                }
        
                Console.WriteLine($"Electron app launched successfully from: {workingDirectory}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to launch Electron app from: {workingDirectory}. Error: {ex.Message}");
                return false;
            }
        }

        private static void KillElectronProcesses()
        {
            try
            {
                // Get all processes named "electron"
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

            // Allow the process to terminate
            e.Cancel = false;
        }
    }
}