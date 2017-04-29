using GART.Controls;
using HtmlAgilityPack;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Windows.System;
using Location = System.Device.Location.GeoCoordinate;

namespace MyWay
{
  public partial class MainPage : PhoneApplicationPage
  {
    // Конструктор
    public MainPage()
    {
      ApplicationBar = ApplicationBar_Routes;

      this.Loaded += (sender, e) =>
      {
        Data.ForceUpdateScheduled(); // смотрим, не пора ли нам обновить кэш. пора? ок, просто удалим всё что там есть.

        Routes_Init();
        Stops_Init();
      };

      InitializeComponent();
    }

    #region Системные методы

    protected async override void OnNavigatedTo(NavigationEventArgs e)
    {
      ARDisplay.StartServices();

      base.OnNavigatedTo(e);

      Settings_Init();

      if (e.NavigationMode == NavigationMode.New)
        await Favourite_Init(true);
      else
        await Favourite_Init(false);
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
    private static System.Windows.Media.Color _StandartFontColor = Microsoft.Phone.Shell.SystemTray.ForegroundColor;

    private void PivotItem_Init()
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

      switch (Pivot_Main.SelectedIndex)
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
          Pivot_Current = "Settings";

          resource = "ApplicationBar_Hidden";
          
          break;
        case 3:
          Pivot_Current = "About";

          resource = "ApplicationBar_Hidden";

          Microsoft.Phone.Shell.SystemTray.BackgroundColor = Util.ConvertStringToColor("#FF455580");
          Microsoft.Phone.Shell.SystemTray.ForegroundColor = Util.ConvertStringToColor("#FFFFFFFF");
          Pivot_About_Background_Animation(Util.ConvertStringToColor("#FF455580"), 250);
          Pivot_Title.Foreground = new SolidColorBrush(Util.ConvertStringToColor("#FFFFFFFF"));

          break;
        case 4:
          Pivot_Current = "Favourite";

          if (Favourite_Items.Visibility == System.Windows.Visibility.Visible)
            resource = "ApplicationBar_Favourite";
          else
            resource = "ApplicationBar_Hidden";
          
          break;
      }

      ApplicationBar = (ApplicationBar)Resources[resource];
      if (resource != "ApplicationBar_Hidden")
        ApplicationBar.IsVisible = true;
    }

