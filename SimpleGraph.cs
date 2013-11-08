using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NIER2014.Utils;

public class SimpleGraph
{
// 2013-11-08 TRS: First commit with basic functionality. Uses GraphViz
//   for rendering graphs to human-viewable form. Only minimal
//   processing is implemented, i.e. scale timestamp-based weights
//   into [0.0, 1.0] and remove links lighter than 1%.
//   TODO:
//     - Make the DOT files prettier, e.g. different node styles for
//       different types.
//     - Add qualified names to the DOT files. Classes are currently
//       excluded to prevent them from masking the finer structures.
//       The only implementations I can think of right now involve
//       nontrivial changes to the src2srcml parsing or kluge repeated
//       lookups with flagrant layering violations.
//     - Improve/tune postprocessing.
//     - Add functional-style utilities (I love lisps).

	public static void normalize_graph(Dictionary<EntityLink,double> graph)
	{
		// Scales the edge weights so that the lightest is about 0.0 and the
		// heaviest is about 1.0, to within floating point errors. 

		List<EntityLink> graph_keys = new List<EntityLink> (graph.Keys);
		// remove a constant offset from each edge
		double lightest_link = graph.Min(g => g.Value);
		for (int i = 0; i < graph_keys.Count; i++) {
			graph [graph_keys [i]] -= lightest_link;
		}

		// now scale everyone
		double heaviest_link = graph.Max(g => g.Value);
		for (int i = 0; i < graph_keys.Count; i++) {
			graph [graph_keys [i]] /= heaviest_link;
		}
	}

	public static void filter_highpass(Dictionary<EntityLink,double> graph,
	                                   double cutoff)
	{
		List<EntityLink> graph_keys = new List<EntityLink> (graph.Keys);
		for (int i = 0; i < graph_keys.Count; i++) {
			if (graph [graph_keys [i]] < cutoff) {
				graph.Remove (graph_keys [i]);
			}
		}
	}

	public static void dump_DOT(System.IO.TextWriter str,
	                            Dictionary<EntityLink,double> graph) {
		// This function has some ugliness, but IO/format
		// conversions often do.
		str.WriteLine ("digraph SimpleGraph {");
		foreach (KeyValuePair<EntityLink,double> g in graph) {
			string lname;
			string rname;
			if (((EntityLink)g.Key).left.Type ==
			    SourceCodeEntityType.COMMENT) {
				lname = "commentL" +
					g.Key.left.LineStart.ToString () +
						"C" + g.Key.left.ColumnStart.ToString ();
			} else {
				lname = g.Key.left.Name;
			}
			if (g.Key.right.Type ==
			    SourceCodeEntityType.COMMENT) {
				rname = "commentL" +
					g.Key.right.LineStart.ToString () +
						"C" + g.Key.right.ColumnStart.ToString ();
			} else {
				rname = g.Key.right.Name;
			}
			str.WriteLine ("\"" + lname.Replace ("\"", "\'\'") + "\" -> \"" +
			                rname.Replace ("\"", "\'\'") + "\" [weight=" +
			                g.Value.ToString ("F16") + "];");
		}
		str.WriteLine ("}");
	}

	public static int Main (string[] args)
	{
		List<SourceCodeEntityType> EXCLUDED_TYPES =
			new List<SourceCodeEntityType> ();

		// Including classes tends to blow everyone else
		// away since classes contain everything but get
		// parsed in the same way; including comments and,
		// to a lesser extent, attributes makes the
		// graph noisy.
  		EXCLUDED_TYPES.Add (SourceCodeEntityType.COMMENT);
		EXCLUDED_TYPES.Add (SourceCodeEntityType.CLASS);
		EXCLUDED_TYPES.Add (SourceCodeEntityType.ATTRIBUTE);
//		EXCLUDED_TYPES.Add (SourceCodeEntityType.METHOD);

		// I don't recommend making a composite of several sessions.
		// The differing timestamps would throw off the weights such
		// that earlier sessions count less. If composites are a
		// design goal, a preprocessing step wouldn't be hard to add
		// to the src2srcml reader.
		// Split files from the same session should be OK, though.
		if (args.Length < 3) {
			Console.WriteLine ("USAGE: SimpleGraph.exe out-file " +
			"source-directory gaze-result(s)");
			Console.WriteLine ("\tIf <out-file> is - print to stdout.");
			Console.WriteLine ("\t<source-directory> is recursively " +
			                   "searched for .java source files.");
			Console.WriteLine ("\t<gaze-result(s)> is/are XML eye tracking " +
			                   "data over the source\n\t  files in <source-directory>.");
			return 1;
		}

		Config config = new Config ();
		SourceCodeEntitiesFileCollection collection = SrcMLCodeReader.run (
			                                              config.src2srcml_path, args [1]);
		List<string> gaze_files = new List<string> ();
		for (int i = 0; i < args.Length - 2; i++) {
			gaze_files.Add (args [i + 2]);
		}
		GazeResults gaze_results = GazeReader.run (gaze_files) [0];
		Dictionary<EntityLink,double> gaze_links = new Dictionary<EntityLink,double> ();
		SourceCodeEntity previous = null;
		SourceCodeEntity current = null;
		foreach (GazeData gaze_data in gaze_results.gazes) {
			// find out which SourceCodeEntity the subject
			// was looking at for this GazeData
			foreach (SourceCodeEntitiesFile file in collection) {
				if (gaze_data.filename != file.FileName) {
					continue;
				}
				foreach (SourceCodeEntity entity in file) {
					// if this GazeData looks at an ignored type, skip
					if (EXCLUDED_TYPES.Contains (entity.Type)) {
						continue;
					}
					// Sorry about the ugliness, but I hate code
					// duplication more than this; it should be
					// write-only, anyway.
					if (((gaze_data.line > entity.LineStart) &&
					    (gaze_data.line < entity.LineEnd)) ||
					    ((gaze_data.line == entity.LineStart) &&
					    (gaze_data.col >= entity.ColumnStart)) ||
					    ((gaze_data.line == entity.LineEnd) &&
					    (gaze_data.col <= entity.ColumnEnd))) {
						current = entity;
						break;
					}
				}
			}
			// if there was a change of entity, make a note of it
			if ((current != previous) &&
			    (previous != null)) {
				EntityLink link = new EntityLink ();
				link.left = previous;
				link.right = current;
				if (gaze_links.ContainsKey (link)) {
					gaze_links [link] += (double)gaze_data.timestamp;
				} else {
					gaze_links [link] = (double)gaze_data.timestamp;
				}
			}
			previous = current;
		}

		normalize_graph (gaze_links);
		filter_highpass (gaze_links, 0.01);

		// now write the graph to a DOT file (or stdout)
		// to be rendered using GraphViz
		if (args [0] == "-") {
			dump_DOT (System.Console.Out, gaze_links);
		} else {
			using (System.IO.StreamWriter file =
		       new System.IO.StreamWriter(args[0])) {
				dump_DOT (file, gaze_links);
			}
		}
		return 0;
	}
}
