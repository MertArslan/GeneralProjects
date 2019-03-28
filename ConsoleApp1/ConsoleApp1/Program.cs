
using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

using PuppeteerSharp;
using PuppeteerSharp.Input;
using Newtonsoft.Json.Linq;

namespace ConsoleApp1
{
    class Program
    {
        //Also try "Sahai," a word that appears on the 2nd page.
        static string query = "CEO";
        static string target = "";

        static HashSet<String> googleTerms = new HashSet<string>();

        //Set up set
        static void Setmaker()
        {
            //Set up googleTerms set
            googleTerms.Add("Cached");
            googleTerms.Add("CachedSimilar");
            googleTerms.Add("");
            googleTerms.Add("Account");
            googleTerms.Add("Search");
            googleTerms.Add("Maps");
            googleTerms.Add("YouTube");
            googleTerms.Add("Play");
            googleTerms.Add("News");
            googleTerms.Add("Gmail");
            googleTerms.Add("Contacts");
            googleTerms.Add("Drive");
            googleTerms.Add("Calendar");
            googleTerms.Add("Google+");
            googleTerms.Add("Translate");
            googleTerms.Add("Photos");
            googleTerms.Add("Shopping");
            googleTerms.Add("Finance");
            googleTerms.Add("Docs");
            googleTerms.Add("Books");
            googleTerms.Add("Blogger");
            googleTerms.Add("Hangouts");
            googleTerms.Add("Keep");
            googleTerms.Add("Jamboard");
            googleTerms.Add("Earth");
            googleTerms.Add("Collections");
            googleTerms.Add("Languages");
            googleTerms.Add("Custom range...");
            googleTerms.Add(" ");
            googleTerms.Add("Similar");
        }

        //Find substrings (for url selection)
        static bool IsSubstring(string s1, string s2)
        {
            int m = s1.Length;
            int n = s2.Length;

            //A loop to slide pat[] one by one
            for (int i = 0; i <= n - m; i++)
            {
                int j;
                //For current index i, check for pattern match
                for (j = 0; j < m; j++)
                {
                    if (s2[i + j] != s1[j])
                    {
                        break;
                    }
                }
                if (j == m)
                {
                    return true;
                }
            }

            return false;
        }

        //
        public static async Task pageSearcher(Page page, string resultsSelector) {

            //Find all descriptions
            var descriptions = await page.EvaluateFunctionAsync(@"(resultsSelector) => {
                const anchors = Array.from(document.querySelectorAll('span'));
                return anchors.map(anchor => {
                    const body = anchor.textContent;
                    return `${body}`;
                });
            }", resultsSelector);

            //Find all links
            var jsSelectAllAnchors = @"Array.from(document.querySelectorAll('a')).map(a => a.href);";
            var links = await page.EvaluateExpressionAsync<string[]>(jsSelectAllAnchors);

            List<JToken> results = new List<JToken>();
            List<JToken> urls = new List<JToken>();

            //Rearrange descriptions into easier to use format
            int start = 1;
            foreach (var desc in descriptions)
            {
                if (desc.ToString().Contains("More images"))
                {
                    start = 0;
                } 
                else if (start == 0 && IsSubstring("Cached", desc.ToString())) {
                    start = 1;
                } 
                else if (IsSubstring("Page Navigation", desc.ToString()))
                {
                    start = 0;
                }
                if (!googleTerms.Contains(desc.ToString()) && start == 1)
                {
                    if (desc.ToString().Contains("-"))
                    {
                        string[] halves = desc.ToString().Split('-');
                        if (halves[1].Equals("") || halves[1].Equals(" ")) {
                            Console.WriteLine("halves: " + desc.ToString());
                            continue;
                        }
                    }
                    results.Add(desc);
                }
            }

            results.RemoveAt(0);
            results.RemoveAt(0);

            //Rearrange descriptions into easier to use format
            foreach (var link in links)
            {
                if (!link.ToString().Equals("") && !IsSubstring("google.com", link.ToString()) && !IsSubstring("webcache", link.ToString()))
                {
                    urls.Add(link.ToString());
                }
            }

            urls.RemoveAt(0);
            urls.RemoveAt(0);

            foreach (var desc in results)
            {
                Console.WriteLine(desc.ToString());
                Console.WriteLine("");
            }
            foreach (var desc in urls)
            {
                Console.WriteLine(desc.ToString());
                Console.WriteLine("");
            }

            //Find query
            int count = 0;
            foreach (var desc in results)
            {
                string s = desc.ToString();
                if (s.Contains(query))
                {
                    break;
                }
                count++;
            }

            //Get link of query
            if (urls.Count > count)
            {
                Console.WriteLine(urls.ElementAt(count));
                target = urls.ElementAt(count).ToString();
            }
            else
            {
                Console.WriteLine("No query in this page");
                target = "";
            }
        }

        public static async Task Main(string[] args)
        {
            Setmaker();

            //Set option to no GUI
            LaunchOptions opt = new LaunchOptions
            {
                Headless = true
            };
            
            //Launch browser
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            Browser browser = await Puppeteer.LaunchAsync(opt);

            Console.WriteLine("Working");

            //Go to google
            Page page = await browser.NewPageAsync();

            await page.GoToAsync("https://www.google.com");

            //Search for "Seyalioglu"
            Keyboard typer = page.Keyboard;
            await page.ClickAsync("[name=q]");
            await typer.TypeAsync("Seyalioglu");
            await typer.PressAsync("Enter");

            string resultsSelector = "h3.LC20lb";
            int loopLimiter = 10;

            await page.WaitForSelectorAsync(resultsSelector);

            //Loop through google pages until query is found
            while(target.Equals("") && loopLimiter > 0)
            {
                await pageSearcher(page, resultsSelector);

                //Go to next page
                await page.ClickAsync("#pnnext > span:nth-child(2)");
                await page.WaitForSelectorAsync(resultsSelector);

                loopLimiter--;
            }

            if (target.Equals(""))
            {
                Console.WriteLine("Query not found");
                await browser.CloseAsync();
            }
            else
            {
                await page.GoToAsync(target);

                //Screenshot
                await page.ScreenshotAsync("prntscrn.jpg");

                Console.WriteLine("Query found");
                await browser.CloseAsync();

            }
            if (!args.Any(arg => arg == "auto-exit"))
            {
                Console.ReadLine();
            }
        }
    }
}

