using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Markdig;

class Program {

	static void Main(string[] args) {

		if (args.Length < 1) {
			Console.WriteLine("Oops! Please provide a path to your zone source, you can do this by dragging and dropping a zone source folder onto this executable.");
			Console.ReadKey(true);
			return;
		}

		if (!Directory.Exists(args[0])) {
			Console.WriteLine("Oops! The provided path is invalid or not a directory.");
			Console.ReadKey(true);
			return;
		}

		Console.WriteLine("Going to work on: '{0}'!", args[0]);

		StringBuilder stringBuilder = new StringBuilder();

		string header = "";
		string footer = "";

		string head = "<!DOCTYPE html>\n<html>\n<head>\n<title>Title Text</title>\n<link rel=\"icon\" href=\"../images/favicon.png\" type=\"image/x-icon\"/>\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\n<meta charset=\"UTF-8\">\n<link href=\"../style/style.css\" rel=\"stylesheet\" type=\"text/css\" media=\"all\">\n</head>\n<body>\n";

		bool hasFavicon = true;

		Queue<string> directories = new Queue<string>();

		directories.Enqueue(args[0]);

		List<string> postArchive = new List<string>();
		List<string> postTitles = new List<string>();
		List<string> postDates = new List<string>();

		List<string> pageFilenames = new List<string>();
		List<string> pageData = new List<string>();

		string rss = string.Empty;
		string rssWebsiteLink = string.Empty;

		string disqus = string.Empty;

		while (directories.Count > 0) {
			string directoryPath = directories.Dequeue();

			if (Directory.Exists(directoryPath)) {
				string[] files = Directory.GetFiles(directoryPath);

				// Check for files
				if (files.Length == 0 && directoryPath == args[0]) {
					Console.WriteLine("Oops! There are no files in your source directory!");
					Console.ReadKey(true);
					return;
				}

				// Process site-wide files and data first
				if (directoryPath == args[0]) {
					// Check for index page
					bool indexExists = false;
					for (int i = 0; i < files.Length; i++) {
						string filePath = files[i];
						string fileName = Path.GetFileNameWithoutExtension(filePath);

						if (fileName.Contains('_')) {
							if (fileName.Split('_')[0] == "index") {
								indexExists = true;
								break;
							}
						}
						else {
							if (fileName == "index") {
								indexExists = true;
								break;
							}
						}
					}

					if (!indexExists) {
						Console.WriteLine("Oops! You don't have an index file in your top directory, this is a required file!");
						Console.ReadKey(true);
						return;
					}

					// Check for favicon
					if (!Directory.Exists(directoryPath + "\\images")) {
						Console.WriteLine("WARNING: You don't have an images folder containing a favicon; site will be built without one.");
						hasFavicon = false;
					}
					else if (!File.Exists(directoryPath + "\\images\\favicon.ico") && !File.Exists(directoryPath + "\\images\\favicon.png")) {
						Console.WriteLine("WARNING: There's no favicon in your images folder; site will be built without one.");
						hasFavicon = false;
					}

					// Check if style folder exists with a style.css, it must
					if (!Directory.Exists(directoryPath + "\\style")) {
						Console.WriteLine("Oops! You must have a style folder containing a style.css file.");
						Console.ReadKey(true);
						return;
					}
					if (!File.Exists(directoryPath + "\\style\\style.css")) {
						Console.WriteLine("Oops! You need to have a style.css file in your style folder.");
						Console.ReadKey(true);
						return;
					}

					// Process header and footer
					for (int i = 0; i < files.Length; i++) {
						string filePath = files[i];
						string fileName = Path.GetFileNameWithoutExtension(filePath);

						if (fileName.ToLowerInvariant() != "header" && fileName.ToLowerInvariant() != "footer") {
							continue;
						}

						string fileContent = String.Empty;
						string fileType = Path.GetExtension(files[i]);

						bool isMarkdown = false;

						if (fileType == ".htm" || fileType == ".html" || fileType == ".htmls") { // TODO: Allow RSS and Disqus stuff for .html headers and footers
							HtmlDocument document = new HtmlDocument();
							document.Load(filePath);

							HtmlNode legacyZoneletNode = document.DocumentNode.SelectSingleNode("//*[@id='content']");

							if (legacyZoneletNode != null) { // The regex is to remove all spaces and tabulation for a cleaner file
								fileContent = Regex.Replace(legacyZoneletNode.InnerHtml, @"^([\ \x09]+)", string.Empty, RegexOptions.Multiline);
							}
							else {
								fileContent = Regex.Replace(document.DocumentNode.InnerHtml, @"^([\ \x09]+)", string.Empty, RegexOptions.Multiline) + "\n";
							}
						}
						else if (fileType == ".md" || fileType == ".txt") {
							string markdown = File.ReadAllText(filePath);

							// Generate links
							foreach (Match match in Regex.Matches(markdown, @"(\(\.\/|\(\.\.\/)([^(.)]*)\)", RegexOptions.Multiline)) {
								markdown = markdown.Replace("/" + match.Groups[2].Value + ")", "/" + match.Groups[2].Value + (match.Groups[2].Value.Contains(".html") ? string.Empty : ".html") + ")");
							}

							fileContent = Markdown.ToHtml(markdown);
							isMarkdown = true;
						}

						if (fileContent.Trim() == String.Empty) {
							Console.WriteLine("Oops! '{0}' is empty, this is a required file.", filePath);
							Console.ReadKey(true);
							return;
						}

						HtmlDocument headerFooterDocument = new HtmlDocument();
						headerFooterDocument.LoadHtml(fileContent);

						// Grab data for RSS if it exists
						HtmlNode rssTitle = headerFooterDocument.DocumentNode.SelectSingleNode("//rss-title");
						HtmlNode rssDescription = headerFooterDocument.DocumentNode.SelectSingleNode("//rss-description");
						HtmlNode rssLink = headerFooterDocument.DocumentNode.SelectSingleNode("//rss-link");
						if (rssTitle != null && rssDescription != null && rssLink != null) {
							stringBuilder.Clear();
							stringBuilder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n<rss version=\"2.0\" xmlns:atom=\"http://www.w3.org/2005/Atom\">\n<channel>");
							stringBuilder.Append("\t<title>" + rssTitle.InnerText + "</title>\n");
							stringBuilder.Append("\t<description>" + rssDescription.InnerText + "</description>\n");
							stringBuilder.Append("\t<link>" + rssLink.InnerText + "</link>\n");
							stringBuilder.Append("\t<atom:link href=\"" + rssLink.InnerText + "\" rel=\"self\"></atom:link>\n");
							rssWebsiteLink = rssLink.InnerText;
							rssTitle.Remove();
							rssDescription.Remove();
							rssLink.Remove();
							HtmlNode rssLanguage = headerFooterDocument.DocumentNode.SelectSingleNode("//rss-language");
							if (rssLanguage != null) {
								stringBuilder.Append("\t<language>" + rssLanguage.InnerText + "</language>\n");
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
							}
							else {
								stringBuilder.Append("\t<ttl>1440</ttl>\n\n");
							}

							rss = stringBuilder.ToString();
						}
						else if (fileName.ToLowerInvariant() == "header") {
							Console.WriteLine("WARNING: RSS will not be built for your site, refer to the example site's header for how to implement RSS.");
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
							headerFooterDocument.DocumentNode.SelectSingleNode("//zone").Remove();
						}

						// Build header and footer
						if (fileName.ToLowerInvariant() == "header") {
							stringBuilder.Clear();
							stringBuilder.Append("<header id=\"header\">\n");
							stringBuilder.Append(headerFooterDocument.DocumentNode.OuterHtml);
							stringBuilder.Append("</header>\n");
							header = stringBuilder.ToString();
						}
						else if (fileName.ToLowerInvariant() == "footer") {
							stringBuilder.Clear();
							stringBuilder.Append("<footer id=\"footer\">\n");
							stringBuilder.Append(headerFooterDocument.DocumentNode.OuterHtml);
							stringBuilder.Append("</footer>\n");
							footer = stringBuilder.ToString();
						}

						Console.WriteLine("Processed file: '{0}'.", filePath);
					}

					// If a header or footer wasn't found
					if (header.Trim() == String.Empty) {
						Console.WriteLine("Oops! There's no header file in your top directory, this is a required file!");
						Console.ReadKey(true);
						return;
					}

					if (footer.Trim() == String.Empty) {
						Console.WriteLine("Oops! There's no footer file in your top directory, this is a required file!");
						Console.ReadKey(true);
						return;
					}

					// Build post list if this site has posts
					if (Directory.Exists(directoryPath + "\\posts")) {
						string[] posts = Directory.GetFiles(directoryPath + "\\posts");

						for (int i = posts.Length - 1; i >= 0; i--) {
							if (Directory.Exists(posts[i])) {
								continue;
							}

							string postType = Path.GetExtension(posts[i]);
							if (postType == ".md" || postType == ".txt" || postType == ".html" || postType == ".htm" || postType == ".htmls") {
								string postName = Path.GetFileNameWithoutExtension(posts[i]);

								// Generate date and .html link name
								DateTime dateTime;
								string postDate = string.Empty;

								if (postName.Length > 10 && DateTime.TryParseExact(postName.Substring(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime)) {
									// Generate title
									string postTitle = string.Empty;
									string postNameNoDate = postName.Substring(11, postName.Length - 11);

									if (postNameNoDate.Trim() == string.Empty) { // Check if empty first
										continue;
									}

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

									// Parse and add .html link name and date
									if (postName.Contains('_')) {
										postArchive.Add(postName.Split('_')[0] + ".html");
									}
									else {
										postArchive.Add(postName + ".html");
									}

									postDates.Add(dateTime.ToString("yyyy-MM-dd"));
								}
							}
						}
					}
				}

				// Skip images and style folders
				if (directoryPath == args[0] + "\\images" || directoryPath == args[0] + "\\style") {
					continue;
				}

				// Final process of all the pages and posts
				for (int i = 0; i < files.Length; i++) {
					string filePath = files[i];
					string fileName = Path.GetFileNameWithoutExtension(filePath);

					// Ignore header and footer
					if (fileName.ToLowerInvariant() == "header" || fileName.ToLowerInvariant() == "footer") {
						continue;
					}

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
						Console.WriteLine("WARNING: '{0}' filename has no title, post will not be built!", filePath);
						continue;
					}

					// Date
					DateTime dateTime = new DateTime();
					string postDate = string.Empty;

					if (fileName.Length > 10 && DateTime.TryParseExact(fileName.Substring(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime)) {
						postDate = dateTime.ToString("d MMMM yyyy");
					}
					else if (directoryPath != args[0]) {
						Console.WriteLine("WARNING: '{0}' has no date or an invalid date prepending the filename, post will not be built! Make sure you have leading zeros in the day and month spots if they're single digit. Also make sure your date is formatted year-month-day-, tailing with a dash!", filePath);
						continue;
					}

					// Parse contents
					string fileContent = String.Empty;
					string fileType = Path.GetExtension(files[i]);

					if (fileType == ".htm" || fileType == ".html" || fileType == ".htmls") {
						HtmlDocument document = new HtmlDocument();
						document.Load(filePath);

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
					else if (fileType == ".md" || fileType == ".txt") {
						string markdown = File.ReadAllText(filePath);

						// Generate links
						foreach (Match match in Regex.Matches(markdown, @"(\(\.\/|\(\.\.\/)([^(.)]*)\)", RegexOptions.Multiline)) {
							markdown = markdown.Replace("/" + match.Groups[2].Value + ")", "/" + match.Groups[2].Value + (match.Groups[2].Value.Contains(".html") ? string.Empty : ".html") + ")");
						}

						fileContent = Markdown.ToHtml(markdown);
					}
					else {
						Console.WriteLine("WARNING: '{0}' is either not a supported filetype or an image that should go in your images folder, will not be built.", filePath);
						continue;
					}

					if (fileContent.Trim() == String.Empty) {
						Console.WriteLine("WARNING: '{0}' is empty, will not be built.", filePath);
						continue;
					}

					// Generate head
					HtmlDocument headDocument = new HtmlDocument();
					headDocument.LoadHtml((directoryPath == args[0] ? head.Replace("\"../", "\"./") : head.Replace("\"./", "\"../"))); // Make links relative
					HtmlNode titleNode = headDocument.DocumentNode.SelectSingleNode("//title");
					titleNode.InnerHtml = titleNode.InnerHtml.Replace(titleNode.InnerHtml, title);

					if (!hasFavicon) {
						headDocument.DocumentNode.SelectSingleNode("//link").Remove();
					}

					// Build page TODO: Replace all the link replacements with a single string one at the end instead of individually
					stringBuilder.Clear();
					// HtmlAgilityPack auto-closes tags when loading HTML, so we subtract the body and html tags
					stringBuilder.Append(headDocument.DocumentNode.InnerHtml.Substring(0, headDocument.DocumentNode.InnerHtml.Length - 14));
					stringBuilder.Append("<div id=\"container\">\n");
					stringBuilder.Append((directoryPath == args[0] ? header.Replace("\"../", "\"./") : header.Replace("\"./", "\"../")));
					stringBuilder.Append("<article id=\"content\">\n");
					if (directoryPath != args[0]) { // Header for posts if not None
						if (fileName.Contains('_')) {
							if (fileName.Split('_')[1] != "None") {
								stringBuilder.Append("<h1>" + title + "</h1>\n");
								stringBuilder.Append("<h4>" + postDate + "</h4>\n");
							}
						}
						else {
							stringBuilder.Append("<h1>" + title + "</h1>\n");
							stringBuilder.Append("<h4>" + postDate + "</h4>\n");
						}
					}
					stringBuilder.Append((directoryPath == args[0] ? fileContent.Replace("\"../", "\"./") : fileContent.Replace("\"./", "\"../")));
					if (directoryPath != args[0]) {
						// "Next Post" and "Previous Post" buttons for posts
						stringBuilder.Append("<nav id=\"nextprev\">");
						int postIndex = postTitles.IndexOf(title);
						if (postIndex != 0) {
							string nextFileName = postArchive[postIndex - 1];
							if (nextFileName.Contains('_')) {
								nextFileName = nextFileName.Split('_')[0];
							}
							stringBuilder.Append("<a href=\"./" + nextFileName + "\">« Next Post</a> | ");
						}
						stringBuilder.Append("<a href=\"../index.html\">Home</a>");
						if (postIndex + 1 != postTitles.Count) {
							string previousFileName = postArchive[postIndex + 1];
							if (previousFileName.Contains('_')) {
								previousFileName = previousFileName.Split('_')[0];
							}
							stringBuilder.Append(" | <a href=\"./" + previousFileName + "\">Previous Post »</a>");
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

					HtmlDocument finalDocument = new HtmlDocument();
					finalDocument.LoadHtml(stringBuilder.ToString());

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
							rssNode.ParentNode.Remove();
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
						if (fileName.Contains('_')) {
							stringBuilder.Append("\t\t<link>" + rssWebsiteLink + "posts/" + fileName.Split('_')[0] + ".html" + "</link>\n");
						}
						else {
							stringBuilder.Append("\t\t<link>" + rssWebsiteLink + "posts/" + fileName + ".html" + "</link>\n");
						}
						stringBuilder.Append("\t\t<pubDate>\n\t\t\t" + dateTime.ToString("ddd, dd MMM yyyy HH:mm:ss", CultureInfo.CreateSpecificCulture("en-US")) + " +0000\n\t\t</pubDate>\n");
						stringBuilder.Append("\t</item>\n\n");

						rss += stringBuilder.ToString();
					}

					// Remove tag if rss isn't setup
					if (rss == string.Empty && directoryPath != args[0]) {
						HtmlNode rssNode = finalDocument.DocumentNode.SelectSingleNode("//rss");
						if (rssNode != null) {
							rssNode.ParentNode.Remove();
							Console.WriteLine("WARNING: RSS is not setup in your header, but '{0}' has an <rss> tag!", filePath);
						}
					}

					// Generate archives if they exist in the page
					HtmlNodeCollection archiveNodes = finalDocument.DocumentNode.SelectNodes("//archive");
					if (archiveNodes != null && archiveNodes.Count > 0 && postArchive.Count > 0) {
						foreach (HtmlNode archiveNode in archiveNodes) {
							stringBuilder.Clear();
							stringBuilder.Append("<div id=\"postlistdiv");
							stringBuilder.Append("\">\n<ul>\n");
							int postCount = (archiveNode.GetAttributeValue("count", 0) == 0 ? postArchive.Count : archiveNode.GetAttributeValue("count", 0));
							int j = 0;
							for (; j < postCount; j++) {
								if (j > postArchive.Count - 1) {
									break;
								}
								stringBuilder.Append("<li><a href=\"./posts/" + postArchive[j] + "\">" + postDates[j] + " » " + postTitles[j] + "</a></li>\n");
							}

							if (archiveNode.GetAttributeValue("more", string.Empty) != string.Empty && j < postArchive.Count) {
								stringBuilder.Append("<li class=\"moreposts\"><a href=\"" + archiveNode.GetAttributeValue("more", string.Empty) + ".html\">» more posts</a></li>\n");
							}
							stringBuilder.Append("</ul>\n</div>\n");

							string builtArchive = stringBuilder.ToString();
							builtArchive = (directoryPath == args[0] ? builtArchive.Replace("\"../", "\"./") : builtArchive.Replace("\"./", "\"../"));

							HtmlNode builtArchiveNode = HtmlNode.CreateNode(Markdown.ToHtml(builtArchive));

							archiveNode.ParentNode.ReplaceChild(builtArchiveNode, archiveNode);
						}
					}

					// Store page for writing to build directory TODO: I could probably reuse the postArchive List<> here, and rename postArchive to postLinks or something
					pageFilenames.Add((directoryPath != args[0] ? "posts/" : string.Empty) + (fileName.Contains('_') ? fileName.Split('_')[0] : fileName) + ".html");
					pageData.Add(finalDocument.DocumentNode.OuterHtml);

					Console.WriteLine("Processed file: '{0}'.", filePath);
				}

				string[] subdirectories = Directory.GetDirectories(directoryPath);

				for (int i = 0; i < subdirectories.Length; i++) {
					directories.Enqueue(subdirectories[i]);
				}
			}
			else {
				Console.WriteLine("'{0}' is not a valid directory.", directoryPath);
			}
		}

		// Close RSS
		if (rss != string.Empty) {
			rss += "</channel>\n</rss>";
		}

		// Package zone, we can't fail now!
		string buildDirectory = args[0].Substring(0, args[0].LastIndexOf('\\')) + args[0].Substring(args[0].LastIndexOf('\\'), args[0].Length - args[0].LastIndexOf('\\')) + "-built";

		Directory.CreateDirectory(buildDirectory);

		// Copy images
		if (Directory.Exists((args[0] + "\\images"))) {
			Directory.CreateDirectory(buildDirectory + "\\images");
			FileInfo[] imageFiles = new DirectoryInfo(args[0] + "\\images").GetFiles();
			foreach (FileInfo file in imageFiles) {
				file.CopyTo(Path.Combine(buildDirectory + "\\images", file.Name), true);
			}
		}

		// Copy style
		Directory.CreateDirectory(buildDirectory + "\\style");
		FileInfo[] styleFile = new DirectoryInfo(args[0] + "\\style").GetFiles();
		foreach (FileInfo file in styleFile) {
			file.CopyTo(Path.Combine(buildDirectory + "\\style", file.Name), true);
		}

		// Build pages
		Directory.CreateDirectory(buildDirectory + "\\posts");
		for (int i = 0; i < pageData.Count; i++) {
			File.WriteAllText(Path.Combine(buildDirectory, pageFilenames[i]), pageData[i]);
		}

		// RSS
		File.WriteAllText(Path.Combine(buildDirectory, "feed.xml"), rss);

		Console.WriteLine("SUCCESS: Your zone has been built to: '{0}'.", buildDirectory);
		Console.ReadKey(true);
	}
}
