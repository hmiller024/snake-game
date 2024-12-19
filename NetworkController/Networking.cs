using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetworkUtil;

public static class Networking
{
    /////////////////////////////////////////////////////////////////////////////////////////
    // Server-Side Code
    /////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Starts a TcpListener on the specified port and starts an event-loop to accept new clients.
    /// The event-loop is started with BeginAcceptSocket and uses AcceptNewClient as the callback.
    /// AcceptNewClient will continue the event-loop.
    /// </summary>
    /// <param name="toCall">The method to call when a new connection is made</param>
    /// <param name="port">The the port to listen on</param>
    public static TcpListener StartServer(Action<SocketState> toCall, int port)
    {
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        Tuple<TcpListener, Action<SocketState>> ListenerState = new Tuple<TcpListener, Action<SocketState>>(listener, toCall);

        try
        {
            listener.Start();
            listener.BeginAcceptSocket(AcceptNewClient, ListenerState);
        }
        catch (Exception)
        {
        }
        return listener;
    }

    /// <summary>
    /// To be used as the callback for accepting a new client that was initiated by StartServer, and 
    /// continues an event-loop to accept additional clients.
    ///
    /// Uses EndAcceptSocket to finalize the connection and create a new SocketState. The SocketState's
    /// OnNetworkAction should be set to the delegate that was passed to StartServer.
    /// Then invokes the OnNetworkAction delegate with the new SocketState so the user can take action. 
    /// 
    /// If anything goes wrong during the connection process (such as the server being stopped externally), 
    /// the OnNetworkAction delegate should be invoked with a new SocketState with its ErrorOccurred flag set to true 
    /// and an appropriate message placed in its ErrorMessage field. The event-loop should not continue if
    /// an error occurs.
    ///
    /// If an error does not occur, after invoking OnNetworkAction with the new SocketState, an event-loop to accept 
    /// new clients should be continued by calling BeginAcceptSocket again with this method as the callback.
    /// </summary>
    /// <param name="ar">The object asynchronously passed via BeginAcceptSocket. It must contain a tuple with 
    /// 1) a delegate so the user can take action (a SocketState Action), and 2) the TcpListener</param>
    private static void AcceptNewClient(IAsyncResult ar)
    {
        Tuple<TcpListener, Action<SocketState>> ListenerState = (Tuple<TcpListener, Action<SocketState>>)ar.AsyncState!;

        Socket socket;
        SocketState state;

        try
        {
            socket = ListenerState.Item1.EndAcceptSocket(ar);
            state = new SocketState(ListenerState.Item2, socket);
        }
        catch (Exception e)
        {
            // if something goes wrong with the connection, create a new socket state indicating the error
            // and stop accepting new clients
            state = new SocketState(ListenerState.Item2, e.ToString());
            state.ErrorOccurred = true;
            state.OnNetworkAction(state);
            return;
        }

        // let the user decide what to do after successfully connected
        state.OnNetworkAction(state);

        try
        {
            // start accepting new client
            ListenerState.Item1.BeginAcceptSocket(AcceptNewClient, ListenerState);
        }
        catch (Exception e)
        {
            state = new SocketState(ListenerState.Item2, e.ToString());
            state.OnNetworkAction(state);
        }
    }

