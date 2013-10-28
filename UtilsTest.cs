using System;
using System.Collections.Generic;
using System.ComponentModel;
using NIER2014.Utils;

public class UtilsTest
{
  public static void Main()
  {
    GazeResults gaze_results = GazeReader.run(
                               new List<string> { "data/gazedata1.xml" })[0];
    foreach (GazeData gaze_data in gaze_results.gazes)
    {
      foreach (PropertyDescriptor descriptor in TypeDescriptor.
                                                GetProperties(gaze_data))
      {
        Console.Write("{0}={1}; ", descriptor.Name,
                      descriptor.GetValue(gaze_data));
      }
      Console.WriteLine("");
    }
  }
}
