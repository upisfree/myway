using Microsoft.Phone.Controls;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace MyWay
{
  public partial class StopPredict : PhoneApplicationPage
  {
    public StopPredict()
    {
      InitializeComponent();

      ShowPredicts();
    }

    public class PropertyFields
    {
      public string Number { get; set; }
      public string Type   { get; set; }
      public string Desc   { get; set; }
      public string Time   { get; set; }
    }

    public void ShowPredicts()
    {
      List<PropertyFields> propertyList = new List<PropertyFields>();

      for (int i = 0; i < 100; ++i)
      {
        string name = i.ToString();
        string type = i.ToString();
        string desc = i.ToString();
        string time = i.ToString();

        propertyList.Add(new PropertyFields() { Number = name, Type = type, Desc = desc, Time = time });
      }

      Grid Predict = ContentPanel.FindName("Predict") as Grid;
      Predict.DataContext = propertyList;

      //var template = ContentPanel.FindName("frontTemplate") as DataTemplate;
      //var stackPanel = template.LoadContent() as StackPanel;
    }
  }
}