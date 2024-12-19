using Model;
using NetworkUtil;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace Server

{
    internal class Server
    {

        private GameSettings gameSettings;
        private Dictionary<long, SocketState> clients;
        private ServerModel model;

        static void Main(string[] args)
        {

            // start server
            Server server = new Server();
            server.StartServer();

            // make a loop update the world and send to clients
            // there maybe race condition when receiving data and sending data to clients, make sure to add "lock"

            Stopwatch watch = new Stopwatch();
            watch.Start();
            
            //loop that keeps the server running and updating the mode.
            while (true)
            {

                while (watch.ElapsedMilliseconds <  server.gameSettings.MSPerFrame)
                { Thread.Sleep(1); }

                watch.Restart();

                 
                server.CallUpdateModel();

                server.SendDataToClients();

            }

        }

        public Server()
        {
            gameSettings = ReadSettings();
            model = new ServerModel(gameSettings);

            clients = new Dictionary<long, SocketState>();
        }
        /// <summary>
        /// Reads the xml file containing the gamesettings and stores them in the model
        /// </summary>
        /// <returns></returns>
        private GameSettings ReadSettings()
        {
            XmlReader reader = XmlReader.Create("settings.xml");
            DataContractSerializer serializer = new DataContractSerializer(typeof(GameSettings));
            return (GameSettings)serializer.ReadObject(reader)!;
        }

        /// <summary>
        /// Starts the server
        /// </summary>
        public void StartServer()
        {
            Networking.StartServer(NewClientConnected, 11000);

            Console.WriteLine("Server is running!");

        }
        /// <summary>
        /// Connects a new client to the server
        /// </summary>
        /// <param name="state"></param>
        private void NewClientConnected(SocketState state)
        {
            if (state.ErrorOccurred)
                return;

            state.OnNetworkAction = RecivePlayerName;

            Networking.GetData(state);

            Console.WriteLine("New client connected!");
        }
        /// <summary>
        /// Hanshake method that communicates with the client to recieve their player name and completes the rest of the handshake by 
        /// passing the client their ID and the initial game settings like the walls.
        /// </summary>
        /// <param name="state"></param>
        private void RecivePlayerName(SocketState state)
        {

            long snakeID = state.ID;
            string name = state.GetData();
            state.RemoveData(0, name.Length);

            lock (clients)
            {
                model.NewPlayer(snakeID, name);
            }

            state.OnNetworkAction = HandleRequest;

            // package up information
            StringBuilder startUpInfo = new StringBuilder();

            // store snkae ID and world size
            startUpInfo.AppendLine(snakeID.ToString());
            startUpInfo.AppendLine(gameSettings.UniverseSize.ToString());

            // store walls
            foreach (Wall w in gameSettings.Walls)
            {
                startUpInfo.AppendLine(JsonConvert.SerializeObject(w));
            }

            Networking.Send(state.TheSocket, startUpInfo.ToString());

            lock (clients)
            {
                clients[state.ID] = state;
            }

            Networking.GetData(state);
        }
        /// <summary>
        /// Recieves data from clients and passes it to the model for parsing
        /// </summary>
        /// <param name="state"></param>
        private void HandleRequest(SocketState state)
        {
            //Console.WriteLine("Recived request!");

            if (state.ErrorOccurred)
            {
                RemoveClient(state.ID);
                return;
            }

            model.RecievedMessage(state.ID, state.GetData());
            state.RemoveData(0, state.GetData().Length);
            // Continue the event loop that receives messages from this client
            Networking.GetData(state);
        }
        /// <summary>
        /// Removes a client from the server upon disconnection.
        /// </summary>
        /// <param name="id"></param>
        private void RemoveClient(long id)
        {
            Console.WriteLine("Client " + id + " disconnected");
            lock (clients)
            {
                clients.Remove(id);
            }
        }
        /// <summary>
        /// Helper method that calls update model from the model
        /// </summary>
        public void CallUpdateModel()
        {
            lock (clients)
            {
                model.UpdateModel();
            }
        }

        /// <summary>
        /// Sends Json formatted data to clients on every update.
        /// </summary>
        public void SendDataToClients()
        {
            lock (clients)
            {
                foreach (var client in clients.Values)
                    Networking.Send(client.TheSocket, model.InfoToSend());
            }
        }
    }
}