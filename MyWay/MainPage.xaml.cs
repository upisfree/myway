using HtmlAgilityPack;
using Microsoft.Phone.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Windows.Navigation;

namespace MyWay
{
    public partial class MainPage : PhoneApplicationPage
    {
        public class GroupByNumber
        {
            public string Number { get; set; }
            public string Type   { get; set; }
            public string Desc   { get; set; }
            public string ToStop { get; set; }
        }

        public class KeyedList<TKey, TItem> : List<TItem>
        {
            public TKey Key { protected set; get; }

            public KeyedList(TKey key, IEnumerable<TItem> items)
                : base(items)
            {
                Key = key;
            }

            public KeyedList(IGrouping<TKey, TItem> grouping)
                : base(grouping)
            {
                Key = grouping.Key;
            }
        }

        // Конструктор
        public MainPage()
        {
            InitializeComponent();
        }

        // Загрузка данных для элементов ViewModel
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();

                ShowRoutesList();
            }
        }

        public async void ShowRoutesList()
        {
            List<GroupByNumber> RoutesList = new List<GroupByNumber>();

            string htmlPage = "";

            using (var client = new HttpClient())
            {
                htmlPage = await new HttpClient().GetStringAsync("http://t.bus55.ru/index.php/app/get_routes/");
            }

            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(htmlPage);

            foreach (var a in htmlDocument.DocumentNode.SelectNodes("//a"))
            {
                var elem = a.ChildNodes.ToArray();

                string number = elem[0].InnerText.Trim();
                string type   = elem[1].InnerText.Trim();
                string desc   = elem[2].InnerText.Trim();
                string toStop = a.Attributes["href"].Value + "|" + number + " " + type;

                RoutesList.Add(new GroupByNumber() { Number = number, Type = " " + type, Desc = desc, ToStop = toStop });
            }

            var groupedRoutesList =
                    from list in RoutesList
                    group list by list.Number[0] into listByGroup
                    select new KeyedList<char, GroupByNumber>(listByGroup);

            Routes.ItemsSource = new List<KeyedList<char, GroupByNumber>>(groupedRoutesList);
        }
    }
}