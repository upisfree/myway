using HtmlAgilityPack;
using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
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
        string b = Regex.Match(a.GetValue(a.Length - 1).ToString(), @"\d+").Value; // получаем последную часть ссылки, id прогноза

        LayoutRoot.Tag = link + "|" + b;

        if (Data.File.IsExists("Stops/" + b + "/a.db"))
          ShowStopsOffline(link, b);
        else
          ShowStopsOnline(link, b);
      }
    }

    public class Stop
    {
      public string Text { get; set; }
      public string Link { get; set; }
    }

    public async void ShowStopsOnline(string link, string number)
    {
      if (Util.IsInternetAvailable())
      {
        Util.Hide(Error);
        
        if (await Data.Folder.IsExists("Stops") == false)
          await Data.Folder.Create("Stops");

        if (await Data.Folder.IsExists(number) == false)
          await Data.Folder.Create("Stops/" + number);

        List<Stop> stopsA = new List<Stop>();
        List<Stop> stopsB = new List<Stop>();

        string wA = null;
        string wB = null;

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

            string text = Util.TypographString(elem[0].InnerText.Trim());
            string href = Util.TypographString(elem[0].Attributes["href"].Value.Trim());

            string c = text + "|" + href;

            if (i == 1)
            {
              wA += c + "\n";

              stopsA.Add(new Stop() { Text = text, Link = href });

              if (b.IndexOf(a) == b.Count - 1 && stopsB.Count == 0)
              {
                PivotRoot.Items.Remove(PivotB);

                PivotA.Header = "кольцевой";
              }
            }
            else
            {
              wB += c + "\n";

              stopsB.Add(new Stop() { Text = text, Link = href });
            }
          }
          else
          {
            i++;
          }
        }

        Util.Hide(Load);

        wA = wA.Substring(0, wA.Length - 1);
        await Data.File.Write("Stops/" + number + "/a.db", wA);

        StopsA.ItemsSource = stopsA;

        if (stopsB.Count != 0)
        {
          wB = wB.Substring(0, wB.Length - 1);
          await Data.File.Write("Stops/" + number + "/b.db", wB);

          StopsB.ItemsSource = stopsB;
        }
      }
      else
      {
        Util.Show(Error);
        Util.Hide(Load);
      }
    }

    public async void ShowStopsOffline(string link, string number)
    {
      List<Stop> stopsA = new List<Stop>();
      List<Stop> stopsB = new List<Stop>();

      string s = await Data.File.Read("Stops/" + number + "/a.db");
      Array stopsAdb = s.Split(new Char[] { '\n' });

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

      if (Data.File.IsExists("Stops/" + number + "/b.db"))
      {
        string s2 = await Data.File.Read("Stops/" + number + "/b.db");
        Array stopsBdb = s2.Split(new Char[] { '\n' });

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

      Util.Hide(Load);

      StopsA.ItemsSource = stopsA;

      if (stopsB.Count != 0)
        StopsB.ItemsSource = stopsB;
      else
      {
        PivotRoot.Items.Remove(PivotB);

        PivotA.Header = "кольцевой";
      }
    }

    private void ShowStopsAgain(object sender, System.Windows.Input.GestureEventArgs e) // Гениальное название :)
    {
      string[] tag = LayoutRoot.Tag.ToString().Split(new char[] { '|' });

      string a = tag[0];
      string b = tag[1];

      ShowStopsOnline(a, b);
    }

    public void OpenPredict(object sender, EventArgs e)
    {
      TextBlock text = (TextBlock)sender;

      string link = text.Tag.ToString();
      string name = text.Text;

      (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/Predicts.xaml?link=" + link + "&name=" + name, UriKind.Relative));
    }
  }
}