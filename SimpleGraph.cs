using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using NIER2014.Utils;

public class SimpleGraph
{
// 2013-11-08 TRS: First commit with basic functionality. Uses GraphViz
//   for rendering graphs to human-viewable form. Only minimal
//   processing is implemented, i.e. scale timestamp-based weights
//   into [0.0, 1.0] and remove links lighter than 1%.
// 2013-11-13 TRS: Adjusted processing so that new links are initialized
//   to t^2 and repeat links get t added. Postprocessing now scales and
//   applies a highpass filter at 90%. Also, filenames are now inlcuded
//   in the nodes; thanks Braden.
//   TODO:
//     - Make the DOT files prettier, e.g. different node styles for
//       different types.
//     - Improve/tune postprocessing.
//     - Add functional-style utilities (I love lisps).

	// Including classes tends to blow everyone else
	// away since classes contain everything but get
	// parsed in the same way; including comments and,
	// to a lesser extent, attributes makes the
	// graph noisy.
	private static SourceCodeEntityType[] EXCLUDED_TYPES = {
			SourceCodeEntityType.COMMENT,
			SourceCodeEntityType.CLASS,
			//SourceCodeEntityType.ATTRIBUTE,
			//SourceCodeEntityType.METHOD,
		};

	public static Dictionary<EntityLink, double> gen_graph(
		GazeResults gaze_results, SourceCodeEntitiesFileCollection collection)
	{
		Dictionary<EntityLink,double> src2src_links =
			new Dictionary<EntityLink,double> ();
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
					if (((IList<SourceCodeEntityType>) EXCLUDED_TYPES).Contains(
							entity.Type)) {
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
				if (src2src_links.ContainsKey (link)) {
					src2src_links [link] += Math.Pow (gaze_data.timestamp, 1.0);
				} else {
					src2src_links [link] = Math.Pow (gaze_data.timestamp, 2.0);
				}
			}
			previous = current;
		}

