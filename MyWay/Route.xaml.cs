using HtmlAgilityPack;
using Microsoft.Phone.Controls;
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

                foreach (var a in htmlDocument.DocumentNode.SelectNodes("//li"))
                {
                    if (a.Attributes["class"] != null)
                    {
                        i++;
                    }
                    else
                    {
                        var elem = a.ChildNodes.ToArray();

                        string name = elem[0].InnerText.Trim();

                        Thickness margin = new Thickness();
                        margin.Bottom = 10;

                        TextBlock txt = new TextBlock()
                        { 
                            Text = name,
                            Height = 55,
                            Width = 436,
                            Margin = margin,
                            FontFamily = new FontFamily("Segoe WP SemiLight"),
                            FontSize = 36
                        };

                        if (i == 1)
                            RouteA.Children.Add(txt);
                        else
                            RouteB.Children.Add(txt);
                    }
                }
            }
        }
    }
}