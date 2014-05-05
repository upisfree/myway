using HtmlAgilityPack;
using Microsoft.Phone.Controls;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;

namespace MyWay
{
    public partial class Route : PhoneApplicationPage
    {
        public Route()
        {
            InitializeComponent();
        }

        protected async override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string link = "";
            string name = "";

            if (NavigationContext.QueryString.TryGetValue("name", out name))
            {
                PivotMain.Title = name;
            }

            if (NavigationContext.QueryString.TryGetValue("link", out link))
            {
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

                        string stop    = elem[0].InnerText.Trim();
                        string predict = elem[0].Attributes["href"].Value.Trim();

                        Thickness margin = new Thickness();
                        margin.Bottom = 10;

                        TextBlock txt = new TextBlock()
                        {
                            Text = stop,
                            Tag = predict,
                            Height = 55,
                            Width = 436,
                            Margin = margin,
                            FontFamily = new FontFamily("Segoe WP SemiLight"),
                            FontSize = 36
                        };

                        txt.Tap += OpenStopPredict;

                        if (i == 1)
                        {
                            RouteA.Children.Add(txt);

                            if (b.IndexOf(a) == b.Count - 1 && RouteB.Children.Count == 0)
                            {
                                PivotMain.Items.Remove(RoutePivotB);

                                RoutePivotA.Header = "кольцевой";
                            }
                        }
                        else
                        {
                            RouteB.Children.Add(txt);
                        }
                   }
                }
            }
        }

        public void OpenStopPredict(object sender, EventArgs e)
        {
          TextBlock text = (TextBlock)sender;
          
          string link = text.Tag.ToString();

          MessageBox.Show(link);
//          string link = text.Tag.ToString().Split(new char[] { '|' })[0]; // да, это говнокод.
//          string name = text.Tag.ToString().Split(new char[] { '|' })[1].ToUpper(); // я писал это в среду, 1:23
//
//          (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/RouteStopsList.xaml?link=" + link + "&name=" + name, UriKind.Relative));
        }
    }
}