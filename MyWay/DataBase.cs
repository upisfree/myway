using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;

namespace MyWay
{
  class DataBase
  {
    // Запись в базу
    public static void WriteToFile(string file, string text)
    {
      IsolatedStorageFile fileStorage = IsolatedStorageFile.GetUserStoreForApplication();
      StreamWriter Writer = new StreamWriter(new IsolatedStorageFileStream(file, FileMode.Append, fileStorage));
      Writer.WriteLine(text);
      Writer.Close();
    }

    // Чтение из базы
    public static string ReadFile(string file)
    {
      IsolatedStorageFile fileStorage = IsolatedStorageFile.GetUserStoreForApplication();
      StreamReader Reader = null;

      string result;
      
      try
      {
        Reader = new StreamReader(new IsolatedStorageFileStream(file, FileMode.Open, fileStorage));
        string textFile = Reader.ReadToEnd();
        result = textFile;
        Reader.Close();

        return result;
      }
      catch
      {
        return "false";
      }
    }

    // Удаление базы
    public static void DeleteFile(string file)
    {
      IsolatedStorageFile fileStorage = IsolatedStorageFile.GetUserStoreForApplication();
      StreamWriter Writer = new StreamWriter(new IsolatedStorageFileStream(file, FileMode.Truncate, fileStorage));
      Writer.Close();
    }
  }
}
