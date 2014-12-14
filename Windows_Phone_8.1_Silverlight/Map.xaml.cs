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
using GART;
using Location = System.Device.Location.GeoCoordinate;
using GART.Data;
using GART.Controls;
using GART.BaseControls;


namespace MyWay
{
  public partial class Map : PhoneApplicationPage
  {
    public Map()
    {
      InitializeComponent();

      Init();
    }

    protected async override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
    {
      ARDisplay.StartServices();

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
        if (mode == "route")
          await ShowRoute(id);
        else if (mode == "stop" && NavigationContext.QueryString.TryGetValue("lon", out lon) && NavigationContext.QueryString.TryGetValue("lat", out lat))
          ShowStop(id, lat, lon);

      AddNearbyLabels();

      base.OnNavigatedTo(e);
    }

    protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
    {
      // Stop AR services
      ARDisplay.StopServices();

      base.OnNavigatedFrom(e);
    }

    /// <summary>
    /// To support any orientation, override this method and call
    /// ARDisplay.HandleOrientationChange() method
    /// </summary>
    /// <param name="e"></param>
    protected override void OnOrientationChanged(OrientationChangedEventArgs e)
    {
      base.OnOrientationChanged(e);
      
      ControlOrientation orientation = ControlOrientation.Default;

      switch (e.Orientation)
      {
        case PageOrientation.LandscapeLeft:
          orientation = ControlOrientation.Clockwise270Degrees;
          break;
        case PageOrientation.LandscapeRight:
          orientation = ControlOrientation.Clockwise90Degrees;
          break;
      }

      ARDisplay.Orientation = orientation;
    }


    // Нажатие на клавишу «Назад»
    protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
    {
    }

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

