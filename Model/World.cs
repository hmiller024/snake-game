using System;
using System.Numerics;
using Model;
public class World
{
    public Dictionary<long, Snake> snakes;
    public Dictionary<long, Powerup> powerups;
    public Dictionary<long, Wall> walls;
    public long playerID { get; set; }

    public int powerSpawnFrameDelay { get; set; }
    public int Size { get;  set; }
    public World(int size)
    {
        snakes = new Dictionary<long, Snake>();
        powerups = new Dictionary<long, Powerup>();
        walls = new Dictionary<long, Wall>();
        playerID = 0;
        this.Size = size;
        powerSpawnFrameDelay = 0;
    }

}
