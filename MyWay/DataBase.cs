using System.IO;
using System.IO.IsolatedStorage;

namespace MyWay
{
  class DataBase
  {
    // Запись в базу
    public static void Write(string file, string text)
    {
      IsolatedStorageFile fileStorage = IsolatedStorageFile.GetUserStoreForApplication();
      StreamWriter Writer = new StreamWriter(new IsolatedStorageFileStream(file, FileMode.Append, fileStorage));

      Writer.WriteLine(text);

      Writer.Close();
    }

    // Чтение из базы
    public static string Read(string file)
    {
      IsolatedStorageFile fileStorage = IsolatedStorageFile.GetUserStoreForApplication();
      StreamReader Reader = null;
      Reader = new StreamReader(new IsolatedStorageFileStream(file, FileMode.Open, fileStorage));
      
      string textFile = Reader.ReadToEnd();
      
      Reader.Close();

      return textFile;
    }

    // Удаление базы
    public static void Delete(string file)
    {
      IsolatedStorageFile fileStorage = IsolatedStorageFile.GetUserStoreForApplication();

      fileStorage.DeleteFile(file);
    }

    // Проверка на существование файла
    public static bool IsExists(string file)
    {
      IsolatedStorageFile fileStorage = IsolatedStorageFile.GetUserStoreForApplication();

      return fileStorage.FileExists(file);
    }

    // Создание папки
    public static void CreateDir(string name)
    {
      IsolatedStorageFile fileStorage = IsolatedStorageFile.GetUserStoreForApplication();

      fileStorage.CreateDirectory(name);
    }

    // Проверка на существование папки
    public static bool IsDirExists(string dir)
    {
      IsolatedStorageFile fileStorage = IsolatedStorageFile.GetUserStoreForApplication();

      return fileStorage.DirectoryExists(dir);
    }

    // Удаление всех баз данных
    public static void RemoveAll(string directory)
    {
      IsolatedStorageFile fileStorage = IsolatedStorageFile.GetUserStoreForApplication();

      if (fileStorage.DirectoryExists(directory))
      {
        string[] files = fileStorage.GetFileNames(directory + @"/*");
        foreach (string file in files)
        {
          fileStorage.DeleteFile(directory + @"/" + file);
        }

        string[] subDirectories = fileStorage.GetDirectoryNames(directory + @"/*");
        foreach (string subDirectory in subDirectories)
        {
          RemoveAll(directory + @"/" + subDirectory);
        }

        fileStorage.DeleteDirectory(directory);
      }
    }
  }
}
