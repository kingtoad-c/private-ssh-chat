using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using System.Collections.Generic;

public class TextingApp
{
    private static string username;
    private static TcpListener server;
    private static TcpClient client;
    private static NetworkStream stream;
    private static StreamReader reader;
    private static StreamWriter writer;
    private static Thread listenerThread;
    private static Thread updateConsoleThread;
    private static string encryptionKey = "0123456789abcdef";
    private static string iv = "abcdef9876543210";

    private static List<string> messageList = new List<string>();
    private static string currentMessage = "";


    public static void Main()
    {
        tune();
        string asciiArt = @"
+-----------------------------------------------------+
|                             _        _              |
|   ___ __   __ ___  _ __  __| | _ __ (_)__   __ ___  |
|  / _ \\ \ / // _ \| '__|/ _` || '__|| |\ \ / // _ \ |
| | (_) |\ V /|  __/| |  | (_| || |   | | \ V /|  __/ |
|  \___/  \_/  \___||_|   \__,_||_|   |_|  \_/  \___| |
+-----------------------------------------------------+
by enom v1.4
";
        Console.Clear();
        Console.Title = $"Atc - loged out";
        Console.Write("Enter your username: ");
        username = Console.ReadLine();
        Console.Title = $"Atc - {username}";
        if (username == "admin")
        {
            Console.Write("enter your password:");
            string pass = Console.ReadLine();
            if (pass == "root")
            {
                Console.WriteLine("correct");
            }
            else
            {
                return;
            }
        }
        while (true)
        {
            Console.Clear();
            Console.WriteLine(asciiArt);
            Console.WriteLine("\nChoose an option:");
            Console.WriteLine("1. Host Server");
            Console.WriteLine("2. Connect to Server");
            Console.WriteLine("3. theme");
            Console.Write("Enter your choice (1 or 2): ");
            string choice = Console.ReadLine();

            if (choice == "1")
            {
                StartServer();
            }
            else if (choice == "2")
            {
                ConnectToServer();
            }
            else if (choice == "3")
            {
                SetChatTheme();
            }
            else
            {
                Console.WriteLine("Invalid choice.");
            }
        }
    }
    
    public static void tune()
    {
        Console.Beep(400, 200);
        Console.Beep(600, 200);
        Console.Beep(300, 200);
        Console.Beep(600, 200);
        Console.Beep(300, 100);
        Console.Beep(300, 100);
        Console.Beep(600, 200);
    }
    public static void SetChatTheme()
    {
        Console.WriteLine("Choose a color theme: (1) Dark (2) Light (3) green (4) purple/blue (5) red");
        if (username == "admin")
        {
            Console.WriteLine(">hidden admin theme type 99");
        }
        string themeChoice = Console.ReadLine();
        if (username == "admin")
        {
            if (themeChoice == "99")
            {
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.ForegroundColor = ConsoleColor.Black;
            }
        }
        if (themeChoice == "1")
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
        }
        else if (themeChoice == "2")
        {
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
        }
        else if (themeChoice == "3")
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Green;
        }
        else if (themeChoice == "4")
        {
            Console.BackgroundColor = ConsoleColor.Magenta;
            Console.ForegroundColor = ConsoleColor.Blue;
        }
        else if (themeChoice == "5")
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Red;
        }
        Console.Clear();
    }

    private static void StartServer()
    {
        try
        {
            string localIp = GetLocalIpAddress();
            server = new TcpListener(IPAddress.Parse(localIp), 8888);
            server.Start();
            Console.WriteLine($"\nServer started on IP {localIp} and Port 8888...");
            Console.WriteLine("Waiting for a client to connect...");
            Console.Title = $"Atc {username} | Waiting for client...";
            client = server.AcceptTcpClient();
            stream = client.GetStream();
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream);
            Console.WriteLine("Client connected!");
            Console.Title = $"Atc {username} | Connected";
            listenerThread = new Thread(ListenForMessages);
            listenerThread.Start();
            updateConsoleThread = new Thread(UpdateConsoleOutput);
            updateConsoleThread.Start();
            SendMessages();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting server: {ex.Message}");
        }
    }

    private static string GetLocalIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }

    private static void ConnectToServer()
    {
        Console.Write("Enter the server IP address (e.g., 127.0.0.1): ");
        string serverIp = Console.ReadLine();

        try
        {
            client = new TcpClient(serverIp, 8888);
            stream = client.GetStream();
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream);
            Console.WriteLine($"Connected to server at {serverIp}!");
            Console.Title = $"Atc {username} | Connected to {serverIp}";
            listenerThread = new Thread(ListenForMessages);
            listenerThread.Start();
            updateConsoleThread = new Thread(UpdateConsoleOutput);
            updateConsoleThread.Start();
            SendMessages();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to server: {ex.Message}");
            Console.WriteLine("Retrying in 5 seconds...");
            Thread.Sleep(5000);
            ConnectToServer();
        }
    }

    private static void ListenForMessages()
    {
        while (true)
        {
            try
            {
                string encryptedMessage = reader.ReadLine();
                if (encryptedMessage != null)
                {
                    string decryptedMessage = DecryptMessage(encryptedMessage);
                    string[] parts = decryptedMessage.Split(':');
                    if (parts.Length >= 2)
                    {
                        lock (messageList)
                        {
                            messageList.Add($"{parts[0]}: {parts[1]}");
                        }
                        currentMessage = "";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving message: {ex.Message}");
                break;
            }
        }
    }

    private static void SendMessages()
    {
        while (true)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write($"{username}: {currentMessage}");

            ConsoleKeyInfo keyInfo = Console.ReadKey(true);
            char keyChar = keyInfo.KeyChar;

            if (keyInfo.Key == ConsoleKey.Enter)
            {
                string message = currentMessage;
                if (!string.IsNullOrEmpty(message))
                {
                    string formattedMessage = $"{username}: {message}";
                    string encryptedMessage = EncryptMessage(formattedMessage);
                    writer.WriteLine(encryptedMessage);
                    writer.Flush();
                    lock (messageList)
                    {
                        messageList.Add(formattedMessage);
                    }
                    currentMessage = "";
                }
            }
            else if (keyInfo.Key == ConsoleKey.Backspace && currentMessage.Length > 0)
            {
                currentMessage = currentMessage.Substring(0, currentMessage.Length - 1);
            }
            else if (!char.IsControl(keyChar))
            {
                currentMessage += keyChar;
            }
        }
    }


    private static string EncryptMessage(string message)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Encoding.UTF8.GetBytes(encryptionKey);
            aesAlg.IV = Encoding.UTF8.GetBytes(iv);

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(message);
                    }
                }
                return Convert.ToBase64String(msEncrypt.ToArray());
            }
        }
    }

    private static string DecryptMessage(string encryptedMessage)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Encoding.UTF8.GetBytes(encryptionKey);
            aesAlg.IV = Encoding.UTF8.GetBytes(iv);

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(encryptedMessage)))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
        }
    }

    private static void UpdateConsoleOutput()
    {
        int lastMessageCount = 0;

        while (true)
        {
            Thread.Sleep(100);

            bool shouldUpdate = false;

            lock (messageList)
            {
                if (messageList.Count != lastMessageCount)
                {
                    shouldUpdate = true;
                    lastMessageCount = messageList.Count;
                }
            }

            if (shouldUpdate)
            {
                Console.Beep();
                int currentCursorTop = Console.CursorTop;
                int currentCursorLeft = Console.CursorLeft;

                Console.Clear();
                lock (messageList)
                {
                    foreach (var message in messageList)
                    {
                        Console.WriteLine(message);
                    }
                }
                Console.Write($"{username}: {currentMessage}");
                Console.SetCursorPosition($"{username}: {currentMessage}".Length, messageList.Count);
            }
        }
    }

}