    private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      PivotItem_Init();
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
        case "Settings":
          a = 2;
          break;
        case "About":
          a = 3;
          break;
        case "Favourite":
          a = 4;
          break;
      }

      return a;
    }

    #endregion

    #region Загрузка & Кеширование & Демонстрация маршрутов / остановок

    public class IO
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
            case "Stops":
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
          case "Stops":
            string jsonText = html.DocumentNode.InnerText;

            List<Stops.Model> json = JsonConvert.DeserializeObject<List<Stops.Model>>(jsonText);
            Stops.Model_List_Comparer mc = new Stops.Model_List_Comparer();

            List<Stops.Model> f = new List<Stops.Model>();
            List<string> j = new List<string>();
            string k = null;

            for (int i = 0; i < json.Count - 1; i++)
            {
              try
              {
                Stops.Model a = json[i];

                if (!f.Contains(a, mc))
                {
                  f.Add(a);
                  j.Add(a.Id + "|" + a.Lon + "|" + a.Lat + "|" + a.Name);
                  k += a.Id + "|" + a.Lon + "|" + a.Lat + "|" + a.Name + "\n";
                }
              }
              catch { }
            }

            k = k.Substring(0, k.Length - 1);

            await Data.File.Write("Stops.db", k);

            result = j.OrderBy(x => x).ToArray(); // OrderBy — сортировка по алфавиту

            break;
        }

        return result;
      }

      public async static Task<string[]> Get(string mode)
      {
        if (Data.File.IsExists(mode + ".db"))
        {
          string a = await Data.File.Read(mode + ".db");
          return a.Split(new Char[] { '\n' }).OrderBy(x => x).ToArray(); // OrderBy — сортировка по алфавиту
        }
        else
        {
          HtmlDocument a = await Download(mode);
          return await WriteAndGet(mode, a);
        }
      }
    }

    #endregion

    #region Маршруты

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

      public class Comparer : IEqualityComparer<Model>
      {
        public bool Equals(Model x, Model y)
        {
          return (Util.IsStringContains(x.Number + " " + x.Type, y.Number + " " + y.Type));
        }

        public int GetHashCode(Model obj)
        {
          return obj.GetHashCode();
        }
      }

    }

    private async Task Routes_Init()
    {
      string[] b = await IO.Get("Routes");

      if (b != null)
      {
        Util.Hide(Routes_Error);

        List<Routes.Model> list = new List<Routes.Model>();
        Routes.Comparer mc = new Routes.Comparer();

        foreach (string a in b)
        {
          try
          {
            string[] line = a.Split(new Char[] { '|' });

            string number = Util.TypographString(line[0]);
            string type   = Util.TypographString(line[1]);
            string desc   = Util.TypographString(line[2]);
            string toStop = Util.TypographString(line[3] + "|" + number + " " + type + "|" + desc);

            Routes.Model _line = new Routes.Model() { Number = number, Type = " " + type, Desc = desc, ToStop = toStop };

            if (!list.Contains(_line, mc))
              list.Add(_line);
          }
          catch { }
        }

        var groupedList =
              from _list in list
              group _list by _list.Number[0] into listByGroup
              select new Routes.KeyedList<char, Routes.Model>(listByGroup);

        Util.Hide(Routes_Load);

        Routes_Root.ItemsSource = new List<Routes.KeyedList<char, Routes.Model>>(groupedList);
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
      string desc = a[2].ToUpper();

      NavigationService.Navigate(new Uri("/StopsList.xaml?link=" + link + "&name=" + name + "&desc=" + desc, UriKind.Relative));
    }

    #endregion

    #region Остановки

    public class Stops
    {
      public class Model
      {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Lon { get; set; }
        public string Lat { get; set; }
      }

      public class Model_XAML
      {
        public string Name { get; set; }
        public string All { get; set; }
      }

      public class Model_Near
      {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("lon")]
        public string Lon { get; set; }

        [JsonProperty("lat")]
        public string Lat { get; set; }
      }

      public class Model_List_Comparer : IEqualityComparer<Model>
      {
        public bool Equals(Model x, Model y)
        {
          return (Util.IsStringContains(x.Name, y.Name));
        }

        public int GetHashCode(Model obj)
        {
          return obj.GetHashCode();
        }
      }
      public class Model_XAML_Comparer : IEqualityComparer<Model_XAML>
      {
        public bool Equals(Model_XAML x, Model_XAML y)
        {
          return (Util.IsStringContains(x.Name, y.Name));
        }

        public int GetHashCode(Model_XAML obj)
        {
          return obj.GetHashCode();
        }
      }
    }

    private async Task Stops_Init()
    {
      string[] b = await IO.Get("Stops");
      List<Stops.Model_XAML> StopsList = new List<Stops.Model_XAML>();

      if (b != null)
      {
        Util.Hide(Stops_Error);

        foreach (string a in b)
        {
          try
          {
            string[] line = a.Split(new Char[] { '|' });

            string id = line[0];
            string lon = line[1];
            string lat = line[2];
            string name = Util.TypographString(line[3]);

            StopsList.Add(new Stops.Model_XAML() { Name = name, All = id + "|" + lon + "|" + lat + "|" + name });
          }
          catch { }
        }

        Util.Hide(Stops_Load);

        StopsList = StopsList.OrderBy(x => x.Name).ToList(); // OrderBy — сортировка по алфавиту
        //Stops_Root.ItemsSource = StopsList; // ушло в catch у ближайших остановок
      }
      else
      {
        Util.Show(Stops_Error);
        Util.Hide(Stops_Load);

        await Stops_Init();
      }

      // дубляция (прямо как на странице карты), но мне похуй (прямо как на странице карты)
      try // определение местоположения
      {
        GeoCoordinate currentPosition = ARDisplay.Location;

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

            Stops.Model_Near[] с = JsonConvert.DeserializeObject<Stops.Model_Near[]>(json);

            List<Stops.Model_XAML> list = new List<Stops.Model_XAML>();
            Stops.Model_XAML_Comparer mc = new Stops.Model_XAML_Comparer();

            foreach (Stops.Model_Near a in с)
            {
              string name = Util.TypographString(a.Name);
              string all = a.Id + "|" + a.Lon + "|" + a.Lat + "|" + Util.TypographString(a.Name);

              Stops.Model_XAML c = new Stops.Model_XAML() { Name = name, All = all };

              if (!list.Contains(c, mc))
              {
                list.Add(c);
              }
            }

            StopsList.InsertRange(0, list);

            Stops_Root.ItemsSource = StopsList;
          }
          catch
          {
            Stops_Root.ItemsSource = StopsList;
          }
        };

        client.DownloadStringAsync(new Uri("http://t.bus55.ru/index.php/app/get_stations_geoloc_json/" + Regex.Replace(currentPosition.Latitude.ToString(), ",", ".") + "/" + Regex.Replace(currentPosition.Longitude.ToString(), ",", ".")));
      }
      catch (Exception e3)
      {
        Stops_Root.ItemsSource = StopsList;
      }
    }

    private int Stops_Near_ButtonClicks = 0; // счётчик кликов для перехода на страницу AR
    private void Stops_Near(object sender, EventArgs e)
    {
      // переход на страницу виртуальной реальности
      Stops_Near_ButtonClicks++;

      if (Stops_Near_ButtonClicks >= 3)
      {
        Stops_Near_ButtonClicks = 0;
        
        Stops_AR(null, null);
      }

      Stops_Search_Result.ItemsSource = new List<Stops.Model_XAML>();
      Util.Hide(Stops_Search_NoResults);
      Util.Show(Stops_Root);

      ProgressIndicator pi = new ProgressIndicator();
      pi.IsVisible = true;
      pi.IsIndeterminate = true;
      pi.Text = "Ищу ближайшие остановки...";

      SystemTray.SetProgressIndicator(this, pi);

      try // определение местоположения
      {
        GeoCoordinate currentPosition = ARDisplay.Location;

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

            Stops.Model_Near[] b = JsonConvert.DeserializeObject<Stops.Model_Near[]>(json);

            Util.Show(Stops_Search_Result);
            Util.Hide(Stops_Search_NoResults);
            Util.Hide(Stops_Root);

            List<Stops.Model_XAML> list = new List<Stops.Model_XAML>();
            Stops.Model_XAML_Comparer mc = new Stops.Model_XAML_Comparer();

            foreach (Stops.Model_Near a in b)
            {
              string name = Util.TypographString(a.Name);
              string all = a.Id + "|" + a.Lon + "|" + a.Lat + "|" + Util.TypographString(a.Name);

              Stops.Model_XAML c = new Stops.Model_XAML() { Name = name, All = all };

              if (!list.Contains(c, mc))
              {
                list.Add(c);
              }
            }

            Stops_Search_Result.ItemsSource = list;

            pi.IsVisible = false;
          }
          catch
          {
            pi.IsVisible = false;

            MessageBox.Show("Скорее всего, нет доступа к сети. Проверь его и попробуй ещё раз.", "Ошибочка!", MessageBoxButton.OK);
          }
        };

        client.DownloadStringAsync(new Uri("http://t.bus55.ru/index.php/app/get_stations_geoloc_json/" + Regex.Replace(currentPosition.Latitude.ToString(), ",", ".") + "/" + Regex.Replace(currentPosition.Longitude.ToString(), ",", ".")));
      }
      catch (Exception e3)
      {
        MessageBoxResult mbr = MessageBox.Show("Не могу найти ближайшие остановки, так как отключено определение местоположения.\nОткрыть настройки, чтобы включить его?", "Местоположение", MessageBoxButton.OKCancel);

        if (mbr == MessageBoxResult.OK)
        {
          Launcher.LaunchUriAsync(new Uri("ms-settings-location:"));
        }
      }
    }

    private void Stops_AR(object sender, EventArgs e)
    {
      NavigationService.Navigate(new Uri("/AR.xaml", UriKind.Relative));
    }
    
    private void Stop_Open(object sender, EventArgs e)
    {
      TextBlock text = (TextBlock)sender;
      string[] tag = text.Tag.ToString().Split(new Char[] { '|' });

      Debug.WriteLine(text);

      string id  = tag[0];
      string lon = tag[1];
      string lat = tag[2];
      string name = text.Text; // или tag[3]

      NavigationService.Navigate(new Uri("/DirectionsList.xaml?id=" + id + "&name=" + name + "&lon=" + lon + "&lat=" + lat, UriKind.Relative));
    }

    #endregion

    #region Методы карты
    
    private void Map_Show_Route(object sender, RoutedEventArgs e)
    {
      string[] a = ((MenuItem)sender).Tag.ToString().Split(new Char[] { '|' });
      Array _id = a[0].Split(new Char[] { '/' });
      string id = Regex.Match(_id.GetValue(_id.Length - 1).ToString(), @"\d+").Value; // получаем последную часть ссылки, id прогноза
      string name = a[1];
      string desc = a[2];

      NavigationService.Navigate(new Uri("/Map.xaml?mode=route&id=" + id + "&name=" + name + "&desc=" + desc, UriKind.Relative));
    }

    private void Map_Show_Stop(object sender, RoutedEventArgs e)
    {
      string[] a = ((MenuItem)sender).Tag.ToString().Split(new Char[] { '|' });

      string id   = a[0];
      string lon  = a[1];
      string lat  = a[2];
      string name = a[3];

      NavigationService.Navigate(new Uri("/Map.xaml?mode=stop&id=" + id + "&name=" + name + "&lon=" + lon + "&lat=" + lat, UriKind.Relative));
    }

    #endregion

    #region Настройки

    // Инициализация настроек
    private void Settings_Init()
    {
      // режим просмотра
      switch (Data.Settings.GetOrDefault("MapViewMode", "карта"))
      {
        case "карта":
          MapViewMode_Picker.SelectedIndex = 0;
          break;
        case "спутник":
          MapViewMode_Picker.SelectedIndex = 1;
          break;
        case "карта+спутник":
          MapViewMode_Picker.SelectedIndex = 2;
          break;
        case "карта+рельеф":
          MapViewMode_Picker.SelectedIndex = 3;
          break;
      }

      // цвет
      switch (Data.Settings.GetOrDefault("MapColorMode", "светлый"))
      {
        case "светлый":
          MapColorMode_Picker.SelectedIndex = 0;
          break;
        case "тёмный":
          MapColorMode_Picker.SelectedIndex = 1;
          break;
      }

      MapViewMode_Picker.UpdateLayout();
      MapColorMode_Picker.UpdateLayout();
    }

    // Очистка кэша
    private async void DeleteCache(object sender, System.Windows.Input.GestureEventArgs e)
    {
      await Data.Clear();

      MessageBox.Show("Всё прошло хорошо.", "Кэш очищен", MessageBoxButton.OK);
    }

    // Очистка избранного
    private async void Favourite_Settings_Clear(object sender, System.Windows.Input.GestureEventArgs e)
    {
      await Favourite_Clear();  
    }

    // Скролл к избранному: вкл
    private void Favourite_Scroll_Changed(object sender, RoutedEventArgs e)
    {
      ((ToggleSwitch)sender).Content = "Вкл.";

      Data.Settings.AddOrUpdate("ScrollToFavouriteOnStart", "true");
    }

    // Скролл к избранному: выкл
    private void Favourite_Scroll_Unchanged(object sender, RoutedEventArgs e)
    {
      ((ToggleSwitch)sender).Content = "Выкл.";

      Data.Settings.AddOrUpdate("ScrollToFavouriteOnStart", "false");
    }

    // КАРТА

    private void MapViewMode_Picker_Loaded(object sender, RoutedEventArgs e)
    {
      MapViewMode_Picker.SelectionChanged += MapViewMode_Picker_SelectionChanged;
    }

    private void MapColorMode_Picker_Loaded(object sender, RoutedEventArgs e)
    {
      MapColorMode_Picker.SelectionChanged += MapColorMode_Picker_SelectionChanged;
    }

    private void MapViewMode_Picker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      try
      {
        string data = ((ListPickerItem)e.AddedItems[0]).Content.ToString();

        Data.Settings.AddOrUpdate("MapViewMode", data);
      }
      catch { }
    }

    private void MapColorMode_Picker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      try
      {
        string data = ((ListPickerItem)e.AddedItems[0]).Content.ToString();

        Data.Settings.AddOrUpdate("MapColorMode", data);
      }
      catch { }
    }

    private void MapSettings_Open(object sender, RoutedEventArgs e)
    {
      NavigationService.Navigate(new Uri("/Map.xaml?mode=settings&id=58", UriKind.Relative)); // почему бы и не показывать 14-ый?
    }

    #endregion

    #region О программе

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
    private void About_BusImage_Hold(object sender, System.Windows.Input.GestureEventArgs e)
    {
      Stream stream = TitleContainer.OpenStream("Assets/NoRun.wav");
      SoundEffect effect = SoundEffect.FromStream(stream);
      FrameworkDispatcher.Update();
      effect.Play();  
    }

    // Контакты
    //private void ContactEmail(object sender, RoutedEventArgs e)
    //{
    //  EmailComposeTask emailComposeTask = new EmailComposeTask();

    //  emailComposeTask.To = "info@bus55.ru";
    //  emailComposeTask.Subject = "Замечание или предложение";
    //  emailComposeTask.Body = "\n\nОтправлено из клиента под Windows Phone.";

    //  emailComposeTask.Show();
    //}

    private void ContactVK(object sender, RoutedEventArgs e)
    {
      EmailComposeTask emailComposeTask = new EmailComposeTask();

      emailComposeTask.To = "upisfree@outlook.com";
      emailComposeTask.Subject = "Мой маршрут @ Windows Phone";
      emailComposeTask.Body = "\n\nУкажите, пожалуйста, в письме модель вашего телефона. Спасибо!";

      emailComposeTask.Show();
    }

    #endregion

    #region Избранное

    public async Task Favourite_Init(bool scroll)
    {
      // Вставляю картинку в зависимости от цвета темы
      BitmapImage _bi = new BitmapImage();

      if (Util.GetThemeColor() == "dark")
        _bi.UriSource = new Uri("/Images/favs.png", UriKind.Relative);
      else
        _bi.UriSource = new Uri("/Images/favs.dark.png", UriKind.Relative);

      Favourite_Image.Source = _bi;

      // Инициализация, собственно
      Favourite.Model data = await Favourite.ReadFile();

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
        if (scroll)
          Pivot_Main.SelectedIndex = GetPivotItemIntByName("Favourite");
  
        // менять ничего в настройках не надо, всё выставлено по умолчанию на «да»
      }
      else
      {
        Favourite_Scroll_ToggleSwicth.Content = "Нет";
        Favourite_Scroll_ToggleSwicth.IsChecked = false;
      }
    }

    private async Task Favourite_Clear()
    {
      MessageBoxResult mbr = MessageBox.Show("Очищение удалит из избранного все добавленные маршруты и остановки.\nОчистить избранное?", "Очистка избранного", MessageBoxButton.OKCancel);

      if (mbr == MessageBoxResult.OK)
      {
        await Data.File.Delete("favourite.json");

        Util.Hide(Favourite_Items);
        Util.Show(Favourite_NoItems);

        MessageBox.Show("Всё прошло хорошо.", "Избранное очищено", MessageBoxButton.OK);
      }
    }

    private async void Favourite_AppBar_Clear(object sender, EventArgs e)
    {
      await Favourite_Clear();
      PivotItem_Init();
    }

    // Контекстные меню
    private async void Favourite_ContextMenu_Add_Route(object sender, RoutedEventArgs e)
    {
      string str = ((MenuItem)sender).Tag.ToString();
      
      await Favourite.WriteToFile(str, "Route");

      await Favourite_Init(false);
    }

    private async void Favourite_ContextMenu_Add_Stop(object sender, RoutedEventArgs e)
    {
      string str = ((MenuItem)sender).Tag.ToString();

      await Favourite.WriteToFile(str, "Stop");

      await Favourite_Init(false);
    }

    private async void Favourite_ContextMenu_Remove_Route(object sender, RoutedEventArgs e)
    {
    }

    private async void Favourite_ContextMenu_Remove_Stop(object sender, RoutedEventArgs e)
    {
    }

    #endregion

    #region Поиск

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
            way = "Stops";
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
                    Util.IsStringContains(line[0] + " " + line[1], query))
                {
                  flag = true;
                }

                break;
              case "Stops":
                if (Util.IsStringContains(line[3], query))
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

        return list.Distinct().ToArray();
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

      try
      {
        e1.ItemsSource = null;
      }
      catch { }

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
                  string name = a[3];
                  string all = a[0] + "|" + a[1] + "|" + a[2] + "|" + a[3];

                  e1.Items.Add(new Stops.Model_XAML() { Name = name, All = all });

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

    #endregion

    #region Единые колбэки (TODO: этот коммент надо переименовать)

    private async void Element_Error_Button_Tap(object sender, System.Windows.Input.GestureEventArgs e)
    {
      switch (Pivot_Current)
      {
        case "Routes":
          Util.Hide(Routes_Error);
          Util.Show(Routes_Load);

          await Routes_Init();
          break;
        case "Stops":
          Util.Hide(Stops_Error);
          Util.Show(Stops_Load);

          await Stops_Init();
          break;
      }
    }

    private void Element_Search_Box_Open(object sender, EventArgs e)
    {
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
      }

      if (e1.Y != a + 5)
        Element_Search_Box_Animation(a, a + 5, 0.5, 1, EasingMode.EaseOut);
    }

    private async void Element_Search_Box_LostFocus(object sender, RoutedEventArgs e)
    {
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

    #endregion

    #region Анимации

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
      }

      Util.DoubleAnimation(t, new PropertyPath("(TranslateTransform.Y)"), da);
    }

    private void Pivot_About_Background_Animation(System.Windows.Media.Color to, double time)
    {
      ColorAnimation ca = new ColorAnimation();
      ca.To = to;
      ca.Duration = TimeSpan.FromMilliseconds(time);

      Util.ColorAnimation(LayoutRoot, new PropertyPath("(Panel.Background).(SolidColorBrush.Color)"), ca);
    }

    #endregion
  }
}