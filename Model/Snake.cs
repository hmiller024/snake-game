using SnakeGame;
using Newtonsoft.Json;

namespace Model;
[JsonObject(MemberSerialization.OptIn)]

public class Snake
{
    [JsonProperty(PropertyName = "snake")]
    public long ID { get; private set; }
    [JsonProperty(PropertyName = "name")]
    public string Name { get; set; }
    [JsonProperty(PropertyName = "body")]
    public List<Vector2D> Position { get; set; }
    // snake's head's direction
    [JsonProperty(PropertyName = "dir")]
    public Vector2D Direction { get; set; }
    [JsonProperty(PropertyName = "score")]
    public int Score { get; set; }
    // snake is marked dead on a frame it die
    [JsonProperty(PropertyName = "died")]
    public bool IsDead { get; set; }
    [JsonProperty(PropertyName = "alive")]
    public bool IsAlive { get; set; }
    [JsonProperty(PropertyName = "dc")]
    public bool IsDisconnected { get; set; }
    [JsonProperty(PropertyName = "join")]
    public bool Joined { get; set; }

    public int DeadFrameCounter { get; set; }
    public int PowerupGrowthFrameCounter { get; set; }

    /// <summary>
    /// Default constructor for Json.
    /// </summary>
    private Snake()
    {
        ID = 0;
        Name = "";
        Position = new List<Vector2D>();
        Direction = new Vector2D();
        Score = 0;
        IsDead = true;
        IsAlive = false;
        IsDisconnected = true;
        Joined = false;
        DeadFrameCounter = 0;
        PowerupGrowthFrameCounter = 0;
    }

    public Snake(long ID, string name)
    {
        this.ID = ID;
        Name = name;
        Position = new List<Vector2D>();
        Direction = new Vector2D();
        Score = 0;
        IsDead = false;
        IsAlive = true;
        IsDisconnected = false;
        Joined = false;
        DeadFrameCounter = 0;
        PowerupGrowthFrameCounter = 0;
    }

    public bool SetHeadPosition(Vector2D headposition)
    {
        if (Position.Count == 0) return false;

        if (Position.Count >= 2)
            Position[Position.Count - 1] = headposition;
        else if (Position.Count == 1)
            Position.Add(headposition);
        return true;
    }
    public Vector2D GetHeadPosition()
    {
        return Position[Position.Count - 1];
    }


}


