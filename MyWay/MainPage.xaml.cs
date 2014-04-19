using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Globalization;
using MyWay.Resources;

namespace MyWay
{
    public partial class MainPage : PhoneApplicationPage
    {
        public class GroupByNumber
        {
            public string Number { get; set; }
            public string Type   { get; set; }
            public string Desc   { get; set; }
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

            List<GroupByNumber> RoutesList = new List<GroupByNumber>();

            RoutesList.Add(new GroupByNumber() { Number = "1", Type = " трамвай", Desc = "Пос. Амурский-ПО \"Полет\"" });
            RoutesList.Add(new GroupByNumber() { Number = "10", Type = " автобус", Desc = "здесь что-то" });
            RoutesList.Add(new GroupByNumber() { Number = "101", Type = " автобус", Desc = "\"Полет\"" });
            RoutesList.Add(new GroupByNumber() { Number = "89", Type = " автобус", Desc = "Пос. Амурский-ПО" });
            RoutesList.Add(new GroupByNumber() { Number = "88", Type = " автобус", Desc = "Пос. Амурский-ПО" });
            RoutesList.Add(new GroupByNumber() { Number = "24", Type = " трамвай", Desc = "Пос. Амурский-ПО Пос. Амурский-ПО Пос. Амурский-ПО \"Полет\"" });
            RoutesList.Add(new GroupByNumber() { Number = "254", Type = " трамвай", Desc = "Пос. Амурский-ПО Пос. Амурский-ПО Пос. Амурский-ПО \"Полет\"" });

            var groupedRoutesList =
                    from list in RoutesList
                    group list by list.Number[0] into listByGroup
                    select new KeyedList<char, GroupByNumber>(listByGroup);

            Routes.ItemsSource = new List<KeyedList<char, GroupByNumber>>(groupedRoutesList);
        }

        // Загрузка данных для элементов ViewModel
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }
        }

        private void Pivot_Loaded_1(object sender, RoutedEventArgs e)
        {

        }
    }
}