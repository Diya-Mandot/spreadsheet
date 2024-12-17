// <copyright file="NetworkConnection.cs" company="UofU-CS3500">
// Copyright (c) 2024 UofU-CS3500. All rights reserved.
// </copyright>
//Author- Diya Mandot and Devin Gupta
//Version- 8th November 2024

using System.Net.Sockets;
using System.Text;

namespace CS3500.Networking;

/// <summary>
///   Wraps the StreamReader/Writer/TcpClient together so we
///   don't have to keep creating all three for network actions.
/// </summary>
public sealed class NetworkConnection : IDisposable
{
    /// <summary>
    ///   The connection/socket abstraction
    /// </summary>
    private readonly TcpClient _tcpClient;

    /// <summary>
    ///   Reading end of the connection
    /// </summary>
    private StreamReader? _reader;

    /// <summary>
    ///   Writing end of the connection
    /// </summary>
    private StreamWriter? _writer;

    /// <summary>
    ///   Initializes a new instance of the <see cref="NetworkConnection"/> class.
    ///   <para>
    ///     Create a network connection object.
    ///   </para>
    /// </summary>
    /// <param name="tcpClient">
    ///   An already existing TcpClient
    /// </param>
    public NetworkConnection(TcpClient tcpClient)
    {
        _tcpClient = tcpClient;
        if (IsConnected)
        {
            // Only establish the reader/writer if the provided TcpClient is already connected.
            _reader = new StreamReader(_tcpClient.GetStream(), Encoding.UTF8);
            _writer = new StreamWriter(_tcpClient.GetStream(), Encoding.UTF8) { AutoFlush = true };
        }
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="NetworkConnection"/> class.
    ///   <para>
    ///     Create a network connection object. The tcpClient will be unconnected at the start.
    ///   </para>
    /// </summary>
    public NetworkConnection() : this(new TcpClient())
    {
    }

    /// <summary>
    /// Gets a value indicating whether the socket is connected.
    /// </summary>
    public bool IsConnected
    {
        get
        {
            try
            {
                // Check if the client is connected and the socket exists
                return _tcpClient is not null && _tcpClient.Client is not null && _tcpClient.Connected;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking connection status: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Try to connect to the given host and port.
    /// </summary>
    /// <param name="host">The URL or IP address, e.g., www.cs.utah.edu, or 127.0.0.1.</param>
    /// <param name="port">The port, e.g., 11000.</param>
    public void Connect(string host, int port)
    {
        // Check if already connected
        if (IsConnected)
        {
            throw new InvalidOperationException("Already connected.");
        }

        try
        {
            // Attempt to connect to the given host and port
            _tcpClient.Connect(host, port);

            // Initialize the StreamReader and StreamWriter if the connection is successful
            _reader = new StreamReader(_tcpClient.GetStream(), Encoding.UTF8);
            _writer = new StreamWriter(_tcpClient.GetStream(), Encoding.UTF8) { AutoFlush = true };
            Console.WriteLine($"Connected to server at {host}:{port}");
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"SocketException during connect: {ex.Message}");
            Disconnect();
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception during connect: {ex.Message}");
            Disconnect();
            throw;
        }
    }

    /// <summary>
    ///   Send a message to the remote server. If the <paramref name="message"/> contains
    ///   new lines, these will be treated on the receiving side as multiple messages.
    ///   This method should attach a newline to the end of the <paramref name="message"/>
    ///   (by using WriteLine).
    ///   If this operation can not be completed (e.g. because this NetworkConnection is not
    ///   connected), throw an InvalidOperationException.
    /// </summary>
    /// <param name="message"> The string of characters to send. </param>
    public void Send(string message)
    {
        // Check if the connection is active
        if (!IsConnected || _writer == null)
        {
            throw new InvalidOperationException("The connection is not active. Cannot send message.");
        }

        try
        {
            _writer.WriteLine(message);
            Console.WriteLine($"Sent message: {message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
            Disconnect();
            throw;
        }
    }

    /// <summary>
    ///   Read a message from the remote side of the connection. The message will contain
    ///   all characters up to the first new line. See <see cref="Send"/>.
    ///   If this operation can not be completed (e.g. because this NetworkConnection is not
    ///   connected), throw an InvalidOperationException.
    /// </summary>
    /// <returns> The contents of the message. </returns>
    public string ReadLine()
    {
        // Check if the connection is active
        if (!IsConnected || _reader == null)
        {
            throw new InvalidOperationException("The connection is not active. Cannot read message.");
        }

        try
        {
            string? message = _reader.ReadLine();
            if (message == null)
            {
                throw new InvalidOperationException("Failed to read a message from the remote side.");
            }

            Console.WriteLine($"Received message: {message}");
            return message;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading message: {ex.Message}");
            Disconnect();
            throw;
        }
    }

    /// <summary>
    ///   If connected, disconnect the connection and clean 
    ///   up (dispose) any streams.
    /// </summary>
    public void Disconnect()
    {
        // Check if the TcpClient is connected
        if (IsConnected)
        {
            // Close the TcpClient to disconnect from the remote host
            _tcpClient.Close();
            _tcpClient.Dispose();
        }

        // Dispose of the StreamReader and StreamWriter to free resources
        _reader?.Dispose();
        _writer?.Dispose();

        // Set the streams to null to ensure they are not used again
        _reader = null;
        _writer = null;
        Console.WriteLine("Disconnected from server");
    }

    /// <summary>
    ///   Automatically called with a using statement (see IDisposable)
    /// </summary>
    public void Dispose()
    {
        Disconnect();
    }
}