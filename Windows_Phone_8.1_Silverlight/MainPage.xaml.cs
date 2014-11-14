using HtmlAgilityPack;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Newtonsoft.Json;//////////////////////////////////////////////////////////////// TODO: удаление из избранного
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Windows.Devices.Geolocation;
using Windows.System;

namespace MyWay
{
  public partial class MainPage : PhoneApplicationPage
  {
    // Конструктор
    public MainPage()
    {
      InitializeComponent();
    }

    // Загрузка данных для элементов ViewModel
    protected async override void OnNavigatedTo(NavigationEventArgs e)
    {
      // this.Loaded += new RoutedEventHandler(MainPage_Loaded); ?

      await Data.Clear();

      ApplicationBar = ApplicationBar_Routes;

      await Favourite_Init();

      await Routes_Init();
      await Map_Search_SetSource();

      await Stops_Init();
      await Map_Init();
    }

    // Нажатие на клавишу «Назад»
    protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
    {
      TextBox e1   = null;
      Grid e2      = null;
      ListBox e3   = null;
      UIElement e4 = null;
      string r     = null;

      switch (Pivot_Current)
      {
        case "Routes":
          e1 = Routes_Search_Box;
          e2 = Routes_Search_NoResults;
          e3 = Routes_Search_Result;
          e4 = Routes_Root;
          r = "ApplicationBar_Routes";
          break;
        case "Stops":
          e1 = Stops_Search_Box;
          e2 = Stops_Search_NoResults;
          e3 = Stops_Search_Result;
          e4 = Stops_Root;
          r = "ApplicationBar_Stops";
          break;
      }

      if (e1 != null) // что я вижу?
      {
        if (e1.Text != "") // говнокод???
        {                 // ого!
          Element_Search_Box_Animation(70, 0, 0.5, 0.25, EasingMode.EaseIn);
          e1.Text = "";

          Util.Hide(e2);
          Util.Hide(e3);
          e3.Items.Clear();
          Util.Show(e4);

          ApplicationBar = (ApplicationBar)Resources[r];
          ApplicationBar.IsVisible = true;

          e.Cancel = true;
        }
      }
      
      base.OnBackKeyPress(e);
    }

    // Событие, вызываемое при прокрутке Pivot-ов
    private static string Pivot_Current = "Routes";
    private static Color _StandartFontColor = Microsoft.Phone.Shell.SystemTray.ForegroundColor;
    private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      string resource = "ApplicationBar_Routes";
      string color;

      if (Util.GetThemeColor() == "dark")
        color = "#99999999";
      else
        color = "#55555555";

      Microsoft.Phone.Shell.SystemTray.BackgroundColor = ((SolidColorBrush)Application.Current.Resources["PhoneBackgroundBrush"]).Color;
      Microsoft.Phone.Shell.SystemTray.ForegroundColor = Util.ConvertStringToColor(color);
      Pivot_About_Background_Animation(((SolidColorBrush)Application.Current.Resources["PhoneBackgroundBrush"]).Color, 250);
      Pivot_Title.Foreground = (SolidColorBrush)Application.Current.Resources["PhoneForegroundBrush"];

      switch (((Pivot)sender).SelectedIndex)
      {
        case 0:
          Pivot_Current = "Routes";

          resource = "ApplicationBar_Routes";

          if (Routes_Search_Box.Text != "" || Routes_Error.Visibility == System.Windows.Visibility.Visible)
            resource = "ApplicationBar_Hidden";

          break;
        case 1:
          Pivot_Current = "Stops";

          resource = "ApplicationBar_Stops";

          if (Stops_Search_Box.Text != "" || Stops_Error.Visibility == System.Windows.Visibility.Visible)
            resource = "ApplicationBar_Hidden";

          break;
        case 2:
          Pivot_Current = "Map";

          resource = "ApplicationBar_Map";

          break;
        case 3:
          Pivot_Current = "Settings";

          resource = "ApplicationBar_Hidden";
          
          break;
        case 4:
          Pivot_Current = "About";

          resource = "ApplicationBar_Hidden";

          Microsoft.Phone.Shell.SystemTray.BackgroundColor = Util.ConvertStringToColor("#FF455580");
          Microsoft.Phone.Shell.SystemTray.ForegroundColor = Util.ConvertStringToColor("#FFFFFFFF");
          Pivot_About_Background_Animation(Util.ConvertStringToColor("#FF455580"), 250);
          Pivot_Title.Foreground = new SolidColorBrush(Util.ConvertStringToColor("#FFFFFFFF"));

          break;
        case 5:
          Pivot_Current = "Favourite";

          resource = "ApplicationBar_Hidden";

          break;
      }

