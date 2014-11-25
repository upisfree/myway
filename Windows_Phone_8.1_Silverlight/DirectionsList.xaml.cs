using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using HtmlAgilityPack;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace MyWay
{
  public partial class DirectionsList : PhoneApplicationPage
  {
    public DirectionsList()
    {
      InitializeComponent();
    }

    protected async override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
    {
      base.OnNavigatedTo(e);

      string id = "";
      string lon = "";
      string lat = "";
      string name = "";

      if (NavigationContext.QueryString.TryGetValue("name", out name))
        Title.Text = name.ToUpper();

      if (NavigationContext.QueryString.TryGetValue("id", out id))
        Directions_Root.Tag = "http://t.bus55.ru/index.php/app/get_dir/" + id;

      await Directions_Show(Directions_Root.Tag.ToString());
    }

    private class IO
    {
      public async static Task<HtmlDocument> Download(string link)
      {
        if (Util.IsInternetAvailable())
        {
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

      public async static Task<string[]> WriteAndGet(HtmlDocument html, int id)
      {
        if (html == null)
          return null;

        string[] result = null;

        List<string> s = new List<string>();
        string d = null;

        foreach (var a in html.DocumentNode.SelectNodes("//a"))
        {
          var b = a.ChildNodes.ToArray();

          string direction = b[0].InnerText.Trim();
          string buses     = b[1].InnerText.Trim();
                 buses     = Regex.Replace(buses.Substring(0, buses.Length - 1), ",", ", ");
          string link      = a.Attributes["href"].Value;

          string c = direction + "|" + buses + "|" + link;

          s.Add(c);

          d += c + "\n";
        }

        d = d.Substring(0, d.Length - 1); // символ перевода строки один, а не два (\n)

        if (await Data.Folder.IsExists("Directions") == false)
          await Data.Folder.Create("Directions");

        await Data.File.Write("Directions/" + id + ".db", d);

        result = s.ToArray();

        return result;
      }

      public async static Task<string[]> Get(string link)
      {
        Array _id = link.Split(new Char[] { '/' });
        int id = Convert.ToInt32(Regex.Match(_id.GetValue(_id.Length - 1).ToString(), @"\d+").Value); // получаем последную часть ссылки, id прогноза

        if (Data.File.IsExists("Directions/" + id + ".db"))
        {
          string a = await Data.File.Read("Directions/" + id + ".db");
          return a.Split(new Char[] { '\n' });
        }
        else
        {
          HtmlDocument a = await Download(link);
          return await WriteAndGet(a, id);
        }
      }
    }

    private class Direction
    {
      public string Text  { get; set; }
      public string Buses { get; set; }
      public string Link  { get; set; }
    }

    private async Task Directions_Show(string link)
    {
      string[] b = await IO.Get(link);

      if (b != null)
      {
        Util.Hide(Directions_Error);

        List<Direction> DirectionsList = new List<Direction>();

        foreach (string a in b)
        {
          try
          {
            string[] line = a.Split(new Char[] { '|' });

            string directions = Util.TypographString(line[0]);
            string buses      = Util.TypographString(line[1]);
            string href       = Util.TypographString(line[2]); // дублируется просто

            DirectionsList.Add(new Direction() { Text = directions, Buses = buses, Link = href });
          }
          catch { }
        }

        Util.Hide(Directions_Load);

        Directions_Root.ItemsSource = DirectionsList;
      }
      else
      {
        Util.Show(Directions_Error);
        Util.Hide(Directions_Load);
      }
    }

    private async void Directions_ShowAgain(object sender, System.Windows.Input.GestureEventArgs e)
    {
      Util.Show(Directions_Load);
      Util.Hide(Directions_Error);

      await Directions_Show(Directions_Root.Tag.ToString());
    }

    private void Grid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
    {
      Grid text = (Grid)sender;

      string link = text.Tag.ToString();
      string name = Title.Text;

      (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/Predicts.xaml?link=" + link + "&name=" + name, UriKind.Relative));
    }
  }
}