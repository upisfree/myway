using HtmlAgilityPack;
using Microsoft.Phone.Controls;
using System;
using System.Net;
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

    private void NoFuture_DoubleTap(object sender, System.EventArgs e)
    {
      if (NoFuture_Flag % 2 == 0)
      {
        NoFuture_RandomItem = new Random().Next(0, 10);

        NoFuture.Children[NoFuture_RandomItem].Opacity = 1;

        NoFuture_Animation(0.0, 1.0, 3.5).Begin();
      }
      else
      {
        NoFuture_Animation(1.0, 0.0, 3.5).Begin();

        System.Threading.Timer timer = new System.Threading.Timer(obj => // ждём, пока завершится анимация, скрываем фото
        {
          try
          { 
            NoFuture.Children[NoFuture_RandomItem].Opacity = 0;
          }
          catch { }
        }, null, 3500, System.Threading.Timeout.Infinite);
      }

      NoFuture_Flag++;
    }

    private Storyboard NoFuture_Animation(double from, double to, double time)
    {
      Storyboard sb = new Storyboard();
      DoubleAnimation fadeInAnimation = new DoubleAnimation();
      fadeInAnimation.From = from;
      fadeInAnimation.To = to;
      fadeInAnimation.Duration = new Duration(TimeSpan.FromSeconds(time));

      Storyboard.SetTarget(fadeInAnimation, NoFuture);
      Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath("Opacity"));

      sb.Children.Add(fadeInAnimation);

      return sb;
    }
  }
}