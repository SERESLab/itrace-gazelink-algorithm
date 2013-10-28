using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;

namespace NIER2014.Utils
{
  public class GazeData
  {
    public string filename { get; set; }
    public int line { get; set; }
    public int col { get; set; }
    public int x { get; set; }
    public int y { get; set; }
    public double left_validation { get; set; }
    public double right_validation { get; set; }
    public long timestamp { get; set; }
  }

  public class TrackingEnvironment
  {
    public int screen_width { get; set; }
    public int screen_height { get; set; }
    public int line_height { get; set; }
    public int font_height { get; set; }
  }

  public class GazeResults
  {
    public List<GazeData> gazes { get; set; }
    public TrackingEnvironment environment { get; set; }

    public GazeResults(List<GazeData> gazes, TrackingEnvironment environment)
    {
      this.gazes = gazes;
      this.environment = environment;
    }
  }

  public abstract class GazeReader
  {
    public static List<GazeResults> run(List<string> in_filenames)
    {
      List<GazeResults> results = new List<GazeResults>();
      foreach (string filename in in_filenames)
        results.Add(runSingle(filename));
      return results;
    }

    private static GazeResults runSingle(string in_filename)
    {
      XmlDocument document = new XmlDocument();
      List<GazeData> gazes = new List<GazeData>();
      TrackingEnvironment environment = new TrackingEnvironment();
      try
      {
        document.Load(in_filename);
        XPathNavigator navigator = document.CreateNavigator();
        XPathNodeIterator iterator = (XPathNodeIterator)
          navigator.Evaluate("itrace-records/environment/*");
        while (iterator.MoveNext())
        {
          XPathNavigator element = iterator.Current;
          switch (element.Name)
          {
            case "screen-size":
              string width = element.GetAttribute("width", "");
              string height = element.GetAttribute("height", "");
              if (width != String.Empty && height != String.Empty)
              {
                environment.screen_width = Convert.ToInt32(width);
                environment.screen_height = Convert.ToInt32(height);
              }
              break;
            case "line-height":
              string line_height = element.Value;
              if (line_height != String.Empty)
                environment.line_height = Convert.ToInt32(line_height);
              break;
            case "font-height":
              string font_height = element.Value;
              if (font_height != String.Empty)
                environment.font_height = Convert.ToInt32(font_height);
              break;
          }
        }
        iterator = (XPathNodeIterator)
          navigator.Evaluate("itrace-records/gazes/response");
        while (iterator.MoveNext())
        {
          XPathNavigator element = iterator.Current;
          String filename = element.GetAttribute("file", "");
          String line = element.GetAttribute("line", "");
          String col = element.GetAttribute("col", "");
          String x = element.GetAttribute("x", "");
          String y = element.GetAttribute("y", "");
          String timestamp = element.GetAttribute("timestamp", "");
          String left_validation = element.GetAttribute("left-validation", "");
          String right_validation =
            element.GetAttribute("right-validation", "");
          if (filename != String.Empty && line != String.Empty &&
            col != String.Empty && x != String.Empty && y != String.Empty &&
            timestamp != String.Empty && left_validation != String.Empty &&
            right_validation != String.Empty)
          {
            GazeData gaze_data = new GazeData();
            gaze_data.filename = filename;
            gaze_data.line = Convert.ToInt32(line);
            gaze_data.col = Convert.ToInt32(col);
            gaze_data.x = Convert.ToInt32(x);
            gaze_data.y = Convert.ToInt32(y);
            gaze_data.timestamp = Convert.ToInt64(timestamp);
            gaze_data.left_validation = Convert.ToDouble(left_validation);
            gaze_data.right_validation = Convert.ToDouble(right_validation);
            gazes.Add(gaze_data);
          }
        }
        return new GazeResults(gazes, environment);
      }
      catch (Exception e)
      {
        throw e;
      }
    }
  }
}
