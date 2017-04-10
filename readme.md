# QuickHtml

QuickHtml is a basic static site generator.

It takes a source folder and creates a distribution website where markdown files
are converted to html.


## Project structure

```
Project
|-- docs
|-- src
|   |-- css
|   |   |-- normalize.css
|   |   ·-- style.css
|   |-- images
|   |-- js
|   |-- contents
|   |   ·-- flyer.pdf
|   |-- config.yml
|   |-- favicon.ico
|   |-- index.md
|   |-- layout.html
|   |-- page1.md
|   |-- page2.md
|   |-- robots.md
|   ·-- sitemap.md
|-- tools
|   ·-- qh.bat
|-- .gitattributes
|-- .gitgnore
·-- readme.md
```

### docs folder

This is where the final static site will be generated.

It's an exact copy of the `src` folder structure, except that markdown files are
converted to html files.

### src folder

This is where you create all files and folders for your website.

The `src` folder should at least contain 3 folders for your site assets:
* css
* images (optionally with subfolders)
* js

This folder must have a file with the name `layout.html`:
* It's the template for all pages from your website,
* QuickHtml will not create your static site without this file.

Then you have to create one markdown file for each page you want to publish in
your generated site. This markdown files can be nested in subfolders.

You can also have "static" files in the `src` folder (like "flyer.pdf" or
"favicon.ico"). They will be copied when QuickHtml will generate the final site.

### tools folder

This folder contains a batch file to run QuickHtml and create all the `docs`
content from the `src` folder.

### config.yml

This file contains all the site settings.

```
sitetitle: "My Web Site"
lang: "en"
url: "https://www.my-site.com/"
urltitle: "~ www.My-Site.Com ~"
changefreq: "monthly"
priority: 1.0
```

This "variables" are used to configure your website:

* `{{ sitetitle }}` is the general title of your website,
* `{{ lang }}` is the language of your content,
* `{{ url }}` is the URL of your website,
* `{{ urltitle }}` is a title to link to your site,
* `{{ changefreq }}` is used to create `sitemap.xml`,
* `{{ priority }}` is used to create `sitemap.xml`.

`{{ url }}` define the exact address of your website, like
"https://www.my-site.com/", "http://example.org/" or "http://example.org/test/".
This information is mandatory when QuickHtml need to create a sitemap or a
robots.txt file.

`{{ urltitle }}` is a short text generally used in the footer of each page to
link to your site. It can be your name, a pretty title, or whatever you want. By
default, QuickHtml will use a short text from the URL, like "www.my-site.com",
"example.org" or "example.org/test".

### layout.html

This a pure html file which contains the template for all the pages of your
website. Inside this html code, you can use "variables" to personalize the
content of the final page.

You can use the 6 general variables from `config.yml` and 4 more variables
specific to each page:

* `{{ title }}` define the current page title,
* `{{ description }}` is the content for the description meta header,
* `{{ alttitle }}` should be a title for the index page in the current folder,
* `{{ id }}` can be used to identify the page.

All variables will be replaced with actual values when QuickHtml will convert
markdown files.

Variable names are case sensitive.

### markdown files

A markdown file represents a page for you website. Each file starts with a
header where you can define the values for all variables.

```
---
title: "Welcome"
id: "home"
---

## {{ title }}

Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor
incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis
nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.
Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu
fugiat nulla pariatur.

```

You only have to define variables that are actually used in your `layout.html`
template.

* If you don't define a variable but use it in `layout.html`, an empty value
will be used and no error occurs.
* If you don't define the `alttitle` variable but use it, the `title` value will
be used to replace it.

Inspired from [Pandoc](http://pandoc.org/MANUAL.html#superscripts-and-subscripts),
QuickHtml converts "`...^xyz^...`" to "`...<sup>xyz</sup>...`", although it's
not a CommonMark feature.


## Misc

### sitemap.md

When `src` folder contains a file named "sitemap.md", QuickHtml will use it as
a template to generate "sitemap.xml" in the `docs` folder.

A correct template to generate a standards-compliant site map should be:

```
<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
  <url>
    <loc>{{ loc }}</loc>
    <lastmod>{{ lastmod }}</lastmod>
    <changefreq>{{ changefreq }}</changefreq>
    <priority>{{ priority }}</priority>
  </url>
</urlset>
```

The sitemap template uses 3 "variables" defined in `config.yml`:

* `{{ url }}` to define the website URL,
* `{{ changefreq }}` for page modification frequency: daily, weekly, monthly or
  yearly by default
* `{{ priority }}` for page priority: value between 0.0 to 1.0 (1.0 by default)

If `{{ url }}` does not exist in `config.yml`, the sitemap is not created.

Variables `changefreq` and `priority` can also be defined at the page level to
accept values specific to a page.

The 2 variables `{{ loc }}` and `{{ lastmod }}` are automatically set by
QuickHtml.

### robots.md

QuickHtml can generate a "robots.txt" file in the `docs` folder when your `src`
folder contains a file named "robots.md", with the following template:

```
User-agent: *
Sitemap: {{ url }}sitemap.xml
```

This template accepts only the `{{ url }}` variables as your website URL. If
this variable does not exist in `config.yml`, the robots file is not created.

### config.lang

The `{{ lang }}` variable in `config.yml` defines the language of your website.

This setting accepts one single value in the format defined in the [Tags for
Identifying Languages (BCP47)](http://www.ietf.org/rfc/bcp/bcp47.txt) IETF
document. The default value is `en`.

This variable can be used to define the `lang` attribute of the `html` element
in your `layout.html` template:

```
<!DOCTYPE html>
<html lang="{{ lang }}">
  <head>
```

This value is also used to "smartify" your html, unless it contains "none".


## Using QuickHtml

### Generate `docs` website

Run `qh.bat` from tools folder and QuickHtml process all files in the `src`
folder to generate the `docs` content.

* Combine "layout.html" and "markdown" files to create "html" files,
* Build "sitemap.xml" from "sitemap.md" and markdown files list,
* Simply copy all other "static" files,
* Do not copy restricted files.

Static files are copied when:
* they have a valid extension: ".css", ".gif", ".html", ".ico", ".jpg", ".js",
".pdf", ".png", ".txt" or ".xml",
* they are located in the `src` root, whatever extension they have.

All other files are restricted files and are not copied to the `docs` folder.

Note: a "png" file is not copied when there is a "jpg" file with the same name
in the same folder. In this case, QuickHtml considers the jpeg image is the
optimized version of the png image.

### Deploy website

Once the `docs` folder is generated, you have to copy its content to your
host space.


## Credit

* CommonMark.NET (https://github.com/Knagis/CommonMark.NET)
