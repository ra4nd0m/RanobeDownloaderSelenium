using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System.Diagnostics;
using System.Globalization;
namespace RanobeDownloaderSelenium
{
    internal class Program
    {
        static string Filename { get; set; }
        static string Link { get; set; }
        static int Vol { get; set; }
        static int First_Chapter { get; set; }
        static int Last_Chapter { get; set; }
        static void Main()
        {
            Console.WriteLine("Введите ссылку в формате https://ranobelib.me/no-game-no-life-novel/");
            Link = Console.ReadLine();           
            Console.WriteLine("Выберете режим:");
            Console.WriteLine("1. Диапазон глав. Применимо если нумерация глав между томами сохраняется, как в веб-новеллах.");
            Console.WriteLine("2. Скачать определенный том. Рекомендуется для скачивания печатных изданий, где в каждом томе нумерация идет сначала.");
            int select = int.Parse(Console.ReadLine());
            switch (select)
            {
                case 0:
                    Console.WriteLine("Выберете заново");
                    break;
                case 1:
                    Console.WriteLine("Запустить простой скраппер");
                    Console.WriteLine("Введите номер тома с первой главой: ");
                    Vol = int.Parse(Console.ReadLine());
                    Console.WriteLine("Введите номер первой главы: ");
                    First_Chapter = int.Parse(Console.ReadLine());
                    Console.WriteLine("Введите номер последней главы: ");
                    Last_Chapter = int.Parse(Console.ReadLine());
                    //link = link + 'v' + vol + "/";
                    MakeBook(select);         
                    break;
                case 2:
                    Console.WriteLine("Продвинутый скраппер");
                    Console.WriteLine("Введите номер тома: ");
                    Vol = int.Parse(Console.ReadLine());
                    Console.WriteLine("Введите номер первой главы тома: ");
                    First_Chapter = int.Parse(Console.ReadLine());
                    MakeBook(select);
                    break;
            }
        }
        static void MakeBook(int mode)
        {
            Console.WriteLine("Введите название файла: ");
            Filename = Console.ReadLine();
            StreamWriter sw = new StreamWriter(Filename + ".fb2");
            string html;
            if(mode == 1)
                 html = GetSections();
            else
                 html = GetFullVolume();
            if (html == "-1")
            {
                sw.Close();
                Console.WriteLine("Ошибка!");
                return;
            }
            sw.WriteLine("<?xml version=" + "\"1.0\"" + " encoding=" + "\"utf-8\"" + "?>");
            sw.WriteLine("<FictionBook xmlns=" + "\"http://www.gribuser.ru/xml/fictionbook/2.0\"" + " xmlns:l=" + "\"http://www.w3.org/1999/xlink\">");
            sw.WriteLine("<body>");
            sw.Write(html);
            sw.WriteLine("</body>");
            sw.WriteLine("</FictionBook>");
            sw.Close();
        }
        static string GetSections()
        {
            Process proc = new Process();
            proc.StartInfo.FileName = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            proc.StartInfo.Arguments = "--new-window --remote-debugging-port=9222 --user-data-dir=C:\\Temp";
            proc.Start();
            string html = "";
            var chromeOptions = new ChromeOptions();
            chromeOptions.DebuggerAddress = "127.0.0.1:9222";
            //chromeOptions.AddArgument("headless");
            var driver = new ChromeDriver(chromeOptions);
            //int vol_position=GetVolPos(link);
            //int vol_size=GetVolSize(link,vol_position);
            // int current_volume = GetCurrentVolume(link, vol_position, vol_size);
            driver.Navigate().GoToUrl(Link + "v" + Vol + "/c" + First_Chapter);
            for (int i = First_Chapter; i <= Last_Chapter; i++)
            {
                // string novel_name = driver.FindElement(By.CssSelector(".reader-header-action__text.text-truncate")).Text;
               
                html = html + "<section>\n";
                html = html + "<title>\n";
                html = html + "<p>Глава " + i + "</p>\n";
                html = html + "</title>\n";
                html = html + driver.FindElement(By.CssSelector(".reader-container.container.container_center")).GetAttribute("innerHTML");
                try
                {
                    IReadOnlyList<IWebElement> elements = driver.FindElements(By.ClassName("article-image"));
                    html = ClearImages(html,GetCurrentChapter(driver.Url));
                }
                catch
                {
                    continue;
                }
                html = html + "\n</section>\n";
                html = RemoveSpecialCharacters(html);
                if (i != Last_Chapter)
                    driver.FindElement(By.CssSelector(".reader-next__btn.button.text-truncate.button_label.button_label_right")).Click();
            }
            driver.Quit();
            return html;
        }
        static string GetFullVolume()
        {
            Process proc = new Process();
            proc.StartInfo.FileName = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            proc.StartInfo.Arguments = "--new-window --remote-debugging-port=9222 --user-data-dir=C:\\Temp";
            proc.Start();
            string html = "";
            var chromeOptions = new ChromeOptions();
            chromeOptions.DebuggerAddress = "127.0.0.1:9222";
            //chromeOptions.AddArgument("headless");
            var driver = new ChromeDriver(chromeOptions);          
            driver.Navigate().GoToUrl(Link + "v" + Vol + "/c" + First_Chapter);
            double current_volume = GetCurrentVolume(driver.Url);
            while (current_volume == Vol)
            {
                html = html + "<section>\n";
                html = html + "<title>\n";
                double currentChapter = GetCurrentChapter(driver.Url);
                html = html + "<p>Глава " + currentChapter + "</p>\n";
                html = html + "</title>\n";
                html = html + driver.FindElement(By.CssSelector(".reader-container.container.container_center")).GetAttribute("innerHTML");
                html = html + "\n</section>\n";
                try
                {
                    IReadOnlyList<IWebElement> elements = driver.FindElements(By.ClassName("article-image"));
                    html = ClearImages(html, GetCurrentChapter(driver.Url));
                }
                catch
                {
                    continue;
                }
                html = RemoveSpecialCharacters(html);
                try
                {
                    driver.FindElement(By.CssSelector(".reader-next__btn.button.text-truncate.button_label.button_label_right")).Click();
                    current_volume = GetCurrentVolume(driver.Url);
                }
                catch
                {
                    break;
                }               
            }
            driver.Quit();
            return html;
        }
        static string ClearImages(string input,double current_chapter)
        {
            int i;
            i = input.IndexOf("<div");         
            int endIndex = 0;
            int picCount = 0;
            StreamWriter sw = new StreamWriter(Filename + " picture_links.txt",true);
            while (i != -1)
            {
                int startIndex = i;
                for (int j = i; j < input.Length; j++)
                {
                    if(input[j] == '<' && input[j + 1] == '/' && input[j + 2] == 'd')
                    {
                        endIndex = j + 5;
                        break;
                    }                   
                }
                string substing = input.Substring(startIndex, endIndex - startIndex + 1);
                sw.WriteLine(substing);
                input = input.Remove(startIndex, endIndex - startIndex + 1);
                i = input.IndexOf("<div");
                picCount++;
            }
            sw.Close();
            Console.WriteLine("Кол-во картинок в главе номер "+current_chapter+":"+picCount);
            return input;
        } 

