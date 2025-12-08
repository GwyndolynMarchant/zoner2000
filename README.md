# Zoner2000

Zoner2000 is a small command line tool for easing the construction of [Zonelets](https://zonelets.net/)-style blogs. It's meant to be as simple and accessible as possible. It is a fork of Zoner by [Ryan Trawick](https://twitter.com/aynik_co).

Check out the original Zoner example site: <https://zone-builder.neocities.org/>

## Features

- Simple drag and drop or command line build interface.
- Write your pages in Markdown and let Zoner2000 handle the HTML (or use HTML by itself).
- Automatic RSS/Atom feed generation.
- HTML layout optimized for screen readers.
- [Disqus](https://disqus.com/) comment support - (Allegedly, I haven't touched this since I forked from Ryan's version)
- Backwards compatibility with existing [Zonelets](https://zonelets.net/legacy).
- Support for YAML Frontmatter as used in [Deepdwn](https://support.deepdwn.com/docs/guide/getting-started/#organizing-your-files)
	- Title
	- Tags
- [Open Graph Protocol](https://ogp.me/) support with automatic generation of:
	- Title
	- Tags
- Basic [XFN](https://gmpg.org/xfn/) support
- Building [Sass](https://sass-lang.com/) stylesheets to CSS
- Support for automated [RTA Labelling](https://www.rtalabel.org/?content=howto#individual) via [post tagging](#rta-tagging)

### RTA Tagging
To tag a post as "Restricted to Adults" place a tag in its [frontmatter](https://support.deepdwn.com/docs/guide/getting-started/#organizing-your-files) which begins with [the No One Under Eighteen emoji](https://emojipedia.org/no-one-under-eighteen): "🔞". Simply tagging with the emoji and nothing else is sufficient, but any tag will work as long as the first character in it is the emoji. For example, "🔞", "🔞 R18", "🔞 Adult", will all work, but "Pornographic 🔞" would not. This is part of a general convention I decided upon for my own organization system that emoji categorizers for tags must always begin the tag string. This also makes it rudimentary to work around if you do not wish to enable RTA tagging.

The use of the RTA scheme specifically is not any sort of particularly principled decision-making, but simply one of ease as it seems a relatively commonly-accepted method of providing content restrictions.

## Build

```
dotnet restore
dotnet publish -c release --self-contained true
```

Requires .NET 7.0 to build, does not require .NET libraries to run.

## Dependencies

Zoner2000 makes use of:

- [Markdig](https://github.com/xoofx/markdig) for Markdown processing
- [HtmlAgilityPack](https://www.nuget.org/packages/HtmlAgilityPack/) for document manipulation.
- [SharpSCSS](https://github.com/xoofx/SharpScss) for Sass conversion