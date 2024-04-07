# Changelog

## Version 1.0.0 (01/04/2024)

### Implemented

- Implemented functionality for the chat client according to the IPK24-CHAT protocol.
- Supported both TCP and UDP transport protocols, ensuring compatibility with IPv4 network protocols.
- Defined message types, parameters, and client behavior based on protocol specifications.

### Known Limitations

- The application may experience performance issues and slow responsiveness due to C# implementation, especially under heavy load or rapid command executions.
- Running too many tests too quickly or executing commands rapidly may cause unexpected behavior.
- Occasional errors like "Transport endpoint is not connected" or "Socket timed out" may occur under heavy load.
