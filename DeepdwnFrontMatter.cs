using System;
using System.Text.RegularExpressions;

/** Implements Deepdwn Yaml Frontmatter as defined in: https://support.deepdwn.com/docs/guide/getting-started/ */

namespace Deepdwn {

	class FrontMatter {

		public enum Direction {
			ltr,
			rtl
		}

		public string Title { get; set; }
		public string Category { get; set; }
		public string[] Tags { get; set; }
		public bool Pinned { get; set; }
		public Direction Dir { get; set; }

		public override string ToString() {
			return $"[ title: {this.Title}, category: {this.Category}, tags: [{string.Join(", ", this.Tags)}], pinned: {this.Pinned}, direction: { this.Dir } ]";
		}

		private static string[] DeserializeTags(string yaml) {
			var matches = Regex.Matches(yaml, @"^\s*-\s?(.+)$", RegexOptions.Multiline);
			var tags = new string[matches.Count];
			for (var i = 0; i < matches.Count; i++) {
				tags[i] = matches[i].Groups[1].Value;
				Console.WriteLine($"[Frontmatter] Tag: {tags[i]}");
			}
			return tags;
		}

		public static FrontMatter Deserialize(string yaml) {
			var matches = Regex.Matches(yaml, @"^(\w+):(.*)(\r?\n(?:\s*-.+)+)?", RegexOptions.Multiline);
			var fm = new FrontMatter();
			foreach (Match match in matches) {
				string key = match.Groups[1].Value;
				Console.WriteLine($"[Frontmatter] Key: {key}");
				switch (key) {
					case "title": 		fm.Title 	= match.Groups[2].Value; break;
					case "category": 	fm.Category = match.Groups[2].Value; break;
					case "tags": 		fm.Tags 	= DeserializeTags(match.Groups[3].Value); break;
					case "pinned": 		fm.Pinned 	= match.Groups[2].Value == "true"; break;
					case "dir": 		fm.Dir 		= match.Groups[2].Value == "ltr" ? Direction.ltr : Direction.rtl; break;
					default: continue;
				}
			}
			return fm;
		}
	}
}