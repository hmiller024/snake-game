using Newtonsoft.Json;
using SnakeGame;
using System;
using System.ComponentModel;

[JsonObject(MemberSerialization.OptIn)]
public class Powerup
{
    [JsonProperty(PropertyName = "power")]
    public readonly long ID;
    [JsonProperty(PropertyName = "loc")]
    public Vector2D position { get; set; }
    [JsonProperty(PropertyName = "died")]
    public bool IsDead { get; set; }

    private static long nextID = 0;
    private static readonly object mutexForID = new object();
    public bool SentToClientsWhenDeactived { get; set; }
    /// <summary>
    /// Default constructor for Json.
    /// </summary>
    public Powerup()
	{
		position = new Vector2D();
		IsDead = false;
        SentToClientsWhenDeactived = false;
	}

    public Powerup(Vector2D position)
    {
        lock (mutexForID)
        {
            ID = nextID++;
        }
        this.position = position;
        IsDead = false;
        SentToClientsWhenDeactived = false;
    }
}
