using System;
using System.Collections.Generic;
using NIER2014.Utils;

public class SourceFileGazePath
{
  public static void Main(string[] args)
  {
    if (args.Length == 0)
    {
      Console.WriteLine("USAGE: Provide a gaze file as the first argument.");
      return;
    }

    GazeResults gaze_results = GazeReader.run(new List<string> { args[0] })[0];

    Stack<string> source_filenames = new Stack<string>();
    foreach (GazeData gaze_data in gaze_results.gazes)
    {
      if (source_filenames.Count == 0)
        source_filenames.Push(gaze_data.filename);
      else
      {
        if (source_filenames.Peek() != gaze_data.filename)
          source_filenames.Push(gaze_data.filename);
      }
    }

    Stack<string> backstack = new Stack<string>();
    while (source_filenames.Count > 0)
      backstack.Push(source_filenames.Pop());

    Console.WriteLine("Results:");
    while (backstack.Count > 0)
      Console.WriteLine(" - " + backstack.Pop());
  }
}
