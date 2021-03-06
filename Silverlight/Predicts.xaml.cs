﻿using HtmlAgilityPack;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MyWay
{
  public partial class StopPredict:PhoneApplicationPage
  {
    private string TileName;
    private string TileLink;

    public StopPredict()
    {
      InitializeComponent();

      this.Loaded += (sender, e) =>
      {
        string link = "";
        string name = "";

        if (NavigationContext.QueryString.TryGetValue("name", out name))
          Stop.Text = name.ToUpper();

        if (NavigationContext.QueryString.TryGetValue("link", out link))
          Predicts.Tag = link;

        TileName = name;
        TileLink = link;

        ShowPredicts(Predicts.Tag.ToString());

        // проверяем, есть ли уже такая плиточка. да? убираем кнопку выноса
        if (IsTileOnStart("/Predicts.xaml?link=" + TileLink + "&name=" + TileName) != null)
        {
          ((ApplicationBarMenuItem)ApplicationBar.MenuItems[0]).IsEnabled = false; // http://stackoverflow.com/questions/5334574/applicationbariconbutton-is-null
        }

        System.Windows.Threading.DispatcherTimer dt = new System.Windows.Threading.DispatcherTimer();

        int t = 30000;
        int i = 100;

        dt.Interval = TimeSpan.FromMilliseconds(i);
        dt.Tick += new EventHandler((sender2, e2) =>
        {
          if (progress == t)
          {
            _refresh();
            progress = 0;
            UpdateProgress.Value = 0;
          }

          UpdateProgress.Value = (float)progress / t;

          progress += i;
        });
        dt.Start();
      };
    }

    private int progress = 0;
    protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
    {
      base.OnNavigatedTo(e);

      // на случай, если человек только что разместил плитку и вернулся
      // проверяем, есть ли уже такая плиточка. да? убираем кнопку выноса
      if (IsTileOnStart("/Predicts.xaml?link=" + TileLink + "&name=" + TileName) != null)
      {
        ((ApplicationBarMenuItem)ApplicationBar.MenuItems[0]).IsEnabled = false; // http://stackoverflow.com/questions/5334574/applicationbariconbutton-is-null
      }
    }

    public class Predict
    {
      public string Number { get; set; }
      public string Type   { get; set; }
      public string Desc   { get; set; }
      public string Time   { get; set; }
    }

    public async void ShowPredicts(string link)
    {
      Util.Hide(NoPredicts);
      NoFuture.Opacity = 0;
      NoFuture_Flag = 0;
      Util.Hide(Error);
      Util.Show(Load);

      await Task.Delay(100);

      if (Util.IsInternetAvailable())
      {
        var client = new WebClient();

        client.Headers["If-Modified-Since"] = DateTimeOffset.Now.ToString(); // отключение кэширования

        client.DownloadStringCompleted += (sender, e) =>
        {
          HtmlDocument htmlDocument = new HtmlDocument();
          htmlDocument.LoadHtml(e.Result);

          var b = htmlDocument.DocumentNode.SelectNodes("//li[@class=\"item_predict\"]");

          if (b != null || e.Result == null)
          {
            foreach (var a in b)
            {
              string number = Util.TypographString(a.ChildNodes[0].InnerText.Trim());
              string type   = Util.TypographString(a.ChildNodes[1].InnerText.Trim());
              string desc   = Util.TypographString(a.ChildNodes[4].InnerText.Trim());
              string time   = Util.TypographString(a.ChildNodes[2].InnerText.Trim());

              Predicts.Items.Add(new Predict() { Number = number, Type = " " + type, Desc = desc, Time = time });
            }

            Util.Hide(Load);
          }
          else
          {
            Util.Show(NoPredicts);
            Util.Hide(Load);
          }
        };

        client.DownloadStringAsync(new Uri(link));
      }
      else
      {
        Util.Show(Error);
        Util.Hide(Load);
      }
    }

    private void _refresh()
    {
      progress = 0;
      UpdateProgress.Value = 0;
      
      Predicts.Items.Clear();

      Util.Show(Load);
      Util.Hide(Error);

      ShowPredicts(Predicts.Tag.ToString());
    }

    private void Refresh(object sender, System.EventArgs e)
    {
      _refresh();
    }

    #region Анимации

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

      Util.DoubleAnimation(NoFuture, new PropertyPath("Opacity"), da);
    }

    #endregion

    #region Вынос остановки на рабочий стол

    private ShellTile IsTileOnStart(string uri)
    {
      ShellTile shellTile = ShellTile.ActiveTiles.FirstOrDefault(tile => tile.NavigationUri.ToString().Contains(uri)); // тут стоит проверка на url, чтобы можно было ставить на рабочий стол плитки с одной остановки, но на разные направления 
                                                                                                                       // но названия у них будут одинаковые, без указания стороны движения
      return shellTile;                                                                                                // и похуй.
    }                                                                                                                  // всё равно человек должен помнить это, а если указывать в названии, то оно будет уродсикм

    private void AddToStartButton_Click(object sender, EventArgs e)
    {
      IconicTileData data = new IconicTileData()
      {
        Title = TileName,
        IconImage = new Uri("Assets/SecondaryTiles/stop_middle.png", UriKind.Relative),
        SmallIconImage = new Uri("Assets/SecondaryTiles/stop_small.png", UriKind.Relative)
      };

      string uri = "/Predicts.xaml?link=" + TileLink + "&name=" + TileName;

      // проверяем, есть ли уже такая плиточка
      ShellTile tile = IsTileOnStart(uri);

      if (tile == null)
      {
        ShellTile.Create(new Uri(uri, UriKind.Relative), data, true);
      }
    }

    #endregion
  }
}