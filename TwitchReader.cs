using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace BWPlayerTwitchMagic

{
    public class TwitchReader
    {
        private const string TwitchIrcUrl = "irc.chat.twitch.tv";
        private const int TwitchIrcPort = 6667;

        private string _username;
        private string _oauthToken;
        private string _channel;
        private bool _connected = false;
        private string GambleCommand = null;

        protected string CleanedMessage { get; set; }
        protected string CleanedUsername { get; set; }
        protected string CleanedChannelPoint { get; set; }
        private ClientWebSocket _webSocket;
        private DateTime _lastGambleCommandTime = DateTime.MinValue; // Tracks the last gamble command time


        public TwitchReader(string username, string oauthToken, string channel, string Command)
        {
            _username = username;
            _oauthToken = oauthToken;
            _channel = channel;
            _webSocket = new ClientWebSocket();

            if (string.IsNullOrWhiteSpace(Command))
            {
                Console.WriteLine("Cant be an empty command! Do not include Spaces in the command.");
                GambleCommand = "!Gamble";
            }
            else
            {
                GambleCommand = Command;
            }
        }

        // Connect to Twitch IRC and read selected channel messages.
    private void MessageCleaner(string message)
    {
        // Extract the message content (everything after the last " :")
        int messageIndex = message.IndexOf(" :");
        if (messageIndex >= 0)
        {
            CleanedMessage = message.Substring(messageIndex + 2); // Skip the " :"
        }

        // Extract the username (at the start of the message, before the first "!")
        int usernameStartIndex = message.IndexOf(':') + 1; // Start after the initial ":"
        int usernameEndIndex = message.IndexOf('!', usernameStartIndex);

        if (usernameStartIndex > 0 && usernameEndIndex > usernameStartIndex)
        {
            CleanedUsername = message.Substring(usernameStartIndex, usernameEndIndex - usernameStartIndex);
        }
    }

        public void ConnectAndReadChat()
        {
            DateTime startTime = DateTime.Now;
            try
            {
                using (var tcpClient = new TcpClient(TwitchIrcUrl, TwitchIrcPort))
                using (var networkStream = tcpClient.GetStream())
                using (var reader = new StreamReader(networkStream))
                using (var writer = new StreamWriter(networkStream) { AutoFlush = true })
                {
                    // Authenticate with Twitch IRC
                    writer.WriteLine($"PASS {_oauthToken}");
                    writer.WriteLine($"NICK {_username}");
                    writer.WriteLine($"JOIN #{_channel}");

                    if (_channel == "bloodwolfplayer")
                    {
                        writer.WriteLine($"You have done it BWPlayer, you are reading your own chat.");
                    }
                    else
                    {
                        Console.WriteLine($"Connected to Twitch chat for channel: {_channel}");
                    }

                    // Read messages from the chat
                    while (true)
                    {
                        if (networkStream.DataAvailable)
                        {
                            var message = reader.ReadLine();
                            if (((DateTime.Now - startTime).TotalSeconds >=5 )&& !_connected)
                            {
                                _connected = true;
                                Console.WriteLine($"Connected to Twitch chat for channel: #{_channel}");
                                Console.WriteLine("Last Message was: " + message);
                            }
                            
                            if (message != null && _connected)
                            {
                                if (message.Contains("PRIVMSG"))
                                {
                                    // Clean the message and username
                                    MessageCleaner(message);
                            
                                    // Ensure CleanedMessage and CleanedUsername are not null
                                    if (!string.IsNullOrEmpty(CleanedMessage) && !string.IsNullOrEmpty(CleanedUsername))
                                    {
                                        Console.WriteLine($"\u001b[32m{CleanedMessage}\u001b[0m \u001b[33m|| Sent by ||\u001b[0m \u001b[36m{CleanedUsername}\u001b[0m");
                            
                                        // Handle !Gamble command
                                        if (CleanedMessage.Contains(GambleCommand, StringComparison.OrdinalIgnoreCase) || 
                                            CleanedMessage.Contains("!gambling", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if ((DateTime.Now - _lastGambleCommandTime).TotalSeconds >= 5)
                                            {
                                                Console.WriteLine("\u001b[92mLets go Gambling!\u001b[0m");
                                                SendRerollCommandToElectron();
                                            }
                                            else
                                            {
                                                Console.WriteLine("\u001b[91mGambling command ignored due to cooldown.\u001b[0m");
                                            }
                                        }
                            
                                        // Handle !restart command
                                        if (CleanedMessage.Equals("!restart", StringComparison.OrdinalIgnoreCase) && 
                                            CleanedUsername.Equals("bloodwolfplayer", StringComparison.OrdinalIgnoreCase))
                                        {
                                            Console.WriteLine("\u001b[93mRestart command received. Restarting the application...\u001b[0m");
                                            //RestartApplication();
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Message or username could not be cleaned. Skipping processing.");
                                    }
                                }
                            
                                // Respond to PING messages to keep the connection alive
                                if (message.StartsWith("PING"))
                                {
                                    writer.WriteLine("PONG :tmi.twitch.tv");
                                }
                            }
                        }

                        Thread.Sleep(100); // Prevent CPU overuse
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {

                if (_webSocket.State == WebSocketState.Open)
                {
                    _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).Wait();
                }
                Console.WriteLine("DbDGambler closed, goodbye!");
            }
        }
        private async void SendRerollCommandToElectron()
        {
            try
            {
                if (_webSocket.State != WebSocketState.Open)
                {
                    await _webSocket.ConnectAsync(new Uri("ws://localhost:8080"), CancellationToken.None);
                }
                string rerollCommand = "reroll";
                var messageBuffer = Encoding.UTF8.GetBytes(rerollCommand);
                await _webSocket.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                Console.WriteLine("Reroll command sent to Electron app.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending reroll command to Electron: {ex.Message}");
            }
        }
    }
}