      ApplicationBar = (ApplicationBar)Resources[resource];
      if (resource != "ApplicationBar_Hidden")
        ApplicationBar.IsVisible = true;
    }

    private int GetPivotItemIntByName(string name)
    {
      int a = 0;

      switch (name)
      {
        case "Routes":
          a = 0;
          break;
        case "Stops":
          a = 1;
          break;
        case "Map":
          a = 2;
          break;
        case "Settings":
          a = 3;
          break;
        case "About":
          a = 4;
          break;
        case "Favourite":
          a = 5;
          break;
      }

      return a;
    }

    /*****************************************
     Загрузка & Кеширование & Демонстрация маршрутов / остановок
    *****************************************/

    private class IO
    {
      public async static Task<HtmlDocument> Download(string mode)
      {
        if (Util.IsInternetAvailable())
        {
          string link = "";
          switch (mode)
          {
            case "Routes":
              link = "http://t.bus55.ru/index.php/app/get_routes";
              break;
            case "Stops_Map":
              link = "http://t.bus55.ru/index.php/app/get_stations_json";
              break;
          }

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

      public async static Task<string[]> WriteAndGet(string mode, HtmlDocument html)
      {
        if (html == null)
          return null;

        string[] result = null;

        switch (mode)
        {
          case "Routes":
            List<string> s = new List<string>();
            string d = null;

            foreach (var a in html.DocumentNode.SelectNodes("//a"))
            {
              var b = a.ChildNodes.ToArray();

              string number = b[0].InnerText.Trim();
              string type = b[1].InnerText.Trim();
              string desc = b[2].InnerText.Trim();
              string toStop = a.Attributes["href"].Value + "|" + number + " " + type + "|" + desc;

              string c = number + "|" + type + "|" + desc + "|" + a.Attributes["href"].Value;

              s.Add(c);

              d += c + "\n";
            }

            d = d.Substring(0, d.Length - 1); // символ перевода строки один, а не два (\n)
            await Data.File.Write("Routes.db", d);

            result = s.ToArray();

            break;
          case "Stops_Map":
            string jsonText = html.DocumentNode.InnerText;

            List<Stops.Model_Map> json = JsonConvert.DeserializeObject<List<Stops.Model_Map>>(jsonText);

            string[] r = new string[json.Count - 1];
            string w = null;

            for (int i = 0; i < json.Count - 1; i++)
            {
              Stops.Model_Map a = json[i];

              string c = a.Id + "|" + a.Lat + "|" + a.Lon + "|" + a.Name;

              r[i] = c;

              w += c + "\n";
            }

            w = w.Substring(0, w.Length - 1);

            await Data.File.Write("Stops_Map.db", w);

            result = r;

            break;

          case "Stops_List":
            string[] e = null;

            try
            {
              e = await IO.Get("Stops_Map"); // проверка на отсутсвие интернета, так как основная не проходит (смотри проверку в начале функции)
              int _e = e.Length;            // чтобы try catch поймал исключение, надо произвести какое-либо действие над «e»
            }
            catch
            {
              break; // ловим исключение? валим отсюда.
            }

            List<Stops.Model_List> f = new List<Stops.Model_List>();
            List<string> j = new List<string>();
            string k = null;

            Stops.Model_List_Comparer mc = new Stops.Model_List_Comparer();

            foreach (string g in e)
            {
              try
              {
                string[] line = g.Split(new Char[] { '|' });

                string name = line[3];
                string link = "http://t.bus55.ru/index.php/app/get_dir/" + line[0];

                Stops.Model_List i = new Stops.Model_List() { Name = name, Link = link, All = name + "|" + link };

                if (!f.Contains(i, mc))
                {
                  f.Add(i);
                  j.Add(name + "|" + link);
                  k += name + "|" + link + "\n";
                }
              }
              catch { }
            }

            k = k.Substring(0, k.Length - 1);

            await Data.File.Write("Stops_List.db", k);

            result = j.ToArray();

            break;
        }

        return result;
      }

      public async static Task<string[]> Get(string mode)
      {
        if (Data.File.IsExists(mode + ".db"))
        {
          string a = await Data.File.Read(mode + ".db");
          return a.Split(new Char[] { '\n' });
        }
        else
        {
          if (mode != "Stops_List")
          {
            HtmlDocument a = await Download(mode);
            return await WriteAndGet(mode, a);
          }
          else
            return await WriteAndGet(mode, new HtmlDocument());
        }
      }
    }

    /*****************************************
     Маршруты
    *****************************************/

    public class Routes
    {
      public class Model
      {
        public string Number { get; set; }
        public string Type   { get; set; }
        public string Desc   { get; set; }
        public string ToStop { get; set; }
      }

      public class KeyedList<TKey, TItem> : List<TItem>
      {
        public TKey Key { protected set; get; }
        public KeyedList(TKey key, IEnumerable<TItem> items) : base(items) { Key = key; }
        public KeyedList(IGrouping<TKey, TItem> grouping) : base(grouping) { Key = grouping.Key; }
      }
    }

    private async Task Routes_Init()
    {
      string[] b = await IO.Get("Routes");

      if (b != null)
      {
        Util.Hide(Routes_Error);

        List<Routes.Model> RoutesList = new List<Routes.Model>();

        foreach (string a in b)
        {
          try
          {
            string[] line = a.Split(new Char[] { '|' });

            string number = Util.TypographString(line[0]);
            string type   = Util.TypographString(line[1]);
            string desc   = Util.TypographString(line[2]);
            string toStop = Util.TypographString(line[3] + "|" + number + " " + type + "|" + desc);

            RoutesList.Add(new Routes.Model() { Number = number, Type = " " + type, Desc = desc, ToStop = toStop });
          }
          catch { }
        }

        var groupedRoutesList =
              from list in RoutesList
              group list by list.Number[0] into listByGroup
              select new Routes.KeyedList<char, Routes.Model>(listByGroup);

        Util.Hide(Routes_Load);

        Routes_Root.ItemsSource = new List<Routes.KeyedList<char, Routes.Model>>(groupedRoutesList);
      }
      else
      {
        Util.Show(Routes_Error);
        Util.Hide(Routes_Load);

        await Routes_Init();
      }
    }

    private void Route_GoToStops(object sender, System.Windows.Input.GestureEventArgs e)
    {
      Grid text = (Grid)sender;

      string[] a = text.Tag.ToString().Split(new char[] { '|' });

      string link = a[0];
      string name = a[1].ToUpper();

      (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/StopsList.xaml?link=" + link + "&name=" + name, UriKind.Relative));
    }

    /*****************************************
     Остановки 
    *****************************************/

    public class Stops
    {
      public class Model_Map
      {
        public int Id      { get; set; }
        public double Lon  { get; set; }
        public double Lat  { get; set; }
        public string Name { get; set; }
      }

      public class Model_List
      {
        public string Name { get; set; }
        public string Link { get; set; }
        public string All  { get; set; }
      }

      public class Model_List_Comparer : IEqualityComparer<Model_List>
      {
        public bool Equals(Model_List x, Model_List y)
        {
          return (Util.IsStringContains(x.Name, y.Name));
        }

        public int GetHashCode(Model_List obj)
        {
          return obj.GetHashCode();
        }
      }
    }

    private async Task Stops_Init()
    {
      string[] b = await IO.Get("Stops_List");

      if (b != null)
      {
        Util.Hide(Stops_Error);

        List<Stops.Model_List> StopsList = new List<Stops.Model_List>();

        foreach (string a in b)
        {
          try
          {
            string[] line = a.Split(new Char[] { '|' });

            string name = Util.TypographString(line[0]);
            string link = Util.TypographString(line[1]);

            StopsList.Add(new Stops.Model_List() { Name = name, Link = link, All = name + "|" + link });
          }
          catch { }
        }

        Util.Hide(Stops_Load);

        Stops_Root.ItemsSource = StopsList;
      }
      else
      {
        Util.Show(Stops_Error);
        Util.Hide(Stops_Load);

        await Stops_Init();
      }
    }

    private void Stop_Open(object sender, EventArgs e)
    {
      TextBlock text = (TextBlock)sender;

      string link = text.Tag.ToString();
      string name = text.Text;

      (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/DirectionsList.xaml?link=" + link + "&name=" + name, UriKind.Relative));
    }

    /*****************************************
     Карта
    *****************************************/

    private async Task Map_Init()
    {
      try // определение местоположения
      {
        GeoCoordinate currentPosition = await Map_GetCurrentPosition();

        Map.Center = currentPosition;
        Map.ZoomLevel = 13;

        Map_DrawUser(currentPosition);
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

    public class Map_Search_Model
    {
      public string Title { get; set; }
      public string Desc  { get; set; }
      public int Id       { get; set; }
    }

    private async void Map_Search_Box_SelectionChanged(object sender, SelectionChangedEventArgs e) // карту на странице маршрута + на странице остановки + нажатие кнопки назад в карте + ОБЪЕДИНЕНИЕ В ПОИСКЕ ОСТАНОВОК И МАРШРУТОВ
    {
      if (e.AddedItems.Count <= 0) // ничего не найдено? валим.
      {
        return;
      }

      string oldText = Map_Search_Box.Text;
      Map_Search_Box.Text += " — загрузка";
      Map_Search_Box.IsEnabled = false;

      Map_Search_Model m = (Map_Search_Model)e.AddedItems[0];
      int id = m.Id;

      Util.MapRoute.Model data = await Util.MapRoute.Get(id);

      if (data == null) // не можем загрузить? не можем нормально распарсить? валим. (у меня, например, если нет денег, Билайн отдаёт html страницу и json.net умирает)
      {
        MessageBox.Show("Произошла ошибка при загрузке маршрута.\nМожет, нет подключения к сети?\n\nОшибка не пропадает? Очисти кэш (в настройках).", "Ошибка!", MessageBoxButton.OK);

        Map_Search_Box.Text = "";
        Map_Search_Box.IsEnabled = true;

        return;
      }

      Map_DrawRoute(data);

      Map_Search_Box.Text = oldText;
      Map_Search_Box.IsEnabled = true;
    }

    public async Task Map_Search_SetSource()
    {
      string[] b = await IO.Get("Routes");

      if (b != null)
      {
        List<Map_Search_Model> list = new List<Map_Search_Model>();

        foreach (string a in b)
        {
          try
          {
            string[] line = a.Split(new Char[] { '|' });

            string number = Util.TypographString(line[0]);
            string type = Util.TypographString(line[1]);
            string desc = Util.TypographString(line[2]);
            int id = Int32.Parse(line[3].Split(new Char[] { '/' }).Last());

            list.Add(new Map_Search_Model() { Title = line[0] + " " + line[1], Desc = line[2], Id = id });
          }
          catch { }
        }

        Map_Search_Box.ItemsSource = list;
      }
      else
        MessageBox.Show("Маршруты ещё не загрузились, подожди пожалуйста.");
    }

    private int _mapUsersLayerInt = -1;
    private void Map_DrawUser(GeoCoordinate coordinate)
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
        Map.Layers.Add(layer);

        _mapUsersLayerInt = Map.Layers.Count - 1;
      }
      else
        Map.Layers[_mapUsersLayerInt] = layer;
    }

    private int _mapRoadLayerInt = -1;
    private void Map_DrawRoute(Util.MapRoute.Model data)
    {
      MapPolyline line = new MapPolyline();
      line.StrokeColor = Util.ConvertStringToColor("#FF455580");
      line.StrokeThickness = 7;

      for (int i = 0; i <= data.Coordinates.Count - 1; i++)
      {
        string[] b = data.Coordinates[i];

        line.Path.Add(new GeoCoordinate() { Longitude = Util.StringToDouble(b[0]), Latitude = Util.StringToDouble(b[1]) });
      }

      if (_mapRoadLayerInt == -1)
      {
        Map.MapElements.Add(line);

        _mapRoadLayerInt = Map.MapElements.Count - 1;
      }
      else
        Map.MapElements[_mapRoadLayerInt] = line;
    }

    private async Task<GeoCoordinate> Map_GetCurrentPosition()
    {
      Geolocator locator = new Geolocator();
      Geoposition position = await locator.GetGeopositionAsync();
      Geocoordinate coordinate = position.Coordinate;
      return ConvertGeocoordinate(coordinate);
    }

    public static GeoCoordinate ConvertGeocoordinate(Geocoordinate geocoordinate)
    {
      return new GeoCoordinate
          (
          geocoordinate.Latitude,
          geocoordinate.Longitude,
          geocoordinate.Altitude ?? Double.NaN,
          geocoordinate.Accuracy,
          geocoordinate.AltitudeAccuracy ?? Double.NaN,
          geocoordinate.Speed ?? Double.NaN,
          geocoordinate.Heading ?? Double.NaN
          );
    }

    /*****************************************
     Настройки
    *****************************************/

    // Очистка кэша
    private async void DeleteCache(object sender, System.Windows.Input.GestureEventArgs e)
    {
      await Data.Clear();

      MessageBox.Show("Всё прошло хорошо.", "Кэш очищен", MessageBoxButton.OK);
    }

    // Очистка избранного
    private async void Favourite_Clear(object sender, System.Windows.Input.GestureEventArgs e)
    {
      await Data.File.Delete("favourite.json");

      Util.Hide(Favourite_Items);
      Util.Show(Favourite_NoItems);

      MessageBox.Show("Всё прошло хорошо.", "Избранное очищено", MessageBoxButton.OK);
    }

    // Скролл к избранному: вкл
    private void Favourite_Scroll_Changed(object sender, RoutedEventArgs e)
    {
      ((ToggleSwitch)sender).Content = "да";

      Data.Settings.AddOrUpdate("ScrollToFavouriteOnStart", "true");
    }

    // Скролл к избранному: выкл
    private void Favourite_Scroll_Unchanged(object sender, RoutedEventArgs e)
    {
      ((ToggleSwitch)sender).Content = "нет";

      Data.Settings.AddOrUpdate("ScrollToFavouriteOnStart", "false");
    }

    /*****************************************
     О программе
    *****************************************/

    // Ссылка на сайт мэрии
    private void About_LinkToAdministrationSite_Tap(object sender, System.Windows.Input.GestureEventArgs e)
    {
      var richTB = sender as RichTextBox;
      var textPointer = richTB.GetPositionFromPoint(e.GetPosition(richTB));

      var element = textPointer.Parent as TextElement;
      while (element != null && !(element is Underline))
      {
        if (element.ContentStart != null && element != element.ElementStart.Parent)
        {
          element = element.ElementStart.Parent as TextElement;
        }
        else
        {
          element = null;
        }
      }

      if (element == null) return;

      var underline = element as Underline;
      //underline.Foreground = new SolidColorBrush(Colors.LightGray);
      switch (underline.Name)
      {
        case "About_LinkToAdministrationSite":
          WebBrowserTask webBrowserTask = new WebBrowserTask();
          webBrowserTask.Uri = new Uri("http://admomsk.ru/web/guest/services/transport", UriKind.Absolute);
          webBrowserTask.Show();
          break;
      }
    }

    // Воспоизведение звуков
    private int About_BusImage_RandomInt;
    private void About_BusImage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
    {
      MediaElement sound = new MediaElement();
      About_BusImage_RandomInt = new Random().Next(1, 2);
      sound.Source = new Uri(@"Assets/Sounds/" + About_BusImage_RandomInt + ".wav", UriKind.Relative);
      sound.Play();
    }

    // Контакты
    private void ContactEmail(object sender, RoutedEventArgs e)
    {
      EmailComposeTask emailComposeTask = new EmailComposeTask();

      emailComposeTask.To = "upisfree@outlook.com";
      emailComposeTask.Subject = "fromapp@myway";
      emailComposeTask.Body = "Можно удалить фразу про то, с какого устройства было отправлено письмо, если есть. Я попробую угадать.";

      emailComposeTask.Show();
    }

    private void ContactVK(object sender, RoutedEventArgs e)
    {
      WebBrowserTask webBrowserTask = new WebBrowserTask();

      webBrowserTask.Uri = new Uri("http://vk.com/upisfree", UriKind.Absolute);

      webBrowserTask.Show();
    }

    /*****************************************
     Избранное
    *****************************************/

    private async Task Favourite_Init()
    {
      // Вставляю картинку в зависимости от цвета темы
      BitmapImage _bi = new BitmapImage();

      if (Util.GetThemeColor() == "dark")
        _bi.UriSource = new Uri("/Images/favs.png", UriKind.Relative);
      else
        _bi.UriSource = new Uri("/Images/favs.dark.png", UriKind.Relative);

      Favourite_Image.Source = _bi;

      // Инициализация, собственно
      Favourite_Model data = await Favourite_ReadFile();

      if (data == null)
      {
        Util.Hide(Favourite_Items);
        Util.Show(Favourite_NoItems);

        return;
      }

      Favourive_Routes.ItemsSource = data.Routes;
      Favourive_Stops.ItemsSource  = data.Stops;

      Util.Hide(Favourite_NoItems);
      Util.Show(Favourite_Items);

      // Скролл к избранному, если выбранно и установка нужного значения в настройках
      if (Data.Settings.GetOrDefault("ScrollToFavouriteOnStart", "true") == "true")
      {
        Pivot_Main.SelectedIndex = GetPivotItemIntByName("Favourite");
        // менять ничего в настройках не надо, всё выставлено по умолчанию на «да»
      }
      else
      {
        Favourite_Scroll_ToggleSwicth.Content = "нет";
        Favourite_Scroll_ToggleSwicth.IsChecked = false;
      }
    }

    public class Favourite_Model
    {
      public List<Routes.Model> Routes { get; set; }
      public List<Stops.Model_List> Stops { get; set; }
    }

    private async Task<Favourite_Model> Favourite_ReadFile()
    {
      if (Data.File.IsExists("favourite.json") == false)
        return null;

      string json = await Data.File.Read("favourite.json");

      Debug.WriteLine(json);

      return JsonConvert.DeserializeObject<Favourite_Model>(json);
    }

    private async Task Favourite_WriteToFile(string str, string mode)
    {
      Favourite_Model data = await Favourite_ReadFile();

      if (data == null)
        data = new Favourite_Model() { Routes = new List<Routes.Model>(), Stops = new List<Stops.Model_List>() };

      switch (mode)
      {
        case "Route":
          string[] a = str.Split(new Char[] { '|' });
          string[] b = a[1].Split(new Char[] { ' ' });
          string c = b[1];

          if (b.Length == 3)
            c += " " + b[2];

          Debug.WriteLine(c);

          Routes.Model model = new Routes.Model() { Number = b[0], Type = c, Desc = a[2], ToStop = str };
          data.Routes.Add(model);
          break;
        case "Stop":
          string[] a2 = str.Split(new Char[] { '|' });

          Stops.Model_List model2 = new Stops.Model_List() { Name = a2[0], Link = a2[1], All = str };
          data.Stops.Add(model2);
          break;
      }

      Debug.WriteLine(data);

      await Data.File.WriteJson("favourite.json", JsonConvert.SerializeObject(data));
    }

    private async void Favourite_ContextMenu_Add_Route(object sender, RoutedEventArgs e) // удаление из избранного и карту допилить + все остановки наносить
    {
      string str = ((MenuItem)sender).Tag.ToString();
      
      await Favourite_WriteToFile(str, "Route");

      await Favourite_Init();
    }

    private async void Favourite_ContextMenu_Add_Stop(object sender, RoutedEventArgs e)
    {
      string str = ((MenuItem)sender).Tag.ToString();

      await Favourite_WriteToFile(str, "Stop");

      await Favourite_Init();
    }

    /*****************************************
     Поиск
    *****************************************/

    private class Element_Search
    {
      private async static Task<string> GetData() // сделать необязательный парамерт, дабы не зависить от номера пивота (ты понял (нет)) — для истории оставлю
      {
        string way = null;

        switch (Pivot_Current)
        {
          case "Routes":
            way = "Routes";
            break;
          case "Stops":
            way = "Stops_List";
            break;
        }

        string db = await Data.File.Read(way + ".db");

        return db;
      }

      //private async static Task<string> Routes()
      //{
      //  get { return await Data.File.Read("Routes.db"); }
      //}

      public async static Task<Array> GetEqualData(string query)
      {
        string data = await GetData();

        if (data == null)
          MessageBox.Show("Загрузка ещё не закончилась, подожди пожалуйста.");

        string[] b = data.Split(new Char[] { '\n' });
        List<string[]> list = new List<string[]>();

        query = query.Trim();

        foreach (string a in b)
        {
          try
          {
            string[] line = a.Split(new Char[] { '|' });
            bool flag = false;

            switch (Pivot_Current)
            {
              case "Routes":
                if (Util.IsStringContains(line[0], query) ||
                    Util.IsStringContains(line[1], query) ||
                    Util.IsStringContains(line[2], query) ||
                    Util.IsStringContains(line[0] + " " + line[1], query) ||
                    Util.IsStringContains(line[0] + " " + line[1] + " " + line[2], query))
                {
                  flag = true;
                }

                break;
              case "Stops":
                if (Util.IsStringContains(line[0], query))
                {
                  flag = true;
                }

                break;
            }

            if (flag)
            {
              list.Add(line);
            }
          }
          catch { }
        }

        return list.ToArray();
      }
    }

    private async void Element_Search_Box_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
      TextBox s = (TextBox)sender;

      ListBox e1 = null;
      Grid e2 = null;
      UIElement e3 = null;

      switch (Pivot_Current)
      {
        case "Routes":
          e1 = Routes_Search_Result;
          e2 = Routes_Search_NoResults;
          e3 = Routes_Root;
          break;
        case "Stops":
          e1 = Stops_Search_Result;
          e2 = Stops_Search_NoResults;
          e3 = Stops_Root;
          break;
      }

      e1.Items.Clear();

      if (s.Text != "")
      {
        Util.Show(e1);
        Util.Hide(e2);
        Util.Hide(e3);

        try
        {
          Array b = await Element_Search.GetEqualData(s.Text);

          if (b.Length != 0)
          {
            foreach (string[] a in b)
            {
              switch (Pivot_Current)
              {
                case "Routes":
                  string number = a[0];
                  string type = " " + a[1];
                  string desc = a[2];
                  string toStop = a[3] + "|" + number + " " + type + "|" + desc;

                  e1.Items.Add(new Routes.Model() { Number = number, Type = type, Desc = desc, ToStop = toStop });

                  break;
                case "Stops":
                  string name = a[0];
                  string link = a[1];

                  e1.Items.Add(new Stops.Model_List() { Name = name, Link = link , All = name + "|" + link });

                  break;
              }
            }
          }
          else
          {
            BounceEase be = new BounceEase();
            be.Bounces = 2;
            be.Bounciness = 1;
            be.EasingMode = EasingMode.EaseOut;

            Element_Search_Box_Animation(75, 70, 1, ea: be);
            Util.Show(e2);
          }
        }
        catch { }
      }
      else
      {
        Util.Hide(e1);
        Util.Show(e3);
      }
    }

    /*****************************************
     Единые колбэки (TODO: этот коммент надо переименовать)
    *****************************************/

    private void Element_Error_Button_Tap(object sender, System.Windows.Input.GestureEventArgs e)
    {
      switch (Pivot_Current)
      {
        case "Routes":
          Util.Hide(Routes_Error);
          Util.Show(Routes_Load);

          Routes_Init();
          break;
        case "Stops":
          Util.Hide(Stops_Error);
          Util.Show(Stops_Load);
          
          Stops_Init();
          break;
      }
    }

    private void Element_Search_Box_Open(object sender, EventArgs e)
    {
      if (Pivot_Current == "Map") // так вышло. «Невозможно преобразовать AutoCompleteBox в TextBox»
      {
        if (Map_Search_Box.Text == "")
          Element_Search_Box_Animation(0, 125, 0.5, 0.5, EasingMode.EaseOut);

        Map_Search_Box.Focus(); // что за херня? какого чёрта?

        return;
      }

      TextBox e1 = null;

      switch (Pivot_Current)
      {
        case "Routes":
          e1 = Routes_Search_Box;
          break;
        case "Stops":
          e1 = Stops_Search_Box;
          break;
      }

      if (e1.Text == "")
        Element_Search_Box_Animation(0, 70, 0.5, 0.5, EasingMode.EaseOut);

      e1.Focus();

      ApplicationBar.IsVisible = false;
    }

    private void Element_Search_Box_Tap(object sender, RoutedEventArgs e)
    {
      TranslateTransform e1 = null;
      int a = 70;

      switch (Pivot_Current)
      {
        case "Routes":
          e1 = Routes_Search_Box_Transform;
          break;
        case "Stops":
          e1 = Stops_Search_Box_Transform;
          break;
        case "Map":
          e1 = Map_Search_Box_Transform;
          a = 125;
          break;
      }

      if (e1.Y != a + 5)
        Element_Search_Box_Animation(a, a + 5, 0.5, 1, EasingMode.EaseOut);
    }

    private async void Element_Search_Box_LostFocus(object sender, RoutedEventArgs e)
    {
      //if (Pivot_Current == "Map") // см. коммент в Element_Search_Box_Open
      //{
      //  if (Map_Search_Box.Text == "")
      //    Element_Search_Box_Animation(125, 0, 0.5, 0.25, EasingMode.EaseIn);
      //  else
      //    Element_Search_Box_Animation(130, 125, 0.5, 1, EasingMode.EaseOut);

      //  return;
      //}

      TextBox e1 = null;

      switch (Pivot_Current)
      {
        case "Routes":
          e1 = Routes_Search_Box;
          break;
        case "Stops":
          e1 = Stops_Search_Box;
          break;
      }

      if (e1.Text == "")
      {
        Element_Search_Box_Animation(70, 0, 0.5, 0.25, EasingMode.EaseIn);
        await Task.Delay(400);
        ApplicationBar.IsVisible = true;
      }
      else
        Element_Search_Box_Animation(75, 70, 0.5, 1, EasingMode.EaseOut);
    }

    /*****************************************
     Анимации
    *****************************************/

    private void Element_Search_Box_Animation(double from, double to, double time, double amplitude = 0, EasingMode mode = EasingMode.EaseOut, IEasingFunction ea = null)
    {
      DoubleAnimation da = new DoubleAnimation();

      da.From = from;
      da.To = to;
      da.Duration = new Duration(TimeSpan.FromSeconds(time));

      if (ea != null)
      {
        da.EasingFunction = ea;
      }
      else
      {
        BackEase b = new BackEase();
        b.Amplitude = amplitude;
        b.EasingMode = mode;
        da.EasingFunction = b;
      }

      TranslateTransform t = null;

      switch (Pivot_Current)
      {
        case "Routes":
          t = Routes_Search_Box_Transform;
          break;
        case "Stops":
          t = Stops_Search_Box_Transform;
          break;
        case "Map":
          t = Map_Search_Box_Transform;
          break;
      }

      Util.DoubleAnimation(t, new PropertyPath("(TranslateTransform.Y)"), da);
    }

    private void Pivot_About_Background_Animation(Color to, double time)
    {
      ColorAnimation ca = new ColorAnimation();
      ca.To = to;
      ca.Duration = TimeSpan.FromMilliseconds(time);

      Util.ColorAnimation(LayoutRoot, new PropertyPath("(Panel.Background).(SolidColorBrush.Color)"), ca);
    }
  }
}