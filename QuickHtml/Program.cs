using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommonMark;

namespace QuickHtml
{
    public class Program
    {
        public const string layout_name = "layout.html";
        public const string sitemap_name = "sitemap.md";
        public static CommonMarkSettings md_settings;

        public const string log_file = "qh.log";
        public static string log_buffer = "";
        public static StreamWriter log_writer;

        public static void Main(string[] args)
        {
            // Debug
            if (Debugger.IsAttached)
            {
                args = new[] { @"C:\MVC\saint-privat-qh" };
            }

            // Echo
            Trace("QuickHtml {0}", string.Join(" ", args));

            // Run
            Start(args);

            // Log
            try
            {
                if (log_writer != null) log_writer.Close();
            }
            catch { }

            // Pause
            if (Debugger.IsAttached)
            {
                Console.WriteLine();
                Console.Write("<Enter> to quit...");
                Console.ReadLine();
            }
        }

        private static void Start(string[] args)
        {
            // Set time start
            Trace("---");
            Trace("date", DateTime.Now.ToString());

            // Get source folder
            var src_folder = GetSrcFolder(args);
            if (src_folder.StartsWith("!"))
            {
                Trace("ERROR: {0} is not a valid src folder.", src_folder.Substring(1));
                Trace("---");
                return;
            }
            Trace("src", src_folder);

            // Set project folder
            var proj_folder = Directory.GetParent(src_folder).FullName;

            // Get distribution folder
            var dist_folder = GetDistFolder(args, proj_folder);
            if (dist_folder.StartsWith("!"))
            {
                Trace("ERROR: {0} is not a valid dist folder.", dist_folder.Substring(1));
                Trace("---");
                return;
            }
            Trace("dist", dist_folder);
            Trace("---");

            // Remove dist folder
            try
            {
                Trace("rmdir", "/*.*");
                Directory.Delete(dist_folder, true);
                System.Threading.Thread.Sleep(10);
            }
            catch { }

            // Load layout file
            var layout_path = Path.Combine(src_folder, layout_name);
            var layout = File.ReadAllText(layout_path);

            // Define markdown settings
            md_settings = CommonMarkSettings.Default.Clone();
            md_settings.AdditionalFeatures = CommonMarkAdditionalFeatures.StrikethroughTilde;

            // Copy all files
            var files = GetAllFiles(src_folder);
            files = CheckFiles(files);
            var sitemap = false;
            foreach (var file in files)
            {
                // Create dist folder and subfolders
                var src_dir = Path.GetDirectoryName(file);
                var dist_dir = src_dir.Replace(src_folder, dist_folder);
                if (!Directory.Exists(dist_dir))
                {
                    Directory.CreateDirectory(dist_dir);
                    Trace("mkdir", ShortName(src_dir, src_folder));
                }

                // Mark dist folder
                if (file == layout_path)
                {
                    var mark = Path.Combine(dist_folder, log_file);
                    log_writer = File.CreateText(mark);
                    Trace("touch", ShortName(mark, dist_folder));
                    continue;
                }

                // Omit sitemap.md
                if (file.EndsWith(sitemap_name))
                {
                    sitemap = true;
                    continue;
                }

                // Copy file from src to dist
                var destination = file.Replace(src_folder, dist_folder);
                var sub = (src_dir != src_folder);
                var result = CopyFile(file, destination, layout, sub);

                // Trace file copy/build
                Trace(result, ShortName(file, src_folder));
            }

            // Create sitemap
            if (sitemap)
            {
                WriteSitemap(files, src_folder, dist_folder);
                Trace("WRITE", "/" + sitemap_name);
            }
        }

        private static string GetSrcFolder(string[] args)
        {
            // Get source folder
            var src_folder = Directory.GetCurrentDirectory();
            if (args.Length > 0) src_folder = args[0];
            src_folder = Path.GetFullPath(src_folder);

            // Check source folder
            if (File.Exists(Path.Combine(src_folder, layout_name)))
            {
                // Source folder is actually the source folder
            }
            else if (File.Exists(Path.Combine(src_folder, "src", layout_name)))
            {
                // Source folder was actually the project folder
                // => get the src subfolder
                src_folder = Path.Combine(src_folder, "src");
            }
            else
            {
                // Invalid source folder
                src_folder = "!" + src_folder;
            }

            return src_folder;
        }

        private static string GetDistFolder(string[] args, string proj_folder)
        {
            // Get distribution folder
            var dist_folder = Path.Combine(proj_folder, "dist");
            if (args.Length > 1) dist_folder = args[1];
            dist_folder = Path.GetFullPath(dist_folder);

            // Check distribution folder
            if (Directory.Exists(dist_folder))
            {
                if (File.Exists(Path.Combine(dist_folder, log_file)))
                {
                    // Distribution folder is actually the distribution folder
                }
                else if (File.Exists(Path.Combine(dist_folder, "dist", log_file)))
                {
                    // Distribution folder was actually the project folder
                    // => get the distribution subfolder
                    dist_folder = Path.Combine(dist_folder, "dist");
                }
                else
                {
                    // Invalid distribution folder
                    dist_folder = "!" + dist_folder;
                }

            }

            return dist_folder;
        }

