using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CommonMark;

namespace QuickHtml
{
    public class Program
    {
        public const string config_name = "config.yml";
        public const string layout_name = "layout.html";
        public const string sitemap_name = "sitemap.md";
        public const string robots_name = "robots.md";
        public static CommonMarkSettings md_settings;

        public const string log_file = "qh.log";
        public static QuickLog log;

        public static void Main(string[] args)
        {
            // Debug
            if (Debugger.IsAttached)
            {
                args = new[] { @"\MVC\docteur-francus.eu.org" };
                args = new[] { @"\MVC\saint-privat.eu.org" };
            }

            // Echo
            log = new QuickLog();
            log.Trace("QuickHtml {0}", string.Join(" ", args));

            // Run
            Start(args);
            log.Close();

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
            log.Trace("---");
            log.Trace("date", DateTime.Now.ToString());

            // Get source folder
            var src_folder = GetSrcFolder(args);
            if (src_folder.StartsWith("!"))
            {
                log.Trace("ERROR: {0} is not a valid src folder.", src_folder.Substring(1));
                log.Trace("---");
                return;
            }
            log.Trace("src", src_folder);

            // Set project folder
            var proj_folder = Directory.GetParent(src_folder).FullName;

            // Get docs folder
            var docs_folder = GetDocsFolder(args, proj_folder);
            if (docs_folder.StartsWith("!"))
            {
                log.Trace("ERROR: {0} is not a valid docs folder.", docs_folder.Substring(1));
                log.Trace("---");
                return;
            }
            log.Trace("docs", docs_folder);
            log.Trace("---");

            // Remove docs folder
            try
            {
                log.Trace("rmdir", "/*.*");
                Directory.Delete(docs_folder, true);
                System.Threading.Thread.Sleep(10);
            }
            catch { }
            log.Trace("  dir", "/*.*");

            // Load config file
            var config_path = Path.Combine(src_folder, config_name);
            var config = LoadConfig(config_path);

            // Load layout file
            var layout_path = Path.Combine(src_folder, layout_name);
            var layout = File.ReadAllText(layout_path);

            // Define markdown settings
            md_settings = CommonMarkSettings.Default.Clone();
            md_settings.AdditionalFeatures = CommonMarkAdditionalFeatures.StrikethroughTilde;

            // Full path for sitemap template
            var sitemap_path = Path.Combine(src_folder, sitemap_name);

            // Copy all files
            var files = GetAllFiles(src_folder);
            files = CheckFiles(files);
            var sitemap = false;
            foreach (var file in files)
            {
                // Create docs folder and subfolders
                var src_dir = Path.GetDirectoryName(file);
                var docs_dir = src_dir.Replace(src_folder, docs_folder);
                if (!Directory.Exists(docs_dir))
                {
                    Directory.CreateDirectory(docs_dir);
                    log.Trace("mkdir", ShortName(src_dir, src_folder));
                }

                // Mark docs folder
                if (file == layout_path)
                {
                    var mark = Path.Combine(docs_folder, log_file);
                    log.Open(mark);
                    log.Trace("touch", ShortName(mark, docs_folder));
                    continue;
                }

                // Omit config.yml
                if (file == config_path)
                {
                    continue;
                }

                // Omit sitemap.md
                if (file == sitemap_path)
                {
                    sitemap = true;
                    continue;
                }

                // Copy file from src to docs
                var destination = file.Replace(src_folder, docs_folder);
                var sub = (src_dir != src_folder);
                var result = CopyFile(config, file, destination, layout, sub);

                // Trace file copy/build
                log.Trace(result, ShortName(file, src_folder));
            }

            // Create sitemap
            if (sitemap)
            {
                var result = WriteSitemap(config, src_folder, docs_folder, files);
                log.Trace(result, "/" + sitemap_name);
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

        private static string GetDocsFolder(string[] args, string proj_folder)
        {
            // Get docs distribution folder
            var docs_folder = Path.Combine(proj_folder, "docs");
            if (args.Length > 1) docs_folder = args[1];
            docs_folder = Path.GetFullPath(docs_folder);

            // Check docs folder
            if (Directory.Exists(docs_folder))
            {
                if (File.Exists(Path.Combine(docs_folder, log_file)))
                {
                    // Docs folder is actually the docs folder
                }
                else if (File.Exists(Path.Combine(docs_folder, "docs", log_file)))
                {
                    // Docs folder was actually the project folder
                    // => get the docs subfolder
                    docs_folder = Path.Combine(docs_folder, "docs");
                }
                else
                {
                    // Invalid docs folder
                    docs_folder = "!" + docs_folder;
                }

            }

            return docs_folder;
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

        static string CopyFile(dynamic config, string source, string destination, string layout, bool sub)
        {
            if (source.EndsWith("!!")) return "   no";
            var result = "";
            var f = new FileInfo(source);
            switch (f.Extension)
            {
                case ".css":
                case ".gif":
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
                    if ((sub == false) && (source.EndsWith(robots_name)))
                        result = WriteRobots(config, source, destination);
                    else
                        result = WriteHtml(config, source, destination, layout, sub);
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
                        result = "ALERT: *** {0} with unexpected file extension ***";
                    }
                    break;
            }
            return result;
        }

        private static string WriteHtml(dynamic config, string source, string destination, string layout, bool sub)
        {
            // Load markdown source
            var md = LoadMarkdown(source, config);

            // Convert markdown to html
            var content = MarkdownToHtml(md.Body, config.lang);
            var html = layout.Replace("{{ content }}", content);

            // Variables substitition
            // (3 times in case one variable use another variable)
            for (var i = 0; i < 3; i++)
            {
                html = html.Replace("{{ description }}", md.Meta.description);
                html = html.Replace("{{ id }}", md.Meta.id);
                html = html.Replace("{{ indextitle }}", md.Meta.indextitle);
                html = html.Replace("{{ lang }}", config.lang);
                html = html.Replace("{{ maintitle }}", config.maintitle);
                html = html.Replace("{{ title }}", md.Meta.title);
                html = html.Replace("{{ url }}", config.url);
                html = html.Replace("{{ urltitle }}", config.urltitle);
                if (!html.Contains("{{")) break;
            }

            // Subfolders path
            if (sub)
            {
                html = html.Replace(" href=\"./css/", " href=\"./../css/");
                html = html.Replace(" src=\"./js/", " src=\"./../js/");
                html = html.Replace(" src=\"./images/", " src=\"./../images/");
            }

            // Remove empty content
            html = Regex.Replace(html, "\\s*<meta name=\"description\" content=\"\">", "", RegexOptions.Multiline);
            html = Regex.Replace(html, "\\s*<meta name=\"keywords\" content=\"\">", "", RegexOptions.Multiline);
            html = html.Replace(" id=\"\"", "");

            // Create html file
            destination = destination.Substring(0, destination.Length - 2) + "html";
            File.WriteAllText(destination, html);

            // Check if one variable is not replaced
            if (html.Contains("{{"))
                return "ALERT: *** {0} with unexpected variable name ***";
            return "write";
        }

        private static string WriteRobots(dynamic config, string robots_path, string destination)
        {
            // Load robots.md template
            var md = LoadMarkdown(robots_path, config);

            // Check site url
            if (string.IsNullOrEmpty(config.url)) return "ALERT: *** {0} with no config.url setting ***";

            //
            var content = md.Body.Replace("{{ url }}", config.url);

            // Create robots.txt file
            destination = destination.Substring(0, destination.Length - 2) + "txt";
            File.WriteAllText(destination, content);

            return "write";
        }

        private static string WriteSitemap(dynamic config, string src_folder, string docs_folder, List<string> files)
        {
            // Load sitemap.md template
            var sitemap_path = Path.Combine(src_folder, sitemap_name);
            var md = LoadMarkdown(sitemap_path, config);

            // Check site url
            if (string.IsNullOrEmpty(config.url)) return "ALERT: *** {0} with no config.url setting ***";

            // Full path for robots template
            var robots_path = Path.Combine(src_folder, robots_name);

            // Build url list
            var template = new Regex(@"\s*<url>(.*?)</url>", RegexOptions.Singleline).Match(md.Body).Groups[0].Value;
            var urls = new List<string>();
            foreach (var file in files.Where(f => f.EndsWith(".md")))
            {
                // Skip specific templates
                if (file == sitemap_path) continue;
                if (file == robots_path) continue;

                // Set url location
                var loc = ShortName(file, src_folder).Substring(1);
                loc = config.url + loc.Substring(0, loc.Length - 2) + "html";
                loc = loc.Replace("/index.html", "/");
                // Set url last modification
                var f = new FileInfo(file);
                var lastmod = f.LastWriteTimeUtc.ToString("yyyy-MM-dd").ToString();
                // Get change frequency and priority
                var page = LoadMarkdown(file, config);
                // Add url
                var url = template.Replace("{{ loc }}", loc)
                                    .Replace("{{ lastmod }}", lastmod)
                                    .Replace("{{ changefreq }}", page.Meta.changefreq)
                                    .Replace("{{ priority }}", page.Meta.priority);
                urls.Add(url);
            }

            // Set xml content
            var xml = md.Body.Replace(template, string.Join("", urls.OrderBy(u => u)));

            // Create sitemap.xml file
            var destination = Path.Combine(docs_folder, sitemap_name.Replace(".md", ".xml"));
            File.WriteAllText(destination, xml);

            return "write";
        }

        public static dynamic LoadConfig(string file)
        {
            // Load config variables
            dynamic config = new QuickDynamic();
            if (File.Exists(file))
            {
                // Get file content
                var lines = File.ReadAllLines(file);

                // Parse file content
                foreach (var line in lines)
                {
                    var text = line.Trim();
                    var split = text.IndexOf(": ");
                    if (split != -1)
                    {
                        config.Add(text.Substring(0, split), text.Substring(split + 2).Trim());
                    }
                }
            }

            // Set default values
            config.lang = config.lang ?? "en";
            config.changefreq = config.changefreq ?? "yearly";
            config.priority = config.priority ?? "1.0";

            // Check config variables
            config.maintitle = SmartVariable(config.maintitle, config.lang);
            if (!string.IsNullOrEmpty(config.url))
            {
                var uri = new Uri(config.url);
                config.url = uri.AbsoluteUri;
                if (!config.url.EndsWith("/")) config.url += "/";
                if (string.IsNullOrEmpty(config.urltitle))
                {
                    config.urltitle = config.url.Replace("http://", "").Replace("https://", "");
                    config.urltitle = config.urltitle.Substring(0, config.urltitle.Length - 1);
                }
            }
            config.urltitle = SmartVariable(config.urltitle, config.lang);

            return config;
        }

        public static QuickMarkdown LoadMarkdown(string file, dynamic config)
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
                        {
                            meta++;
                        }
                        else if (text != "")
                        {
                            meta = 2;
                            md.Meta.title = Path.GetFileNameWithoutExtension(file);
                            md.AddLine(line);
                        }
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

            // Check page variables
            md.Meta.title = SmartVariable(md.Meta.title, config.lang);
            md.Meta.indextitle = SmartVariable(md.Meta.indextitle, config.lang) ?? md.Meta.title;
            md.Meta.description = SmartVariable(md.Meta.description, config.lang);
            md.Meta.changefreq = md.Meta.changefreq ?? config.changefreq;
            md.Meta.priority = md.Meta.priority ?? config.priority;

            return md;
        }

        public static string SmartVariable(string text, string lang)
        {
            if (text == null) return text;

            text = text.Replace("\"", "&quot;");
            text = SmartMarkdown(text, lang);
            text = text.Replace("&nbsp;", " ");

            return text;
        }

        public static string SmartChars(string text, string lang)
        {
            // Replace some characters with better equivalent
            if (text == null) return text;

            text = text.Replace("'", "’");
            text = text.Replace("...", "…");
            text = Regex.Replace(text, @"([\s,]*)(--)([\s,])", "$1–$3", RegexOptions.Multiline);
            if (lang.StartsWith("fr"))
                text = text.Replace("oe", "œ");

            return text;
        }

        public static string SmartSpaces(string text, string lang)
        {
            // Replace space with nbsp entity before double punctuation
            // (space has to be present in markdown source text)
            if (!lang.StartsWith("fr")) return text;

            text = text.Replace(" ?", "&nbsp;?");
            text = text.Replace(" ;", "&nbsp;;");
            text = text.Replace(" :", "&nbsp;:");
            text = text.Replace(" !", "&nbsp;!");
            text = text.Replace(" »", "&nbsp;»");
            text = text.Replace("« ", "«&nbsp;");

            return text;
        }

        public static string SmartQuotes(string text, string lang, string previous)
        {
            // Replace &quot; with opening and closing quotes
            // (CommonMark has replaced double quote with &quot; outside tags)

            var opening = "“";
            var closing = "”";
            if (lang.StartsWith("fr"))
            {
                opening = "« ";
                closing = " »";
            }
            else if (!lang.StartsWith("en"))
            {
                return text;
            }

            // If previous text ends with an opening quote,
            // we must first add the closing quote
            if (previous.LastIndexOf(opening.Trim()) > previous.LastIndexOf(closing.Trim()))
            {
                var close = text.IndexOf("&quot;");
                if (close != -1)
                    text = text.Substring(0, close).TrimEnd() + closing + text.Substring(close + 6);
            }

            var open = text.IndexOf("&quot;");
            while (open != -1)
            {
                text = text.Substring(0, open) + opening + text.Substring(open + 6).TrimStart();
                var close = text.IndexOf("&quot;", open + 1);
                if (close != -1)
                {
                    text = text.Substring(0, close).TrimEnd() + closing + text.Substring(close + 6);
                    open = text.IndexOf("&quot;");
                }
                else
                {
                    open = -1;
                }
            }

            return text;
        }

        public static string MarkdownToHtml(string markdown, string lang)
        {
            // Convert markdown to html
            var html = CommonMarkConverter.Convert(markdown, md_settings).Trim();

            // Revert nbsp chars to entities
            // (CommonMark has replaced nbsp entities with nbsp chars)
            html = html.Replace(" ", "&nbsp;");

            // Beautify generated html
            html = SmartMarkdown(html, lang);

            return html;
        }

        private static string SmartMarkdown(string html, string lang)
        {
            // Beautify html outside <code>...</code>, <pre>...</pre>, <script>...</script>, <x...> and </x>
            var after = new StringBuilder();
            html += "<";
            var index = StartOfTag(html);
            while (index != -1)
            {
                // Replace special chars when they are outside tag
                var temp = html.Substring(0, index);
                temp = SmartChars(temp, lang);
                if (temp.Contains("&quot;"))
                    temp = SmartQuotes(temp, lang, after.ToString());
                temp = SmartSpaces(temp, lang);
                after.Append(temp);

                // Check end of tag block
                html = html.Substring(index);
                index = EndOfTag(html);
                if (index != -1)
                {
                    // Get tag block as it
                    after.Append(html.Substring(0, index));
                    html = html.Substring(index);
                    // Check for next tag block
                    index = StartOfTag(html);
                }
            }

            html = after.ToString().Trim().Replace(">– ", ">–&ensp;");
            return html;
        }

        private static int StartOfTag(string html)
        {
            for (var i = 0; i < html.Length; i++)
            {
                if (html[i] == '<')
                {
                    if (i == html.Length - 1)
                        return i;
                    if (html.Substring(i).StartsWith("<code"))
                        return i;
                    if (html.Substring(i).StartsWith("<pre"))
                        return i;
                    if (html.Substring(i).StartsWith("<script"))
                        return i;
                    if (html[i + 1] == '/')
                        return i;
                    if (char.IsLetter(html[i + 1]))
                        return i;
                }
            }

            return -1;
        }

        private static int EndOfTag(string html)
        {
            var tags = new[] { "code", "pre", "script", "" };
            foreach (var tag in tags)
            {
                if (html.StartsWith("<" + tag))
                {
                    var end = tag == "" ? ">" : "</" + tag + ">";
                    var index = html.IndexOf(end);
                    if (index != -1)
                        index += end.Length;
                    return index;
                }
            }

            return -1;
        }

        private static string ShortName(string fullname, string folder)
        {
            var name = fullname.Substring(folder.Length).Replace(@"\", "/");
            if (name.EndsWith("!!")) name = name.Substring(0, name.Length - 2);
            if (name == "") name = "/";

            return name;
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

    public class QuickLog
    {
        private StreamWriter log_writer;
        private string log_buffer = "";
        private DateTime start;
        private int errors = 0;
        private int alerts = 0;

        public QuickLog()
        {
            start = DateTime.Now;
        }

        public void Open(string log_file)
        {
            log_writer = File.CreateText(log_file);
        }

        public void Trace(string action, string detail = "")
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

            if (echo.StartsWith("ERROR")) errors++;
            if (echo.StartsWith("ALERT")) alerts++;

            Console.WriteLine(echo);

            log_buffer += echo;
            log_buffer += Environment.NewLine;
            if (log_writer != null)
            {
                log_writer.Write(log_buffer);
                log_buffer = "";
            }
        }

        public void Close()
        {
            var duration = DateTime.Now.Subtract(start).TotalSeconds;
            var message = string.Format("Site built in {0:N2} seconds ({1} alerts and {2} errors)", duration, alerts, errors);
            message = message.Replace("1 alerts", "1 alert")
                             .Replace("1 errors", "1 error")
                             .Replace("0 alerts", "")
                             .Replace("0 errors", "")
                             .Replace("( and ", "(")
                             .Replace(" and )", ")")
                             .Replace("()", "");
            Trace("---");
            Trace(message);
            try
            {
                if (log_writer != null) log_writer.Close();
            }
            catch { }
        }
    }
}
