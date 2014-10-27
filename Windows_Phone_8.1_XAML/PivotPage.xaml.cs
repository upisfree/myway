using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Newtonsoft.Json;
using QKit.JumpList;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Windows_Phone.Common;

namespace Windows_Phone
{
    // Прокручиваем, чтобы новый элемент оказался видимым.
    // var container = this.pivot.ContainerFromIndex(this.pivot.SelectedIndex) as ContentControl;
    // var listView = container.ContentTemplateRoot as ListView;
    // listView.ScrollIntoView(newItem, ScrollIntoViewAlignment.Leading);

    public sealed partial class PivotPage : Page
    {
        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");

        public CollectionViewSource Routes_Data { get; set; }

        public PivotPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            DataContext = this;

            Routes_Data_Get();
        }

        /*****************************************
         Маршруты
        *****************************************/

        public class KeyedList<TKey, TItem> : List<TItem>
        {
          public TKey Key { protected set; get; }
          public TKey KeyDisplay { protected set; get; }
          public KeyedList(TKey key, IEnumerable<TItem> items) : base(items) { Key = key; KeyDisplay = key; }
          public KeyedList(IGrouping<TKey, TItem> grouping) : base(grouping) { Key = grouping.Key; KeyDisplay = grouping.Key; }
        }

        private async void Routes_Data_Get()
        {
          string[] b = await Library.IO.MainPage.Get("Routes");
          b = b.Where(item => item != "").ToArray(); // отлавливаем пустой элемент, чтобы не вызывать ошибку и задерживать выполнение

          if (b != null)
          {
            Library.Util.Hide(Routes_Error);

            List<Library.Model.Route> list = new List<Library.Model.Route>();

            foreach (string a in b)
            {
              string[] line = a.Split(new Char[] { '|' });

              string number = Library.Util.TypographString(line[0]);
              string type   = Library.Util.TypographString(line[1]);
              string desc   = Library.Util.TypographString(line[2]);
              string toStop = Library.Util.TypographString(line[3] + "|" + number + " " + type);

              list.Add(new Library.Model.Route(number, " " + type, desc, toStop));
            }

            Library.Util.Hide(Routes_Load);

            var groupedRoutesList =
              from _list in list
              group _list by _list.Number[0] into listByGroup
              select new KeyedList<char, Library.Model.Route>(listByGroup);

            CollectionViewSource _data = new CollectionViewSource();
            _data.Source = groupedRoutesList;
            _data.IsSourceGrouped = true;
            Routes_Data = _data;

            Debug.WriteLine(_data);
          }
          else
          {
            Library.Util.Show(Routes_Error);
            Library.Util.Hide(Routes_Load);
          }
        }

        private void Element_Error_Button_Tap(object sender, RoutedEventArgs e)
        {
          Routes_Data_Get();
        }

        private async void Route_GoToStops(object sender, TappedRoutedEventArgs e)
        {
          Grid text = (Grid)sender;

          string[] a = text.Tag.ToString().Split(new char[] { '|' });

          string link = a[0];
          string name = a[1].ToUpper();

          await new MessageDialog(link + "\n" + name).ShowAsync();

          //(Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/StopsList.xaml?link=" + link + "&name=" + name, UriKind.Relative));
        }


        /*****************************************
         Единые колбэки (TODO: этот коммент надо переименовать) 
        *****************************************/

        private void Element_Search_Box_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Element_Search_Box_Tap(object sender, TappedRoutedEventArgs e)
        {

        }

        private void Element_Search_Box_LostFocus(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {

        }

        /*****************************************
         Настройки
        *****************************************/

        private async void Settings_Delete_Cache(object sender, RoutedEventArgs e)
        {
          await Library.Data.Clear();

          // Диалог
          MessageDialog dialog = new MessageDialog("Удаление прошло успешно", "Кэш очищен");

          // Кнопка закрытия диалога
          UICommand cancel = new UICommand("ок"); // самое эпичное, что никакой команды для кнопки нет.
          dialog.Commands.Add(cancel);

          // Показываем диалог
          await dialog.ShowAsync();
        }











        // Непонятные штучки, сделанные Студией
        /// <summary>
        /// Получает объект <see cref="NavigationHelper"/>, связанный с данным объектом <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Получает модель представлений для данного объекта <see cref="Page"/>.
        /// Эту настройку можно изменить на модель строго типизированных представлений.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // TODO: Создание соответствующей модели данных для области проблемы, чтобы заменить пример данных
        }

        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            // TODO: Сохраните здесь уникальное состояние страницы.
        }

        #region Регистрация NavigationHelper

        /// <summary>
        /// Методы, предоставленные в этом разделе, используются исключительно для того, чтобы
        /// NavigationHelper для отклика на методы навигации страницы.
        /// <para>
        /// Логика страницы должна быть размещена в обработчиках событий для 
        /// <see cref="NavigationHelper.LoadState"/>
        /// и <see cref="NavigationHelper.SaveState"/>.
        /// Параметр навигации доступен в методе LoadState 
        /// в дополнение к состоянию страницы, сохраненному в ходе предыдущего сеанса.
        /// </para>
        /// </summary>
        /// <param name="e">Предоставляет данные для методов навигации и обработчики
        /// событий, которые не могут отменить запрос навигации.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async void AddAppBarButton_Click(object sender, RoutedEventArgs e)
        {
          //await new MessageDialog("count: " + Routes_List.ItemsSource.ToString()).ShowAsync();
        }
    }
}
