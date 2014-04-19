using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using MyWay.Resources;

namespace MyWay.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public bool IsDataLoaded
        {
            get;
            private set;
        }

        /// <summary>
        /// Создает и добавляет несколько объектов ItemViewModel в коллекцию элементов.
        /// </summary>
        public void LoadData()
        {
            this.IsDataLoaded = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}