using GART.BaseControls;
using GART.Data;
using HtmlAgilityPack;
using Microsoft.Phone.Controls;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Location = System.Device.Location.GeoCoordinate;
using GART.Controls;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;

namespace MyWay
{
  public partial class AR : PhoneApplicationPage
  {
    public AR()
    {
      InitializeComponent();

      this.Loaded += (sender, e) =>
      {
        Init();
        
        ARDisplay.StartServices();

        //ARDisplay.PhotoCamera.Initialized += (sender2, e2) =>
        //{
        //  //ARDisplay.PhotoCamera.Resolution = ARDisplay.PhotoCamera.AvailableResolutions.Last();
        //};
                
        ARDisplay.Orientation = ControlOrientation.Clockwise270Degrees;

        ARInit(); // инициализируем дополненную реальность
      };
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

      //CompositeTransform t = new CompositeTransform();
      //t.Rotation = 270 - (double)e.Orientation;

      //WorldView.RenderTransform = t;

      //if (e.Orientation == PageOrientation.LandscapeRight)
      //  ARDisplay.Orientation = ControlOrientation.Clockwise270Degrees;

      // return;
      
      //base.OnOrientationChanged(e);
    }

    private void Init()
    {
      System.Windows.Threading.DispatcherTimer ARTimer = new System.Windows.Threading.DispatcherTimer();
      ARTimer.Interval = TimeSpan.FromMilliseconds(10000);
      ARTimer.Tick += new EventHandler((sender, e) =>
      {
        ARInit();
      });

      ARTimer.Start();
    }


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

        //private string _description;
        //public string Description
        //{
        //  get
        //  {
        //    return _description;
        //  }
        //  set
        //  {
        //    if (_description != value)
        //    {
        //      _description = value;
        //    }
        //  }
        //}
      }
    }

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
          }
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
  }
}