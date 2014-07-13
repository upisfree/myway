using HtmlAgilityPack;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

namespace MyWay
{
  public partial class MainPage:PhoneApplicationPage
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

        //Stops.Download();

        Routes_Show();
      }
    }

    // Нажатие на клавишу «Назад»
    protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
    {
      if (Routes_Search_Box.Text != "")
      {
        Routes_Search_Box_Animation(110, 0, 0.5, 0.25, EasingMode.EaseIn);
        Routes_Search_Box.Text = "";

        Routes_Search_NoResults.Visibility = System.Windows.Visibility.Collapsed;
        Routes_Search_Result.Visibility = System.Windows.Visibility.Collapsed;
        Routes_Search_Result.Items.Clear();
        Routes_Root.Visibility = System.Windows.Visibility.Visible;

        ApplicationBar.IsVisible = true;

        e.Cancel = true;
      }

      base.OnBackKeyPress(e);
    }

    // Событие, вызываемое при прокрутке Pivot-ов
    private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      bool flag = true;

      switch (((Pivot)sender).SelectedIndex)
      {
        case 0:
          if (Routes_Search_Box.Text != "")
            flag = false;
          break;
        case 1:
          flag = false;
          break;
        default:
          flag = true;
          break;
      }

      ApplicationBar.IsVisible = flag;
    }

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

      public async static Task<HtmlDocument> Download()
      {
        if (Util.IsInternetAvailable())
        {
          string htmlPage = "";

          try
          {
            htmlPage = await new HttpClient().GetStringAsync("http://t.bus55.ru/index.php/app/get_routes");
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

      public async static Task<string[]> WriteAndGet(HtmlDocument html)
      {
        if (html == null)
          return null;

        List<string> s = new List<string>();

        foreach (var a in html.DocumentNode.SelectNodes("//a"))
        {
          var b = a.ChildNodes.ToArray();

          string number = b[0].InnerText.Trim();
          string type   = b[1].InnerText.Trim();
          string desc   = b[2].InnerText.Trim();
          string toStop = a.Attributes["href"].Value + "|" + number + " " + type;

          string c = number + "|" + type + "|" + desc + "|" + a.Attributes["href"].Value;

          s.Add(c);
          await Data.File.Write("Routes.db", c);
        }

        return s.ToArray();
      }

      public async static Task<string[]> Get()
      {
        if (Data.File.IsExists("Routes.db"))
        {
          string a = await Data.File.Read("Routes.db");
          return a.Split(new Char[] { '\n' });
        }
        else
        {
          HtmlDocument a = await Download();
          return await WriteAndGet(a);
        }
      }
    }

    private async void Routes_Show()
    {
      string[] b = await Routes.Get();

      if (b != null)
      {
        Routes_Error.Visibility = System.Windows.Visibility.Collapsed;

        List<Routes.Model> RoutesList = new List<Routes.Model>();

        foreach (string a in b)
        {
          try
          {
            string[] line = a.Split(new Char[] { '|' });

            string number = line[0].ToString();
            string type   = line[1].ToString();
            string desc   = line[2].ToString();
            string toStop = line[3].ToString() + "|" + number + " " + type;

            RoutesList.Add(new Routes.Model() { Number = number, Type = " " + type, Desc = desc, ToStop = toStop });
          }
          catch { }
        }

        var groupedRoutesList =
              from list in RoutesList
              group list by list.Number[0] into listByGroup
              select new Routes.KeyedList<char, Routes.Model>(listByGroup);

        Routes_Load.Visibility = System.Windows.Visibility.Collapsed;

        Routes_Root.ItemsSource = new List<Routes.KeyedList<char, Routes.Model>>(groupedRoutesList);
      }
      else
        Routes_Error.Visibility = System.Windows.Visibility.Visible;
    }

    private void Routes_Error_Button_Tap(object sender, System.Windows.Input.GestureEventArgs e)
    {
      Routes_Show();
    }

    // Поиск маршрутов
    private class Routes_Search
    {
      private async static Task<string> GetRoutes()
      {
        string db = await Data.File.Read("Routes.db");

        return db;
      }

      //private async static Task<string> Routes()
      //{
      //  get { return await Data.File.Read("Routes.db"); }
      //}

      public async static Task<Array> GetEqualRoutes(string query)
      {
        string routes = await GetRoutes();
        string[] b = routes.Split(new Char[] { '\n' });
        List<string[]> list = new List<string[]>();

        query = query.Trim();

        foreach (string a in b)
        {
          try
          {
            string[] line = a.Split(new Char[] { '|' });

            if (Util.IsStringContains(line[0], query) ||
                Util.IsStringContains(line[1], query) ||
                Util.IsStringContains(line[2], query) ||
                Util.IsStringContains(line[0] + " " + line[1], query) ||
                Util.IsStringContains(line[0] + " " + line[1] + " " + line[2], query))
            {
              list.Add(line);
            }
          }
          catch { }
        }

        return list.ToArray();
      }
    }

    private async void Routes_Search_Box_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
      TextBox s = (TextBox)sender;

      Routes_Search_Result.Items.Clear();

      if (s.Text != "")
      {
        Routes_Search_Result.Visibility = System.Windows.Visibility.Visible;
        Routes_Search_NoResults.Visibility = System.Windows.Visibility.Collapsed;
        Routes_Root.Visibility = System.Windows.Visibility.Collapsed;

        try
        {
          Array b = await Routes_Search.GetEqualRoutes(s.Text);

          if (b.Length != 0)
          {
            foreach (string[] a in b)
            {
              string number = a[0];
              string type = " " + a[1];
              string desc = a[2];
              string toStop = a[3] + "|" + number + " " + type;

              Routes_Search_Result.Items.Add(new Routes.Model() { Number = number, Type = type, Desc = desc, ToStop = toStop });
            }
          }
          else
          {
            BounceEase be = new BounceEase();
            be.Bounces = 2;
            be.Bounciness = 1;
            be.EasingMode = EasingMode.EaseOut;

            Routes_Search_Box_Animation(75, 70, 1, ea: be);
            Routes_Search_NoResults.Visibility = System.Windows.Visibility.Visible;
          }
        }
        catch { }
      }
      else
      {
        Routes_Search_Result.Visibility = System.Windows.Visibility.Collapsed;
        Routes_Root.Visibility = System.Windows.Visibility.Visible;
      }
    }

    private void Routes_Search_Box_Open(object sender, EventArgs e)
    {
      if (Routes_Search_Box.Text == "")
        Routes_Search_Box_Animation(0, 70, 0.5, 0.5, EasingMode.EaseOut);

      Routes_Search_Box.Focus();

      ApplicationBar.IsVisible = false;
    }

    private void Routes_Search_Box_Tap(object sender, RoutedEventArgs e)
    {
      if (Routes_Search_Box_Transform.Y != 75)
        Routes_Search_Box_Animation(70, 75, 0.5, 1, EasingMode.EaseOut);
    }

    private async void Routes_Search_Box_LostFocus(object sender, RoutedEventArgs e)
    {
      if (Routes_Search_Box.Text == "")
      {
        Routes_Search_Box_Animation(70, 0, 0.5, 0.25, EasingMode.EaseIn);
        await Task.Delay(400);
        ApplicationBar.IsVisible = true;
      }
      else
        Routes_Search_Box_Animation(75, 70, 0.5, 1, EasingMode.EaseOut);
    }

    private void Route_GoToStops(object sender, System.Windows.Input.GestureEventArgs e)
    {
      Grid text = (Grid)sender;

      string[] a = text.Tag.ToString().Split(new char[] { '|' });

      string link = a[0];
      string name = a[1].ToUpper();

      (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/StopsList.xaml?link=" + link + "&name=" + name, UriKind.Relative));
    }

    // Остановки
    private class Stops
    {
      public static void Download()
      {
        String myUrl = "http://t.bus55.ru/index.php/app/get_stations_json";
        WebClient client = new WebClient();
        client.DownloadStringAsync(new Uri(myUrl), UriKind.Relative);
        client.DownloadStringCompleted += (sender, e) =>
        {
          MessageBox.Show(e.Result);
        };
      }
    }









    // Очистка кэша
    private async void DeleteCache(object sender, System.Windows.Input.GestureEventArgs e)
    {
      await Data.Clear();

      MessageBox.Show("Удаление прошло успешно", "Кэш очищен", MessageBoxButton.OK);
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

    // Анимация
    private void Routes_Search_Box_Animation(double from, double to, double time, double amplitude = 0, EasingMode mode = EasingMode.EaseOut, IEasingFunction ea = null)
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

      Util.Animation(Routes_Search_Box_Transform, new PropertyPath("(TranslateTransform.Y)"), da);
    }
  }
}