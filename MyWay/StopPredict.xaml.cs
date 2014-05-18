using Microsoft.Phone.Controls;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MyWay;
using System.Net.Http;
using HtmlAgilityPack;

namespace MyWay
{
  public partial class StopPredict : PhoneApplicationPage
  {
    public StopPredict()
    {
      InitializeComponent();
    }

    protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
    {
      base.OnNavigatedTo(e);

      string link = "";
      string name = "";

      if (NavigationContext.QueryString.TryGetValue("name", out name))
        Title.Text = name.ToUpper();

      if (NavigationContext.QueryString.TryGetValue("link", out link))
        ShowPredicts(link, name);
    }

    public class Predict
    {
      public string Number { get; set; }
      public string Type { get; set; }
      public string Desc { get; set; }
      public string Time { get; set; }
    }

    public async void ShowPredicts(string link, string name)
    {
      List<Predict> p = new List<Predict>();

      string htmlPage = "";

      using (var client = new HttpClient())
      {
        htmlPage = await new HttpClient().GetStringAsync(link);
      }

      HtmlDocument htmlDocument = new HtmlDocument();
      htmlDocument.LoadHtml(htmlPage);

      var b = htmlDocument.DocumentNode.SelectNodes("//li[@class=\"item_predict\"]");

      if (b != null)
      {
        foreach (var a in b)
        {
          string number = a.ChildNodes[0].InnerText.Trim();
          string type   = a.ChildNodes[1].InnerText.Trim();
          string desc   = a.ChildNodes[4].InnerText.Trim();
          string time   = a.ChildNodes[2].InnerText.Trim();

          p.Add(new Predict() { Number = number, Type = " " + type, Desc = desc, Time = time });
        }

        Predicts.ItemsSource = p;
      }
      else
      {
        NoPredicts.Visibility = System.Windows.Visibility.Visible;
      }
    }
  }
}