// ***************************************************
// *                                                 *
// *  IPK Project 1: Client for a chat server        *
// *  Made by: Aleksandrov Vladimir (xaleks03)       *
// *                                                 *
// *  File: UDPClient.cs                             *
// *                                                 *
// ***************************************************

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;


public class UDPClient {
    private State state; // State of the UDP client
    private Socket clientUdpSocket; // UDP client socket
    private System.Int16 messageID; // Message ID
    private int timeoutTime; // Timeout time
    private int retryCount; // Retry count
    private string errorMessage; // Error message
    private byte[]? savedMessage; // Saved message
    private bool messageReceived; // Flag to indicate if message received
    EndPoint remoteEndpoint; // Remote endpoint

    private struct User {
        public string username { get; set; }
        public string displayName { get; set; }
        public string secret { get; set; }
    }
    User user = new(); // User struct to hold user information

    // Constructor
    public UDPClient(string ip, int port, int timeoutTime, int retryCount) {
        state = State.AUTH_STATE; // Initial state is AUTH_STATE
        clientUdpSocket  = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp); // UDP client socket
        this.timeoutTime = timeoutTime; // Assign timeout time
        this.retryCount = retryCount; // Assign retry count
        IPAddress ipAddress = IPAddress.Parse(ip); // Parse IP address
        remoteEndpoint = new IPEndPoint(ipAddress, port); // Create remote endpoint
        clientUdpSocket.Bind(new IPEndPoint(IPAddress.Any, 0)); // Bind client socket
        savedMessage = null; // Initialize saved message to null
        messageReceived = false; // Initialize message received flag to false
        messageID = 0x0000; // Initialize message ID
        errorMessage = ""; // Initialize error message
    }

    // Method to start the client
    public void Run()
    {
        Thread thread = new Thread(async()=>await ReceiveMessage()); // Start a new thread to receive messages asynchronously
        thread.Start();
        while (true)
        {
            
            if (state == State.AUTH_STATE)
            {
                Console.WriteLine("Please enter your authentication details in the following format:");
                Console.WriteLine("/auth Username Nick_Name Secret");
                Console.WriteLine("Press Enter after inputting your details.");
			   	messageReceived = WaitForConfirmation(() =>  Auth()); // Wait for confirmation after authentication
                if (!messageReceived)
                {
                    errorMessage ="Message not confirmed"; // Set error message if message not confirmed
                    state = State.ERROR_STATE; // Set state to ERROR_STATE
                }else{ state = State.OPEN_STATE; }

            }
            else if (state == State.OPEN_STATE)
            {
                Open(); // Perform operations in OPEN_STATE
            }
            else if (state == State.ERROR_STATE)
            {
                Console.Error.WriteLine("ERR: "+errorMessage); // Print error message
                ConfirmMessage(); // Confirm message
                SendMessage(0xFE, ConvertMessage() + user.displayName + "\x00" + errorMessage + "\x00"); // Send error message
                state = State.END_STATE; // Set state to END_STATE
            }
            else if (state == State.END_STATE)
            {
                End(); // End the client
                break; // Exit the loop
            }
        }
    }

    // Method to authenticate the client
    public void Auth()
    {
        string input = Console.ReadLine() ?? ""; // Read input from console

        string[] inputParts = input.Split(' '); // Split input by spaces
        
        // Check if input format is valid
        if (inputParts.Length != 4 || inputParts[0] != "/auth")
        {
            state = State.ERROR_STATE; // Set state to ERROR_STATE
            errorMessage = "Invalid input format. Please enter the details in the format: /auth Username Nick_Name Secret"; // Set error message
            return;
        }
        
        // Assign user details from input
        user.username = inputParts[1];
        user.secret = inputParts[2];
        user.displayName = inputParts[3];
        
        // Send authentication message to server
        SendMessage(0x02, ConvertMessage()+ user.username + "\x00" + user.displayName + "\x00" + user.secret + "\x00");
    }

    // Method to handle operations in the open state
    public void Open()
    {
        string? input = Console.ReadLine(); // Read input from console
        if (input == null){
            state = State.END_STATE; // If input is null, set state to END_STATE and return
            return;
        }
        if (input.StartsWith("/join"))
        {
            string[] parts = input.Split(' ');
            if (parts.Length == 2) 
            {
                // Wait for confirmation after sending join message
                bool messageReceived = WaitForConfirmation(() =>  SendMessage(0x03,ConvertMessage() + parts[1] + "\x00" + user.displayName + "\x00"));
                if (!messageReceived)
                {
                    errorMessage ="Message not confirmed"; // Set error message if message not confirmed
                    state = State.ERROR_STATE; // Set state to ERROR_STATE
                }
        
            }
            else
            {
                Console.WriteLine("Invalid /join command. Format: /join channelID"); // Print error message for invalid /join command
            }
        }
        else if (input.StartsWith("/help"))
        {
            // Print help message
            Console.WriteLine("Chat usage:\n");
            Console.WriteLine("/auth {Username} {Secret} {DisplayName} - Authentication. \n");
            Console.WriteLine("/join {ChannelID} - Join channel with provided id. \n");
            Console.WriteLine("/rename 	{DisplayName} - Rename displayed name. \n");
            Console.WriteLine("/help - Prints this message. \n");
        }
        else if (input.StartsWith("/rename"))
        {
            string[] parts = input.Split(' ');  
            user.displayName =  parts[1]; // Rename displayed name
        }
        else if (input.StartsWith("/auth"))
        {
            errorMessage ="Double auth"; // Set error message for double authentication
            state = State.ERROR_STATE; // Set state to ERROR_STATE
        }
        else 
        {
            // Wait for confirmation after sending message
            messageReceived = WaitForConfirmation(() => SendMessage(0x04, ConvertMessage() + user.displayName + "\x00" + input + "\x00") );
            if (!messageReceived)
            {
                errorMessage ="Message not confirmed"; // Set error message if message not confirmed
                state = State.ERROR_STATE; // Set state to ERROR_STATE
            }
        }
    }
    
    // Method to wait for confirmation from server
    public bool WaitForConfirmation(Action sendMessageFunc)
    {
        messageReceived = false; // Set message received flag to false
        for (int i = 0; i <= retryCount; i++)
        {
            if (state == State.ERROR_STATE)
                break;
            savedMessage = null;
            sendMessageFunc(); // Invoke the function provided as argument
            Thread.Sleep(timeoutTime); // Sleep for specified timeout time
            if (savedMessage != null && state != State.ERROR_STATE)
            {
                messageReceived = true; // Set flag to true if message is received
                break;
            }
        }
        return messageReceived; // Return message received flag
    }

    // Method to convert message ID to string
    public string ConvertMessage(){
        byte msb = (byte)((messageID >> 8) );
        byte lsb = (byte)(messageID); 
        return Encoding.ASCII.GetString(new byte[] { msb, lsb });
    }

    // Method to send message to server
    public void SendMessage(byte bt,string message)
    {
        byte[] messageBytes = Encoding.ASCII.GetBytes(message); // Convert message to bytes and add new first element
        byte[] sendData = new byte[messageBytes.Length + 1];
        sendData[0] = bt;
        Array.Copy(messageBytes, 0, sendData, 1, messageBytes.Length);

        // Send the sendData array
        if (state == State.AUTH_STATE){
            clientUdpSocket.SendTo(sendData,remoteEndpoint); // Send message to remote endpoint
        }else
            clientUdpSocket.Send(sendData, SocketFlags.None); // Send message
        
    }   

    // Method to confirm message reception
    public void ConfirmMessage()
    {
        if (savedMessage != null){
            byte[] data = Encoding.ASCII.GetBytes("\x00"+ConvertMessage());
            clientUdpSocket.Send(data, SocketFlags.None); // Send confirmation message
            messageID++;
            savedMessage = null;
        }else{
            Console.Error.WriteLine("No confirm received"); // Print error message if no confirmation received
        }
    }   

    // Method to receive messages asynchronously
    public async Task ReceiveMessage()
    {
        try
        {
	        byte[] buffer = new byte[4096]; // Buffer to store received data
            while (true) 
            {
                int bytesRead = await clientUdpSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None); // Receive data asynchronously
                byte[] receivedBytes = new byte[bytesRead]; 
                Array.Copy(buffer, receivedBytes, bytesRead); // Copy received bytes to array

                string receivedMessage = Encoding.ASCII.GetString(receivedBytes); // Convert received bytes to string
                
                if (state == State.AUTH_STATE ){
					Respond(receivedBytes); // Respond to received message
                    SocketReceiveFromResult result = await clientUdpSocket.ReceiveFromAsync(new ArraySegment<byte>(buffer), SocketFlags.None, remoteEndpoint); // Receive data from remote endpoint
                    byte[] receivedMessage2 = new byte[result.ReceivedBytes]; // trims the buffer
                    Array.Copy(buffer, receivedMessage2, result.ReceivedBytes);
                    EndPoint newEndpoint = result.RemoteEndPoint; // gets the address of server
                    clientUdpSocket.Connect(newEndpoint); // Connect to server
                    Respond(receivedMessage2); // Respond to received message
                }
                if (state == State.OPEN_STATE)
                {
					Respond(receivedBytes); // Respond to received message
                }
                if (state == State.ERROR_STATE)
                {
                    Console.Error.WriteLine("ERR: "+errorMessage); // Print error message
                    ConfirmMessage(); // Confirm message
                    SendMessage(0xFE,ConvertMessage() + user.displayName + "\x00" + errorMessage + "\x00"); // Send error message
                    state = State.END_STATE; // Set state to END_STATE
                }
                else if (state == State.END_STATE)
                {
                    // Handle END_STATE 
                    End(); // End the client
                    break; // Exit the loop
                }
            }
        }
        catch (Exception ex)
        {
			state = State.ERROR_STATE; // Set state to ERROR_STATE
			errorMessage = $"An error occurred while receiving messages: {ex.Message}"; // Set error message
        }
    }
    
	// Method to respond to received message
	public void Respond(byte[] receivedBytes){
		// Check if the received message starts with the correct header
        string receivedMessage = Encoding.ASCII.GetString(receivedBytes); // Convert received bytes to string
        
        // Check message header and handle accordingly
        if(receivedBytes[0] == 0x00 ){
            savedMessage = receivedBytes;
        }
        else if(receivedBytes[0] == 0x01 && receivedBytes.Length >= 8){
            // Extract fields from the received message
            byte result = receivedBytes[3]; // Assuming the result byte is at index 2
            // Extract other fields as needed

            // Determine the confirmation state based on the result field
            switch (result)
            {
                case 0x00: // Assuming 0x01 indicates failure
                    Console.Error.WriteLine("Failure: " + GetMessageContents(receivedBytes)); // Print failure message
                    messageReceived = false; // Set message received flag to false
                    ConfirmMessage(); // Confirm message
                    break;
                case 0x01: // Assuming 0x00 indicates success
                    Console.Error.WriteLine("Success: " +  GetMessageContents(receivedBytes)); // Print success message
                    messageReceived = true; // Set message received flag to true
                    ConfirmMessage(); // Confirm message
                    break;
                default:
                    errorMessage = "Unknown reply from server.1"; // Set error message
                    state = State.ERROR_STATE; // Set state to ERROR_STATE
                    break;
            }
        }
        else if (receivedBytes[0] == 0x04)
        {
            //Message
            Console.WriteLine(GetStart(receivedMessage,3)); // Print message
            ConfirmMessage(); // Confirm message
        }else if (receivedBytes[0] == 0xFE)
        {
            //Error
            System.Int16 check = (System.Int16)((receivedBytes[1] << 8) | receivedBytes[2]); // Check message ID
            messageID=check;
            Console.Error.WriteLine("ERR FROM " + GetStart(receivedMessage,3)); // Print error message
            ConfirmMessage(); // Confirm message
            state = State.END_STATE; // Set state to END_STATE
        }
        else {
            errorMessage ="Unknown reply from server.3"; // Set error message
            state = State.ERROR_STATE; // Set state to ERROR_STATE
        }
	}
    
    // Method to extract message contents from received message
    private string GetMessageContents(byte[] receivedMessage)
    {
        string messageString = Encoding.ASCII.GetString(receivedMessage);
        return messageString.Substring(6);
    }

    // Method to extract start of message
	public string GetStart(string receivedMessage, int index)
    {
        // Find the index of the first occurrence of \x00 after the index symbols
        int nullIndex = -1;
        for (int i = index; i < receivedMessage.Length - 1; i++)
        {
            if (receivedMessage[i] == '\x00')
            {
                nullIndex = i;
                break;
            }
        }

        // If no \x00 found after the index symbols, return empty string
        if (nullIndex == -1)
            return "";

        // Extract the substring until the first \x00 after the index symbols
        string betweenStrings = receivedMessage.Substring(index, nullIndex-index);
        string afterStrings = receivedMessage.Substring(nullIndex+1);
        return betweenStrings + ": " +afterStrings;
    }

    // Method to end the client
    public void End()
    {
        SendMessage(0xFF,ConvertMessage()); // Send end message
        clientUdpSocket.Close(); // Close the UDP socket
    }
}
