// ***************************************************
// *                                                 *
// *  IPK Project 1: Client for a chat server        *
// *  Made by: Aleksandrov Vladimir (xaleks03)       *
// *                                                 *
// *  File: TCPClient.cs                             *
// *                                                 *
// ***************************************************

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public enum ConfirmationState {
	Hold,
    Positive,
    Negative
};

public class TCPClient{
    private State state; // State of the client
    private ConfirmationState c_state; // Confirmation state
    private TcpClient tcpClient; // TCP client object
    private NetworkStream tcpStream; // Network stream for sending and receiving data
    private string ip; // IP address of the server
    private int port; // Port number
    private string errorMessage; // Error message

    // Struct to hold user information
    private struct User
    {
        public string username { get; set; }
        public string displayName { get; set; }
        public string secret { get; set; }
    } 
    User user = new(); // Create an instance of the User struct

    // Constructor
    public TCPClient(string ip, int port)
    {
        state = State.AUTH_STATE; // Initial state is AUTH_STATE
        c_state = ConfirmationState.Negative; // Initial confirmation state is Negative
        this.ip = ip; // Assign IP address
        this.port = port; // Assign port number
        tcpClient = new TcpClient(ip, port); // Initialize TCP client
        tcpStream = tcpClient.GetStream(); // Get the network stream
        errorMessage = ""; // Initialize error message
    }

	// Method to send message to the server
    public void SendMessage(string message)
    {
        tcpStream.Write(Encoding.ASCII.GetBytes(message + "\r\n")); // Send message
    }  

    // Method that takes care of the client states
    public void Run()
    {
        // Start a new thread to receive messages asynchronously
        Thread thread = new Thread(async()=>await ReceiveMessage());
        thread.Start();

		// Main loop
        while (true)
        {
            // Handle different states of the client
            if (state == State.AUTH_STATE)
            {
                if (c_state == ConfirmationState.Negative){
                    Auth(); // Perform authentication
                    c_state = ConfirmationState.Hold; // Set confirmation state to Hold
                }   
                else if (c_state == ConfirmationState.Positive){
                    state = State.OPEN_STATE; // Move to OPEN_STATE
                    c_state = ConfirmationState.Negative; // Reset confirmation state
                }
            }
            else if (state == State.OPEN_STATE)
            {
                Open(); // Perform operations in OPEN_STATE
            }
            else if (state == State.ERROR_STATE)
            {
                // Handle error state
                SendMessage("ERR FROM " + user.displayName  +  " IS " + errorMessage); // Send error message to server
                Console.Error.WriteLine("ERR: "+errorMessage); // Print error message to console
                state = State.END_STATE; // Move to END_STATE
            }
            else if (state == State.END_STATE)
            {
                End(); // End the client
                break; // Exit the loop
            }
        }
    }

    // Method to handle authentication process
    public void Auth()
    {
        // Read input from console
        string input = Console.ReadLine() ?? "";

        // Split input by spaces
        string[] inputParts = input.Split(' ');
        
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
        SendMessage("AUTH " + user.username + " AS " + user.displayName + " USING " + user.secret);
    }