    /// <summary>
    /// Stops the given TcpListener.
    /// </summary>
    public static void StopServer(TcpListener listener)
    {
        try
        {
            listener.Stop();
        }
        catch (Exception)
        {
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////
    // Client-Side Code
    /////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Begins the asynchronous process of connecting to a server via BeginConnect, 
    /// and using ConnectedCallback as the method to finalize the connection once it's made.
    /// 
    /// If anything goes wrong during the connection process, toCall should be invoked 
    /// with a new SocketState with its ErrorOccurred flag set to true and an appropriate message 
    /// placed in its ErrorMessage field. Depending on when the error occurs, this should happen either
    /// in this method or in ConnectedCallback.
    ///
    /// This connection process should timeout and produce an error (as discussed above) 
    /// if a connection can't be established within 3 seconds of starting BeginConnect.
    /// 
    /// </summary>
    /// <param name="toCall">The action to take once the connection is open or an error occurs</param>
    /// <param name="hostName">The server to connect to</param>
    /// <param name="port">The port on which the server is listening</param>
    public static void ConnectToServer(Action<SocketState> toCall, string hostName, int port)
    {
        SocketState erroState;

        // Establish the remote endpoint for the socket.
        IPHostEntry ipHostInfo;
        IPAddress ipAddress = IPAddress.None;

        // Determine if the server address is a URL or an IP
        try
        {
            ipHostInfo = Dns.GetHostEntry(hostName);
            bool foundIPV4 = false;
            foreach (IPAddress addr in ipHostInfo.AddressList)
                if (addr.AddressFamily != AddressFamily.InterNetworkV6)
                {
                    foundIPV4 = true;
                    ipAddress = addr;
                    break;
                }
            // Didn't find any IPV4 addresses
            if (!foundIPV4)
            {
                erroState = new SocketState(toCall, "IP4 address is not found!");
                toCall(erroState);
                return;
            }
        }
        catch (Exception)
        {
            // see if host name is a valid ipaddress
            try
            {
                ipAddress = IPAddress.Parse(hostName);
            }
            catch (Exception e)
            {
                erroState = new SocketState(toCall, e.ToString());
                toCall(erroState);
                return;
            }
        }

        // Create a TCP/IP socket.
        Socket socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        // This disables Nagle's algorithm (google if curious!)
        // Nagle's algorithm can cause problems for a latency-sensitive 
        // game like ours will be 
        socket.NoDelay = true;

        SocketState state = new SocketState(toCall, socket);

        try
        {
            // if the connection can't connect within 3 seconds, times out and close the socket
            // if the connection is made successful, it invoke and proceed to connectedCallBack
            if (!socket.BeginConnect(ipAddress, port, ConnectedCallback, state).AsyncWaitHandle.WaitOne(3000, true))
            {
                state.TheSocket.Close();
            }
        }
        catch (Exception e)
        {
            // create an new socket to indicate the error to the user
            erroState = new SocketState(toCall, e.ToString());
            erroState.ErrorOccurred = true;
            toCall(erroState);
        }
    }

    /// <summary>
    /// To be used as the callback for finalizing a connection process that was initiated by ConnectToServer.
    ///
    /// Uses EndConnect to finalize the connection.
    /// 
    /// As stated in the ConnectToServer documentation, if an error occurs during the connection process,
    /// either this method or ConnectToServer should indicate the error appropriately.
    /// 
    /// If a connection is successfully established, invokes the toCall Action that was provided to ConnectToServer (above)
    /// with a new SocketState representing the new connection.
    /// 
    /// </summary>
    /// <param name="ar">The object asynchronously passed via BeginConnect</param>
    private static void ConnectedCallback(IAsyncResult ar)
    {
        SocketState state = (SocketState)ar.AsyncState!;
        Socket socket = state.TheSocket;

        try
        {
            socket.EndConnect(ar);
        }
        catch (Exception e)
        {
            state.ErrorMessage = e.ToString();
            state.ErrorOccurred = true;
        }

        // let the user decide what to do after successfully connected
        state.OnNetworkAction(state);
    }


    /////////////////////////////////////////////////////////////////////////////////////////
    // Server and Client Common Code
    /////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Begins the asynchronous process of receiving data via BeginReceive, using ReceiveCallback 
    /// as the callback to finalize the receive and store data once it has arrived.
    /// The object passed to ReceiveCallback via the AsyncResult should be the SocketState.
    /// 
    /// If anything goes wrong during the receive process, the SocketState's ErrorOccurred flag should 
    /// be set to true, and an appropriate message placed in ErrorMessage, then the SocketState's
    /// OnNetworkAction should be invoked. Depending on when the error occurs, this should happen either
    /// in this method or in ReceiveCallback.
    /// </summary>
    /// <param name="state">The SocketState to begin receiving</param>
    public static void GetData(SocketState state)
    {
        try
        {
            state.TheSocket.BeginReceive(state.buffer, 0, state.buffer.Length, SocketFlags.None, ReceiveCallback, state);
        }
        catch (Exception e)
        {
            state.ErrorOccurred = true;
            state.ErrorMessage = e.ToString();
            state.OnNetworkAction(state);
        }
    }

    /// <summary>
    /// To be used as the callback for finalizing a receive operation that was initiated by GetData.
    /// 
    /// Uses EndReceive to finalize the receive.
    ///
    /// As stated in the GetData documentation, if an error occurs during the receive process,
    /// either this method or GetData should indicate the error appropriately.
    /// 
    /// If data is successfully received:
    ///  (1) Read the characters as UTF8 and put them in the SocketState's unprocessed data buffer (its string builder).
    ///      This must be done in a thread-safe manner with respect to the SocketState methods that access or modify its 
    ///      string builder.
    ///  (2) Call the saved delegate (OnNetworkAction) allowing the user to deal with this data.
    /// </summary>
    /// <param name="ar"> 
    /// This contains the SocketState that is stored with the callback when the initial BeginReceive is called.
    /// </param>
    private static void ReceiveCallback(IAsyncResult ar)
    {
        SocketState state = (SocketState)ar.AsyncState!;

        try
        {
            int numbyte = state.TheSocket.EndReceive(ar);

            // collect data only if the socket receive more than 0 byte
            if (numbyte > 0)
            {
                // collect data and put them to the buffer
                string message = Encoding.UTF8.GetString(state.buffer, 0, numbyte);

                lock (state.data)
                {
                    state.data.Append(message);
                }
                // let the user decide what to do with the data
                state.OnNetworkAction(state);
                return;
            }
            else
            {
                state.ErrorOccurred = true;
                state.ErrorMessage = "Socket disconneted!";
            }

        }
        catch (Exception e)
        {
            state.ErrorMessage = e.ToString();
            state.ErrorOccurred = true;
        }
        state.OnNetworkAction(state);
    }

    /// <summary>
    /// Begin the asynchronous process of sending data via BeginSend, using SendCallback to finalize the send process.
    /// 
    /// If the socket is closed, does not attempt to send.
    /// 
    /// If a send fails for any reason, this method ensures that the Socket is closed before returning.
    /// </summary>
    /// <param name="socket">The socket on which to send the data</param>
    /// <param name="data">The string to send</param>
    /// <returns>True if the send process was started, false if an error occurs or the socket is already closed</returns>
    public static bool Send(Socket socket, string data)
    {
        // stocket is closed, do not send
        if (!socket.Connected)
            return false;

        try
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            socket.BeginSend(dataBytes, 0, dataBytes.Length, SocketFlags.None, SendCallback, socket);
        }
        catch (Exception)
        {
            try
            {
                socket.Close();
            }
            catch (Exception)
            {
            }
            return false;
        }

        return true;
    }

    /// <summary>
    /// To be used as the callback for finalizing a send operation that was initiated by Send.
    ///
    /// Uses EndSend to finalize the send.
    /// 
    /// This method must not throw, even if an error occurred during the Send operation.
    /// </summary>
    /// <param name="ar">
    /// This is the Socket (not SocketState) that is stored with the callback when
    /// the initial BeginSend is called.
    /// </param>
    private static void SendCallback(IAsyncResult ar)
    {
        try
        {
            Socket socket = (Socket)ar.AsyncState!;
            socket.EndSend(ar);
        }
        catch (Exception)
        {
        }
    }


    /// <summary>
    /// Begin the asynchronous process of sending data via BeginSend, using SendAndCloseCallback to finalize the send process.
    /// This variant closes the socket in the callback once complete. This is useful for HTTP servers.
    /// 
    /// If the socket is closed, does not attempt to send.
    /// 
    /// If a send fails for any reason, this method ensures that the Socket is closed before returning.
    /// </summary>
    /// <param name="socket">The socket on which to send the data</param>
    /// <param name="data">The string to send</param>
    /// <returns>True if the send process was started, false if an error occurs or the socket is already closed</returns>
    public static bool SendAndClose(Socket socket, string data)
    {
        // stocket is closed, do not send
        if (!socket.Connected)
            return false;

        try
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            socket.BeginSend(dataBytes, 0, dataBytes.Length, SocketFlags.None, SendAndCloseCallback, socket);
        }
        catch (Exception)
        {
            try
            {
                socket.Close();
            }
            catch (Exception)
            {
            }
            return false;
        }

        return true;
    }

    /// <summary>
    /// To be used as the callback for finalizing a send operation that was initiated by SendAndClose.
    ///
    /// Uses EndSend to finalize the send, then closes the socket.
    /// 
    /// This method must not throw, even if an error occurred during the Send operation.
    /// 
    /// This method ensures that the socket is closed before returning.
    /// </summary>
    /// <param name="ar">
    /// This is the Socket (not SocketState) that is stored with the callback when
    /// the initial BeginSend is called.
    /// </param>
    private static void SendAndCloseCallback(IAsyncResult ar)
    {
        try
        {
            Socket socket = (Socket)ar.AsyncState!;
            socket.EndSend(ar);
            socket.Close();
        }
        catch (Exception)
        {
        }
    }
}