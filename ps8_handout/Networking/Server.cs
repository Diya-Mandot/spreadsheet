// <copyright file="NetworkConnection.cs" company="UofU-CS3500">
// Copyright (c) 2024 UofU-CS3500. All rights reserved.
// </copyright>
//Author- Diya Mandot and Devin Gupta
//Version- 8th November 2024

using System.Net;
using System.Net.Sockets;

namespace CS3500.Networking;

/// <summary>
///   Represents a server task that waits for connections on a given
///   port and calls the provided delegate when a connection is made.
/// </summary>
public static class Server
{
    /// <summary>
    ///   Wait on a TcpListener for new connections. Alert the main program
    ///   via a callback (delegate) mechanism.
    /// </summary>
    /// <param name="handleConnect">
    ///   Handler for what the user wants to do when a connection is made.
    ///   This should be run asynchronously via a new thread.
    /// </param>
    /// <param name="port"> The port (e.g., 11000) to listen on. </param>
    public static void StartServer(Action<NetworkConnection> handleConnect, int port)
    {
        TcpListener? listener = null;
        try
        {
            // Create and start the listener
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine($"Server started on port {port}.");

            // Continue accepting clients until the application is terminated
            while (true)
            {
                try
                {
                    // Wait for a client to connect
                    TcpClient client = listener.AcceptTcpClient();
                    NetworkConnection connection = new NetworkConnection(client);

                    // Start a new thread to handle this client
                    Thread clientThread = new Thread(() =>
                    {
                        try
                        {
                            handleConnect(connection);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error in client thread: {ex.Message}");
                            connection.Disconnect();
                        }
                    });

                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Socket exception on accepting client: {ex.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"General exception on accepting client: {ex.Message}");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception starting server: {ex.Message}");
        }
        finally
        {
            listener?.Stop();
            Console.WriteLine("Server stopped.");
        }
    }
}