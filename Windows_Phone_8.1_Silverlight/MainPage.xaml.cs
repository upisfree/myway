﻿using HtmlAgilityPack;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

namespace MyWay
{
  public partial class MainPage : PhoneApplicationPage//////////////////////////////////////////////////////////////// TODO: удаление из избранного
  {//////////////////////////////////////////////////////////////////////////////////////////////////////////////////        убирание дубликатов в поиске маршрутов
    // Конструктор//////////////////////////////////////////////////////////////////////////////////////////////////         настройки карты
    public MainPage()
    {
      ApplicationBar = ApplicationBar_Routes;

      Routes_Init();
      Stops_Init();

      InitializeComponent();
    }

    protected async override void OnNavigatedTo(NavigationEventArgs e)
    {
      base.OnNavigatedTo(e);

      if (e.NavigationMode == NavigationMode.New)
        await Favourite_Init(true);
      else
        await Favourite_Init(false);
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

    /*****************************************
     Загрузка & Кеширование & Демонстрация маршрутов / остановок
    *****************************************/

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
      string desc = a[2].ToUpper();

      (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/StopsList.xaml?link=" + link + "&name=" + name + "&desc=" + desc, UriKind.Relative));
    }

    /*****************************************
     Остановки 
    *****************************************/

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
    }

    private async Task Stops_Init()
    {
      string[] b = await IO.Get("Stops");
      b = b.OrderBy(x => x).ToArray();

      if (b != null)
      {
        Util.Hide(Stops_Error);

        List<Stops.Model_XAML> StopsList = new List<Stops.Model_XAML>();

        foreach (string a in b)
        {
          try
          {
            string[] line = a.Split(new Char[] { '|' });

            string id = line[0];
            string lon = line[1];
            string lat = line[2];
            string name = Util.TypographString(line[3]);

            StopsList.Add(new Stops.Model_XAML() { Name = name, All = id + "|" + lon + "|" + lat + "|" + name  });
          }
          catch { }
        }

        Util.Hide(Stops_Load);

        Stops_Root.ItemsSource = StopsList; // OrderBy — сортировка по алфавиту
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
      string[] tag = text.Tag.ToString().Split(new Char[] { '|' });

      Debug.WriteLine(text);

      string id  = tag[0];
      string lon = tag[1];
      string lat = tag[2];
      string name = text.Text; // или tag[3]

      (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/DirectionsList.xaml?id=" + id + "&name=" + name + "&lon=" + lon + "&lat=" + lat, UriKind.Relative));
    }

    /*****************************************
     Карта
    *****************************************/

    private void Map_Show_Route(object sender, RoutedEventArgs e)
    {
      string[] a = ((MenuItem)sender).Tag.ToString().Split(new Char[] { '|' });
      Array _id = a[0].Split(new Char[] { '/' });
      string id = Regex.Match(_id.GetValue(_id.Length - 1).ToString(), @"\d+").Value; // получаем последную часть ссылки, id прогноза
      string name = a[1];
      string desc = a[2];

      (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/Map.xaml?mode=route&id=" + id + "&name=" + name + "&desc=" + desc, UriKind.Relative));
    }

    private void Map_Show_Stop(object sender, RoutedEventArgs e)
    {
      string[] a = ((MenuItem)sender).Tag.ToString().Split(new Char[] { '|' });

      string id   = a[0];
      string lon  = a[1];
      string lat  = a[2];
      string name = a[3];

      (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/Map.xaml?mode=stop&id=" + id + "&name=" + name + "&lon=" + lon + "&lat=" + lat, UriKind.Relative));
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
    private async void Favourite_Settings_Clear(object sender, System.Windows.Input.GestureEventArgs e)
    {
      await Favourite_Clear();  
    }

    // Скролл к избранному: вкл
    private void Favourite_Scroll_Changed(object sender, RoutedEventArgs e)
    {
      ((ToggleSwitch)sender).Content = "Да";

      Data.Settings.AddOrUpdate("ScrollToFavouriteOnStart", "true");
    }

    // Скролл к избранному: выкл
    private void Favourite_Scroll_Unchanged(object sender, RoutedEventArgs e)
    {
      ((ToggleSwitch)sender).Content = "Нет";

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
                    Util.IsStringContains(line[0] + " " + line[1], query) ||
                    Util.IsStringContains(line[0] + " " + line[1] + " " + line[2], query))
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

    /*****************************************
     Единые колбэки (TODO: этот коммент надо переименовать)
    *****************************************/

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