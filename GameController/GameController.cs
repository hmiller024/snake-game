namespace Controller
{
    using NetworkUtil;
    using Model;
    using System;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json;
    using System.Diagnostics;
    using System.ComponentModel;
    using System.Text;
    using System.Net.Sockets;
    using System.Net;

    public class GameController
    {
        private String playerName;
        private World world;
        private bool handshake;
        private SocketState state;


        public delegate void GameUpdateHandler();
        public event GameUpdateHandler? GameUpdate;

        public delegate void ErrorHandler();
        public event ErrorHandler? ErrorEvent;

        public GameController()
        {
            world = new World(4000);
            playerName = "";
            handshake = false;
            state = null!;
        }

        /// <summary>
        /// Connect method for accpeting inputs from the "View"
        /// </summary>
        /// <param name="Ip_Address">The ip address.</param>
        /// <param name="playerName">Name of the player.</param>
        public void Connect(string Ip_Address, string playerName)
        {
            this.playerName = playerName;
            Networking.ConnectToServer(Onconnect, Ip_Address, 11000);
        }

        /// <summary>
        /// Called Onconnects When the connection is made
        /// </summary>
        /// <param name="state">The state.</param>
        private void Onconnect(SocketState state)
        {
            this.state = state;
            if (state.ErrorOccurred)
            {
                ErrorEvent?.Invoke();
                return;
            }
            Networking.Send(state.TheSocket, playerName);
            state.OnNetworkAction = ReceiveData;

            // get data for the first time
            Networking.GetData(state);
        }

        /// <summary>
        /// Receives the data from server.
        /// </summary>
        /// <param name="state">The state.</param>
        private void ReceiveData(SocketState state)
        {
            if (state.ErrorOccurred)
            {
                ErrorEvent?.Invoke();
                return;
            }

            ProcessData(state);

            // continue the event loop to receive more data from the server
            Networking.GetData(state);
        }

        /// <summary>
        /// Processes the recevied data.
        /// </summary>
        /// <param name="state">The state.</param>
        private void ProcessData(SocketState state)
        {
            string message = state.GetData();
            
            int toRemove = 0;

            string[] strArray = message.Split('\n');

            if (handshake == false)
            {

                long thisSnakeID = long.Parse(strArray[0]);
                int worldSize = int.Parse(strArray[1]);

                // create a local world and set the snake to this player
                world = new World(worldSize);
                world.playerID = thisSnakeID;
                handshake = true;
            }

            for (int i = 0; i < strArray.Length - 1; i++)
            {
                toRemove += strArray[i].Length + 1;
                bool thrown = false;
                try
                {
                    JObject.Parse(strArray[i]);
                }
                catch (Exception)
                {
                    thrown = true;
                }

                if (strArray[i] == null || strArray[i].Equals("") || thrown)
                    continue;


                JObject obj = JObject.Parse(strArray[i]);

                // determine whether the Json object is wall or snake or power and add it to the world
                if (obj["wall"] != null)
                {
                    lock (world.walls)
                    {
                        Wall wall = JsonConvert.DeserializeObject<Wall>(strArray[i])!;
                        world.walls.Add(wall.ID, wall);
                    }
                }
                else if (obj["snake"] != null)
                {
                    lock (world.snakes)
                    {
                        Snake snake = JsonConvert.DeserializeObject<Snake>(strArray[i])!;
                        if (world.snakes.ContainsKey(snake.ID))
                            world.snakes[snake.ID] = snake;                        
                        else
                            world.snakes.Add(snake.ID, snake);


                        // remove snake that is disconnected
                        if (snake.IsDisconnected)
                            world.snakes.Remove(snake.ID);
                    }
                }
                else if (obj["power"] != null)
                {
                    lock (world.powerups)
                    {
                        Powerup powerup = JsonConvert.DeserializeObject<Powerup>(strArray[i])!;
                        if (world.powerups.ContainsKey(powerup.ID))
                            world.powerups[powerup.ID] = powerup;
                        else
                            world.powerups.Add(powerup.ID, powerup);

                        // remove powerup that is not active
                        if (powerup.IsDead)
                            world.powerups.Remove(powerup.ID);
                    }
                }
            }
            state.RemoveData(0, toRemove);

            GameUpdate?.Invoke(); 
        }

        /// <summary>
        /// Gets the world.
        /// </summary>
        /// <returns></returns>
        public World GetWorld()
        {
            return world;
        }

        /// <summary>
        /// Response to keyboard Input W.
        /// </summary>
        public void InputW()
        {
            Networking.Send(state.TheSocket, "{\"moving\":\"up\"}" + "\n");
        }
        /// <summary>
        /// Response to keyboard Input S.
        /// </summary>
        public void InputS()
        {
            Networking.Send(state.TheSocket, "{\"moving\":\"down\"}" + "\n");
        }
        /// <summary>
        /// Response to keyboard Input A.
        /// </summary>
        public void InputA()
        {
            Networking.Send(state.TheSocket, "{\"moving\":\"left\"}" + "\n");
        }
        /// <summary>
        /// Response to keyboard Input D.
        /// </summary>
        public void InputD()
        {
            Networking.Send(state.TheSocket, "{\"moving\":\"right\"}" + "\n");
        }

    }
}