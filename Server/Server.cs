namespace Server;

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

// entry point
internal static class Program
{
    private static void Main(string[] args)
    {
        // check command args for port number
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: Server {0} <port>", AppDomain.CurrentDomain.FriendlyName);
            Environment.Exit(1);
        }

        var listenPort = int.Parse(args[0]);
        var server = new Server();
        server.Start(listenPort);
    }
}

public class Server
{
    private readonly List<TcpClient> _clients = new(); // list to hold all the Clients

    public void Start(int port)
    {
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"Listening on port {port}");

        // infinite loop for handling client connections
        while (true)
        {
            var client = listener.AcceptTcpClient();

            // get client address info
            var remoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint!;
            var clientHostname = remoteEndPoint.Address.ToString();
            var clientPort = remoteEndPoint.Port;

            Console.WriteLine($"Connected to ({clientHostname}, {clientPort})");
            _clients.Add(client);

            // new thread to handle client communication
            var clientThread = new Thread(() => HandleClient(client));
            clientThread.Start();
        }
    }

    private static void SendMessage(TcpClient client, string message)
    {
        var stream = client.GetStream();
        var messageBytes = Encoding.UTF8.GetBytes(message);
        stream.Write(messageBytes, 0, messageBytes.Length);
    }

    private void HandleClient(TcpClient client)
    {
        try
        {
            var stream = client.GetStream();
            var buffer = new byte[1024];
            var directoryInfo = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent;

            if (directoryInfo != null)
                while (true)
                {
                    // reading message from client
                    var bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead <= 0)
                        break;

                    var clientMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine("Received from client: " + clientMessage);

                    var messageParts = clientMessage.Split(':');

                    // close client here
                    if (messageParts[0] == "Exit")
                    {
                        Console.WriteLine($"Client is logging out.");
                        break;
                    }

                    var username = messageParts[1];

                    var usersFolderPath = Path.Combine(directoryInfo.FullName, "Users");
                    var userFolderPath = Path.Combine(usersFolderPath, username);

                    // define the full file path
                    var infoFilePath = Path.Combine(userFolderPath, username + "-info.txt");
                    var dailyMacrosFilePath = Path.Combine(userFolderPath, username + "-daily-macros.txt");
                    string? data = null;

                    switch (messageParts[0])
                    {
                        case "RegisterUser":
                        {
                            var gender = messageParts[2];
                            var age = messageParts[3];
                            var weight = messageParts[4];
                            var height = messageParts[5];
                            var weightOption = messageParts[6];

                            NewUser(username, gender, age, weight, height, weightOption);
                            break;
                        }
                        case "CheckUsername":
                        {
                            if (File.Exists(infoFilePath))
                            {
                                SendMessage(client, "FileExists");
                            }
                            else
                            {
                                SendMessage(client, "FileDoesntExist");
                                Console.WriteLine($"{infoFilePath} does not exist.");
                            }

                            break;
                        }
                        case "GetMacros":
                        {
                            if (File.Exists(dailyMacrosFilePath))
                            {
                                foreach (var item in File.ReadAllLines(dailyMacrosFilePath)) data = item;

                                if (data == null)
                                {
                                    Console.WriteLine("Error reading data from file.");
                                    return;
                                }
                                else
                                {
                                    SendMessage(client, data);
                                }
                            }
                            else
                            {
                                Console.WriteLine($"{dailyMacrosFilePath} does not exist.");
                            }

                            break;
                        }
                        case "UpdateMacros":
                        {
                            var updatedTotalCalories = messageParts[2];
                            var updatedCarbs = messageParts[3];
                            var updatedProtein = messageParts[4];
                            var updatedFat = messageParts[5];
                            var updatedSugar = messageParts[6];
                            var updatedSaturated = messageParts[7];

                            if (File.Exists(dailyMacrosFilePath))
                                File.WriteAllText(dailyMacrosFilePath, $"{updatedTotalCalories}, {updatedCarbs}, " +
                                                                       $"{updatedProtein}, {updatedFat}, {updatedSugar}, " +
                                                                       $"{updatedSaturated}");
                            else
                                Console.WriteLine($"{dailyMacrosFilePath} does not exist.");

                            break;
                        }
                        case "NewDay":
                        {
                            var filePath = Path.Combine(userFolderPath, username + "-macros.txt");

                            if (File.Exists(filePath))
                            {
                                if (File.Exists(dailyMacrosFilePath))
                                {
                                    foreach (var item in File.ReadAllLines(filePath))
                                    {
                                        Console.WriteLine(item);
                                        data = item;
                                    }

                                    File.WriteAllText(dailyMacrosFilePath, data);


                                    if (data == null)
                                    {
                                        Console.WriteLine("Error reading data from file.");
                                        return;
                                    }
                                    else
                                    {
                                        SendMessage(client, data);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"{dailyMacrosFilePath} does not exist.");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"{filePath} does not exist.");
                            }

                            break;
                        }
                    }
                }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error handling client " + e.Message);
        }
        finally
        {
            client.Close();
            _clients.Remove(client);

            Console.WriteLine("Client disconnected");
        }
    }

    private static void NewUser(string username, string gender, string age, string weight,
        string height, string weightOption)
    {
        // get directory where the project is located
        var projectDirectory = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.FullName;
        if (projectDirectory != null)
        {
            var usersFolderPath = Path.Combine(projectDirectory, "Users"); // define Users folder inside the project
            Directory.CreateDirectory(usersFolderPath); // create Users directory if it doesn't exist

            var userFolderPath = Path.Combine(usersFolderPath, username); // subfolder for the specific user
            Directory.CreateDirectory(userFolderPath); // create username-specific folder if it doesn't exist

            // define file path inside the user's folder
            var filePath = Path.Combine(userFolderPath, username + "-info.txt");

            // write the user info to the file
            File.WriteAllText(filePath, username +
                                        "\n" + gender + "," + age + "," +
                                        weight + "," + height + "," + weightOption);

            Console.WriteLine($"Wrote {username} info to {Path.GetFullPath(filePath)}");
        }
        else
        {
            Console.WriteLine($"{projectDirectory} does not exist.");
        }

        CalculateMacros(username, gender, age, weight, height, weightOption);
    }

    private static void CalculateMacros(string username, string gender, string ageStr, string weightStr,
        string heightStr,
        string weightOptionStr)
    {
        var parentFullName = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.FullName;
        if (parentFullName == null)
        {
            Console.WriteLine($"{parentFullName} does not exist.");
            return;
        }

        var usersFolderPath = Path.Combine(parentFullName, "Users");
        var userFolderPath = Path.Combine(usersFolderPath, username);
        var filePath = Path.Combine(userFolderPath, username + "-macros.txt");
        var dailyMacrosPath = Path.Combine(userFolderPath, username + "-daily-macros.txt");

        // convert to ints
        var age = int.Parse(ageStr);
        var weight = double.Parse(weightStr);
        var height = double.Parse(heightStr);
        var weightOption = int.Parse(weightOptionStr);

        double bmi = 0;

        if (gender == "f")
            bmi = 10 * weight + 6.25 * height - 5 * age - 161;
        else if (gender == "m")
            bmi = 10 * weight + 6.25 * height - 5 * age - 5;

        // assuming moderate activity
        var totalCalories = bmi * 1.55 + weightOption;

        // calculate the macros
        var carbs = (int)(totalCalories * .5 / 4);
        var protein = (int)(totalCalories * .3 / 4);
        var fat = (int)(totalCalories * .2 / 9);
        var sugar = (int)(totalCalories * .05 / 4);
        var satFat = (int)(totalCalories * .06 / 9);

        File.WriteAllText(filePath, $"{Math.Round(totalCalories)},{carbs},{protein},{fat},{sugar},{satFat}");
        File.WriteAllText(dailyMacrosPath, $"{Math.Round(totalCalories)},{carbs},{protein},{fat},{sugar},{satFat}");
        Console.WriteLine($"Wrote macros to {Path.GetFullPath(filePath)}");
    }
}