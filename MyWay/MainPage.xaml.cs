﻿using Windows.Devices.Geolocation;
using HtmlAgilityPack;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

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
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
      if (!App.ViewModel.IsDataLoaded)
      {
        App.ViewModel.LoadData();

        Routes_Show();
        Stops_Show();
      }
    }

    // Нажатие на клавишу «Назад»
    protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
    {
      TextBox e1   = null;
      Grid e2      = null;
      ListBox e3   = null;
      UIElement e4 = null;

      switch (Pivot_Current)
      {
        case "Routes":
          e1 = Routes_Search_Box;
          e2 = Routes_Search_NoResults;
          e3 = Routes_Search_Result;
          e4 = Routes_Root;
          break;
        case "Stops":
          e1 = Stops_Search_Box;
          e2 = Stops_Search_NoResults;
          e3 = Stops_Search_Result;
          e4 = Stops_Root;
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

          ApplicationBar.IsVisible = true;

          e.Cancel = true;
        }

      }
      
      base.OnBackKeyPress(e);
    }

    // Событие, вызываемое при прокрутке Pivot-ов
    private static string Pivot_Current = "Routes";
    private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      bool flag = true;

      Pivot_About_Background_Animation(((SolidColorBrush)Application.Current.Resources["PhoneBackgroundBrush"]).Color, 250);
      Pivot_Title.Foreground = (SolidColorBrush)Application.Current.Resources["PhoneForegroundBrush"];
      
      Microsoft.Phone.Shell.SystemTray.BackgroundColor = ((SolidColorBrush)Application.Current.Resources["PhoneBackgroundBrush"]).Color;

      switch (((Pivot)sender).SelectedIndex)
      {
        case 0:
          Pivot_Current = "Routes";

          if (Routes_Search_Box.Text != "" || Routes_Error.Visibility == System.Windows.Visibility.Visible)
            flag = false;
          else
            flag = true;

          break;
        case 1:
          Pivot_Current = "Stops";

          if (Stops_Search_Box.Text != "" || Stops_Error.Visibility == System.Windows.Visibility.Visible)
            flag = false;
          else
            flag = true;

          break;
        case 2:
          Pivot_Current = "Map";

          flag = true;

          break;
        case 3:
          Pivot_Current = "Settings";
          
          flag = false;
          
          break;
        case 4:
          Pivot_Current = "About";

          Pivot_About_Background_Animation(Util.ConvertStringToColor("#FF455580"), 250);
          Pivot_Title.Foreground = new SolidColorBrush(Util.ConvertStringToColor("#FFFFFFFF"));
          
          Microsoft.Phone.Shell.SystemTray.BackgroundColor = Util.ConvertStringToColor("#FF455580");
          
          flag = false;

          break;
        default:
          flag = true;

          break;
      }

      ApplicationBar.IsVisible = flag;
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
              string toStop = a.Attributes["href"].Value + "|" + number + " " + type;

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

                Stops.Model_List i = new Stops.Model_List() { Name = name, Link = link };

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

    private class Routes
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

    private async void Routes_Show()
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
            string toStop = Util.TypographString(line[3] + "|" + number + " " + type);

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

    private async void Stops_Show()
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

            StopsList.Add(new Stops.Model_List() { Name = name, Link = link });
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

    private void Map_Init()
    {

    }

    //private void Map_SetCenter()
    //{
    //  Map_Root.Center
    //}

    private void Map_GetPhonePosition()
    {

    }


    /*****************************************
     Настройки
    *****************************************/

    // Очистка кэша
    private async void DeleteCache(object sender, System.Windows.Input.GestureEventArgs e)
    {
      await Data.Clear();

      MessageBox.Show("Удаление прошло успешно", "Кэш очищен", MessageBoxButton.OK);
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
      emailComposeTask.Body = "Можно удалить фразу про то, с какого устройства было отправлено письмо. Я попробую угадать.";

      emailComposeTask.Show();
    }

    private void ContactVK(object sender, RoutedEventArgs e)
    {
      WebBrowserTask webBrowserTask = new WebBrowserTask();

      webBrowserTask.Uri = new Uri("http://vk.com/upisfree", UriKind.Absolute);

      webBrowserTask.Show();
    }

    /*****************************************
     Поиск
    *****************************************/

    private class Element_Search
    {
      private async static Task<string> GetData() // сделать необязательный парамерт, дабы не зависить от номера пивота (ты понял)
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
                  string toStop = a[3] + "|" + number + " " + type;

                  e1.Items.Add(new Routes.Model() { Number = number, Type = type, Desc = desc, ToStop = toStop });

                  break;
                case "Stops":
                  string name = a[0];
                  string link = a[1];

                  e1.Items.Add(new Stops.Model_List() { Name = name, Link = link });

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
          Routes_Show();
          break;
        case "Stops":
          Stops_Show();
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

      switch (Pivot_Current)
      {
        case "Routes":
          e1 = Routes_Search_Box_Transform;
          break;
        case "Stops":
          e1 = Stops_Search_Box_Transform;
          break;
      }

      if (e1.Y != 75)
        Element_Search_Box_Animation(70, 75, 0.5, 1, EasingMode.EaseOut);
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

      System.Windows.Media.TranslateTransform t = null;

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