        static string RemoveSpecialCharacters(string input)
        {
            int i = input.IndexOf("&nbsp;");
            if (i == -1)
                return input;
            while(i != -1)
            {
                input = input.Remove(i, 6);
                i = input.IndexOf("&nbsp;");
            }
            
            return input;
        }
        static double GetCurrentVolume(string link)
        {
            int vol_position = 0;
            for (int i = 0; i < link.Length; i++)
            {
                if (link[i] == '/' && link[i + 1] == 'v')
                    vol_position = i + 2;
            }
            int size = 0;
            for (int i = vol_position; i < link.Length; i++)
            {
                if (link[i] != '/')
                    size++;
                else
                    break;
            }
            string vol = "" + link[vol_position];
            while (size > 0 && size!= 1)
            {
                vol_position++;
                vol = vol + link[vol_position];
                size--;
            }
            return double.Parse(vol, CultureInfo.InvariantCulture);
        }
        static double GetCurrentChapter(string link)
        {
            int chapter_position = 0;
            for(int i = 0; i < link.Length; i++)
            {
                if (link[i] == '/' && link[i + 1] == 'c')
                    chapter_position = i + 2;
            }
            int size = 0;
            for(int i = chapter_position; i < link.Length; i++)
            {
                size++;
            }
            string chap = "" + link[chapter_position];
            while (size>0&& size != 1)
            {
                chapter_position++;
                chap = chap + link[chapter_position];
                size--;
            }
            return double.Parse(chap, CultureInfo.InvariantCulture);
        }
    }
}