    // Method to handle actions in open state
    public void Open()
    {
        // Read input from console
        string? input = Console.ReadLine();
        if (input == null){
            state = State.END_STATE; // If input is null, set state to END_STATE and return
            return;
        }
        if (input.StartsWith("/join"))
        {
            string[] parts = input.Split(' ');
            if (parts.Length == 2) 
            {
                string channelId = parts[1];
                if (c_state == ConfirmationState.Negative){
                    SendMessage("JOIN " + channelId +  " AS " + user.displayName); // Send join message to server
                    c_state = ConfirmationState.Hold; // Set confirmation state to Hold
                }   
                if (c_state == ConfirmationState.Positive){
                    state = State.OPEN_STATE; // Move to OPEN_STATE
                    c_state = ConfirmationState.Negative; // Reset confirmation state
                }
            }
            else
            {
				state = State.ERROR_STATE; // Set state to ERROR_STATE
          		errorMessage = ("Invalid /join command. Format: /join channelID"); // Print error message for invalid /join command
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
			if (parts.Length == 2) 
           	 	user.displayName = parts[1]; // Rename displayed name
			else
            {
				state = State.ERROR_STATE; // Set state to ERROR_STATE
          		errorMessage = ("Invalid /rename command. Format: /rename newName"); // Print error message for invalid /rename command
            }	
        }
        else if (input.StartsWith("/auth"))
        {
            errorMessage ="Double auth"; // Set error message for double authentication
            state = State.ERROR_STATE; // Set state to ERROR_STATE
        }
        else 
        {
            SendMessage("MSG FROM " + user.displayName + " IS " + input); // Send message to server
        }
    }

	// Method to asynchronously receive messages from the server
    public async Task ReceiveMessage()
    {
        try
        {
            byte[] buffer = new byte[1024];
            while (true) 
            {

                int bytesRead = await tcpStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    Console.WriteLine("Connection closed by the remote server.");
                    state = State.END_STATE; // Set state to END_STATE if connection is closed
                }
                string receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);
				
                if (state == State.AUTH_STATE)
                {
					Respond(receivedMessage); // Respond to authentication messages
					if (receivedMessage.StartsWith("ERR"))
                    {
                    	string st = GetStart(receivedMessage,"FROM");
						string en = GetEnd(receivedMessage);
						Console.Error.WriteLine("ERR FROM " + st + ": " + en);
                		state = State.END_STATE; // Set state to END_STATE if authentication error occurs
					}
                }
                else if (state == State.OPEN_STATE)
                {
					Respond(receivedMessage); // Respond to open state messages
                    if (receivedMessage.StartsWith("MSG"))
                    {
                        string st = GetStart(receivedMessage,"FROM");
                        string en = GetEnd(receivedMessage);
                        Console.WriteLine(st + ": " + en); // Print received message
                    }else if (receivedMessage.StartsWith("ERR"))
                    {
                        string st = GetStart(receivedMessage,"FROM");
                        string en = GetEnd(receivedMessage);
                        Console.Error.WriteLine("ERR FROM " + st + ": " + en);
                        state = State.END_STATE; // Set state to END_STATE if error message received
                    }else if (receivedMessage.StartsWith("BYE")){
                        state = State.END_STATE; // Set state to END_STATE if BYE message received
                    }else {
                        errorMessage ="Unknown reply from server.";
                        state = State.ERROR_STATE; // Set state to ERROR_STATE if unknown message received
                    }
					
                }
           		if (state == State.ERROR_STATE)
                {
                    SendMessage("ERR FROM " + user.displayName  +  " IS " + errorMessage); // Send error message
                    Console.Error.WriteLine("ERR: "+errorMessage); // Print error message
                }
                else if (state == State.END_STATE)
                {
                    break; // Exit the loop if state is END_STATE
                }
            }
        }
        catch (Exception ex)
        {
            state = State.ERROR_STATE; // Set state to ERROR_STATE if exception occurs
            errorMessage = $"An error occurred while receiving messages: {ex.Message}"; // Set error message
        }
    }
	
	// Method to respond to server messages
    public void Respond(string receivedMessage){
        if (receivedMessage.StartsWith("REPLY"))
        {
                if (receivedMessage.Contains("NOK"))
                {
                    string st = GetStart(receivedMessage,"REPLY");
                    string en = GetEnd(receivedMessage);
                    Console.Error.WriteLine("Failure: "+en); // Print failure message
                    c_state = ConfirmationState.Negative; // Set confirmation state to Negative
                }
                else if (receivedMessage.Contains("OK"))
                {
                    c_state = ConfirmationState.Positive; // Set confirmation state to Positive
                    string st = GetStart(receivedMessage,"REPLY");
                    string en = GetEnd(receivedMessage);
                    Console.Error.WriteLine("Success: "+en); // Print success message
                }
                else
                {
                    errorMessage ="Unknown reply from server.";
                    state = State.ERROR_STATE; // Set state to ERROR_STATE if unknown reply received
                }
        }
    }
	
	// Method to extract substring after a specified index
    public string GetStart(string receivedMessage, string index){
        int fromIndex = receivedMessage.IndexOf(index);
        int isIndex = receivedMessage.IndexOf("IS", StringComparison.OrdinalIgnoreCase);
        if (fromIndex == -1 || isIndex == -1)
            return "";

        string betweenStrings = receivedMessage.Substring(fromIndex + index.Length + 1, isIndex - (fromIndex + index.Length) - 2);
        return (betweenStrings);
    }
	
	// Method to extract substring after "IS"
    public string GetEnd(string receivedMessage){
        int isIndex = receivedMessage.IndexOf("IS", StringComparison.OrdinalIgnoreCase);

        if (isIndex == -1)
            return "";

        string afterIs = receivedMessage.Substring(isIndex + 3).TrimEnd();

        return afterIs;
    }
	
    // Method to end the client
    public void End()
    {
        SendMessage("BYE"); // Send BYE message
        tcpClient.Close(); // Close TCP client
    }
}