    /*****************************************
     IO 
    *****************************************/

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
        catch (Exception e)
        {
          return null;
        }
      }
    }

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
      catch (Exception e)
      {
        MessageBoxResult mbr = MessageBox.Show("Не могу отобразить тебя на карте, так как у тебя отключено определение местоположения.\nОткрыть настройки, чтобы включить его?", "Местоположение", MessageBoxButton.OKCancel);

        if (mbr == MessageBoxResult.OK)
        {
          Launcher.LaunchUriAsync(new Uri("ms-settings-location:"));
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
      line.StrokeThickness = 7;

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
        Image img = new Image();
        BitmapImage bi = new BitmapImage();
        bi.UriSource = new Uri("/Assets/stop.png", UriKind.Relative);
        img.Source = bi;
        img.Height = 20;
        img.Width = 20;

        border.Child = img;
        border.Width = 35;
        border.Height = 35;
        border.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        border.BorderThickness = new Thickness(2);
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
            Image img = new Image();
            BitmapImage bi = new BitmapImage();
            bi.UriSource = new Uri("/Assets/bus.png", UriKind.Relative);
            img.Source = bi;
            img.Height = 25;
            img.Width = 100;
            //img.RenderTransform = new RotateTransform() { Angle = a.Course };
            img.Tag = "http://t.bus55.ru/index.php/app/get_stations/" + id + "|" + Util.TypographString(a.Info);
            img.Tap += (sender2, e2) =>
            {
              string str = ((Image)sender2).Tag.ToString();

              MapLayer _layer = new MapLayer();
              Pushpin pushpin = new Pushpin();

              Debug.WriteLine(Util.StringToDouble(a.Coordinates[0]) + " " + Util.StringToDouble(a.Coordinates[1]));

              pushpin.GeoCoordinate = new GeoCoordinate() { Longitude = Util.StringToDouble(a.Coordinates[0]), Latitude = Util.StringToDouble(a.Coordinates[1]) };
              pushpin.Content = Util.TypographString(a.Info);
              //pushpin.RenderTransform.Transform(new Point(1, -100));

              MapOverlay _overlay = new MapOverlay();
              _overlay.Content = pushpin;
              _overlay.GeoCoordinate = new GeoCoordinate() { Longitude = Util.StringToDouble(a.Coordinates[0]), Latitude = Util.StringToDouble(a.Coordinates[1]) };
              _layer.Add(_overlay);

              MapPanel.Layers.Add(_layer);
            };
            img.LostFocus += (sender3, e3) =>
            {
              //pu
            };

            MapOverlay overlay = new MapOverlay();
            overlay.Content = img;
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

      _busTimer.Interval = TimeSpan.FromMilliseconds(30000);
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
      Image img = new Image();
      BitmapImage bi = new BitmapImage();
      bi.UriSource = new Uri("/Assets/stop.png", UriKind.Relative);
      img.Source = bi;
      img.Height = 25;
      img.Width = 25;

      border.Child = img;
      border.Width = 35;
      border.Height = 35;
      border.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
      border.BorderThickness = new Thickness(2);
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
    
    /*****************************************
     Всё, что связано с доп. реальностью
    *****************************************/

    public class MapItem : ARItem
    {
      private string _description;

      public string Description
      {
        get
        {
          return _description;
        }
        set
        {
          if (_description != value)
          {
            _description = value;
            //NotifyPropertyChanged(() => Desc);
          }
        }
      }
    }

    private void AddLabel(Location location, string name, string desc)
    {
      // We'll use the specified text for the content and we'll let 
      // the system automatically project the item into world space
      // for us based on the Geo location.
      MapItem item = new MapItem()
      {
        Content = name,
        Description = desc,
        GeoLocation = location,
      };

      ARDisplay.ARItems.Add(item);
    }

    private void AddNearbyLabels()
    {
      // Start with the current location
      var current = ARDisplay.Location;

      // загрузка данных
      var client = new WebClient();

      client.Headers["If-Modified-Since"] = DateTimeOffset.Now.ToString(); // отключение кэширования

      client.DownloadStringCompleted += (sender2, e2) =>
      {
        HtmlDocument htmlDocument = new HtmlDocument();

        try
        {
          htmlDocument.LoadHtml(e2.Result);
          string json = htmlDocument.DocumentNode.InnerText;
          json = Regex.Replace(json, "[«»]", "\"");

          MyWay.MainPage.Stops.Model_Near[] b = JsonConvert.DeserializeObject<MyWay.MainPage.Stops.Model_Near[]>(json);

          foreach (MyWay.MainPage.Stops.Model_Near a in b)
          {
            string name = Util.TypographString(a.Name);
            Debug.WriteLine(a.Name);
            // Create a new location based on the users location plus
            // a random offset.
            Location offset = new Location()
            {
              Latitude = Util.StringToDouble(a.Lat),
              Longitude = Util.StringToDouble(a.Lon),
              Altitude = Double.NaN // NaN will keep it on the horizon
            };

            AddLabel(offset, name, "sdfjksdnjfjsdfjnsdjnfdsj");
          }
        }
        catch { }
      };

      client.DownloadStringAsync(new Uri("http://t.bus55.ru/index.php/app/get_stations_geoloc_json/" + Regex.Replace(current.Latitude.ToString(), ",", ".") + "/" + Regex.Replace(current.Longitude.ToString(), ",", ".")));
    }

    private void ApplicationBarIconButton_Click(object sender, EventArgs e)
    {
      if (MapPanel.Visibility == System.Windows.Visibility.Visible)
      {
        MapPanel.Visibility = System.Windows.Visibility.Collapsed;
        ARDisplay.Visibility = System.Windows.Visibility.Visible;
      }
      else
      {
        MapPanel.Visibility = System.Windows.Visibility.Visible;
        ARDisplay.Visibility = System.Windows.Visibility.Collapsed;
      }
    }

    private void ApplicationBarIconButton_Click_1(object sender, EventArgs e)
    {
      if (OverheadMap.Visibility == System.Windows.Visibility.Visible)
        OverheadMap.Visibility = System.Windows.Visibility.Collapsed;
      else
        OverheadMap.Visibility = System.Windows.Visibility.Visible;
    }
  }
}