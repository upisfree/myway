﻿using HtmlAgilityPack;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Toolkit;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Devices.Geolocation;
using Windows.System;
using Location = System.Device.Location.GeoCoordinate;
using MathHelper = Microsoft.Xna.Framework.MathHelper;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace MyWay
{
  public partial class Map : PhoneApplicationPage
  {
    #region Системные методы
    public Map()
    {
      InitializeComponent();

      this.Loaded += (sender, e) =>
      {
        Init();

        // Токены карты
        Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = "f5a261f6-05a4-4d6b-b5b4-4cdcf97351b6";
        Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = "iIHLArElHh4MMH3piVYXTw";
        
        // Инициализация настроек карты
        // режим просмотра
        switch (Data.Settings.GetOrDefault("MapViewMode", "карта"))
        {
          case "карта":
            MapPanel.CartographicMode = MapCartographicMode.Road;
            break;
          case "спутник":
            MapPanel.CartographicMode = MapCartographicMode.Aerial;
            break;
          case "карта+спутник":
            MapPanel.CartographicMode = MapCartographicMode.Hybrid;
            break;
          case "карта+рельеф":
            MapPanel.CartographicMode = MapCartographicMode.Terrain;
            break;
        }

        // цвет
        switch (Data.Settings.GetOrDefault("MapColorMode", "светлый"))
        {
          case "светлый":
            MapPanel.ColorMode = MapColorMode.Light;
            break;
          case "тёмный":
            MapPanel.ColorMode = MapColorMode.Dark;
            break;
        }
        // </>

        string mode = "";
        string id = "";
        string name = "";
        string desc = "";
        string lat = "";
        string lon = "";

        if (NavigationContext.QueryString.TryGetValue("name", out name))
          Name.Text = name.ToUpper();

        if (NavigationContext.QueryString.TryGetValue("desc", out desc))
          Desc.Text = desc.ToUpper();

        if (NavigationContext.QueryString.TryGetValue("mode", out mode) && NavigationContext.QueryString.TryGetValue("id", out id))
          switch (mode)
          {
            case "route":
            case "settings":
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
              ShowRoute(id); // await убрал
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
              break;
            case "stop":
              if (NavigationContext.QueryString.TryGetValue("lon", out lon) && NavigationContext.QueryString.TryGetValue("lat", out lat))
                ShowStop(id, lat, lon);
              break;
          }
      };
    }
    
    protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
    {
      base.OnNavigatedTo(e);
    }

    protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
    {
      // Stop AR services
      //ARDisplay.StopServices();

      base.OnNavigatedFrom(e);
    }

    // Нажатие на клавишу «Назад»
    protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
    {
    }

    #endregion

    #region Модели

    /*****************************************
     Модели
    *****************************************/

    private class Model
    {
      public IList<string[]> Coordinates { get; set; }
      public IList<_Models.Stations> Stations { get; set; }
    }

    public class Search_Model
    {
      public string Title { get; set; }
      public string Desc { get; set; }
      public int Id { get; set; }
    }

    /*****************************************
     Json 
    *****************************************/

    private class _Models
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

    #endregion

    #region IO
    private class IO
    {
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

          HtmlDocument htmlDocument = new HtmlDocument();
          htmlDocument.LoadHtml(htmlPage);
          return htmlDocument;
        }
        else
          return null;
      }

      private async static Task<string> WriteAndGet(HtmlDocument html, int id)
      {
        try
        {
          string json = html.DocumentNode.InnerText;

          if (await Data.Folder.IsExists("Map") == false)
            await Data.Folder.Create("Map");

          await Data.File.Write("Map/" + id + ".db", json);

          return json;
        }
        catch
        {
          return null;
        }
      }

      public async static Task<Model> Get(int id)
      {
        string json;

        if (Data.File.IsExists("Map/" + id + ".db") == false)
          json = await WriteAndGet(await Download(id), id);
        else
          json = await Data.File.Read("Map/" + id + ".db");

        try
        {
          json = Regex.Replace(json, "[«»]", "\"");

          _Models.Main _m = JsonConvert.DeserializeObject<_Models.Main>(json);
          return new Model() { Coordinates = _m.Features[0].Geometry.Geometries[0].Coordinates, Stations = _m.Stations };
        }
#pragma warning disable CS0168 // The variable 'e' is declared but never used
        catch (Exception e)
#pragma warning restore CS0168 // The variable 'e' is declared but never used
        {
          return null;
        }
      }
    }

    #endregion

    /*****************************************
     Название бы придумать 
    *****************************************/

    private void Init()
    {
      ShowUser(false);

      System.Windows.Threading.DispatcherTimer userTimer = new System.Windows.Threading.DispatcherTimer();
      userTimer.Interval = TimeSpan.FromMilliseconds(20000);
      userTimer.Tick += new EventHandler((sender, e) =>
      {
        ShowUser(false);
      });
      userTimer.Start();
    }

    private int _mapUsersLayerInt = -1;
    private void DrawUser(GeoCoordinate coordinate)
    {
      Image img = new Image();
      BitmapImage b = new BitmapImage();
      b.UriSource = new Uri("/Assets/MapUser.png", UriKind.Relative);
      img.Source = b;
      img.Height = 75;
      img.Width = 40;

      MapOverlay overlay = new MapOverlay();
      overlay.Content = img;
      overlay.PositionOrigin = new Point(0.5, 0.5);
      overlay.GeoCoordinate = coordinate;

      MapLayer layer = new MapLayer();
      layer.Add(overlay);

      if (_mapUsersLayerInt == -1)
      {
        MapPanel.Layers.Add(layer);

        _mapUsersLayerInt = MapPanel.Layers.Count - 1;
      }
      else
        MapPanel.Layers[_mapUsersLayerInt] = layer;
    }
    private void ShowUser(bool focus)
    {
      try // определение местоположения
      {
        Location currentPosition = ARDisplay.Location;

        if (focus)
        {
          MapPanel.SetView(currentPosition, 13);
        }

        DrawUser(currentPosition);
      }
#pragma warning disable CS0168 // The variable 'e' is declared but never used
      catch (Exception e)
#pragma warning restore CS0168 // The variable 'e' is declared but never used
      {
        MessageBoxResult mbr = MessageBox.Show("Не могу отобразить тебя на карте, так как у тебя отключено определение местоположения.\nОткрыть настройки, чтобы включить его?", "Местоположение", MessageBoxButton.OKCancel);

        if (mbr == MessageBoxResult.OK)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
          Launcher.LaunchUriAsync(new Uri("ms-settings-location:"));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
        }
      }
    }
    private void AppBar_ShowUser(object sender, EventArgs e)
    {
      ShowUser(true);
    }

    private int _mapRoadLayerInt = -1;
    private void DrawRoute(Model data)
    {
      if (data == null)
      {
        MapPolyline _line = new MapPolyline();
        _line.StrokeThickness = 0;

        if (_mapRoadLayerInt == -1) // да, дубляция, знаю.
        {
          MapPanel.MapElements.Add(_line);

          _mapRoadLayerInt = MapPanel.MapElements.Count - 1;
        }
        else
          MapPanel.MapElements[_mapRoadLayerInt] = _line;

        return;
      }

      MapPolyline line = new MapPolyline();
      line.StrokeColor = Util.ConvertStringToColor("#FF101D80");
      line.StrokeThickness = 2;

      for (int i = 0; i <= data.Coordinates.Count - 1; i++)
      {
        string[] b = data.Coordinates[i];

        line.Path.Add(new GeoCoordinate() { Longitude = Util.StringToDouble(b[0]), Latitude = Util.StringToDouble(b[1]) });
      }

      if (_mapRoadLayerInt == -1)
      {
        MapPanel.MapElements.Add(line);

        _mapRoadLayerInt = MapPanel.MapElements.Count - 1;
      }
      else
        MapPanel.MapElements[_mapRoadLayerInt] = line;
    }

    private int _mapStopsLayerInt = -1;
    private void DrawStops(Model data)
    {
      if (data == null)
      {
        if (_mapStopsLayerInt == -1) // да, дубляция, знаю.
        {
          MapPanel.Layers.Add(new MapLayer());

          _mapStopsLayerInt = MapPanel.Layers.Count - 1;
        }
        else
          MapPanel.Layers[_mapStopsLayerInt] = new MapLayer();

        return;
      }

      // Отрисовка остановок
      MapLayer layer = new MapLayer();

      for (int i = 0; i <= data.Stations.Count - 1; i++)
      {
        _Models.Stations b = data.Stations[i];

        Border border = new Border();
        border.Width = 20;
        border.Height = 20;
        border.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        border.BorderThickness = new Thickness(1);
        border.BorderBrush = new SolidColorBrush(Util.ConvertStringToColor("#FF455580"));
        border.CornerRadius = new CornerRadius(100);
        border.Tag = b.Id + "|" + Util.TypographString(b.Name);
        border.Tap += (sender, e) =>
        {
          string[] str = ((Border)sender).Tag.ToString().Split(new Char[] { '|' });

          MessageBoxResult mbr = MessageBox.Show("Открыть прогнозы для этой остановки?", str[1], MessageBoxButton.OKCancel);

          if (mbr == MessageBoxResult.OK)
          {
            (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/Predicts.xaml?link=" + "http://t.bus55.ru/index.php/app/get_predict/" + str[0] + "&name=" + str[1], UriKind.Relative));
          }
        };

        MapOverlay overlay = new MapOverlay();
        overlay.Content = border;
        overlay.PositionOrigin = new Point(0.5, 0.5);
        overlay.GeoCoordinate = new GeoCoordinate() { Longitude = Util.StringToDouble(b.Coordinates[0]), Latitude = Util.StringToDouble(b.Coordinates[1]) };

        layer.Add(overlay);
      }

      if (_mapStopsLayerInt == -1)
      {
        MapPanel.Layers.Add(layer);

        _mapStopsLayerInt = MapPanel.Layers.Count - 1;
      }
      else
        MapPanel.Layers[_mapStopsLayerInt] = layer;
    }

    private System.Windows.Threading.DispatcherTimer _busTimer = new System.Windows.Threading.DispatcherTimer();
    private int _mapBusesLayerInt = -1;
    private int _mapPushpinsLayerInt = -1;
    private class BusesModel
    {
      public class Main
      {
        [JsonProperty("vehicles")]
        public IList<Vehicle> Vehicles { get; set; }
      }

      public class Vehicle
      {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("coordinates")]
        public string[] Coordinates { get; set; }

        [JsonProperty("info")]
        public string Info { get; set; }

        [JsonProperty("course")]
        public int Course { get; set; }
      }
    }
    private void DrawBuses(int id)
    {
      // убираем пушпины
      if (_mapPushpinsLayerInt == -1)
      {
        MapPanel.Layers.Add(new MapLayer());

        _mapPushpinsLayerInt = MapPanel.Layers.Count - 1;
      }
      else
        MapPanel.Layers[_mapPushpinsLayerInt] = new MapLayer();

      if (id == 0)
      {
        if (_mapBusesLayerInt == -1) // да, дубляция, знаю.
        {
          MapPanel.Layers.Add(new MapLayer());

          _mapBusesLayerInt = MapPanel.Layers.Count - 1;
        }
        else
          MapPanel.Layers[_mapBusesLayerInt] = new MapLayer();

        return;
      }

      // Отрисовка автобусов
      MapLayer layer = new MapLayer();

      var client = new WebClient();

      client.Headers["If-Modified-Since"] = DateTimeOffset.Now.ToString(); // отключение кэширования

      client.DownloadStringCompleted += (sender, e) =>
      {
        HtmlDocument htmlDocument = new HtmlDocument();
        try
        {
          htmlDocument.LoadHtml(e.Result);
          string json = htmlDocument.DocumentNode.InnerText;
          json = Regex.Replace(json, "[«»]", "\"");

          BusesModel.Main b = JsonConvert.DeserializeObject<BusesModel.Main>(json);

          if (b.Vehicles.Count == 0)
          {
            if (_mapBusesLayerInt == -1) // да, уже три раза!
            {
              MapPanel.Layers.Add(new MapLayer());

              _mapBusesLayerInt = MapPanel.Layers.Count - 1;
            }
            else
              MapPanel.Layers[_mapBusesLayerInt] = new MapLayer();

            return;
          }

          foreach (BusesModel.Vehicle a in b.Vehicles)
          {
            int course = 45 * (int)Math.Round(a.Course / 45.0);
            
            Grid grid = new Grid();
            Image bus = new Image();
            BitmapImage bi = new BitmapImage();
            bi.UriSource = new Uri("/Assets/Buses/" + course + ".png", UriKind.Relative);
            
            bus.Source = bi;
            bus.Width = 45;
            bus.Height = 45;
            bus.Tag = "http://t.bus55.ru/index.php/app/get_stations/" + id + "|" + Util.TypographString(a.Info);
            bus.Tap += (sender2, e2) =>
            {
              string str = ((Image)sender2).Tag.ToString();

              if (_mapPushpinsLayerInt == -1)
              {
                MapPanel.Layers.Add(new MapLayer());

                _mapPushpinsLayerInt = MapPanel.Layers.Count - 1;
              }
              else
                MapPanel.Layers[_mapPushpinsLayerInt] = new MapLayer();

              MapLayer _layer = new MapLayer();
              Pushpin pushpin = new Pushpin();

              pushpin.GeoCoordinate = new GeoCoordinate() { Longitude = Util.StringToDouble(a.Coordinates[0]), Latitude = Util.StringToDouble(a.Coordinates[1]) };
              pushpin.Content = Util.TypographString(a.Info);
              pushpin.Margin = new Thickness() { Top = -60 };

              MapOverlay _overlay = new MapOverlay();
              _overlay.Content = pushpin;
              _overlay.GeoCoordinate = new GeoCoordinate() { Longitude = Util.StringToDouble(a.Coordinates[0]), Latitude = Util.StringToDouble(a.Coordinates[1]) };
              _layer.Add(_overlay);

              MapPanel.Layers[_mapPushpinsLayerInt] = _layer;
            };

            grid.Width = 50;
            grid.Height = 50;
            grid.Children.Add(bus);

            MapOverlay overlay = new MapOverlay();
            overlay.Content = grid;
            overlay.PositionOrigin = new Point(0.5, 0.5);
            overlay.GeoCoordinate = new GeoCoordinate() { Longitude = Util.StringToDouble(a.Coordinates[0]), Latitude = Util.StringToDouble(a.Coordinates[1]) };

            layer.Add(overlay);
          }
        }
        catch { }
      };

      client.DownloadStringAsync(new Uri("http://bus.admomsk.ru/index.php/getroute/getbus/" + id));

      if (_mapBusesLayerInt == -1)
      {
        MapPanel.Layers.Add(layer);

        _mapBusesLayerInt = MapPanel.Layers.Count - 1;
      }
      else
        MapPanel.Layers[_mapBusesLayerInt] = layer;
    }

    private async Task ShowRoute(string _id)
    {
      _busTimer.Stop();

      int id = Convert.ToInt32(_id);

      Model data = await IO.Get(id);

      if (data == null) // не можем загрузить? не можем нормально распарсить? валим. (у меня, например, если нет денег, Билайн отдаёт html страницу и json.net умирает)
      {
        MessageBox.Show("Произошла ошибка при загрузке маршрута.\nМожет, нет подключения к сети?\n\nОшибка не пропадает? Очисти кэш (в настройках).", "Ошибка!", MessageBoxButton.OK);

        DrawRoute(null);

        return;
      }

      DrawRoute(data);
      DrawStops(data);
      DrawBuses(id);

      double a = Util.StringToDouble(data.Coordinates[Convert.ToInt32(data.Coordinates.Count / 1.5)][1]);
      double b = Util.StringToDouble(data.Coordinates[Convert.ToInt32(data.Coordinates.Count / 1.5)][0]);

      MapPanel.SetView(new GeoCoordinate(a, b), 11.5);

      _busTimer.Interval = TimeSpan.FromMilliseconds(20000);
      _busTimer.Tick += new EventHandler((sender2, e2) =>
      {
        DrawBuses(id);
      });
      _busTimer.Start();
    }

    private void ShowStop(string _id, string _lat, string _lon) // да, дубляция, иди нахуй, всё равно его больше развивать не планирую, только не на Silverlight'е
    {
      int id = Convert.ToInt32(_id);
      double lat = Util.StringToDouble(_lat);
      double lon = Util.StringToDouble(_lon);

      GeoCoordinate coordinate = new GeoCoordinate(lat, lon);

      MapLayer layer = new MapLayer();

      Border border = new Border();
      border.Width = 20;
      border.Height = 20;
      border.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
      border.BorderThickness = new Thickness(1);
      border.BorderBrush = new SolidColorBrush(Util.ConvertStringToColor("#FF455580"));
      border.CornerRadius = new CornerRadius(100);
      border.Tag = id + "|" + lon + "|" + lat + "|" + Util.TypographString(Name.Text);
      border.Tap += (sender, e) =>
      {
        string[] str = ((Border)sender).Tag.ToString().Split(new Char[] { '|' });

        MessageBoxResult mbr = MessageBox.Show("Открыть прогнозы для этой остановки?", str[3], MessageBoxButton.OKCancel);

        if (mbr == MessageBoxResult.OK)
        {
          (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/Predicts.xaml?link=" + "http://t.bus55.ru/index.php/app/get_predict/" + str[0] + "&name=" + str[3], UriKind.Relative));
        }
      };

      MapOverlay overlay = new MapOverlay();
      overlay.Content = border;
      overlay.PositionOrigin = new Point(0.5, 0.5);
      overlay.GeoCoordinate = new GeoCoordinate() { Longitude = lon, Latitude = lat };

      layer.Add(overlay);

      MapPanel.Layers.Add(layer);

      MapPanel.Center = coordinate;
      MapPanel.ZoomLevel = 16;
    }
  }
}