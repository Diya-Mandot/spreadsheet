// <copyright file="NetworkConnection.cs" company="UofU-CS3500">
// Copyright (c) 2024 UofU-CS3500. All rights reserved.
// </copyright>
//Author- Diya Mandot and Devin Gupta
//Version- 8th November 2024

using CS3500.Networking;
using System.Collections.Concurrent;

namespace CS3500.Chatting;

/// <summary>
///   A simple ChatServer that handles clients separately and broadcasts messages.
/// </summary>
public partial class ChatServer
{
    // Thread-safe dictionary to store active connections and usernames
    private static readonly ConcurrentDictionary<NetworkConnection, string> Clients = new();

    private static readonly object broadcastLock = new object();

    /// <summary>
    ///   The main program.
    /// </summary>
    /// <param name="args"> ignored. </param>
    private static void Main(string[] args)
    {
        // Start the server on port 11000 and set HandleConnect as the connection handler
        Console.WriteLine("Chat Server starting on port 11000...");
        Server.StartServer(HandleConnect, 11000);
        Console.WriteLine("Server running. Press Enter to exit.");
        Console.Read();
    }

    /// <summary>
    /// Broadcasts a message to all connected clients (including the sender)
    /// </summary>
    private static void BroadcastMessage(string message, NetworkConnection? excludeConnection = null)
    {
        lock (broadcastLock)
        {
            foreach (var client in Clients.Keys)
            {
                // Skip the excluded connection if specified
                if (client != excludeConnection && client.IsConnected)
                {
                    try
                    {
                        client.Send(message);
                        Console.WriteLine($"Message sent to {Clients[client]}: {message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error broadcasting to client: {ex.Message}");
                        DisconnectClient(client);
                    }
                }
            }
        }
    }

    /// <summary>
    ///   Handles a new client connection.
    /// </summary>
    private static void HandleConnect(NetworkConnection connection)
    {
        try
        {
            // Get the first message as the client’s name
            string? name = connection.ReadLine();

            if (string.IsNullOrWhiteSpace(name))
            {
                connection.Send("Error: Name cannot be empty");
                connection.Disconnect();
                return;
            }

            // Register the connection and the name
            if (!Clients.TryAdd(connection, name))
            {
                connection.Send("Error: Could not register your connection");
                connection.Disconnect();
                return;
            }

            Console.WriteLine($"{name} has connected.");

            // First send a message to just this client that they've connected successfully
            connection.Send($"Server: You have joined the chat as {name}.");

            // Notify other clients that a new user has joined
            BroadcastMessage($"Server: {name} has joined the chat.", connection);

            // Handle subsequent messages from the client
            while (true)
            {
                string message = connection.ReadLine();
                if (!string.IsNullOrEmpty(message))
                {
                    BroadcastMessage($"{name}: {message}", connection);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in HandleConnect for client: {ex.Message}");
            DisconnectClient(connection);
        }
    }

    /// <summary>
    /// Disconnects a client and removes them from the clients dictionary
    /// </summary>
    private static void DisconnectClient(NetworkConnection connection)
    {
        if (Clients.TryRemove(connection, out string? name))
        {
            Console.WriteLine($"{name} has disconnected.");
            BroadcastMessage($"Server: {name} has left the chat.");
            try
            {
                connection.Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during disconnect for client {name}: {ex.Message}");
            }
        }
    }
}