using HtmlAgilityPack;
using Microsoft.Phone.Controls;
using System;
using System.Net;
using System.Windows;

namespace MyWay
{
  public partial class StopPredict:PhoneApplicationPage
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
        Stop.Text = name.ToUpper();

      if (NavigationContext.QueryString.TryGetValue("link", out link))
        Predicts.Tag = link;

      ShowPredicts(Predicts.Tag.ToString());
    }

    public class Predict
    {
      public string Number { get; set; }
      public string Type   { get; set; }
      public string Desc   { get; set; }
      public string Time   { get; set; }
    }

    public void ShowPredicts(string link)
    {
      NoPredicts.Visibility = System.Windows.Visibility.Collapsed;
      Error.Visibility = System.Windows.Visibility.Collapsed;
      Load.Visibility = System.Windows.Visibility.Visible;

      if (Util.IsInternetAvailable())
      {
        var client = new WebClient();

        client.Headers["If-Modified-Since"] = DateTimeOffset.Now.ToString(); // отключение кэширования

        client.DownloadStringCompleted += (sender, e) =>
        {
          HtmlDocument htmlDocument = new HtmlDocument();
          htmlDocument.LoadHtml(e.Result);

          var b = htmlDocument.DocumentNode.SelectNodes("//li[@class=\"item_predict\"]");

          if (b != null)
          {
            foreach (var a in b)
            {
              string number = a.ChildNodes[0].InnerText.Trim();
              string type = a.ChildNodes[1].InnerText.Trim();
              string desc = a.ChildNodes[4].InnerText.Trim();
              string time = a.ChildNodes[2].InnerText.Trim();

              Predicts.Items.Add(new Predict() { Number = number, Type = " " + type, Desc = desc, Time = time });
            }

            Load.Visibility = System.Windows.Visibility.Collapsed;
          }
          else
          {
            NoPredicts.Visibility = System.Windows.Visibility.Visible;
            Load.Visibility = System.Windows.Visibility.Collapsed;
          }
        };

        client.DownloadStringAsync(new Uri(link));
      }
      else
      {
        Error.Visibility = System.Windows.Visibility.Visible;
        Load.Visibility = System.Windows.Visibility.Collapsed;
      }
    }

    private void Refresh(object sender, System.EventArgs e)
    {
      Predicts.Items.Clear();

      ShowPredicts(Predicts.Tag.ToString());
    }
  }
}