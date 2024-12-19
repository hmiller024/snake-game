using Model;
using System.Runtime.Serialization;
using System.Xml.Linq;


/// <summary>
/// Object that contains all information parsed from the settings xml file.
/// </summary>
[DataContract(Namespace = "")]
public class GameSettings
{
    [DataMember(Order = 0, Name = "FramesPerShot")]
    public int FramesPerShot { get; set; }
    [DataMember(Order = 1, Name = "MSPerFrame")]
    public int MSPerFrame { get; set; }
    [DataMember(Order = 2, Name = "RespawnRate")]
    public int RespawnRate { get; set; }
    [DataMember(Order = 3, Name = "UniverseSize")]
    public int UniverseSize { get; set; }
    [DataMember(Order = 4, Name = "Walls")]
    public List<Wall> Walls { get; set; }
    public GameSettings()
    {
        Walls = null!;
    }
}
