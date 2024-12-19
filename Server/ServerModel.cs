using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SnakeGame;

namespace Server
{
    internal class ServerModel
    {
        private double snakeOffset = 5;
        private double powerupRadius = 5;
        private double wallOffset = 25;

        private double deftBodyLength = 120;

        private int minSpawnRangeX;
        private int maxSpawnRangeX;
        private int minSpawnRangeY;
        private int maxSpawnRangeY;



        private Random random = new Random();
        private World worldOnServer { get; set; }
        private GameSettings gameSettings { get; set; }
        public ServerModel(GameSettings settings)
        {
            gameSettings = settings;
            worldOnServer = new World(gameSettings.UniverseSize);

            minSpawnRangeX = -worldOnServer.Size / 2;
            maxSpawnRangeX = worldOnServer.Size / 2;
            minSpawnRangeY = -worldOnServer.Size / 2;
            maxSpawnRangeY = worldOnServer.Size / 2;

            createWalls();
        }

        /// <summary>
        /// Creates a new snake to add to the world when a new player joins.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="name"></param>
        public void NewPlayer(long ID, string name)
        {
            lock (worldOnServer.snakes)
            {
                worldOnServer.snakes.Add(ID, new Snake(ID, name));
                worldOnServer.snakes[ID].IsAlive = false;
                spawnSnake(worldOnServer.snakes[ID]);
            }

            lock (worldOnServer.powerups)
            {
                spawnPowerup();
            }
        }
        /// <summary>
        /// Takes each wall from the game settings and adds it to the server world.
        /// </summary>
        private void createWalls()
        {
            lock (worldOnServer.walls)
            {

                foreach (Wall w in gameSettings.Walls)
                    worldOnServer.walls[w.ID] = w;
            }
        }
        /// <summary>
        /// Spawns a powerup in a safe location.
        /// </summary>
        private void spawnPowerup()
        {
            lock (worldOnServer.powerups)
            {
                double powerPosiX = random.NextDouble() * random.Next(minSpawnRangeX, maxSpawnRangeX);
                double powerPosiY = random.NextDouble() * random.Next(minSpawnRangeY, maxSpawnRangeY);


                while (SpawnCollisionOccured(powerPosiX, powerPosiY, powerupRadius, -1))
                {
                    // choose a new location to spawn
                    powerPosiX = random.NextDouble() * random.Next(minSpawnRangeX, maxSpawnRangeX);
                    powerPosiY = random.NextDouble() * random.Next(minSpawnRangeY, maxSpawnRangeY);
                }

                Powerup powerup = new Powerup(new Vector2D(powerPosiX, powerPosiY));
                worldOnServer.powerups.Add(powerup.ID, powerup);

                int maxPowerDelay = 200;
                worldOnServer.powerSpawnFrameDelay = random.Next(maxPowerDelay);
            }
        }

