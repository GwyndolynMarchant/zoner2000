# Zoner2000

Zoner2000 is a small command line tool for easing the construction of [Zonelets](https://zonelets.net/)-style blogs. It's meant to be as simple and accessible as possible. It is a fork of Zoner by [Ryan Trawick](https://twitter.com/aynik_co).

Check out the original Zoner example site: <https://zone-builder.neocities.org/>

## Features

- Simple drag and drop or command line build interface.
- Write your pages in Markdown and let Zoner handle the HTML (or use HTML by itself).
- Automatic RSS/Atom feed generation.
- HTML layout optimized for screen readers.
- Disqus comment support.
- Backwards compatibility with existing Zonelets.
- Support for YAML Frontmatter as used in [Deepdwn](https://support.deepdwn.com/docs/guide/getting-started/#organizing-your-files)
	- Title
	- Tags
- [Open Graph Protocol](https://ogp.me/) support with automatic generation of:
	- Title
	- Tags
- Basic [XFN](https://gmpg.org/xfn/) support

## Build

```
dotnet restore
dotnet publish -c release -r <RUNTIME>
```

Requires .NET 7.0 to build, does not require .NET libraries to run.

## Dependencies

Zoner2000 makes use of [Markdig](https://github.com/xoofx/markdig) for Markdown processing and [HtmlAgilityPack](https://www.nuget.org/packages/HtmlAgilityPack/) for document manipulation.