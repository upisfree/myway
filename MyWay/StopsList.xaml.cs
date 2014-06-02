using HtmlAgilityPack;
using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;

namespace MyWay
{
  public partial class Route:PhoneApplicationPage
  {
    public Route()
    {
      InitializeComponent();
    }

    protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
    {
      base.OnNavigatedTo(e);

      string link = "";
      string name = "";

      if (NavigationContext.QueryString.TryGetValue("name", out name))
        PivotRoot.Title = name;

      if (NavigationContext.QueryString.TryGetValue("link", out link))
      {
        Array a = link.Split(new Char[] { '/' });
        string b = Regex.Match(a.GetValue(a.Length - 1).ToString(), @"\d+").Value; // мне правда было лень создавать новые переменные. правда.

        if (DataBase.IsDirExists("Stops/" + b))
          ShowStopsOffline(link, b);
        else
          ShowStops(link, b);
      }
    }

    public class Stop
    {
      public string Text { get; set; }
      public string Link { get; set; }
    }

    public async void ShowStops(string link, string number)
    {
//      MessageBox.Show("онлайн");

      if (!DataBase.IsDirExists("Stops"))
        DataBase.CreateDir("Stops");

      if (!DataBase.IsDirExists(number))
        DataBase.CreateDir("Stops/" + number);

      List<Stop> stopsA = new List<Stop>();
      List<Stop> stopsB = new List<Stop>();

      string htmlPage = "";

      using (var client = new HttpClient())
      {
        htmlPage = await new HttpClient().GetStringAsync(link);
      }

      HtmlDocument htmlDocument = new HtmlDocument();
      htmlDocument.LoadHtml(htmlPage);

      int i = 0;

      var b = htmlDocument.DocumentNode.SelectNodes("//li");

      foreach (var a in b)
      {
        if (a.Attributes["class"] == null)
        {
          var elem = a.ChildNodes.ToArray();

          string text = elem[0].InnerText.Trim();
          string href = elem[0].Attributes["href"].Value.Trim();

          if (i == 1)
          {
            DataBase.Write("Stops/" + number + "/a.db", text + "|" + href);

            stopsA.Add(new Stop() { Text = text, Link = href });

            if (b.IndexOf(a) == b.Count - 1 && stopsB.Count == 0)
            {
              PivotRoot.Items.Remove(PivotB);

              PivotA.Header = "кольцевой";
            }
          }
          else
          {
            DataBase.Write("Stops/" + number + "/b.db", text + "|" + href);

            stopsB.Add(new Stop() { Text = text, Link = href });
          }
        }
        else
        {
          i++;
        }
      }

      StopsA.ItemsSource = stopsA;

      if (stopsB.Count != 0)
        StopsB.ItemsSource = stopsB;
    }

    public void ShowStopsOffline(string link, string number)
    {
//      MessageBox.Show("офлайн");

      List<Stop> stopsA = new List<Stop>();
      List<Stop> stopsB = new List<Stop>();

      Array stopsAdb = DataBase.Read("Stops/" + number + "/a.db").Split(new Char[] { '\n' });

      foreach (string a in stopsAdb)
      {
        try
        {
          Array line = a.Split(new Char[] { '|' });

          string text = line.GetValue(0).ToString();
          string href = line.GetValue(1).ToString();

          stopsA.Add(new Stop() { Text = text, Link = href });
        }
        catch { }
      }

      if (DataBase.IsExists("Stops/" + number + "/b.db"))
      {
        Array stopsBdb = DataBase.Read("Stops/" + number + "/b.db").Split(new Char[] { '\n' });

        foreach (string a in stopsBdb)
        {
          try
          {
            Array line = a.Split(new Char[] { '|' });

            string text = line.GetValue(0).ToString();
            string href = line.GetValue(1).ToString();

            stopsB.Add(new Stop() { Text = text, Link = href });
          }
          catch { }
        }
      }

      StopsA.ItemsSource = stopsA;

      if (stopsB.Count != 0)
        StopsB.ItemsSource = stopsB;
      else
      {
        PivotRoot.Items.Remove(PivotB);

        PivotA.Header = "кольцевой";
      }
    }

    public void OpenPredict(object sender, EventArgs e)
    {
      TextBlock text = (TextBlock)sender;
      text.Foreground = new SolidColorBrush(Colors.Black);

      string link = text.Tag.ToString();
      string name = text.Text;

      (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/Predicts.xaml?link=" + link + "&name=" + name, UriKind.Relative));
    }
  }
}