        /// <summary>
        /// Spawns a snake in a safe location.
        /// </summary>
        /// <param name="snake"></param>
        private void spawnSnake(Snake snake)
        {
            lock (worldOnServer)
            {

                double headPosiX = random.NextDouble() * random.Next(minSpawnRangeX, maxSpawnRangeX);
                double headPosiY = random.NextDouble() * random.Next(minSpawnRangeY, maxSpawnRangeY);


                while (SpawnCollisionOccured(headPosiX, headPosiY, snakeOffset, snake.ID))
                {
                    // choose a new location to spawn
                    headPosiX = random.NextDouble() * random.Next(minSpawnRangeX, maxSpawnRangeX);
                    headPosiY = random.NextDouble() * random.Next(minSpawnRangeY, maxSpawnRangeY);
                }


                int tailDirection = random.Next(4);

                double tailPosiX = headPosiX;
                double tailPosiY = headPosiY;

                bool tailChanged = false;

                #region determine snake tail and its direction
                if (tailDirection == 0)
                {
                    bool noCollision = true;
                    // check the entire body length not to be collided
                    for (double i = 0; i <= deftBodyLength; i++)
                    {
                        noCollision = noCollision && !SpawnCollisionOccured(tailPosiX, headPosiY + i, snakeOffset, snake.ID);

                        if (i == deftBodyLength && noCollision)
                        {
                            // snake going up
                            snake.Direction = new Vector2D(0, -1);
                            tailPosiY = headPosiY + deftBodyLength;
                            tailChanged = true;
                        }
                        else if (!noCollision)
                        {
                            // choose a new location
                            spawnSnake(snake);
                            break;
                        }
                    }
                }
                else if (tailDirection == 1)
                {
                    bool noCollision = true;
                    // check the entire body length not to be collided
                    for (double i = 0; i <= deftBodyLength; i++)
                    {
                        noCollision = noCollision && !SpawnCollisionOccured(tailPosiX, headPosiY - i, snakeOffset, snake.ID);

                        if (i == deftBodyLength && noCollision)
                        {
                            // snake going down
                            snake.Direction = new Vector2D(0, 1);
                            tailPosiY = headPosiY - deftBodyLength;
                            tailChanged = true;
                        }
                        else if (!noCollision)
                        {
                            // choose a new location
                            spawnSnake(snake);
                            break;
                        }
                    }
                }
                else if (tailDirection == 2)
                {
                    bool noCollision = true;
                    // check the entire body length not to be collided
                    for (double i = 0; i <= deftBodyLength; i++)
                    {
                        noCollision = noCollision && !SpawnCollisionOccured(headPosiX + i, tailPosiY, snakeOffset, snake.ID);

                        if (i == deftBodyLength && noCollision)
                        {
                            // snake going left
                            snake.Direction = new Vector2D(-1, 0);
                            tailPosiX = headPosiX + deftBodyLength;
                            tailChanged = true;
                        }
                        else if (!noCollision)
                        {
                            // choose a new location
                            spawnSnake(snake);
                            break;
                        }
                    }
                }
                else if (tailDirection == 3)
                {
                    bool noCollision = true;

                    // check the entire body length not to be collided
                    for (double i = 0; i <= deftBodyLength; i++)
                    {
                        noCollision = noCollision && !SpawnCollisionOccured(headPosiX - i, tailPosiY, snakeOffset, snake.ID);

                        if (i == deftBodyLength && noCollision)
                        {
                            // snkae going right
                            snake.Direction = new Vector2D(1, 0);
                            tailPosiX = headPosiX - deftBodyLength;
                            tailChanged = true;
                        }
                        else if (!noCollision)
                        {
                            // choose a new location
                            spawnSnake(snake);
                            break;
                        }
                    }
                }
                #endregion

                if (tailChanged)
                {
                    Vector2D head = new Vector2D(headPosiX, headPosiY);
                    Vector2D tail = new Vector2D(tailPosiX, tailPosiY);

                    snake.Position = new List<Vector2D>();
                    snake.Position.Add(tail);
                    snake.SetHeadPosition(head);

                    snake.IsAlive = true;
                    snake.DeadFrameCounter = 0;
                }
            }
        }
        /// <summary>
        /// Checks if a collision occurs when spawning in objects.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="objOffset"></param>
        /// <param name="ID"></param>
        /// <returns></returns>
        private bool SpawnCollisionOccured(double x, double y, double objOffset, long ID)
        {
            return WallCollisionHappened(x, y, objOffset) || SnakeCollisionHappened(x, y, objOffset, ID) || PowerupCollisionHappened(x, y, objOffset);
        }
        /// <summary>
        /// Checks if a collision occurs with a snake as it is moving around.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="objOffset"></param>
        /// <param name="ID"></param>
        /// <returns></returns>
        private bool MovingCollisionOccured(double x, double y, double objOffset, long ID)
        {
            return WallCollisionHappened(x, y, objOffset) || SnakeCollisionHappened(x, y, objOffset, ID) || SnakeSelfCollision(ID, objOffset);
        }
        /// <summary>
        /// Checks if an object collides with a wall. Returns true if it does, false if not.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="objOffset"></param>
        /// <returns></returns>
        private bool WallCollisionHappened(double x, double y, double objOffset)
        {
            lock(worldOnServer)
            { 
            double uperDimension = 0, lowerDimension = 0, leftDimesion = 0, rightDimesion = 0;

                foreach (Wall wall in worldOnServer.walls.Values)
                {
                    Vector2D point1 = wall.FirstPosition;
                    Vector2D point2 = wall.SecondPosition;

                    #region Determine wall's dimesions
                    // horizonal walls
                    //       ______________________
                    //      |                      |
                    //      | *                  * |
                    //      |______________________|
                    //
                    if (point1.Y == point2.Y)
                    {
                        double smallerX = 0;
                        double largerX = 0;

                        if (point1.X <= point2.X)
                        {
                            smallerX = point1.X;
                            largerX = point2.X;
                        }
                        else if (point2.X < point1.X)
                        {
                            smallerX = point2.X;
                            largerX = point1.X;
                        }

                        uperDimension = point1.Y - wallOffset;
                        lowerDimension = point1.Y + wallOffset;

                        leftDimesion = smallerX - wallOffset;
                        rightDimesion = largerX + wallOffset;
                    }
                    // vertical walls
                    //      __________
                    //      |         |
                    //      |    *    |
                    //      |         |
                    //      |         |
                    //      |         |
                    //      |         |
                    //      |         |
                    //      |    *    |
                    //      |_________|
                    //
                    else if (point1.X == point2.X)
                    {
                        double smallerY = 0;
                        double largerY = 0;

                        if (point1.Y <= point2.Y)
                        {
                            smallerY = point1.Y;
                            largerY = point2.Y;
                        }
                        else if (point2.Y < point1.Y)
                        {
                            smallerY = point2.Y;
                            largerY = point1.Y;
                        }

                        uperDimension = smallerY - wallOffset;
                        lowerDimension = largerY + wallOffset;

                        leftDimesion = point1.X - wallOffset;
                        rightDimesion = point1.X + wallOffset;
                    }
                    #endregion

                    #region Logic dealing with wall collision
                    // collision happens 
                    if (leftDimesion <= x + objOffset && x - objOffset <= rightDimesion &&
                        uperDimension <= y + objOffset && y - objOffset <= lowerDimension)
                    {
                        return true;
                    }
                    #endregion
                }
            }
            return false;
        }
        /// <summary>
        /// Checks if Snake collides with itself. Returns true if it does, false if not.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="objOffset"></param>
        /// <returns></returns>
        private bool SnakeSelfCollision(long ID, double objOffset)
        {
            lock (worldOnServer.snakes)
            {
                Snake s = worldOnServer.snakes[ID];
                double x = s.Position[s.Position.Count - 1].X;
                double y = s.Position[s.Position.Count - 1].Y;
                Vector2D headDir = s.Direction;
                int startSeg = 0;
                //gets index to start detecting for collision
                // index for start is first segment that runs opposite direction of head
                if (s.Position.Count > 2)
                { }
                for (int i = s.Position.Count - 1; i > 1; i--)
                {
                    Vector2D segDir = new Vector2D();
                    if (s.Position[i - 1].Y == s.Position[i - 2].Y)
                    {
                        if (s.Position[i - 1].X >= s.Position[i - 2].X)
                        {
                            segDir = new Vector2D(1, 0);
                        }
                        else
                        {
                            segDir = new Vector2D(-1, 0);
                        }
                    }
                    else
                    {
                        if (s.Position[i - 1].Y >= s.Position[i - 2].Y)
                        {
                            segDir = new Vector2D(0, 1);
                        }
                        else
                        {
                            segDir = new Vector2D(0, -1);
                        }
                    }


                    if (headDir.IsOppositeCardinalDirection(segDir))
                    {
                        startSeg = i - 2;
                        break;

                    }
                }
                //Normal snake collision is run but skips segments until one is the opposite direction of the head then run collision checks on rest of snake
                double uperDimension = 0, lowerDimension = 0, leftDimesion = 0, rightDimesion = 0;
                for (int i = 0; i < startSeg; i++)
                {
                    Vector2D point1 = s.Position[i];
                    Vector2D point2 = s.Position[i + 1];

                    // prevent self collision with its own head


                    #region Determine snakesegment's dimesions
                    // horizonal snakesegment
                    //       ______________________
                    //      |                      |
                    //      | *                  * |
                    //      |______________________|
                    //
                    if (point1.Y == point2.Y)
                    {
                        double smallerX = 0;
                        double largerX = 0;

                        if (point1.X <= point2.X)
                        {
                            smallerX = point1.X;
                            largerX = point2.X;
                        }
                        else if (point2.X < point1.X)
                        {
                            smallerX = point2.X;
                            largerX = point1.X;
                        }

                        uperDimension = point1.Y - snakeOffset;
                        lowerDimension = point1.Y + snakeOffset;

                        leftDimesion = smallerX - snakeOffset;
                        rightDimesion = largerX + snakeOffset;


                    }
                    // vertical snakesegment
                    //      __________
                    //      |         |
                    //      |    *    |
                    //      |         |
                    //      |         |
                    //      |         |
                    //      |         |
                    //      |         |
                    //      |    *    |
                    //      |_________|
                    //
                    else if (point1.X == point2.X)
                    {
                        double smallerY = 0;
                        double largerY = 0;

                        if (point1.Y <= point2.Y)
                        {
                            smallerY = point1.Y;
                            largerY = point2.Y;
                        }
                        else if (point2.Y < point1.Y)
                        {
                            smallerY = point2.Y;
                            largerY = point1.Y;
                        }

                        uperDimension = smallerY - snakeOffset;
                        lowerDimension = largerY + snakeOffset;

                        leftDimesion = point1.X - snakeOffset;
                        rightDimesion = point1.X + snakeOffset;
                    }
                    #endregion

                    #region Logic dealing with snakesegment collision
                    // collision happens 
                    if (leftDimesion <= x + objOffset && x - objOffset <= rightDimesion &&
                        uperDimension <= y + objOffset && y - objOffset <= lowerDimension)
                    {
                        return true;
                    }
                    #endregion
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if an object collides with a snake. Return true if it does, false if not.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="objOffset"></param>
        /// <param name="ID"></param>
        /// <returns></returns>
        private bool SnakeCollisionHappened(double x, double y, double objOffset, long ID)
        {
            lock (worldOnServer.snakes)
            {
                
            
            double uperDimension = 0, lowerDimension = 0, leftDimesion = 0, rightDimesion = 0;

                foreach (Snake snake in worldOnServer.snakes.Values)
                {
                    if (!snake.IsAlive)
                        continue;

                    //Only checks collission for other snakes
                    if (snake.ID != ID)
                    {


                        for (int i = 0; i < snake.Position.Count - 1; i++)
                        {
                            Vector2D point1 = snake.Position[i];
                            Vector2D point2 = snake.Position[i + 1];

                            // prevent self collision with its own head
                            if (point2.X == x && point2.Y == y)
                                continue;

                            #region Determine snakesegment's dimesions
                            // horizonal snakesegment
                            //       ______________________
                            //      |                      |
                            //      | *                  * |
                            //      |______________________|
                            //
                            if (point1.Y == point2.Y)
                            {
                                double smallerX = 0;
                                double largerX = 0;

                                if (point1.X <= point2.X)
                                {
                                    smallerX = point1.X;
                                    largerX = point2.X;
                                }
                                else if (point2.X < point1.X)
                                {
                                    smallerX = point2.X;
                                    largerX = point1.X;
                                }

                                uperDimension = point1.Y - snakeOffset;
                                lowerDimension = point1.Y + snakeOffset;

                                leftDimesion = smallerX - snakeOffset;
                                rightDimesion = largerX + snakeOffset;


                            }
                            // vertical snakesegment
                            //      __________
                            //      |         |
                            //      |    *    |
                            //      |         |
                            //      |         |
                            //      |         |
                            //      |         |
                            //      |         |
                            //      |    *    |
                            //      |_________|
                            //
                            else if (point1.X == point2.X)
                            {
                                double smallerY = 0;
                                double largerY = 0;

                                if (point1.Y <= point2.Y)
                                {
                                    smallerY = point1.Y;
                                    largerY = point2.Y;
                                }
                                else if (point2.Y < point1.Y)
                                {
                                    smallerY = point2.Y;
                                    largerY = point1.Y;
                                }

                                uperDimension = smallerY - snakeOffset;
                                lowerDimension = largerY + snakeOffset;

                                leftDimesion = point1.X - snakeOffset;
                                rightDimesion = point1.X + snakeOffset;
                            }
                            #endregion

                            #region Logic dealing with snakesegment collision
                            // collision happens 
                            if (leftDimesion <= x + objOffset && x - objOffset <= rightDimesion &&
                                uperDimension <= y + objOffset && y - objOffset <= lowerDimension)
                            {
                                return true;
                            }
                            #endregion
                        }
                    }
                }

            }
            return false;
        }

        /// <summary>
        /// Checks if an object colliede with a powerup when spawning in. Returns true if it does.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="objOffset"></param>
        /// <returns></returns>
        private bool PowerupCollisionHappened(double x, double y, double objOffset)
        {
            lock(worldOnServer.powerups)
            {
                foreach (Powerup powerup in worldOnServer.powerups.Values)
                {
                    if (powerup.IsDead)
                        continue;

                    Vector2D point = powerup.position;

                    #region Determine powerup's dimesions
                    //      powerup 
                    //       ______
                    //      |      |
                    //      |   *  |
                    //      |______|
                    //

                    double uperDimension = point.Y - powerupRadius;
                    double lowerDimension = point.Y + powerupRadius;

                    double leftDimesion = point.X - powerupRadius;
                    double rightDimesion = point.X + powerupRadius;
                    #endregion

                    #region Logic dealing with powerup collision
                    // collision happens
                    if (leftDimesion <= x + objOffset && x - objOffset <= rightDimesion &&
                        uperDimension <= y + objOffset && y - objOffset <= lowerDimension)
                    {
                        return true;
                    }
                    #endregion
                }
            }
            return false;
        }

        /// <summary>
        /// Boolean that checks if the snake head has collided with a powerup when run. Returns false if no collision, true if collision.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="objOffset"></param>
        /// <returns></returns>
        private bool SnakeCollidePowerupHappened(double x, double y, double objOffset)
        {
            lock(worldOnServer.powerups)
            {


                foreach (Powerup powerup in worldOnServer.powerups.Values)
                {
                    if (powerup.IsDead)
                        continue;

                    Vector2D point = powerup.position;

                    #region Determine powerup's dimesions
                    //      powerup 
                    //       ______
                    //      |      |
                    //      |   *  |
                    //      |______|
                    //

                    double uperDimension = point.Y - powerupRadius;
                    double lowerDimension = point.Y + powerupRadius;

                    double leftDimesion = point.X - powerupRadius;
                    double rightDimesion = point.X + powerupRadius;
                    #endregion

                    #region Logic dealing with powerup collision
                    // collision happens
                    if (leftDimesion <= x + objOffset && x - objOffset <= rightDimesion &&
                        uperDimension <= y + objOffset && y - objOffset <= lowerDimension)
                    {
                        powerup.IsDead = true;
                        return true;
                    }
                    #endregion
                }
            }
            return false;
        }
        /// <summary>
        /// Keeps snakes and powerups updated
        /// </summary>
        public void UpdateModel()
        {
            lock (worldOnServer.snakes)
            {
                foreach (Snake snake in worldOnServer.snakes.Values)
                {
                    UpdateSnake(snake);
                }
            }

            lock (worldOnServer.powerups)
            {
                UpdatePowerUp();
            }
        }
        /// <summary>
        /// Keeps the amount of powerups in the world consistent and removes and adds new ones.
        /// </summary>
        private void UpdatePowerUp()
        {
            lock (worldOnServer.powerups)
            {
                worldOnServer.powerSpawnFrameDelay--;

                int maxPowerPresent = 20;

                if (maxPowerPresent <= worldOnServer.powerups.Count)
                {
                    return;
                }


                foreach (Powerup powerup in worldOnServer.powerups.Values)
                {
                    if (powerup.SentToClientsWhenDeactived)
                        worldOnServer.powerups.Remove(powerup.ID);
                }


                if (worldOnServer.powerSpawnFrameDelay == 0)
                    spawnPowerup();
            }
        }

        /// <summary>
        /// Keeps track of each of the snake segments and behavior if it is dead, spawns, or hits a powerup.
        /// </summary>
        /// <param name="snake"></param>
        private void UpdateSnake(Snake snake)
        {
            lock (worldOnServer.snakes)
            {

                if (!snake.IsAlive)
                {
                    if (snake.DeadFrameCounter == gameSettings.RespawnRate)
                        spawnSnake(snake);

                    else
                    {
                        snake.DeadFrameCounter++;
                        return;
                    }
                }

                Vector2D headPosition = snake.GetHeadPosition();

                if (MovingCollisionOccured(headPosition.X, headPosition.Y, snakeOffset, snake.ID))
                {
                    snake.IsDead = true;
                    snake.IsAlive = false;
                    snake.Score = 0;
                    snake.DeadFrameCounter++;

                    return;
                }

                snake.Direction.Normalize();
                int snakeGrowthFramePerPowerup = 12;

                double deftSpeed = 3;
                Vector2D velocity = snake.Direction * deftSpeed;

                snake.SetHeadPosition(headPosition + velocity);
                headPosition = snake.GetHeadPosition();

                lock (worldOnServer.powerups)
                {
                    if (SnakeCollidePowerupHappened(headPosition.X, headPosition.Y, snakeOffset))
                    {
                        snake.PowerupGrowthFrameCounter += snakeGrowthFramePerPowerup;
                        snake.Score++;
                    }
                }

                if (snake.PowerupGrowthFrameCounter != 0)
                {
                    // do not update tail
                    snake.PowerupGrowthFrameCounter--;
                }
                else
                {
                    Vector2D tailPosition = snake.Position[0];
                    Vector2D turningP = snake.Position[1];

                    // Horizontal 
                    if (tailPosition.Y == turningP.Y)
                    {

                        if (tailPosition.X < turningP.X)
                        {
                            double newTailPosiX = tailPosition.X + deftSpeed;

                            if (turningP.X <= newTailPosiX)
                                snake.Position.Remove(snake.Position[0]);
                            else
                                snake.Position[0].X = newTailPosiX;
                        }
                        else if (turningP.X < tailPosition.X)
                        {
                            double newTailPosiX = tailPosition.X - deftSpeed;

                            if (newTailPosiX <= turningP.X)
                                snake.Position.Remove(snake.Position[0]);
                            else
                                snake.Position[0].X = newTailPosiX;
                        }
                    }
                    // Vertical
                    else if (tailPosition.X == turningP.X)
                    {
                        if (tailPosition.Y < turningP.Y)
                        {
                            double newTailPosiY = tailPosition.Y + deftSpeed;

                            if (turningP.Y <= newTailPosiY)
                                snake.Position.Remove(snake.Position[0]);
                            else
                                snake.Position[0].Y = newTailPosiY;
                        }
                        else if (turningP.Y < tailPosition.Y)
                        {
                            double newTailPosiY = tailPosition.Y - deftSpeed;

                            if (newTailPosiY <= turningP.Y)
                                snake.Position.Remove(snake.Position[0]);
                            else
                                snake.Position[0].Y = newTailPosiY;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles the messages recieved from the clients.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="command"></param>
        public void RecievedMessage(long ID, string command)
        {
            Snake snake = worldOnServer.snakes[ID];

            bool up = snake.Direction.Y == -1;
            bool down = snake.Direction.Y == 1;
            bool left = snake.Direction.X == -1;
            bool right = snake.Direction.X == 1;
            Vector2D newDir = new Vector2D();
            

            lock (worldOnServer)
            {
                if (command == "{\"moving\":\"up\"}\n")
                {

                    if (!left && !right)
                        return;
                    newDir = new Vector2D(0, -1);

                }
                else if (command == "{\"moving\":\"down\"}\n")
                {
                    if (!left && !right)
                        return;
                    newDir = new Vector2D(0, 1);

                }
                else if (command == "{\"moving\":\"left\"}\n")
                {
                    if (!up && !down)
                        return;
                    newDir = new Vector2D(-1, 0);
                }
                else if (command == "{\"moving\":\"right\"}\n")
                {
                    if (!up && !down)
                        return ;
                    newDir = new Vector2D(1, 0);
                }
                else
                {
                    return;
                }

                // Determines whether or not the snake is allowed to do a 180
                Vector2D thirdSeg = new Vector2D(0, 0);
                if(snake.Position.Count > 2)
                {
                    if (snake.Position[snake.Position.Count-2].Y == snake.Position[snake.Position.Count-3].Y)
                    {
                        if (snake.Position[snake.Position.Count - 2].X >= snake.Position[snake.Position.Count-3].X)
                        {
                            thirdSeg = new Vector2D(1, 0);
                        }
                        else
                        {
                            thirdSeg = new Vector2D(-1, 0);
                        }
                    }
                    else
                    {
                        if (snake.Position[snake.Position.Count - 2].Y >= snake.Position[snake.Position.Count-3].Y)
                        {
                            thirdSeg = new Vector2D(0, 1);
                        }
                        else
                        {
                            thirdSeg = new Vector2D(0, -1);
                        }
                    }
                    Vector2D head = snake.Position[snake.Position.Count - 1];
                    Vector2D second = snake.Position[snake.Position.Count - 2];
                    double disBetween = Math.Sqrt((head.X - second.X) * (head.X - second.X) + (head.Y - second.Y) * (head.Y - second.Y));
                    if (newDir.IsOppositeCardinalDirection(thirdSeg) && Math.Abs(disBetween) > 7)
                    {
                        snake.Direction = newDir;
                        snake.Position.Add(snake.Position[snake.Position.Count - 1]);
                    }
                    else if(!newDir.IsOppositeCardinalDirection(thirdSeg))
                    {
                        snake.Direction = newDir;
                        snake.Position.Add(snake.Position[snake.Position.Count - 1]);
                    }
                }
                else
                {
                    snake.Direction = newDir;
                        snake.Position.Add(snake.Position[snake.Position.Count - 1]);
                }
                
            }
            
        }

        /// <summary>
        /// Converts the data from the world to send to the clients and returns it to the server to be sent.
        /// </summary>
        /// <returns></returns>
        public string InfoToSend()
        {
            StringBuilder data = new StringBuilder();

            lock (worldOnServer.snakes)
            {
                foreach (Snake snake in worldOnServer.snakes.Values)
                {
                    data.AppendLine(JsonConvert.SerializeObject(snake));
                }
            }

            lock (worldOnServer.powerups)
            {
                foreach (Powerup power in worldOnServer.powerups.Values)
                {
                    if (power.IsDead)
                        power.SentToClientsWhenDeactived = true;

                    data.AppendLine(JsonConvert.SerializeObject(power));
                }
            }
            return data.ToString();
        }

    }
}
