using System;
using System.Collections.Generic;

using Deepdwn;

namespace Zoner {
	class PostItem {
		public string Archive { get; set; }
		public string Title { get; set; }
		public string Date { get; set; }
		public FrontMatter Frontmatter { get; set; }

		public PostItem(string archive, string title, string date) {
			this.Archive = archive;
			this.Title = title;
			this.Date = date;
		}

		public static int FindIndexOfPostWithTitle(List<PostItem> posts, string title) {
			for (int i = 0; i < posts.Count; i++) {
				// Console.WriteLine($"[PostItem] Looking for '{title}', found: '{posts[i].Title}'");
				if (posts[i].Title == title) {
					// Console.WriteLine("[PostItem] Index of post: " + i);
					return i;
				}
			}
			return -1;
		}

		public static PostItem FindPostWithArchive(List<PostItem> posts, string archive) {
			foreach (PostItem post in posts) {
				// Console.WriteLine($"[PostItem] Looking for '{archive}', found: '{post.Archive}'");
				if (post.Archive == archive) return post;
			}
			return null;
		}
	}
}