        private static List<string> GetAllFiles(string folder)
        {
            var list = new List<string>();

            var files = Directory.GetFiles(folder);
            foreach (var file in files)
            {
                list.Add(file);
            }

            var dirs = Directory.GetDirectories(folder);
            foreach (var dir in dirs)
            {
                list.AddRange(GetAllFiles(dir));
            }

            return list;
        }

        private static List<string> CheckFiles(List<string> files)
        {
            var list = new List<string>();

            foreach (var file in files)
            {
                if (file.EndsWith(".png"))
                {
                    var jpeg = file.Substring(0, file.Length - 4) + ".jpg";
                    if (files.Exists(f => f == jpeg))
                    {
                        list.Add(file + "!!");
                        continue;
                    }
                }
                list.Add(file);
            }

            return list;
        }

        static string CopyFile(string source, string destination, string layout, bool sub)
        {
            if (source.EndsWith("!!")) return "   no";
            var result = "";
            var f = new FileInfo(source);
            switch (f.Extension)
            {
                case ".css":
                case ".html":
                case ".ico":
                case ".jpg":
                case ".js":
                case ".pdf":
                case ".png":
                case ".txt":
                case ".xml":
                    // Copy files with specific extensions
                    File.Copy(source, destination);
                    result = " copy";
                    break;
                case ".md":
                    // Create html files from markdown files
                    WriteHtml(source, destination, layout, sub);
                    result = "WRITE";
                    break;
                default:
                    if (!sub)
                    {
                        // Copy all files from src root
                        File.Copy(source, destination);
                        result = " copy";
                    }
                    else
                    {
                        // All other files are not copied to dest folder
                        result = "ALERT: *** {0} has no valid file extension ***";
                    }
                    break;
            }
            return result;
        }

        private static void WriteHtml(string source, string destination, string layout, bool sub)
        {
            // Load markdown source 
            var md = LoadMarkdown(source);

            // Convert markdown to html
            var content = MarkdownToHtml(md.Body);

            // Temporary hack
            content = content.Replace("<p><img ", "  <img ");
            content = content.Replace(" /></p>", ">");
            content = content.Replace(" alt=\"\"", "");

            // Quick and dirty format
            content = content.Trim();
            content = content.Replace("<h", Environment.NewLine + "<h");
            content = content.Replace("<p", Environment.NewLine + "<p");
            content = content.Replace("<ul", Environment.NewLine + "<ul");
            content = content.Replace("<ol", Environment.NewLine + "<ol");
            content = content.Replace("<li", "  <li");
            content = content.Replace("<div", Environment.NewLine + "<div");
            content = content.Replace(Environment.NewLine + "</div>", Environment.NewLine + Environment.NewLine + "</div>");
            content = content.Trim();
            content = "    " + content.Replace(Environment.NewLine, Environment.NewLine + "    ");

            // Meta substitition
            var html = layout.Replace("{{ content }}", content);
            html = html.Replace("{{ title }}", md.Meta.title);
            html = html.Replace("{{ index }}", md.Meta.index);
            html = html.Replace("{{ id }}", md.Meta.id);
            html = html.Replace("{{ description }}", md.Meta.description);

            // Subfolders path
            if (sub)
            {
                html = html.Replace(" href=\"./css/", " href=\"./../css/");
                html = html.Replace(" src=\"./js/", " src=\"./../js/");
                html = html.Replace(" src=\"./images/", " src=\"./../images/");
            }

            // Create html file
            destination = destination.Substring(0, destination.Length - 2) + "html";
            File.WriteAllText(destination, html);
        }

        private static void WriteSitemap(List<string> files, string src_folder, string dist_folder)
        {
            // Load markdown sitemap
            var md = LoadMarkdown(Path.Combine(src_folder, sitemap_name));

            // Check site url 
            if (!md.Meta.url.EndsWith("/")) md.Meta.url += "/";

            // Build url list
            var template = new Regex(@"\s*<url>(.*?)</url>", RegexOptions.Singleline).Match(md.Body).Groups[0].Value;
            var urls = new List<string>();
            foreach (var file in files.Where(f => f.EndsWith(".md")))
            {
                if (!file.EndsWith(sitemap_name))
                {
                    // Set url location
                    var loc = ShortName(file, src_folder).Substring(1);
                    if (loc == "index.md")
                        loc = md.Meta.url;
                    else
                        loc = md.Meta.url + loc.Substring(0, loc.Length - 2) + "html";
                    // Set url last modification
                    var f = new FileInfo(file);
                    var lastmod = f.LastWriteTimeUtc.ToString("yyyy-MM-dd").ToString();
                    // Set url change frequency
                    var page = LoadMarkdown(file);
                    var changefreq = page.Meta.changefreq ?? md.Meta.changefreq ?? "yearly";
                    // Set url priority
                    var priority = page.Meta.priority ?? md.Meta.priority ?? "1.0";
                    // Add url
                    var url = template.Replace("{{ loc }}", loc)
                                      .Replace("{{ lastmod }}", lastmod)
                                      .Replace("{{ changefreq }}", changefreq)
                                      .Replace("{{ priority }}", priority);
                    urls.Add(url);
                }
            }

            // Set xml content
            var xml = md.Body.Replace(template, string.Join("", urls.OrderBy(u => u)));

            // Create sitemap.xml file
            var destination = Path.Combine(dist_folder, sitemap_name.Replace(".md", ".xml"));
            File.WriteAllText(destination, xml);
        }

