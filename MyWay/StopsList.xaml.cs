using HtmlAgilityPack;
using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
        ShowStops(link);
    }

    public class Stop
    {
      public string Text { get; set; }
      public string Link { get; set; }
    }

    protected async void ShowStops(string link)
    {
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
        if (a.Attributes["class"] != null)
        {
          i++;
        }
        else
        {
          var elem = a.ChildNodes.ToArray();

          string text = elem[0].InnerText.Trim();
          string href = elem[0].Attributes["href"].Value.Trim();

          if (i == 1)
          {
            stopsA.Add(new Stop() { Text = text, Link = href });

            if (b.IndexOf(a) == b.Count - 1 && stopsB.Count == 0)
            {
              PivotRoot.Items.Remove(PivotB);

              PivotA.Header = "кольцевой";
            }
          }
          else
          {
            stopsB.Add(new Stop() { Text = text, Link = href });
          }
        }
      }

      StopsA.ItemsSource = stopsA;

      if (stopsB.Count != 0)
        StopsB.ItemsSource = stopsB;
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