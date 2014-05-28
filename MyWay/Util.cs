using System.Windows.Controls;

namespace MyWay
{
  class Util
  {
    public static void RemoveLoader(Grid loader)
    {
      loader.Visibility = System.Windows.Visibility.Collapsed;
    }
  }
}
