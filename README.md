## Zoner

Zoner is a small command line tool for easing the construction of [Zonelets](https://zonelets.net/)-style blogs. It's meant to be as simple and accessible as possible.

Download the binaries on itch: <https://ryantrawick.itch.io/zoner>

### Features

- Simple drag and drop or command line build interface.
- Write your pages in Markdown and let Zoner handle the HTML (or use HTML by itself).
- Automatic RSS/Atom feed generation.
- HTML layout optimized for screen readers.
- Disqus comment support.
- Backwards compatibility with existing Zonelets.

### Build

```
dotnet restore
dotnet publish -c release -r <RUNTIME>
```

Requires .NET 5.0 to build, does not require .NET libraries to run.

### Dependencies

Zoner makes use of [Markdig](https://github.com/xoofx/markdig) for Markdown processing and [HtmlAgilityPack](https://www.nuget.org/packages/HtmlAgilityPack/) for document manipulation.

### Future

I'm intimidated by all the cool projects on here and I love how small the build sizes are. I'd like to port this tool to C so it'll be smaller, but I'm not quite experienced enough yet. I found [Node](https://sr.ht/~tagglink/node/) and I'll probably take a look at that in the future, you might want to give it a try if you don't like the size of Zoner.

I'd also like to add some QoL features like link checking, but I wonder if that'd narrow the use of Zoner? Technically since Zoner doesn't clean the built site directory you can use Zoner to make only part of your website and you can link to other, handmade HTML pages. I guess the link checking could work on the built site directory.

I want to add some command line options for changing how dates are rendered and how the archive list is ordered/structured; possibly splitting them by years and months.

I avoided dotfiles to keep site configurations entirely declarative to the data so maintenance would be smoother. The header does, however, act like a dotfile of sorts, and that feels a little clunky to me. So I might move the site declarations to a dotfile anyways.

Lastly I'd like to create an image optimization/dithering tool. But Zoner is simple enough that you can easily chain it with an existing image optimizer of your choice.

---

If you have any problems or suggestions contact me on Twitter: [@aynik_co](https://twitter.com/aynik_co)