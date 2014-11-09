using HtmlAgilityPack;
using Microsoft.Phone.Net.NetworkInformation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MyWay
{
  public static class Util
  {
    public static bool IsInternetAvailable()
    {
      var ni = NetworkInterface.NetworkInterfaceType;

      bool isConnected = false;

      if ((ni == NetworkInterfaceType.Wireless80211) || (ni == NetworkInterfaceType.MobileBroadbandCdma) || (ni == NetworkInterfaceType.MobileBroadbandGsm))
        isConnected = true;
      else if (ni == NetworkInterfaceType.None)
        isConnected = false;

      return isConnected;
    }

    public static bool IsStringContains(this string source, string toCheck) // case insensitive
    {
      return source.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static string TypographString(string s)
    {
      s = Regex.Replace(s, "\"([а-яА-Я0-9\\s\\-]+)\"", "«$1»"); // "слово" -> «слово»
      s = Regex.Replace(s, "([^0-9]|-[0-9])-((?!([а-я0-9]|Омск|Восточное))|[а-я]\\.)", "$1 — $2"); // текст-текст -> текст — текст
      s = Regex.Replace(s, " - ", " — "); // текст - текст -> текст — текст
      s = Regex.Replace(s, "[\\n\\t\\s]{2,}", " "); // текст  текст -> текст текст

      return s;
    }

    public static Color ConvertStringToColor(String hex)
    {
      // remove the # at the front
      hex = hex.Replace("#", "");

      byte a = 255;
      byte r = 255;
      byte g = 255;
      byte b = 255;

      int start = 0;

      // handle ARGB strings (8 characters long)
      if (hex.Length == 8)
      {
        a = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        start = 2;
      }

      // convert RGB characters to bytes
      r = byte.Parse(hex.Substring(start, 2), System.Globalization.NumberStyles.HexNumber);
      g = byte.Parse(hex.Substring(start + 2, 2), System.Globalization.NumberStyles.HexNumber);
      b = byte.Parse(hex.Substring(start + 4, 2), System.Globalization.NumberStyles.HexNumber);

      return Color.FromArgb(a, r, g, b);
    }

    public static double StringToDouble(string s)
    {
      return Double.Parse(Regex.Replace(s, "\\.", ","));
    }

    public static string GetThemeColor()
    {
      Visibility v = (Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"];

      // Write the theme background value.
      if (v == Visibility.Visible)
      {
        return "dark";
      }
      else
      {
        return "light";
      }
    }

    public static void Show(UIElement e)
    {
      e.Visibility = Visibility.Visible;
    }

    public static void Hide(UIElement e)
    {
      e.Visibility = Visibility.Collapsed;
    }

    /*****************************************
     Анимации 
    *****************************************/

    public static void DoubleAnimation(DependencyObject target, PropertyPath property, DoubleAnimation animation) // TODO: объединить
    {
      Storyboard sb = new Storyboard();

      Storyboard.SetTarget(animation, target);
      Storyboard.SetTargetProperty(animation, property);

      sb.Children.Add(animation);

      sb.Begin();
    }

    public static void ColorAnimation(DependencyObject target, PropertyPath property, ColorAnimation animation)
    {
      Storyboard sb = new Storyboard();

      Storyboard.SetTarget(animation, target);
      Storyboard.SetTargetProperty(animation, property);

      sb.Children.Add(animation);

      sb.Begin();
    }

    /*****************************************
     Карты
    *****************************************/
    public static class MapRoute
    {
      // Публичная модель
      public class Model
      {
        public IList<string[]> Coordinates { get; set; }
        public IList<_Models.Stations> Stations { get; set; }
      }

      // Внутренние модели, нужная для преобразования Json'а в .NET объект
      public class _Models
      {
        public class Main
        {
          [JsonProperty("features")]
          public IList<Features> Features { get; set; }

          [JsonProperty("stations")]
          public IList<Stations> Stations { get; set; }
        }

        // Маршрут
        public class Features
        {
          [JsonProperty("geometry")]
          public Geometry Geometry { get; set; }
        }

        public class Geometry
        {
          [JsonProperty("geometries")]
          public IList<Geometries> Geometries { get; set; }
        }

        public class Geometries
        {
          [JsonProperty("coordinates")]
          public IList<string[]> Coordinates { get; set; }
        }

        // Остановки
        public class Stations
        {
          [JsonProperty("id")]
          public int Id { get; set; }

          [JsonProperty("name")]
          public string Name { get; set; }

          [JsonProperty("coordinates")]
          public string[] Coordinates { get; set; }
        }
      }

      // Методы
      private async static Task<HtmlDocument> Download(int id)
      {
        if (Util.IsInternetAvailable())
        {
          string link = "http://bus.admomsk.ru/index.php/getroute/routecol_geo/" + id + "/undefined/";
          string htmlPage = "";

          try
          {
            htmlPage = await new HttpClient().GetStringAsync(link);
          }
          catch
          {
            return null;
          }

          Debug.WriteLine(id);

          HtmlDocument htmlDocument = new HtmlDocument();
          htmlDocument.LoadHtml(htmlPage);
          return htmlDocument;
        }
        else
          return null;
      }

      private async static Task<string> WriteAndGet(HtmlDocument html, int id)
      {
        string json = html.DocumentNode.InnerText;

        if (await Data.Folder.IsExists("Map") == false)
          await Data.Folder.Create("Map");

        await Data.File.Write("Map/" + id + ".db", json);

        return json;
      }

      public async static Task<Model> Get(int id)
      {
        string json;

        if (Data.File.IsExists("Map/" + id + ".db") == false)
          json = await WriteAndGet(await Download(id), id);
        else
          json = await Data.File.Read("Map/" + id + ".db");

        json = Regex.Replace(json, "[«»]", "\"");

        try
        {
          Debug.WriteLine(json);
          _Models.Main _m = JsonConvert.DeserializeObject<_Models.Main>(json);
          return new Model() { Coordinates = _m.Features[0].Geometry.Geometries[0].Coordinates, Stations = _m.Stations };
        }
        catch (Exception e)
        {
          Debug.WriteLine(e.Message);
          return null;
        }
      }
    }
  }
}
