// This controller reads the user cookie to select a random unity madewith project.
// If the cookie does not exist, it will create one and populate it.
// The project url is then scraped and returned in a custom display.

using HtmlAgilityPack;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;


namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            string url;
            
            if (Request.Cookies.Get("Sequence") == null)
            {
                // New user, generate a new cookie
                url = generateCookie();
            }
            else
            {
                // Known user, parse their cookie
                url = parseCookie();
            }
            
            WebClient reader = new WebClient();
            HtmlDocument html = new HtmlDocument();
            html.Load(reader.OpenRead(url), System.Text.Encoding.UTF8);

            HtmlNode main = html.DocumentNode.SelectSingleNode("//*[@id='main']");

            // Remove unwanted divs

            foreach (HtmlNode node in main.SelectNodes("//h1"))
            {
                if (node.GetAttributeValue("class", "").Equals("section-hero-title title-huge gsap-text-1"))
                {
                    ViewBag.game = node.InnerHtml;
                    node.SetAttributeValue("style", "visibility: hidden");
                }
            }

            foreach (HtmlNode node in main.SelectNodes("//div"))
            {
                if (node.GetAttributeValue("class", "").Equals("section-hero-studio"))
                {
                    ViewBag.studio = node.InnerHtml;
                }
                if (node.GetAttributeValue("class", "").Equals("section section-story-hero"))
                {
                    string style = node.GetAttributeValue("style", "");
                    style = style.Substring(0, style.Length - 4).Substring(28);
                    style = "<img src='" + style + "' style:'max-width:100%;'>";
                    ViewBag.bg = style;
                    node.SetAttributeValue("style", "display:none");
                }
            }
            
            // Fix links to display images
            foreach (HtmlNode node in main.SelectNodes("//img[@src]"))
            {
                string src = "https://unity.com" + node.Attributes["src"].Value;

                node.SetAttributeValue("src", src);
            }
            
            string result = main.OuterHtml;

            ViewBag.htmlStr = result; // Store html for display
            
            return View();
        }

        // Generates array of all items in the showcase.
        // Only needs to be run once per user
        //
        // Returns: an array of links to each showcase project
        private string[] generateLinks()
        {
            string url = @"https://unity.com/madewith/";
            var html = new HtmlDocument();
            html.LoadHtml(new WebClient().DownloadString(url));

            var root = html.DocumentNode;
            var nodes = root.Descendants()
                .Where(n => n.GetAttributeValue("href", "").Contains("/madewith/"));

            string[] links = new string[nodes.Count()];
            int index = 0;
            
            foreach (var node in nodes)
            {
                links[index] = "https://unity.com" + node.Attributes["href"].Value;
                index++;
            }
            return links;
        }

        // Generate a new cookie for the user. Cookie contains two key/val pairs.
        // Links stores a serialized array of all project links.
        // LastIdx stores the index of the project displayed the last time the site was visited.
        // The initial index is generated randomly.
        //
        // Returns: link to be loaded
        private string generateCookie()
        {
            string[] links = generateLinks();

            string name1 = "Links";
            string val1 = new JavaScriptSerializer().Serialize(links);

            int rand = new Random().Next(links.Length);
            string name2 = "LastIdx";
            string val2 = rand.ToString();

            HttpCookie cookie = new HttpCookie("Sequence");
            cookie.Values.Add(name1, val1);
            cookie.Values.Add(name2, val2);

            Response.Cookies.Add(cookie);
            return links[rand];
        }

        // Parses an existing cookie
        //
        // Returns: link to be loaded
        private string parseCookie()
        {
            string serLinks = Request.Cookies.Get("Sequence")["Links"];
            string[] links = new JavaScriptSerializer().Deserialize<string[]>(serLinks);
            int index = Convert.ToInt32(Request.Cookies.Get("Sequence")["LastIdx"]);
            index = (index + 1) % links.Length;
            updateCookie(serLinks, index);
            return links[index];
        }

        // Updates LastIdx of an existing cookie to reflect the site being visited
        private void updateCookie(string links, int index)
        {
            string name1 = "Links";
            string val1 = links;

            int rand = new Random().Next(links.Length);
            string name2 = "LastIdx";
            string val2 = index.ToString();

            HttpCookie cookie = new HttpCookie("Sequence");
            cookie.Values.Add(name1, val1);
            cookie.Values.Add(name2, val2);

            Response.Cookies.Add(cookie);
        }

        public ActionResult About()
        {
            return View();
        }
    }
}