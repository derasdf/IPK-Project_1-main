// ***************************************************
// *                                                 *
// *  IPK Project 1: Client for a chat server        *
// *  Made by: Aleksandrov Vladimir (xaleks03)       *
// *                                                 *
// *  File: Main.cs                                  *
// *                                                 *
// ***************************************************

using System;
using System.Net;
using System.Net.Sockets;

// Enum representing different states of the client
public enum State {
    AUTH_STATE,
    OPEN_STATE,
    ERROR_STATE,
    END_STATE
};

// Class representing the client application
class Client
{
    // Main method
    public static void Main(string[] args)
    {
        try
        {
            string? ipAddressString = null; // IP address string
            int port = 4567; // Default port
            string? protocol = null; // Default protocol is TCP
            int udpConfirmationTimeout = 250; // Default UDP confirmation timeout
            int maxUdpRetransmissions = 3; // Default maximum UDP retransmissions
            bool showHelp = false; // Flag to indicate whether to show help or not

            
            // Parse command-line arguments
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-s" && i + 1 < args.Length)
                {
                    if (!IPAddress.TryParse(args[i + 1], out IPAddress? ipAddress))
                    {
                        // If not a valid IP address, try resolving as hostname
                        IPAddress[] hostAddresses = Dns.GetHostAddresses(args[i + 1]);
                        foreach (IPAddress address in hostAddresses)
                        {
                            if (address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                ipAddressString = address.ToString();
                                break;
                            }
                        }
                        if (ipAddressString == null)
                        {
                            Console.Error.WriteLine($"Failed to resolve hostname '{args[i + 1]}' to an IPv4 address.");
                            return;
                        }
                    }
                    else
                    {
                        ipAddressString = args[i + 1];
                    }
                }
                else if (args[i] == "-p" && i + 1 < args.Length)
                {
                    int.TryParse(args[i + 1], out port);
                }
                else if (args[i] == "-t" && i + 1 < args.Length)
                {
                    protocol = args[i + 1].ToLower();
                    if (protocol != "tcp" && protocol != "udp")
                    {
                        Console.Error.WriteLine("Invalid protocol. Supported protocols are tcp and udp.");
                        return;
                    }
                }
                else if (args[i] == "-d" && i + 1 < args.Length)
                {
                    int.TryParse(args[i + 1], out udpConfirmationTimeout);
                }
                else if (args[i] == "-r" && i + 1 < args.Length)
                {
                    int.TryParse(args[i + 1], out maxUdpRetransmissions);
                }
                else if (args[i] == "-h")
                {
                    showHelp = true;
                }
            }

            if (showHelp)
            {
                ShowHelp(); // Show help message
                return;
            }

            if (ipAddressString == null || port == 0)
            {
                Console.Error.WriteLine("Usage: Client -s <ip/hostname> -p <port> [-t <tcp/udp>] [-d <udpConfirmationTimeout>] [-r <maxUdpRetransmissions>] [-h]");
                return;
            }

            TCPClient? tcpClient = null;
            UDPClient? udpClient = null;
            if (protocol == "tcp")
            {
                tcpClient = new TCPClient(ipAddressString, port);
            }
            else if (protocol == "udp")
            {
                udpClient = new UDPClient(ipAddressString, port, udpConfirmationTimeout, maxUdpRetransmissions);
            }

            // Handle Ctrl+C interruption gracefully
            Console.CancelKeyPress += (sender, eventArgs) => {
                eventArgs.Cancel = true;
                Console.WriteLine("Ctrl+C detected. Exiting gracefully...");
                if (tcpClient != null)
                    tcpClient.End();
                if (udpClient != null)
                    udpClient.End();
                Environment.Exit(0);
            };

            // Run the appropriate client based on the protocol
            if (tcpClient != null)
                tcpClient.Run();
            else if (udpClient != null)
                udpClient.Run();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e); // Print any exception that occurs
        }
    }
    

    // Method to display help message
    private static void ShowHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("Client -s <ip> -p <port> [-t <tcp/udp>] [-d <udpConfirmationTimeout>] [-r <maxUdpRetransmissions>] [-h]");
        Console.WriteLine("-s <ip>: Specifies the IP address to connect to.");
        Console.WriteLine("-p <port>: Specifies the port number to connect to.");
        Console.WriteLine("-t <tcp/udp>: Specifies the protocol type (default is tcp).");
        Console.WriteLine("-d <udpConfirmationTimeout>: Sets the UDP confirmation timeout.");
        Console.WriteLine("-r <maxUdpRetransmissions>: Sets the maximum number of UDP retransmissions.");
        Console.WriteLine("-h: Displays this help message.");
    }
}
