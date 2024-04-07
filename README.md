# IPK Project 1: Client for a Chat Server

## Introduction

This documentation outlines the implementation details and testing procedures for the IPK Project 1, involving the creation of a client for a chat server using the IPK24-CHAT protocol. The application supports both TCP and UDP transport protocols with IPv4 network compatibility.

## Run

First compile server by entering following command to terminal
```console
$ make
```
Next you can run the server by using:
```console
$ ./ipk24chat-client -t <udp/tcp> -s <ip> -p <port> -d <Confirmation timeout> -r <Retry count>
```

Where:
- udp/tcp - protocol for usage
- ip - ip or hostname
- port - optional port number
- Confirmation timeout - optional amount of time that client waits a response
- Retry count - optional amount of retries before error


## Implementation

Implementation consists of files contained in this repository.


Those are:
- ***Main.cs*** - File containing `main` function and being responsible for argument parsing and starting the client with propper mode.
- ***UDPClient.cs*** - Module responsible for client UDP side.
- ***TCPClient.cs*** - Module responsible for client TCP side.


### TCP Implementation

- Upon initializing the TCP client, the constructor performs the following tasks:
  - Initializes the TCP client object.
  - Retrieves the network stream for sending and receiving data.
  - Initializes the IP address and port number.
  - Sets the initial state and confirmation state.
  - Initializes the error message.

- The `SendMessage` method sends a message to the server over the TCP connection.

- The `Run` method handles the main loop of the client and manages different states:
  - In `AUTH_STATE`, it performs authentication by prompting the user for input and sending an authentication message to the server.
  - In `OPEN_STATE`, it processes user input for various commands like joining a channel, renaming, or sending messages.
  - In `ERROR_STATE`, it handles errors by sending error messages to the server and terminating the client.
  - In `END_STATE`, it terminates the client.

- The `Auth` method handles the authentication process by parsing user input and sending an authentication message to the server.

- The `Open` method handles actions in the open state, such as processing user commands and sending messages to the server.

- The `ReceiveMessage` method asynchronously receives messages from the server and responds accordingly based on the client's state.

- The `Respond` method responds to server messages, updating the confirmation state accordingly.

- The `GetStart` method extracts a substring after a specified index in the received message.

- The `GetEnd` method extracts a substring after "IS" in the received message.

- The `End` method gracefully terminates the client by sending a "BYE" message to the server and closing the TCP client.

---

### UDP Implementation

- Built on implementation of TCP.

- Uses different `SendMessage` to work with UDP.

- Instead of `ConfirmationStates` like planned, uses `ConfirmMessage` function for waiting and confirmation of requests.

---

## Testing

Thorough testing procedures were conducted to ensure the functionality and robustness of the client application. Testing involved multiple runs of the application under different scenarios and inputs, including authentication, channel joining, and message sending. A local server setup was utilized for testing, with small tests conducted using ncat (`nc -4 -c -l -v 127.0.0.1 4567`). Testing outputs were analyzed to verify proper functionality and identify any errors or unexpected behavior.

- What was tested: Authentication, channel joining, message sending, and termination functionalities.
- Why it was tested: To validate proper functionality and identify potential issues or edge cases.
- How it was tested: Multiple runs of the application under different scenarios and inputs.
- Testing environment: Utilization of a local server setup with ncat for small tests and analysis of testing outputs.
- Inputs: Various user inputs for authentication, channel joining, and message sending.
- Expected outputs: Successful authentication, channel joining, and message delivery.
- Actual outputs: Observation of application behavior and any errors encountered during testing.

