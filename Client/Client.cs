namespace Client;

using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

internal static class Client
{
    private static readonly ConcurrentQueue<string?> MessageQueue = new();

    private static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: client <ip> <port>");
            Environment.Exit(1);
        }

        var host = args[0];
        var port = int.Parse(args[1]);

        var client = new TcpClient(host, port);
        var receiveThread = new Thread(() => ReceiveMessage(client));
        receiveThread.Start();
        ClientStart(client);
    }

    private static void ReceiveMessage(TcpClient client)
    {
        try
        {
            var stream = client.GetStream();
            var buffer = new byte[256];

            while (true)
            {
                // read message from the server
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                    break;

                var serverMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                
                MessageQueue.Enqueue(serverMessage);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error receiving message from server: " + ex.Message);
        }
    }
    
    private static string ServerMessageQueue()
    {
        string? message = null;
        
        while (MessageQueue.TryDequeue(out _)) 
        { } // empty the queue
        
        while (message == null)
        {
            if (MessageQueue.TryDequeue(out var receivingMessage))
                message = receivingMessage; // retrieve the response
        }
        
        return message;
    }

    private static void SendMessage(TcpClient client, string message)
    {
        var stream = client.GetStream();
        var messageBytes = Encoding.ASCII.GetBytes(message);
        stream.Write(messageBytes, 0, messageBytes.Length);
    }

    private static void ClientStart(TcpClient client)
    {
        // getting network stream to send messages to the server
        var stream = client.GetStream();

        while (true)
        {
            int option = Menu();
            
            // if user exits, break out of loop
            if (option == 3)
                break;

            switch (option)
            {
                case 1:
                    LogIn(client);
                    break;
                case 2:
                    RegisterUser(client);
                    break;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
        
        string input = "Exit";
        var messageBytes = Encoding.ASCII.GetBytes(input);
        stream.Write(messageBytes, 0, messageBytes.Length);
    }

    private static int Menu()
    {
        Console.WriteLine("Main Menu: ");
        Console.WriteLine("1. Login");
        Console.WriteLine("2. Register");
        Console.WriteLine("3. Exit");
        Console.Write("Please enter an option: ");
        
        int input = Convert.ToInt16(Console.ReadLine());
        Console.WriteLine();

        // check if valid input
        while (input < 1 || input > 3)
        {
            Console.WriteLine("Invalid input.");
            Console.Write("Please enter an option [1, 2, 3]: ");
    
            input = Convert.ToInt16(Console.ReadLine());
            Console.WriteLine();
        }

        return input;
    }

    // assuming exercise is active: daily or intense 3-4 times a week
    private static void RegisterUser(TcpClient client)
    {
        Console.Write("Please enter a username: ");
        var stringInput = Console.ReadLine() ?? string.Empty;
        var username = EmptyStringCheck(stringInput, "username");

        Console.Write("Please enter your gender [m/f]: ");
        stringInput = Console.ReadLine() ?? string.Empty;

        var genderInput = EmptyStringCheck(stringInput, "gender [m/f]");
        var gender = Convert.ToChar(genderInput);
        gender = char.ToLower(gender);

        if (gender != 'm' && gender != 'f')
        {
            Console.WriteLine("Invalid gender. Please try again.\n");
            return;
        }

        Console.Write("Please enter your age: ");
        var intInput = Console.ReadLine() ?? string.Empty;
        var age = IntCheck(intInput);

        Console.Write("Please enter your weight: ");
        intInput = Console.ReadLine() ?? string.Empty;
        
        var weightInPounds = IntCheck(intInput);
        double weight = weightInPounds / 2.2046; // convert weight to kg
        weight = Math.Round(weight, 2);

        Console.Write("Please enter your height (e.g., 5'0): ");
        stringInput = Console.ReadLine() ?? string.Empty;
        
        string heightInFeet = EmptyStringCheck(stringInput, "height (e.g., 5'7)");

        // convert height to cm
        string[] heightParts = heightInFeet.Split("'");

        while (heightParts.Length != 2)
        {
            Console.Write("Invalid input. Please enter a valid height (e.g., 5'7): ");
            stringInput = Console.ReadLine() ?? string.Empty;
            
            heightInFeet = EmptyStringCheck(stringInput, "height");
            heightParts = heightInFeet.Split("'");
        }
        
        double height = ((double.Parse(heightParts[0]) * 12) + double.Parse(heightParts[1])) * 2.54;

        double option = WeightOptions();

        if (option == -1)
            return;
        
        // join strings to send to server to update in file
        string message = string.Join(":", username, gender,
            age, weight, height, option);
        
        SendMessage(client, $"RegisterUser:{message}");
        InputToContinue();
    }

    private static double WeightOptions()
    {
        double option;
        
        Console.WriteLine("\nPlease choose an option: ");
        Console.WriteLine("1. Lose weight (0.5 pounds)");
        Console.WriteLine("2. Lose weight (1 pound)");
        Console.WriteLine("3. Maintain weight");
        Console.WriteLine("4. Gain weight (0.5 pounds)");
        Console.WriteLine("5. Gain weight (1 pound)");
        Console.Write("Please enter an option: ");
        
        var inputStr = Console.ReadLine() ?? string.Empty;
        var input = IntCheck(inputStr);

        switch (input)
        {
            case 1:
                option = -250;
                break;
            case 2:
                option = -500;
                break;
            case 3:
                option = 0;
                break;
            case 4:
                option = 250;
                break;
            case 5:
                option = 500;
                break;
            default:
                Console.WriteLine("Invalid input. Returning to main menu.");
                option = -1;
                break;
        }

        Console.WriteLine();
        return option;
    }

    private static void LogIn(TcpClient client)
    {
        Console.Write("Please enter your username: ");
        var username = Console.ReadLine() ?? string.Empty;
        
        bool usernameExists = CheckUsername(client, username);

        if (!usernameExists)
            return;

        while (true)
        {
            Console.WriteLine("1. View macros");
            Console.WriteLine("2. Add data");
            Console.WriteLine("3. Start new day");
            Console.WriteLine("4. Log out");
            Console.Write("Please enter an option: ");
            var inputStr = Console.ReadLine() ?? string.Empty;

            while (string.IsNullOrEmpty(inputStr))
            {
                Console.Write("Please enter a valid option: ");
                inputStr = Console.ReadLine() ?? string.Empty;
            }

            int input = Convert.ToInt32(inputStr);

            Console.WriteLine();

            if (input == 4)
            {
                Console.WriteLine("Logging out.");
                break;
            }
            
            SendMessage(client, $"GetMacros:{username}");
            
            string serverMessage = ServerMessageQueue();

            string[] messageParts = serverMessage.Split(",");

            var totalCalories = double.Parse(messageParts[0]);
            var carbs = double.Parse(messageParts[1]);
            var protein = double.Parse(messageParts[2]);
            var fat = double.Parse(messageParts[3]);
            var sugar = double.Parse(messageParts[4]);
            var satFat = double.Parse(messageParts[5]);

            switch (input)
            {
                case 1:
                {
                    Console.WriteLine($"Total Calories: {totalCalories} Calories/day");
                    Console.WriteLine($"Carbs: {carbs} grams/day");
                    Console.WriteLine($"Proteins: {protein} grams/day");
                    Console.WriteLine($"Fat: {fat} grams/day");
                    Console.WriteLine($"Sugar: {sugar} grams/day");
                    Console.WriteLine($"Saturated Fat: {satFat} grams/day");

                    InputToContinue();
                    break;
                }
                case 2:
                {
                    Console.WriteLine("Please update everything accordingly.");
                    Console.WriteLine("If you do not know/no change, enter 0.\n");

                    Console.Write("Total calories: ");
                    var updatedTotalCalories = Convert.ToDouble(Console.ReadLine());
                    updatedTotalCalories = totalCalories - updatedTotalCalories;

                    Console.Write("Carbs: ");
                    var updatedCarbs = Convert.ToDouble(Console.ReadLine());
                    updatedCarbs = carbs - updatedCarbs;

                    Console.Write("Protein: ");
                    var updatedProtein = Convert.ToDouble(Console.ReadLine());
                    updatedProtein = protein - updatedProtein;

                    Console.Write("Fat: ");
                    var updatedFat = Convert.ToDouble(Console.ReadLine());
                    updatedFat = fat - updatedFat;

                    Console.Write("Sugar: ");
                    var updatedSugar = Convert.ToDouble(Console.ReadLine());
                    updatedSugar = sugar - updatedSugar;

                    Console.Write("Saturated fat: ");
                    var updatedSaturated = Convert.ToDouble(Console.ReadLine());
                    updatedSaturated = satFat - updatedSaturated;

                    Console.WriteLine();
                    Console.WriteLine("Your macronutrients left: ");
                    Console.WriteLine($"Total Calories: {updatedTotalCalories} Calories left");
                    Console.WriteLine($"Carbs: {updatedCarbs} grams left");
                    Console.WriteLine($"Proteins: {updatedProtein} grams left");
                    Console.WriteLine($"Fat: {updatedFat} grams left");
                    Console.WriteLine($"Sugar: {updatedSugar} grams left");
                    Console.WriteLine($"Saturated Fat: {updatedSaturated} grams left");

                    SendMessage(client, $"UpdateMacros:{username}:" +
                                        $"{updatedTotalCalories}:{updatedCarbs}:" +
                                        $"{updatedProtein}:{updatedFat}:{updatedSugar}:" +
                                        $"{updatedSaturated}");

                    InputToContinue();
                    break;
                }
                case 3:
                {
                    Console.WriteLine("Sending data...");
                    SendMessage(client, $"NewDay:{username}");

                    Console.WriteLine("Starting a new day!");
                    InputToContinue();
                    break;
                }
                default:
                    Console.WriteLine("Please enter a valid option.\n");
                    break;
            }

        }

    }

    private static bool CheckUsername(TcpClient client, string username)
    {
        SendMessage(client, $"CheckUsername:{username}");

        string serverMessage = ServerMessageQueue();

        switch (serverMessage)
        {
            case "FileExists":
                return true;
            case "FileDoesntExist":
                Console.WriteLine("No account found. Please register an account.\n");
                return false;
            default:
                Console.WriteLine("Error receiving message from server.");
                return false;
        }
    }
    
    private static void InputToContinue()
    {
        Console.Write("\nPress [enter] to continue!\n\n");
        Console.ReadLine();
    }

    private static string EmptyStringCheck(string? input, string message)
    {
        while (string.IsNullOrWhiteSpace(input))
        {
            Console.Write($"Error: Please enter a valid {message}: ");
            input = Console.ReadLine();
        }

        return input;
    }

    private static int IntCheck(string? input)
    {
        int result;
        EmptyStringCheck(input, "input");
        
        while (!int.TryParse(input, out result)) // validate the input
        {
            Console.Write("Invalid input. Please try again: ");
            input = Console.ReadLine(); // prompt for new input
        }
        
        return result;
    }
}