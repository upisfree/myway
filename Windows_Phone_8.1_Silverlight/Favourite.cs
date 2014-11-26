using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyWay
{
  class Favourite
  {
    public class Model
    {
      public List<MyWay.MainPage.Routes.Model> Routes { get; set; }
      public List<MyWay.MainPage.Stops.Model_XAML> Stops { get; set; }
    }

    public static async Task<Model> ReadFile()
    {
      if (Data.File.IsExists("favourite.json") == false)
        return null;

      string json = await Data.File.Read("favourite.json");

      if (json == "" || json == null)
        return null;

      return JsonConvert.DeserializeObject<Model>(json);
    }

    public static async Task WriteToFile(string str, string mode)
    {
      Model data = await ReadFile();

      if (data == null)
        data = new Model() { Routes = new List<MyWay.MainPage.Routes.Model>(), Stops = new List<MyWay.MainPage.Stops.Model_XAML>() };

      switch (mode)
      {
        case "Route":
          string[] a = str.Split(new Char[] { '|' });
          string[] b = a[1].Split(new Char[] { ' ' });
          string c = b[1];

          if (b.Length == 3)
            c += " " + b[2];

          MyWay.MainPage.Routes.Model model = new MyWay.MainPage.Routes.Model() { Number = b[0], Type = c, Desc = a[2], ToStop = str };
          data.Routes.Add(model);
          break;
        case "Stop":
          string[] a2 = str.Split(new Char[] { '|' });

          MyWay.MainPage.Stops.Model_XAML model2 = new MyWay.MainPage.Stops.Model_XAML() { Name = a2[3], All = str };
          data.Stops.Add(model2);
          break;
      }

      await Data.File.WriteJson("favourite.json", JsonConvert.SerializeObject(data));
    }
  }
}
