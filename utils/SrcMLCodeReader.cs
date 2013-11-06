using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

namespace NIER2014.Utils
{
  public enum SourceCodeEntityType
  {
    CLASS = 1,
    ATTRIBUTE = 2,
    METHOD = 3,
    COMMENT = 4
  }

  public class SourceCodeEntity
  {
    public int LineStart { get; set; }
    public int LineEnd { get; set; }
    public int ColumnStart { get; set; }
    public int ColumnEnd { get; set; }
    public SourceCodeEntityType Type { get; set; }
    public string Name { get; set; }
  }

  public class SourceCodeEntitiesFile : List<SourceCodeEntity>
  {
    public string FileName { get; set; }

    public SourceCodeEntitiesFile() : base()
    {
      //Must be defined, but do nothing.
    }
  }

  public class SourceCodeEntitiesFileCollection : List<SourceCodeEntitiesFile>
  {
    public SourceCodeEntitiesFileCollection() : base()
    {
      //Must be defined, but do nothing.
    }
  }

  public abstract class SrcMLCodeReader
  {
    private class EntityPositionInfo
    {
      public int line_start { get; set; }
      public int col_start { get; set; }
      public int line_end { get; set; }
      public int col_end { get; set; }
    }

    private const string SRCML_ARG_FORMAT = "--position \"{0}\" -o \"{1}\"";
    private const string XML_GLOBAL_NS = "http://www.sdml.info/srcML/src";
    private const string XML_POS_NS = "http://www.sdml.info/srcML/position";

    public static SourceCodeEntitiesFileCollection run(string src2srcml_path,
                                                       string input_directory)
    {
      if (!verifySrc2SrcMLExecutable(src2srcml_path))
        Console.WriteLine("WARNING: src2srcml_path does not appear to be a " +
                          "src2srcml executable");

      List<FileInfo> files = SrcMLCodeReader.getFiles(
          new DirectoryInfo(input_directory), "*.java");
      SourceCodeEntitiesFileCollection collection =
          new SourceCodeEntitiesFileCollection();
      foreach (FileInfo file in files)
      {
          XmlDocument document = new XmlDocument();
          document.PreserveWhitespace = true;
          document.LoadXml(loadSourceInfo(file, src2srcml_path));
          SourceCodeEntitiesFile scef = processSrcML(document);
          scef.FileName = file.Name;
          collection.Add(scef);
      }

      return collection;
    }

    private static SourceCodeEntitiesFile processSrcML(XmlDocument doc)
    {
      SourceCodeEntitiesFile scef = new SourceCodeEntitiesFile();
      XPathNavigator navigator = doc.CreateNavigator();

      collectEntitiesByType(scef, navigator, "class",
        SourceCodeEntityType.CLASS, "*[local-name() = 'name']");
      collectEntitiesByType(scef, navigator, "decl_stmt",
        SourceCodeEntityType.ATTRIBUTE,
        "*[local-name() = 'decl']/*[local-name() = 'name']");
      collectEntitiesByType(scef, navigator, "function",
        SourceCodeEntityType.METHOD, "*[local-name() = 'name']");
      collectEntitiesByType(scef, navigator, "comment",
        SourceCodeEntityType.COMMENT, ".");

      return scef;
    }

    private static void collectEntitiesByType(SourceCodeEntitiesFile scef,
      XPathNavigator navigator, string srcml_name,
      SourceCodeEntityType type, string name_xpath)
    {
      XPathNodeIterator iterator = (XPathNodeIterator) navigator.Evaluate(
        "//*[local-name() = '" + srcml_name + "']");
      while (iterator.MoveNext())
      {
        XPathNavigator element = iterator.Current;
        EntityPositionInfo position = getEntityPosition(element);

        string name = "";
        XPathNodeIterator search_name = (XPathNodeIterator)
          element.Evaluate(name_xpath);
        if (search_name.MoveNext())
          name = search_name.Current.Value;

        SourceCodeEntity sce = new SourceCodeEntity();
        sce.LineStart = position.line_start;
        sce.LineEnd = position.line_end;
        sce.ColumnStart = position.col_start;
        sce.ColumnEnd = position.col_end;
        sce.Type = type;
        sce.Name = name;
        scef.Add(sce);
      }
    }

    private static EntityPositionInfo getEntityPosition(
      XPathNavigator current_node)
    {
      EntityPositionInfo result = new EntityPositionInfo();

      //Search for position.
      XPathNavigator search_node = current_node;
      while (search_node != null)
      {
        string line = search_node.GetAttribute("line", XML_POS_NS);
        string column =
          search_node.GetAttribute("column", XML_POS_NS);
        if (line != String.Empty && result.line_start == 0)
          result.line_start = Convert.ToInt32(line);
        if (column != String.Empty && result.col_start == 0)
          result.col_start = Convert.ToInt32(column);

        if (result.line_start != 0 && result.col_start != 0)
          break;

        XPathNodeIterator iter = search_node.SelectChildren(
          XPathNodeType.Element);
        if (iter.MoveNext())
          search_node = iter.Current;
        else
          search_node = null;
      }
      if (result.line_start == 0 || result.col_start == 0)
        throw new Exception("Line/column of a source code entity " +
                            "could not be found.");

      string[] lines = Regex.Split(current_node.Value, "\r\n|\r|\n");
      result.line_end = result.line_start + (lines.Length - 1);
      result.col_end = lines.Length > 1 ? lines[lines.Length - 1].Length :
                                          result.col_start + lines[0].Length;

      return result;
    }

    public static List<FileInfo> getFiles(DirectoryInfo directory,
                                          string search)
    {
      List<FileInfo> files = new List<FileInfo>();

      //Files in current directory.
      foreach (FileInfo cur_file in directory.GetFiles(search))
      {
        try
        {
          files.Add(cur_file);
        }
        catch
        {
          //Ignore file.
        }
      }

      //Files in children directories.
      foreach (DirectoryInfo cur_directory in directory.GetDirectories())
      {
        List<FileInfo> dir_files = getFiles(cur_directory, search);
        foreach (FileInfo file in dir_files)
          files.Add(file);
      }

      return files;
    }

    private static bool verifySrc2SrcMLExecutable(string src2srcml_path)
    {
      //Execute, querying version information.
      ProcessStartInfo info = new ProcessStartInfo(src2srcml_path, "-V");
      info.RedirectStandardOutput = true;
      info.UseShellExecute = false;
      Process process = Process.Start(info);
      string stdout_string = process.StandardOutput.ReadToEnd();
      process.WaitForExit();

      //Correct executable should output "src2srcml".
      return stdout_string.Contains("src2srcml");
    }

    private static string loadSourceInfo(FileInfo file_info,
                                         string src2srcml_path)
    {
      string output_filename = file_info.FullName + ".xml";

      //If srcML already exists, use that.
      if (File.Exists(output_filename))
        return File.ReadAllText(output_filename);

      //Execute src2srcml.
      string arg = string.Format(SRCML_ARG_FORMAT, file_info.FullName,
                                 output_filename);
      ProcessStartInfo info = new ProcessStartInfo(src2srcml_path, arg);
      info.RedirectStandardError = true;
      info.UseShellExecute = false;
      info.CreateNoWindow = true;
      Process process = Process.Start(info);
      string stderr_string = process.StandardError.ReadToEnd();
      process.WaitForExit();
      process.Close();

      //If stderr, log error.
      if (stderr_string != String.Empty)
        throw new Exception("src2srcml reported an error:\n" + stderr_string);

      //Load srcml data to string.
      string result = File.ReadAllText(output_filename);

      return result;
    }
  }
}
