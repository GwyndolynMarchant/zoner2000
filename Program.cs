using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Markdig;
using SharpScss;

using Zoner;
using Deepdwn;

class Program {
	static string RawName(string fileName) { return fileName.Contains('_') ? fileName.Split('_')[0] : fileName; }

	static HtmlDocument LoadHtmlFromPath(string path) {
		var doc = new HtmlDocument();
		doc.Load(path);
		return doc;
	}

	static HtmlDocument LoadHtmlFromContent(string content) {
		var doc = new HtmlDocument();
		doc.LoadHtml(content);
		return doc;
	}

	static string OpengraphProperty(string property, string content) {
		return $"<meta property='og:{property}' content='{content}'/>";
	}

	private static StringBuilder stringBuilder = new StringBuilder();

	static void Main(string[] args) {

		if (args.Length < 1) {
			throw new ApplicationException("Oops! Please provide a path to your zone source, you can do this by dragging and dropping a zone source folder onto this executable.");
		}

		args[0] = Path.GetFullPath(args[0]).TrimEnd(Path.DirectorySeparatorChar);

		// Path which is a subdirectory from the provided project root
		string SubPath(string sub) { return Path.Combine(args[0], sub); }

		if (!Directory.Exists(args[0])) {
			throw new ApplicationException("Oops! The provided path is invalid or not a directory.");
		}

		Console.WriteLine($"Going to work on: '{args[0]}'!");

		string header = "";
		string footer = "";

		string head = """
			<!DOCTYPE html>
			<html prefix="og: https://ogp.me/ns#">
				<head profile="http://gmpg.org/xfn/11">
					<title>Title Text</title>
					<meta name="viewport" content="width=device-width, initial-scale=1.0">
					<meta charset="UTF-8">
					<link href="../style/style.css" rel="stylesheet" type="text/css" media="all">
				</head>
				<body>
		""";

		Queue<string> directories = new Queue<string>();
		directories.Enqueue(args[0]);

		List<string> postArchive = new List<string>();
		List<string> postTitles = new List<string>();
		List<string> postDates = new List<string>();
		Dictionary<string, FrontMatter> postFrontMatter = new Dictionary<string, FrontMatter>();

		List<string> pageFilenames = new List<string>();
		List<string> pageData = new List<string>();

		string rss = string.Empty;
		string rssWebsiteLink = string.Empty;

		string disqus = string.Empty;

		List<string> universalHeadNodes = new List<string>();
		List<string> articleHeadNodes = new List<string>();
		void AppendHeadNodes(List<string> headNodes, HtmlNodeCollection nodes) {
			if (nodes != null) {
				foreach (HtmlNode node in nodes) {
					headNodes.Add(node.OuterHtml);
				}
			}
		}

		// Configure the pipeline, adding Yaml Frontmatter support
		var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseYamlFrontMatter().Build();

		while (directories.Count > 0) {
			string directoryPath = directories.Dequeue();
			directoryPath = Path.GetFullPath(directoryPath);
			string DirPath(string sub) { return Path.Combine(directoryPath, sub); }

			if (!Directory.Exists(directoryPath)) {
				Console.WriteLine($"'{directoryPath}' is not a valid directory.");
				continue;
			}

			string[] files = Directory.GetFiles(directoryPath);

			// Check for files
			if (files.Length == 0 && directoryPath == args[0]) {
				throw new ApplicationException("Oops! There are no files in your source directory!");
			}

			// Process site-wide files and data first
			if (directoryPath == args[0]) {
				// Check for index page
				bool indexExists = Array.Exists(Directory.GetFiles(directoryPath),
					filePath => (new Regex("^index")).Match(Path.GetFileNameWithoutExtension(filePath)).Success);

				if (!indexExists) { throw new ApplicationException("Oops! You don't have an index file in your top directory, this is a required file!"); }

				// Check for favicon
				if (!Directory.Exists(DirPath("images"))) {
					Console.WriteLine("[Zoner] WARNING: You don't have an images folder containing a favicon; site will be built without one.");
				} else if (File.Exists(DirPath("images\\favicon.ico"))) {
					universalHeadNodes.Add("<link rel=\"icon\" href=\"../images/favicon.ico\" type=\"image/x-icon\"/>");
				} else if (File.Exists(DirPath("images\\favicon.png"))) {
					universalHeadNodes.Add("<link rel=\"icon\" href=\"../images/favicon.png\" type=\"image/x-icon\"/>");
				} else {
					Console.WriteLine("[Zoner] WARNING: There's no favicon in your images folder; site will be built without one.");
				}

				// Check if style folder exists with a style.css, it must
				if (!Directory.Exists(DirPath("style"))) {
					throw new ApplicationException("Oops! You must have a style folder containing a style.css file.");
				}
				if (!File.Exists(DirPath("style\\style.css")) && !File.Exists(DirPath("style\\style.scss"))) {
					throw new ApplicationException("Oops! You need to have a style.css or style.scss file in your style folder.");
				}

				// Process header and footer
				for (int i = 0; i < files.Length; i++) {
					string filePath = files[i];
					string fileName = Path.GetFileNameWithoutExtension(filePath);

					if (fileName.ToLowerInvariant() != "header" && fileName.ToLowerInvariant() != "footer") { continue; }

					string fileContent = String.Empty;

					void processHTML() {
						HtmlDocument document = LoadHtmlFromPath(filePath);

						HtmlNode legacyZoneletNode = document.DocumentNode.SelectSingleNode("//*[@id='content']");

						if (legacyZoneletNode != null) { // The regex is to remove all spaces and tabulation for a cleaner file
							fileContent = Regex.Replace(legacyZoneletNode.InnerHtml, @"^([\ \x09]+)", string.Empty, RegexOptions.Multiline);
						} else {
							fileContent = Regex.Replace(document.DocumentNode.InnerHtml, @"^([\ \x09]+)", string.Empty, RegexOptions.Multiline) + "\n";
						}
					}

					void processMarkdown() {
						string markdown = File.ReadAllText(filePath);

						// Generate links
						foreach (Match match in Regex.Matches(markdown, @"(\(\.\/|\(\.\.\/)([^(.)]*)\)", RegexOptions.Multiline)) {
							markdown = markdown.Replace("/" + match.Groups[2].Value + ")", "/" + match.Groups[2].Value + (match.Groups[2].Value.Contains(".html") ? string.Empty : ".html") + ")");
						}

						fileContent = Markdown.ToHtml(markdown, pipeline);
					}

					bool checkFileType() {
						switch (Path.GetExtension(files[i])) {
							case ".md":
							case ".txt":
								processMarkdown();
								return true; // Is markdown
							case ".htm":
							case ".html":
							case ".htmls":
								processHTML();
								return false;
							default:
								return false; // IDK
						}
					}

					bool isMarkdown = checkFileType();

					if (fileContent.Trim() == String.Empty) { throw new ApplicationException($"Oops! {filePath} is empty, this is a required file."); }

					HtmlDocument headerFooterDocument = LoadHtmlFromContent(fileContent);

					// Look for any meta tags and add them to the list of meta tags
					AppendHeadNodes(universalHeadNodes, headerFooterDocument.DocumentNode.SelectNodes("//meta"));

					// Grab data for RSS if it exists
					HtmlNode rssTitle = headerFooterDocument.DocumentNode.SelectSingleNode("//rss-title");
					HtmlNode rssDescription = headerFooterDocument.DocumentNode.SelectSingleNode("//rss-description");
					HtmlNode rssLink = headerFooterDocument.DocumentNode.SelectSingleNode("//rss-link");
					if (rssTitle != null && rssDescription != null && rssLink != null) {
						stringBuilder.Clear();
						stringBuilder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n<rss version=\"2.0\" xmlns:atom=\"http://www.w3.org/2005/Atom\">\n<channel>");
						stringBuilder.Append($"\t<title>{rssTitle.InnerText}</title>\n");
						stringBuilder.Append($"\t<description>{rssDescription.InnerText}</description>\n");
						stringBuilder.Append($"\t<link>{rssLink.InnerText}</link>\n");
						stringBuilder.Append($"\t<atom:link href=\"{rssLink.InnerText}\" rel=\"self\"></atom:link>\n");
						rssWebsiteLink = rssLink.InnerText;
						rssTitle.Remove();
						rssDescription.Remove();
						rssLink.Remove();
						HtmlNode rssLanguage = headerFooterDocument.DocumentNode.SelectSingleNode("//rss-language");
						if (rssLanguage != null) {
							stringBuilder.Append($"\t<language>{rssLanguage.InnerText}</language>\n");
							rssLanguage.Remove();
						}
						else {
							stringBuilder.Append("\t<language>en</language>\n");
						}
						stringBuilder.Append("\t<lastBuildDate>\n" + DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss", CultureInfo.CreateSpecificCulture("en-US")) + " +0000\n</lastBuildDate>\n");
						HtmlNode rssTtl = headerFooterDocument.DocumentNode.SelectSingleNode("//rss-ttl");
						if (rssTtl != null) {
							stringBuilder.Append("\t<ttl>" + rssTtl.InnerText + "</ttl>\n\n");
							rssTtl.Remove();
						} else { stringBuilder.Append("\t<ttl>1440</ttl>\n\n"); }

						rss = stringBuilder.ToString();
					}
					else if (fileName.ToLowerInvariant() == "header") {
						Console.WriteLine("[Zoner] WARNING: RSS will not be built for your site, refer to the example site's header for how to implement RSS.");
					}

					// Disqus
					HtmlNode disqusNode = headerFooterDocument.DocumentNode.SelectSingleNode("//disqus");
					if (disqusNode != null) {
						disqus = disqusNode.InnerHtml;
						disqusNode.RemoveAll();
						disqusNode.Remove();
					}

					// TODO: Change to a <zone> tag that wraps them, then just delete that tag's <div> if Markdown
					if ((rss != string.Empty || disqus != string.Empty) && isMarkdown) { // With Markdown you have to wrap the RSS and Disqus top tags in a <div>
						HtmlNode zoneNode = headerFooterDocument.DocumentNode.SelectSingleNode("//zone");
						if (zoneNode != null) {
							zoneNode.RemoveAll();
							zoneNode.Remove();
						}
					}

					// Build header and footer
					if (fileName.ToLowerInvariant() == "header") {
						header = $"<header id=\"header\">\n{headerFooterDocument.DocumentNode.OuterHtml}</header>\n";
						Console.WriteLine("Processed header.");
					} else if (fileName.ToLowerInvariant() == "footer") {
						footer = $"<footer id=\"footer\">\n{headerFooterDocument.DocumentNode.OuterHtml}</footer>\n";
						Console.WriteLine("Processed footer.");
					}
				}

				// If a header or footer wasn't found
				if (header.Trim() == String.Empty) {
					throw new ApplicationException("Oops! There's no header file in your top directory, this is a required file!");
				}

				if (footer.Trim() == String.Empty) {
					throw new ApplicationException("Oops! There's no footer file in your top directory, this is a required file!");
				}

				// Build post list if this site has posts
				if (Directory.Exists(Path.Combine(directoryPath, "posts"))) {
					string[] posts = Directory.GetFiles(Path.Combine(directoryPath, "posts"));
					Array.Sort(posts);

					for (int i = posts.Length - 1; i >= 0; i--) {
						if (Directory.Exists(posts[i])) { continue; }

						string postType = Path.GetExtension(posts[i]);
						if (postType == ".md" || postType == ".txt" || postType == ".html" || postType == ".htm" || postType == ".htmls") {
							string postName = Path.GetFileNameWithoutExtension(posts[i]);

							// Parse frontmatter
							if (postType == ".md" || postType == ".txt") {
								Console.WriteLine($"[Zoner] Preprocessing {RawName(postName)}...");
								FrontMatter fm = YamlReader.ReadYamlFrontmatter(posts[i]);
								if (fm != null) {
									postFrontMatter.Add(RawName(postName), fm);
									postTitles.Add(fm.Title);
								}
								Console.WriteLine();
							}

							// Generate date and .html link name
							DateTime dateTime;
							string postDate = string.Empty;

							if (postName.Length > 10 && DateTime.TryParseExact(postName.Substring(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime)) {
								// Generate title
								string postTitle = string.Empty;
								string postNameNoDate = postName.Substring(11, postName.Length - 11);

								if (postNameNoDate.Trim() == string.Empty) { continue; } // Check if empty first

								if (!postFrontMatter.ContainsKey(RawName(postName))) {
									if (postNameNoDate.Contains('_')) {
										if (postNameNoDate.Split('_')[1] == "None") {
											postTitles.Add(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(postNameNoDate.Split('_')[0].Replace('-', ' ')));
										}
										else {
											postTitles.Add(postNameNoDate.Split('_')[1]);
										}
									}
									else {
										postTitles.Add(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(postNameNoDate.Replace('-', ' ')));
									}
								}

								// Parse and add .html link name and date
								postArchive.Add(RawName(postName) + ".html");
								postDates.Add(dateTime.ToString("yyyy-MM-dd"));
							}
						}
					}
				}
			}

			// Skip images and style folders
			if (directoryPath == SubPath("images") || directoryPath == SubPath("style")) { continue; }

			// Final process of all the pages and posts
			for (int i = 0; i < files.Length; i++) {
				string filePath = files[i];
				string fileName = Path.GetFileNameWithoutExtension(filePath);

				// Ignore header and footer
				if (fileName.ToLowerInvariant() == "header" || fileName.ToLowerInvariant() == "footer") { continue; }

				// Generate title
				string title = string.Empty;
				string fileNameNoDate = (directoryPath != args[0] && fileName.Length > 10 ? fileName.Substring(11, fileName.Length - 11) : fileName);

				if (fileNameNoDate.Contains('_')) { // TODO: Could replace this with a lookup to the postTitles List<>?
					if (fileNameNoDate.Split('_')[1] == "None") { // This means a post title (and date) will not be generated by Zoner
						// We still have to use the filename's title for the head <title> though
						title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(fileNameNoDate.Split('_')[0].Replace('-', ' '));
					}
					else {
						title = fileNameNoDate.Split('_')[1]; // Use user's title
					}
				}
				else {
					title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(fileNameNoDate.Replace('-', ' '));
				}

				if (directoryPath != args[0] && title.Trim() == string.Empty) {
					Console.WriteLine("[Zoner] WARNING: '{0}' filename has no title, post will not be built!", filePath);
					continue;
				}

				// Date
				DateTime dateTime = new DateTime();
				string postDate = string.Empty;

				if (fileName.Length > 10 && DateTime.TryParseExact(fileName.Substring(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime)) {
					postDate = dateTime.ToString("d MMMM yyyy");
				}
				else if (directoryPath != args[0]) {
					Console.WriteLine("[Zoner] WARNING: '{0}' has no date or an invalid date prepending the filename, post will not be built! Make sure you have leading zeros in the day and month spots if they're single digit. Also make sure your date is formatted year-month-day-, tailing with a dash!", filePath);
					continue;
				}

				// Parse contents
				string fileContent = String.Empty;

				void processHTML() {
					HtmlDocument document = LoadHtmlFromPath(filePath);

					HtmlNode legacyZoneletNode = document.DocumentNode.SelectSingleNode("//*[@id='content']");

					if (legacyZoneletNode != null) { // The regex is to remove all spaces and tabulation for a cleaner file
						fileContent = Regex.Replace(legacyZoneletNode.InnerHtml, @"^([\ \x09]+)", string.Empty, RegexOptions.Multiline);

						HtmlNode nextPreviousNode = document.DocumentNode.SelectSingleNode("//*[@id='nextprev']");
						if (nextPreviousNode != null) {
							nextPreviousNode.RemoveAll();
							nextPreviousNode.Remove();
						}

						HtmlNode disqusNode = document.DocumentNode.SelectSingleNode("//*[@id='disqus_thread']");
						if (disqusNode != null) {
							disqusNode.RemoveAll();
							disqusNode.Remove();
						}
					}
					else {
						fileContent = Regex.Replace(document.DocumentNode.InnerHtml, @"^([\ \x09]+)", string.Empty, RegexOptions.Multiline) + "\n";
					}
				}

				void processMarkdown() {
					string markdown = File.ReadAllText(filePath);

					// Parse yaml frontmatter
					try {
						FrontMatter fm = postFrontMatter[RawName(fileName)];
						title = fm.Title;
						try {
							foreach (string tag in fm.Tags) {
								articleHeadNodes.Add(OpengraphProperty("tag", tag));
							}
						} catch (NullReferenceException e) {
							Console.WriteLine("[Zoner] No tags found for page.");
						}
					} catch (KeyNotFoundException e) {
						Console.WriteLine("[Zoner] No frontmatter for page."); 
					}

					// Generate links
					foreach (Match match in Regex.Matches(markdown, @"(\(\.\/|\(\.\.\/)([^(.)]*)\)", RegexOptions.Multiline)) {
						markdown = markdown.Replace("/" + match.Groups[2].Value + ")", "/" + match.Groups[2].Value + (match.Groups[2].Value.Contains(".html") ? string.Empty : ".html") + ")");
					}

					// Convert the markdown into an html document for further manipulation
					fileContent = Markdown.ToHtml(markdown, pipeline);
					HtmlDocument document = LoadHtmlFromContent($"<html><body>{fileContent}</body></html>");
					
					// Move any further meta tags out
					try {
						HtmlNodeCollection nodes = document.DocumentNode.SelectNodes("//meta");
						AppendHeadNodes(articleHeadNodes, nodes);
						foreach (HtmlNode node in nodes) { node.Remove(); }
						fileContent = document.DocumentNode.InnerHtml;
					} catch {
						Console.WriteLine("[Zoner] No meta tags found");
					}
				}

				switch (Path.GetExtension(files[i])) {
					case ".htm":
					case ".html":
					case ".htmls":
						processHTML(); break;
					case ".md":
					case ".txt":
						processMarkdown(); break;
					default:
						Console.WriteLine("[Zoner] WARNING: '{0}' is either not a supported filetype or an image that should go in your images folder, will not be built.", filePath);
						continue;
				}

				if (fileContent.Trim() == String.Empty) {
					Console.WriteLine($"[Zoner] WARNING: '{filePath}' is empty, will not be built.");
					continue;
				}

				// Generate head
				HtmlDocument headDocument = LoadHtmlFromContent(
					directoryPath == args[0] ? head.Replace("\"../", "\"./") : head.Replace("\"./", "\"../") // Make links relative
				); 
				HtmlNode titleNode = headDocument.DocumentNode.SelectSingleNode("//title");
				titleNode.InnerHtml = titleNode.InnerHtml.Replace(titleNode.InnerHtml, title);
				articleHeadNodes.Add(OpengraphProperty("title", title)); 	// Add opengraph title
				articleHeadNodes.Add(OpengraphProperty("type", "article"));	// Add opengraph type
				
				// Add head nodes back in
				foreach (string nodeString in universalHeadNodes) {
					headDocument.DocumentNode.SelectSingleNode("//head").AppendChild(HtmlNode.CreateNode(nodeString));	
				}
				foreach (string nodeString in articleHeadNodes) {
					headDocument.DocumentNode.SelectSingleNode("//head").AppendChild(HtmlNode.CreateNode(nodeString));
				}
				articleHeadNodes.Clear();

				// Build page TODO: Replace all the link replacements with a single string one at the end instead of individually
				stringBuilder.Clear();
				// HtmlAgilityPack auto-closes tags when loading HTML, so we subtract the body and html tags
				stringBuilder.Append(headDocument.DocumentNode.InnerHtml.Substring(0, headDocument.DocumentNode.InnerHtml.Length - 14));
				stringBuilder.Append("<div id=\"container\">\n");
				stringBuilder.Append((directoryPath == args[0] ? header.Replace("\"../", "\"./") : header.Replace("\"./", "\"../")));
				stringBuilder.Append("<article id=\"content\">\n");
				if (directoryPath != args[0]) { // Header for posts if not None
					if (fileName.Contains('_') && fileName.Split('_')[1] == "None") {
						Console.WriteLine("This post has no title.");
					} else {
						stringBuilder.Append("<h1>" + title + "</h1>\n");
						stringBuilder.Append("<h4>" + postDate + "</h4>\n");
					}
				}
				stringBuilder.Append((directoryPath == args[0] ? fileContent.Replace("\"../", "\"./") : fileContent.Replace("\"./", "\"../")));

				if (directoryPath != args[0]) {
					// "Next Post" and "Previous Post" buttons for posts
					stringBuilder.Append("<nav id=\"nextprev\">");
					int postIndex = postTitles.IndexOf(title);
					if (postIndex > 0) {
						string nextFileName = postArchive[postIndex - 1];
						stringBuilder.Append("<a href=\"./" + RawName(nextFileName) + "\">« Next Post</a> | ");
					}
					stringBuilder.Append("<a href=\"../index.html\">Home</a>");
					if (postIndex + 1 != postTitles.Count) {
						string previousFileName = postArchive[postIndex + 1];
						stringBuilder.Append(" | <a href=\"./" + RawName(previousFileName) + "\">Previous Post »</a>");
					}
					stringBuilder.Append("</nav>\n");

					// Generate Disqus if setup
					if (disqus != string.Empty) {
						stringBuilder.Append(disqus + "\n");
					}
				}
				stringBuilder.Append("</article>\n");
				stringBuilder.Append((directoryPath == args[0] ? footer.Replace("\"../", "\"./") : footer.Replace("\"./", "\"../")));
				stringBuilder.Append("</div>\n</body>\n</html>");

				HtmlDocument finalDocument = LoadHtmlFromContent(stringBuilder.ToString());

				// Custom style sheet reference TODO: Make a checker here that the style exists, warn user if it doesn't
				HtmlNode styleNode = finalDocument.DocumentNode.SelectSingleNode("//style");
				if (styleNode != null) {
					HtmlNode styleLink = finalDocument.DocumentNode.SelectSingleNode("(//link)[2]");
					string originalStyle = styleLink.GetAttributeValue("href", "");
					int lastStyleIndex = originalStyle.LastIndexOf("style");
					styleLink.SetAttributeValue("href", originalStyle.Remove(lastStyleIndex, "style".Length).Insert(lastStyleIndex, styleNode.InnerText));
					styleNode.Remove();
				}

				// Generate RSS item for post if RSS is configured in header
				if (rss != string.Empty && directoryPath != args[0]) {
					string rssArticleDescription = string.Empty;
					HtmlNode rssNode = finalDocument.DocumentNode.SelectSingleNode("//rss");
					if (rssNode != null) {
						rssArticleDescription = rssNode.InnerText;
						rssNode.Remove();
					}
					else {
						HtmlNode articleNode = finalDocument.DocumentNode.SelectSingleNode("//p");
						rssArticleDescription = articleNode.InnerText;
					}

					rssArticleDescription = (rssArticleDescription.Length > 500 ? rssArticleDescription.Substring(0, 500 - 3) + "..." : rssArticleDescription);

					stringBuilder.Clear();
					stringBuilder.Append("\t<item>\n");
					stringBuilder.Append("\t\t<title>" + title + "</title>\n");
					stringBuilder.Append("\t\t<description>" + rssArticleDescription + "</description>\n");
					stringBuilder.Append($"\t\t<link>{rssWebsiteLink}posts/{RawName(fileName)}.html</link>\n");
					stringBuilder.Append("\t\t<pubDate>\n\t\t\t" + dateTime.ToString("ddd, dd MMM yyyy HH:mm:ss", CultureInfo.CreateSpecificCulture("en-US")) + " +0000\n\t\t</pubDate>\n");
					stringBuilder.Append("\t</item>\n\n");

					rss += stringBuilder.ToString();
				}

				// Remove tag if rss isn't setup
				if (rss == string.Empty && directoryPath != args[0]) {
					HtmlNode rssNode = finalDocument.DocumentNode.SelectSingleNode("//rss");
					if (rssNode != null) {
						rssNode.ParentNode.Remove();
						Console.WriteLine($"[Zoner] WARNING: RSS is not setup in your header, but '{filePath}' has an <rss> tag!");
					}
				}

				// Generate archives if they exist in the page
				HtmlNodeCollection archiveNodes = finalDocument.DocumentNode.SelectNodes("//archive");
				if (archiveNodes != null && archiveNodes.Count > 0 && postArchive.Count > 0) {
					foreach (HtmlNode archiveNode in archiveNodes) {
						stringBuilder.Clear();
						stringBuilder.Append("<div id=\"postlistdiv\">\n<ul>\n");
						int postCount = (archiveNode.GetAttributeValue("count", 0) == 0 ? postArchive.Count : archiveNode.GetAttributeValue("count", 0));
						int j = 0;
						for (; j < postCount; j++) {
							if (j > postArchive.Count - 1) { break; }
							stringBuilder.Append($"<li><a href=\"./posts/{postArchive[j]}\">{postDates[j]} » {postTitles[j]}</a></li>\n");
						}

						if (archiveNode.GetAttributeValue("more", string.Empty) != string.Empty && j < postArchive.Count) {
							stringBuilder.Append("<li class=\"moreposts\"><a href=\"" + archiveNode.GetAttributeValue("more", string.Empty) + ".html\">» more posts</a></li>\n");
						}
						stringBuilder.Append("</ul>\n</div>\n");

						string builtArchive = stringBuilder.ToString();
						builtArchive = (directoryPath == args[0] ? builtArchive.Replace("\"../", "\"./") : builtArchive.Replace("\"./", "\"../"));

						HtmlNode builtArchiveNode = HtmlNode.CreateNode(Markdown.ToHtml(builtArchive, pipeline));

						archiveNode.ParentNode.ReplaceChild(builtArchiveNode, archiveNode);
					}
				}

				// Store page for writing to build directory TODO: I could probably reuse the postArchive List<> here, and rename postArchive to postLinks or something
				pageFilenames.Add((directoryPath != args[0] ? "posts/" : string.Empty) + RawName(fileName) + ".html");
				pageData.Add(finalDocument.DocumentNode.OuterHtml);

				Console.WriteLine($"[Zoner] Processed file: '{filePath}'.\n");
			}

			Array.ForEach(Directory.GetDirectories(directoryPath), sub => directories.Enqueue(sub));
		}

		// Close RSS
		if (rss != string.Empty) { rss += "</channel>\n</rss>"; }

		// Package zone, we can't fail now!
		string zoneName = Path.GetFileName(args[0]);

		// Prep the build directory
		string buildDirectory = Path.Combine(Directory.GetParent(Path.GetFullPath(args[0])).ToString(), zoneName + "-built");
		string BuildPath(string sub) { return Path.Combine(buildDirectory, sub); }
		Directory.CreateDirectory(buildDirectory);
		Console.WriteLine(buildDirectory);

		// Helper functions
		FileInfo[] GetFiles(string dir) { return new DirectoryInfo(SubPath(dir)).GetFiles(); }
		void CreateBuildDirectory(string dir) { Directory.CreateDirectory(BuildPath(dir)); }

		// Copy images
		if (Directory.Exists(SubPath("images"))) {
			CreateBuildDirectory("images");
			Array.ForEach(GetFiles("images"), file => file.CopyTo(BuildPath($"images\\{file.Name}"), true));
		}

		// Copy style
		CreateBuildDirectory("style");
		Array.ForEach(GetFiles("style"), file => {
			if (file.Extension == ".scss") {
				File.WriteAllText(BuildPath($"style\\{Path.GetFileNameWithoutExtension(file.Name)}.css"), Scss.ConvertFileToCss(file.FullName).Css);
			} else {
				file.CopyTo(BuildPath($"style\\{file.Name}"), true);
			}
		});

		// Build pages
		CreateBuildDirectory("posts");
		for (int i = 0; i < pageData.Count; i++) {
			File.WriteAllText(BuildPath(pageFilenames[i]), pageData[i]);
		}

		// RSS
		File.WriteAllText(BuildPath("feed.xml"), rss);

		Console.WriteLine($"[Zoner] SUCCESS: Your zone has been built to: '{buildDirectory}'.");
	}
}
