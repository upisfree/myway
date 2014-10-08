using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library
{
  public class Model
  {
    public class Route
    {
      public Route(string number, string type, string desc, string toStop)
      {
        Number = number;
        Type = type;
        Desc = desc;
        ToStop = toStop;
      }

      public string Number { get; set; }
      public string Type   { get; set; }
      public string Desc   { get; set; }
      public string ToStop { get; set; }
    }

    public class Stop
    {
      public class Map
      {
        public int Id { get; set; }
        public double Lon { get; set; }
        public double Lat { get; set; }
        public string Name { get; set; }
      }

      public class List
      {
        public string Name { get; set; }
        public string Link { get; set; }
      }

      public class List_Comparer : IEqualityComparer<List>
      {
        public bool Equals(List x, List y)
        {
          return (Library.Util.IsStringContains(x.Name, y.Name));
        }

        public int GetHashCode(List obj)
        {
          return obj.GetHashCode();
        }
      }
    }
  }
}
