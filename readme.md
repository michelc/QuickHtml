# QuickHtml

QuickHtml is a basic static site generator.

It takes a source folder and creates a distribution website where markdown files
are converted to html.


## Project structure

```
Project
|-- dist
|-- src
|   |-- css
|   |   |-- normalize.css
|   |   ·-- style.css
|   |-- images
|   |-- js
|   |-- contents
|   |   ·-- flyer.pdf
|   |-- favicon.ico
|   |-- index.md
|   |-- layout.html
|   |-- page1.md
|   |-- page2.md
|   |-- robots.txt
|   ·-- sitemap.md
|-- tools
|   ·-- qh.bat
|-- .gitattributes
|-- .gitgnore
·-- readme.md
```

### dist folder

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
* QuickHtml will not create yout static site without this file.

Then you have to create one markdown file for each page you want to publish in
your generated site. This markdown files can be nested in subfolders.

You can also have "static" files in the `src` folder (like "flyer.pdf",
"favicon.ico", "robots.txt" and "sitemap.xml"). They will be copied when
QuickHtml will generate the final site.

### tools folder

This folder contains a batch file to run QuickHtml and create all the `dist`
content from the `src` folder.

### layout.html

This a pure html file which contains the template for all the pages of your
website. Inside this html code, you can use 4 "variables" data to personalize
the content of the final page.

* `{{ title }}` define the current page title,
* `{{ description }}` is the content for the description meta header,
* `{{ index }}` should be a title for the index page in the current folder,
* `{{ id }}` can be used to identify the page.

This variables will be replaced with actual values when QuickHtml will convert
markdown files.

Variable names are case sensitive.

### markdown files

A markdown file represents a page for you website. Each file starts with a
header where you can define the values for all variables.

```
---
title: Welcome
id: index
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
will be used an no error occurs.
* If you don't define the `index` variable but use it, the `title` value
will be used to replace it.


## Misc

### sitemap.md

When `src` folder contains a file named "sitemap.md", QuickHtml will use it as
a template to generate "sitemap.xml" in the `dist` folder.

A correct template to generate a standards-compliant site map shoul be:

```
---
url: https://www.my-site.com/
changefreq: monthly
priority: 1.0
---
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

The sitemap template uses 3 specific "variables":

* `{{ url }}` to define the website domain,
* `{{ changefreq }}` for page modification frequency (daily, weekly or monthly)
* `{{ priority }}` for page priority (a value between 0.0 to 1.0)


## Using QuickHtml

### Generate `dist` website

Run `qh.bat` from tools folder and QuickHtml process all files in the `src`
folder to generate the `dist` content.

* Combine "layout.html" and "markdown" files to create "html" files,
* Build "sitemap.xml" from "sitemap.md" and markdown files list,
* Simply copy all other "static" files,
* Do not copy restricted files.

Static files are copied when:
* they have a valid extension: ".css", ".html", ".ico", ".jpg", ".js", ".pdf",
".png", ".txt" or ".xml",
* they are located in the `src` root, whatever extension they have.

All other files are restricted files and are not copied to the `dist` folder.

Note: a "png" file is not copied when there is a "jpg" file with the same name
in the same folder. In this case, QuickHtml considers the jpeg image is the
optimized version of the png image.

### Deploy website

Once the `dist` folder is generated, you have to copy its content to your
host space.
