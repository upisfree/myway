using HtmlAgilityPack;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using Windows.Networking.Connectivity;

namespace MyWay
{
  public partial class MainPage:PhoneApplicationPage
  {
    // Функции анимаций поля поиска
    public void SearchBox_Show() { (this.Resources["SearchBox_Show"] as Storyboard).Begin(); }
    public void SearchBox_Hide() { (this.Resources["SearchBox_Hide"] as Storyboard).Begin(); }
    public void SearchBox_Hide1() { (this.Resources["SearchBox_Hide1"] as Storyboard).Begin(); }
    public void SearchBox_BeforeFocus() { (this.Resources["SearchBox_BeforeFocus"] as Storyboard).Begin(); }
    public void SearchBox_AfterFocus() { (this.Resources["SearchBox_AfterFocus"] as Storyboard).Begin(); }

    // Конструктор
    public MainPage()
    {
      InitializeComponent();

      SearchBox.LostFocus += (sender, e) =>
      {
        if (((TextBox)sender).Text == "")
        {
          SearchBox_Hide1();

          ApplicationBar.IsVisible = true;
        }
        else
          SearchBox_AfterFocus();
      };
    }

    private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      bool flag = true;

      switch (((Pivot)sender).SelectedIndex)
      {
        case 0:
          if (SearchBox.Text != "")
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

    public class Route
    {
      public string Number { get; set; }
      public string Type   { get; set; }
      public string Desc   { get; set; }
      public string ToStop { get; set; }
    }

    public class KeyedList<TKey, TItem> : List<TItem>
    {
      public TKey Key { protected set; get; }
      public KeyedList(TKey key, IEnumerable<TItem> items) : base(items)    { Key = key; }
      public KeyedList(IGrouping<TKey, TItem> grouping)    : base(grouping) { Key = grouping.Key; }
    }

    // Загрузка данных для элементов ViewModel
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
      if (!App.ViewModel.IsDataLoaded)
      {
        App.ViewModel.LoadData();

        if (DataBase.IsExists("Routes.db"))
          ShowRoutesListOffline();
        else
          ShowRoutesListOnline();
      }
    }

    protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
    {
      if (SearchBox.Text != "")
      {
        SearchBox_Hide();
        SearchBox.Text = "";

        SearchRoutes_NoResults.Visibility = System.Windows.Visibility.Collapsed;
        SearchRoutes_Result.Visibility = System.Windows.Visibility.Collapsed;
        SearchRoutes_Result.Items.Clear();
        Routes.Visibility = System.Windows.Visibility.Visible;

        ApplicationBar.IsVisible = true;

        e.Cancel = true;
      }

      base.OnBackKeyPress(e);
    }

    public async void ShowRoutesListOnline()
    {
      if (Util.IsInternetAvailable())
      {
        Error.Visibility = System.Windows.Visibility.Collapsed;

        List<Route> RoutesList = new List<Route>();

        string htmlPage = "";

        using (var client = new HttpClient())
        {
          htmlPage = await new HttpClient().GetStringAsync("http://t.bus55.ru/index.php/app/get_routes/");
        }

        HtmlDocument htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(htmlPage);

        foreach (var a in htmlDocument.DocumentNode.SelectNodes("//a"))
        {
          var elem = a.ChildNodes.ToArray();

          string number = elem[0].InnerText.Trim();
          string type = elem[1].InnerText.Trim();
          string desc = elem[2].InnerText.Trim();
          string toStop = a.Attributes["href"].Value + "|" + number + " " + type;

          DataBase.Write("Routes.db", number + "|" + type + "|" + desc + "|" + a.Attributes["href"].Value);

          RoutesList.Add(new Route() { Number = number, Type = " " + type, Desc = desc, ToStop = toStop });
        }

        var groupedRoutesList =
              from list in RoutesList
              group list by list.Number[0] into listByGroup
              select new KeyedList<char, Route>(listByGroup);

        Load.Visibility = System.Windows.Visibility.Collapsed;

        Routes.ItemsSource = new List<KeyedList<char, Route>>(groupedRoutesList);
      }
      else
      {
        Error.Visibility = System.Windows.Visibility.Visible;
      }
    }

