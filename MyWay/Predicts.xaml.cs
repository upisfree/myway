using HtmlAgilityPack;
using Microsoft.Phone.Controls;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

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
      NoFuture.Opacity = 0;
      NoFuture_Flag = 0;
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

    // Анимации
    private int NoFuture_Flag;
    private int NoFuture_RandomItem;

    private async void NoFuture_DoubleTap(object sender, System.EventArgs e)
    {
      if (NoFuture_Flag % 2 == 0)
      {
        NoFuture_RandomItem = new Random().Next(0, 9);

        if (NoFuture_RandomItem == NoFuture.Children.Count)
          NoFuture_RandomItem -= 1;
        
        int i = 0;
        while (i < NoFuture.Children.Count)
        {
          NoFuture.Children[i].Opacity = 0;
          i++;
        }

        NoFuture.Children[NoFuture_RandomItem].Opacity = 1;

        NoFuture_Animation(0.0, 1.0, 3.5);
      }
      else
      {
        NoFuture_Animation(1.0, 0.0, 3.5);
        await Task.Delay(3500);
        NoFuture.Children[NoFuture_RandomItem].Opacity = 0;
      }

      NoFuture_Flag++;
    }

    private void NoFuture_Animation(double from, double to, double time)
    {
      DoubleAnimation da = new DoubleAnimation();
      da.From = from;
      da.To = to;
      da.Duration = new Duration(TimeSpan.FromSeconds(time));

      Util.Animation(NoFuture, new PropertyPath("Opacity"), da);
    }
  }
}