		return src2src_links;
	}

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
	public static void edge_filter_highpass(Dictionary<EntityLink,double> graph,
	                                        double cutoff)
	{
		List<EntityLink> graph_keys = new List<EntityLink> (graph.Keys);
		for (int i = 0; i < graph_keys.Count; i++) {
			if (graph [graph_keys [i]] < cutoff) {
				graph.Remove (graph_keys [i]);
			}
		}
	}

	/*
	Determines the score for each source code entity by summing the score of all
	edges containing that entity, and normalising the result.
	*/
	public static Dictionary<SourceCodeEntity, double>
		entity_score_from_edge_score(Dictionary<EntityLink, double> graph)
	{
		var result = new Dictionary<SourceCodeEntity, double>();

		//Go through each link, summing the score of that link for each source code
		//entity.
		foreach (EntityLink key in graph.Keys)
		{
			double value = graph[key];

			//Left entity.
			if (result.ContainsKey(key.left))
				result[key.left] += value;
			else
				result.Add(key.left, value);

			//Right entity.
			if (result.ContainsKey(key.right))
				result[key.right] += value;
			else
				result.Add(key.right, value);
		}

		//Normalise result.
		double max_in_result = result.Max(item => item.Value);
		foreach (var key in new List<SourceCodeEntity>(result.Keys))
			result[key] /= max_in_result;

		return result;
	}

	public static void sce_filter_highpass(
		Dictionary<SourceCodeEntity, double> entity_scores, double cutoff)
	{
		foreach (var key in new List<SourceCodeEntity>(entity_scores.Keys))
			if (entity_scores[key] < cutoff)
				entity_scores.Remove(key);
	}

	/*
	Use entity scores from multiple gaze files and composite the results into one
	set of entity scores. Do this by summing all occurrences of the same entity
	and then normalising the result.
	*/
	public static Dictionary<SourceCodeEntity, double> composite_entity_scores(
		List<Dictionary<SourceCodeEntity, double>> entity_scores)
	{
		var result = new Dictionary<SourceCodeEntity, double>();

		foreach (var entity_score in entity_scores)
		{
			foreach (var key in new List<SourceCodeEntity>(entity_score.Keys))
			{
				double value = entity_score[key];

				if (result.ContainsKey(key))
					result[key] += value;
				else
					result.Add(key, value);
			}
		}

		//Normalise result.
		double max_in_result = result.Max(item => item.Value);
		foreach (var key in new List<SourceCodeEntity>(result.Keys))
			result[key] /= max_in_result;

		return result;
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
				lname = g.Key.left.Name + " (" + g.Key.left.parent_file.FileName + ")";
			}
			if (g.Key.right.Type ==
			    SourceCodeEntityType.COMMENT) {
				rname = "commentL" +
					g.Key.right.LineStart.ToString () +
						"C" + g.Key.right.ColumnStart.ToString ();
			} else {
				rname = g.Key.right.Name + " (" + g.Key.right.parent_file.FileName + ")";
			}
			str.WriteLine ("\"" + lname.Replace ("\"", "\'\'") + "\" -> \"" +
			                rname.Replace ("\"", "\'\'") + "\" [weight=" +
			               g.Value.ToString ("F16") + " penwidth=" + g.Value.ToString ("F16") + "];");
		}
		str.WriteLine ("}");
	}

	/*
	Writes out the names of source code entities and their scores. To be used when
	determining source code entities important to the current task.
	*/
	public static void dump_links(System.IO.TextWriter str,
		Dictionary<SourceCodeEntity, double> entity_scores)
	{
		foreach (SourceCodeEntity key in entity_scores.Keys)
			str.WriteLine("{ \"entity_name\": \"" + key.DotFullyQualifiedName +
			                                 "\", " +
			              "\"file_name\": \"" + key.parent_file.FileName + "\", " +
			              "\"score:\": " + entity_scores[key] + " }");
	}

	public static int Main (string[] args)
	{
		// I don't recommend making a composite of several sessions.
		// The differing timestamps would throw off the weights such
		// that earlier sessions count less. If composites are a
		// design goal, a preprocessing step wouldn't be hard to add
		// to the src2srcml reader.
		// Split files from the same session should be OK, though.
		if (args.Length < 3) {
			Console.WriteLine ("USAGE: SimpleGraph.exe {edge-digraph|importance} " +
			"out-file source-directory gaze-result(s)");
			Console.WriteLine ("\tIf <out-file> is - print to stdout.");
			Console.WriteLine ("\t<source-directory> is recursively " +
			                   "searched for .java source files.");
			Console.WriteLine ("\t<gaze-result(s)> is/are XML eye tracking " +
			                   "data over the source\n\t  files in <source-directory>.");
			return 1;
		}
		Config config = new Config ();
		SourceCodeEntitiesFileCollection collection = SrcMLCodeReader.run (
			                                              config.src2srcml_path, args [2]);
		List<string> gaze_files = new List<string> ();
		for (int i = 0; i < args.Length - 3; i++) {
			gaze_files.Add (args [i + 3]);
		}

		List<GazeResults> gaze_results = GazeReader.run(gaze_files);
		var src2src_links = new List<Dictionary<EntityLink, double>>();
		var entity_scores = new List<Dictionary<SourceCodeEntity, double>>();
		foreach (var gaze_result in gaze_results)
		{
			var cur_src2src_links = gen_graph(gaze_result, collection);
			normalize_graph(cur_src2src_links);
			edge_filter_highpass(cur_src2src_links, 0.95);
			src2src_links.Add(cur_src2src_links);

			if (args[0] == "importance")
			{
				var entity_score = entity_score_from_edge_score(cur_src2src_links);
				sce_filter_highpass(entity_score, 0.95);
				entity_scores.Add(entity_score);
			}
		}
		// Write DOT file for GraphViz or write importance set. Use standard out if
		// specified.
		TextWriter output_writer = System.Console.Out;
		if (args[1] != "-")
			output_writer = new StreamWriter(args[1]);

		if (args[0] == "edge-digraph")
			dump_DOT(output_writer, src2src_links[0]);
		else if (args[0] == "importance")
			dump_links(output_writer, composite_entity_scores(entity_scores));
		else
			Console.WriteLine("Incorrect first parameter");

		return 0;
	}
}