    public void ShowRoutesListOffline()
    {
      List<Route> RoutesList = new List<Route>();

      Array db = DataBase.Read("Routes.db").Split(new Char[] {'\n'});

      foreach (string a in db)
      {
        try
        {
          Array line = a.Split(new Char[] {'|'});

          string number = line.GetValue(0).ToString();
          string type = line.GetValue(1).ToString();
          string desc = line.GetValue(2).ToString();
          string toStop = line.GetValue(3).ToString() + "|" + number + " " + type;

          RoutesList.Add(new Route() { Number = number, Type = " " + type, Desc = desc, ToStop = toStop });
        }
        catch { }
      }

      var groupedRoutesList =
            from list in RoutesList
            group list by list.Number[0] into listByGroup
            select new KeyedList<char, Route>(listByGroup);

      Load.Visibility = System.Windows.Visibility.Collapsed;

      Routes.ItemsSource = new List<KeyedList<char, Route>>(groupedRoutesList);
    }

    private void ShowRoutesAgain(object sender, System.Windows.Input.GestureEventArgs e) // Гениальное название :)
    {
      ShowRoutesListOnline();
    }

    // Поиск
    protected class SearchRoutes
    {
      private async static Task<string> GetRoutes()
      {
        string db = await DataBase.ReadAsync("Routes.db");

        return db;
      }

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

    private async void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
      TextBox s = (TextBox)sender;

      SearchRoutes_Result.Items.Clear();

      if (s.Text != "")
      {
        SearchRoutes_Result.Visibility = System.Windows.Visibility.Visible;
        SearchRoutes_NoResults.Visibility = System.Windows.Visibility.Collapsed;
        Routes.Visibility = System.Windows.Visibility.Collapsed;

        try
        {
          Array b = await SearchRoutes.GetEqualRoutes(s.Text);

          if (b.Length != 0)
          {
            foreach (string[] a in b)
            {
              string number = a[0];
              string type = " " + a[1];
              string desc = a[2];
              string toStop = a[3] + "|" + number + " " + type;

              SearchRoutes_Result.Items.Add(new Route() { Number = number, Type = type, Desc = desc, ToStop = toStop });
            }
          }
          else
          {
            SearchRoutes_NoResults.Visibility = System.Windows.Visibility.Visible;
          }
        }
        catch { }
      }
      else
      {
        SearchRoutes_Result.Visibility = System.Windows.Visibility.Collapsed;
        Routes.Visibility = System.Windows.Visibility.Visible;
      }
    }

    private void SearchRoutes_OpenBox(object sender, EventArgs e)
    {
      if (SearchBox.Text == "")
        SearchBox_Show();

      SearchBox.Focus();

      ApplicationBar.IsVisible = false;
    }

    private void SearchBox_Tap(object sender, RoutedEventArgs e)
    {
      SearchBox_BeforeFocus();
    }

    public void OpenRoute(object sender, EventArgs e)
    {
      Grid text = (Grid)sender;

      string[] a = text.Tag.ToString().Split(new char[] { '|' });

      string link = a[0];
      string name = a[1].ToUpper();

      (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/StopsList.xaml?link=" + link + "&name=" + name, UriKind.Relative));
    }

    // Очистка кэша
    private void DeleteCache(object sender, System.Windows.Input.GestureEventArgs e)
    {
      DataBase.RemoveAll("");

      MessageBox.Show("Удаление прошло успешно", "Кэш очищен", MessageBoxButton.OK);
    }

    // Контакты
    private void ContactEmail(object sender, RoutedEventArgs e)
    {
      EmailComposeTask emailComposeTask = new EmailComposeTask();

      emailComposeTask.To = "upisfree@outlook.com";
      emailComposeTask.Subject = "fromapp@myway";
      emailComposeTask.Body = "Можно удалить фразу про то, с какого устройства было отправлено письмо. Я попробую угадать :)";

      emailComposeTask.Show();
    }

    private void ContactVK(object sender, RoutedEventArgs e)
    {
      WebBrowserTask webBrowserTask = new WebBrowserTask();

      webBrowserTask.Uri = new Uri("http://vk.com/upisfree", UriKind.Absolute);

      webBrowserTask.Show();
    }
  }
}