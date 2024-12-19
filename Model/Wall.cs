using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SnakeGame;

namespace Model
{
    [JsonObject(MemberSerialization.OptIn)]
    [DataContract(Namespace = "")]
    public class Wall
    {
        [JsonProperty(PropertyName = "wall")]
        [DataMember(Order = 0, Name = "ID")]
        public int ID { get; private set; }
        [JsonProperty(PropertyName = "p1")]
        [DataMember(Order = 1, Name = "p1")]
        public Vector2D FirstPosition { get; private set; }
        [JsonProperty(PropertyName = "p2")]
        [DataMember(Order = 2, Name = "p2")]
        public Vector2D SecondPosition { get; private set; }

        /// <summary>
        /// Default constructor for Json.
        /// </summary>
        public Wall()
        {
            ID = 0;
            FirstPosition = new Vector2D();
            SecondPosition = new Vector2D();
        }

    }
}