        public static QuickMarkdown LoadMarkdown(string file)
        {
            // Get file content
            var lines = File.ReadAllLines(file);

            // Parse file content
            var md = new QuickMarkdown();
            var meta = 0;
            foreach (var line in lines)
            {
                var text = line.Trim();
                switch (meta)
                {
                    case 0:
                        if (text == "---")
                            meta++;
                        break;
                    case 1:
                        if (text == "---")
                            meta++;
                        else
                            md.AddMeta(text);
                        break;
                    default:
                        md.AddLine(line);
                        break;
                }
            }

            // Check meta content
            md.Meta.title = FrenchChars(md.Meta.title);
            md.Meta.index = FrenchChars(md.Meta.index) ?? md.Meta.title;

            return md;
        }

        public static string FrenchChars(string text)
        {
            if (text == null) return text;

            text = text.Replace("'", "’");
            text = text.Replace("...", "…");

            return text;
        }

        public static string FrenchSpaces(string text)
        {
            text = text.Replace(" ?", "&nbsp;?");
            text = text.Replace(" ;", "&nbsp;;");
            text = text.Replace(" :", "&nbsp;:");
            text = text.Replace(" !", "&nbsp;!");
            text = text.Replace(" »", "&nbsp;»");
            text = text.Replace("« ", "«&nbsp;");

            return text;
        }

        public static string FrenchQuotes(string text)
        {
            var open = text.IndexOf("&quot;");
            while (open != -1)
            {
                text = text.Substring(0, open) + "«&nbsp;" + text.Substring(open + 6).Trim();
                var close = text.IndexOf("&quot;", open + 1);
                if (close != -1)
                {
                    text = text.Substring(0, close).Trim() + "&nbsp;»" + text.Substring(close + 6);
                    open = text.IndexOf("&quot;");
                }
                else
                {
                    open = -1;
                }
            }

            return text;
        }

        public static string MarkdownToHtml(string markdown)
        {
            // Beautify markdown content
            markdown = FrenchChars(markdown);
            markdown = FrenchSpaces(markdown);

            // Convert markdown to html
            var html = CommonMarkConverter.Convert(markdown, md_settings);

            // Revert nbsp chars to entities
            html = html.Replace(" ", "&nbsp;");

            // Convert entity quotes to pretty quotes
            html = FrenchQuotes(html);

            return html;
        }

        private static string ShortName(string fullname, string folder)
        {
            var name = fullname.Substring(folder.Length).Replace(@"\", "/");
            if (name.EndsWith("!!")) name = name.Substring(0, name.Length - 2);
            if (name == "") name = "/";

            return name;
        }

        private static void Trace(string action, string detail = "")
        {
            var echo = "";
            if (action.Contains("{"))
            {
                echo = string.Format(action, detail);
            }
            else if (string.IsNullOrEmpty(detail))
            {
                echo = action;
            }
            else
            {
                echo = string.Format("{0}: {1}", action, detail);
            }

            Console.WriteLine(echo);

            log_buffer += echo;
            log_buffer += Environment.NewLine;
            if (log_writer != null)
            {
                log_writer.Write(log_buffer);
                log_buffer = "";
            }
        }
    }

    public class QuickMarkdown
    {
        public dynamic Meta { get; set; }
        public string Body { get; set; }

        public QuickMarkdown()
        {
            this.Meta = new QuickDynamic();
            this.Body = "";
        }

        public void AddLine(string line)
        {
            if (this.Body != "") this.Body += Environment.NewLine;
            this.Body += line;
        }

        public void AddMeta(string text)
        {
            var split = text.IndexOf(": ");
            if (split <= 0) return;

            this.Meta.Add(text.Substring(0, split), text.Substring(split + 2).Trim());
        }
    }

    public class QuickDynamic : DynamicObject
    {
        Dictionary<string, object> list = new Dictionary<string, object>();

        public void Add(string key, object value)
        {
            this.list[key] = value;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (!this.list.ContainsKey(binder.Name))
                this.list[binder.Name] = null;

            return this.list.TryGetValue(binder.Name, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            this.list[binder.Name] = value;

            return true;
        }
    }
}
