using System;
using System.IO;
using System.Text.RegularExpressions;

using Deepdwn;

namespace Zoner {
	class YamlReader {
	    public static FrontMatter ReadYamlFrontmatter(string path) {
	    	Console.WriteLine("[YamlReader] Attempting to read Yaml frontmatter...");
	    	var text = File.ReadAllText(path);
	    	var matches = Regex.Matches(text, @"\A---\r?\n((?:.*\r?\n)+?)---", RegexOptions.Multiline);
	    	if (matches.Count > 0) {
	    		var yaml = matches[0].Groups[1].Value;
		        return FrontMatter.Deserialize(yaml);
	    	} else {
	    		Console.WriteLine("[YamlReader] No frontmatter detected.");
	    		return null;
	    	}
	    }
	}
}