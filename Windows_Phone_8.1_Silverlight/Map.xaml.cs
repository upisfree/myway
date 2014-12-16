using HtmlAgilityPack;
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
using System.Windows.Media.Animation;


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

      ARInit(); // инициализируем дополненную реальность

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

      double h = Application.Current.Host.Content.ActualHeight;

      if (e.Orientation == PageOrientation.Portrait || e.Orientation == PageOrientation.PortraitDown || e.Orientation == PageOrientation.PortraitUp)
      {
        Animation(MapPanel_Transform, h, 0, 0.5);
        Animation(ARDisplay_Transform, 0, h, 0.5);
      }
      else
      {
        ControlOrientation orientation = ControlOrientation.Default;

        if (e.Orientation == PageOrientation.LandscapeLeft)
          orientation = ControlOrientation.Clockwise270Degrees;
        else if (e.Orientation == PageOrientation.LandscapeRight)
          orientation = ControlOrientation.Clockwise90Degrees;

        Animation(MapPanel_Transform, 0, h, 0.5);
        Animation(ARDisplay_Transform, h, 0, 0.5);

        ARDisplay.Orientation = orientation;
      }
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

      System.Windows.Threading.DispatcherTimer ARTimer = new System.Windows.Threading.DispatcherTimer();
      ARTimer.Interval = TimeSpan.FromMilliseconds(60000);
      ARTimer.Tick += new EventHandler((sender, e) =>
      {
        ARInit();
      });
      ARTimer.Start();
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

    public class ARModel
    {
      public class Stop : ARItem
      {
        public string Image
        {
          get
          {
            return "/Assets/stop_white.png";
          }
        }

        private string _toPredict;
        public string ToPredict
        {
          get
          {
            return _toPredict;
          }
          set
          {
            if (_toPredict != value)
            {
              _toPredict = value;
            }
          }
        }

        private string _distance;
        public string Distance
        {
          get
          {
            return _distance;
          }
          set
          {
            if (_distance != value)
            {
              _distance = value;
            }
          }
        }
      }

      public class Bus : ARItem
      {
        public string Image
        {
          get
          {
            return "/Assets/bus_white.png";
          }
        }

        private string _distance;
        public string Distance
        {
          get
          {
            return _distance;
          }
          set
          {
            if (_distance != value)
            {
              _distance = value;
            }
          }
        }

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
            }
          }
        }
      }
    }

    private int _stopsNearbyLayer = -1;
    private void ARInit() // близжайшие автобусы?
    {
      var location = ARDisplay.Location;

      // грузим список близжайших остановок
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

          MyWay.MainPage.Stops.Model_Near[] b = JsonConvert.DeserializeObject<MyWay.MainPage.Stops.Model_Near[]>(json);

          // чистим список предметов в доп. реальности
          ARDisplay.ARItems.Clear();

          // подготавливаем остановки
          if (_stopsNearbyLayer == -1) // да, дубляция, знаю.
          {
            MapPanel.Layers.Add(new MapLayer());

            _stopsNearbyLayer = MapPanel.Layers.Count - 1;
          }
          else
            MapPanel.Layers[_stopsNearbyLayer] = new MapLayer();

          MapLayer layer = new MapLayer();

          foreach (MyWay.MainPage.Stops.Model_Near a in b)
          {
            // Добавление остановок в доп. реальность
            ARDisplay.ARItems.Add(new ARModel.Stop()
            {
              Content = Util.TypographString(a.Name),
              ToPredict = a.Id.ToString() + "|" + Util.TypographString(a.Name),
              Distance = Math.Round(location.GetDistanceTo(new Location() { Longitude = Util.StringToDouble(a.Lon), Latitude = Util.StringToDouble(a.Lat) })).ToString() + " м",
              GeoLocation = new Location() { Longitude = Util.StringToDouble(a.Lon), Latitude = Util.StringToDouble(a.Lat), Altitude = Double.NaN }
            });

            // Отрисовка остановок
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
            border.Tag = a.Id + "|" + Util.TypographString(a.Name);
            border.Tap += (sender2, e2) =>
            {
              string[] str = ((Border)sender2).Tag.ToString().Split(new Char[] { '|' });

              MessageBoxResult mbr = MessageBox.Show("Открыть прогнозы для этой остановки?", str[1], MessageBoxButton.OKCancel);

              if (mbr == MessageBoxResult.OK)
              {
                (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/Predicts.xaml?link=" + "http://t.bus55.ru/index.php/app/get_predict/" + str[0] + "&name=" + str[1], UriKind.Relative));
              }
            };

            MapOverlay overlay = new MapOverlay();
            overlay.Content = border;
            overlay.PositionOrigin = new Point(0.5, 0.5);
            overlay.GeoCoordinate = new GeoCoordinate() { Longitude = Util.StringToDouble(a.Lon), Latitude = Util.StringToDouble(a.Lat) };

            layer.Add(overlay);
            // конечно, можно вынести это в отдельный метод, чтобы третий раз не писать, но это уже говнокод, а поддерживать его я не особо собираюсь.
          }

          if (_stopsNearbyLayer == -1)
          {
            MapPanel.Layers.Add(layer);

            _stopsNearbyLayer = MapPanel.Layers.Count - 1;
          }
          else
            MapPanel.Layers[_stopsNearbyLayer] = layer;
        }
        catch { }
      };

      client.DownloadStringAsync(new Uri("http://t.bus55.ru/index.php/app/get_stations_geoloc_json/" + Regex.Replace(location.Latitude.ToString(), ",", ".") + "/" + Regex.Replace(location.Longitude.ToString(), ",", ".")));
    }

    private void ARItem_Tap(object sender, System.Windows.Input.GestureEventArgs e)
    {
      string[] str = ((Grid)sender).Tag.ToString().Split(new Char[] { '|' });

      

      (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/Predicts.xaml?link=" + "http://t.bus55.ru/index.php/app/get_predict/" + str[0] + "&name=" + str[1], UriKind.Relative));
    }

    // Анимации
    private void Animation(TranslateTransform t, double from, double to, double time, double amplitude = 0, EasingMode mode = EasingMode.EaseOut)
    {
      DoubleAnimation da = new DoubleAnimation();

      da.From = from;
      da.To = to;
      da.Duration = new Duration(TimeSpan.FromSeconds(time));

      BackEase b = new BackEase();
      b.Amplitude = amplitude;
      b.EasingMode = mode;
      da.EasingFunction = b;

      Util.DoubleAnimation(t, new PropertyPath("(TranslateTransform.Y)"), da);
    }
  }
}