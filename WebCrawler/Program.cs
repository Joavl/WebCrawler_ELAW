using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace WebCrawler
{
    /// <summary>
    /// Classe principal do programa.
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArgument("--headless");
            chromeOptions.AddArgument("--disable-gpu");

            var jsonFilePath = "proxies.json";
            var sqliteConnectionString = "Data Source=proxies.db";

            var webCrawlerService = new WebCrawlerService(chromeOptions, jsonFilePath, sqliteConnectionString);
            await webCrawlerService.ExtractAndSaveDataAsync();
        }
    }

    /// <summary>
    /// Interface para o serviço do web crawler.
    /// </summary>
    public interface IWebCrawlerService
    {
        /// <summary>
        /// Extrai os proxies das páginas e salva os dados.
        /// </summary>
        Task ExtractAndSaveDataAsync();
    }

    /// <summary>
    /// Implementação do serviço do web crawler.
    /// </summary>
    public class WebCrawlerService : IWebCrawlerService
    {
        private readonly string _urlBase = "https://fineproxy.org/pt/free-proxy/";

        private readonly ChromeOptions _chromeOptions;
        private readonly string _jsonFilePath;
        private readonly string _sqliteConnectionString;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(3);

        /// <summary>
        /// Construtor do serviço do web crawler.
        /// </summary>
        public WebCrawlerService(ChromeOptions chromeOptions, string jsonFilePath, string sqliteConnectionString)
        {
            _chromeOptions = chromeOptions;
            _jsonFilePath = jsonFilePath;
            _sqliteConnectionString = sqliteConnectionString;
        }

        /// <summary>
        /// Extrai os proxies das páginas e salva os dados.
        /// </summary>
        public async Task ExtractAndSaveDataAsync()
        {
            DateTime startTime = DateTime.Now;

            try
            {
                await ExtractAndSaveDataWithThrottlingAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            finally
            {
                DateTime endTime = DateTime.Now;
                TimeSpan duration = endTime - startTime;
                Console.WriteLine($"Execution completed in {duration.TotalSeconds} seconds.");
            }
        }

        /// <summary>
        /// Extrai os proxies das páginas com limitação de execução simultânea.
        /// </summary>
        private async Task ExtractAndSaveDataWithThrottlingAsync()
        {
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++) 
            {
                await _semaphore.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var proxies = await ExtractProxiesAsync();
                        SaveToJson(proxies);

                        await SaveToDatabaseAsync(DateTime.Now, proxies.Count);
                    }
                    finally
                    {
                        _semaphore.Release(); 
                    }
                }));
            }
            await Task.WhenAll(tasks); 
        }

        /// <summary>
        /// Extrai os proxies das páginas disponíveis.
        /// </summary>
        private async Task<List<Proxy>> ExtractProxiesAsync()
        {
            var proxies = new List<Proxy>();
            using (var driver = new ChromeDriver(_chromeOptions))
            {
                driver.Navigate().GoToUrl(_urlBase);
                var totalPages = await GetTotalPagesAsync(driver);

                for (int i = 1; i <= totalPages; i++)
                {
                    var pageSource = driver.PageSource;
                    var pageProxies = ExtractProxiesFromPage(pageSource);
                    proxies.AddRange(pageProxies);

                    var htmlFileName = $"page_{i}.html";
                    await SaveHtmlAsync(pageSource, htmlFileName);

                    if (i < totalPages)
                    {
                        var nextPageUrl = $"{_urlBase}?page={i + 1}";
                        driver.Navigate().GoToUrl(nextPageUrl);
                    }
                }
            }
            return proxies;
        }

        /// <summary>
        /// Extrai os proxies de uma página HTML.
        /// </summary>
        private List<Proxy> ExtractProxiesFromPage(string html)
        {
            var proxies = new List<Proxy>();
            var regex = new Regex(@"<tr>.+?</tr>", RegexOptions.Singleline);
            var matches = regex.Matches(html);

            foreach (Match match in matches)
            {
                var proxy = new Proxy();

                proxy.IpAddress = ExtractValueFromHtml(match.Value, @"<td>(?<valor>.+?)</td>", 0);
                proxy.Port = int.Parse(ExtractValueFromHtml(match.Value, @"<td>(?<valor>.+?)</td>", 1));
                proxy.Country = ExtractValueFromHtml(match.Value, @"<td>(?<valor>.+?)</td>", 2);
                proxy.Protocol = ExtractValueFromHtml(match.Value, @"<td>(?<valor>.+?)</td>", 4);

                proxies.Add(proxy);
            }
            return proxies;
        }

        /// <summary>
        /// Obtém o número total de páginas.
        /// </summary>
        private async Task<int> GetTotalPagesAsync(IWebDriver driver)
        {
            var lastPageLink = driver.FindElement(By.CssSelector("ul.pagination li:last-child a")).GetAttribute("href");
            var match = Regex.Match(lastPageLink, @"page=(\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value) : 1;
        }

        /// <summary>
        /// Extrai um valor de uma string HTML com base em um padrão de expressão regular.
        /// </summary>
        private string ExtractValueFromHtml(string html, string pattern, int groupIndex)
        {
            var match = Regex.Match(html, pattern, RegexOptions.Singleline);
            return match.Success ? match.Groups["valor"].Value : "";
        }

        /// <summary>
        /// Salva uma string HTML em um arquivo.
        /// </summary>
        private async Task SaveHtmlAsync(string html, string fileName)
        {
            var directory = "html_pages";
            Directory.CreateDirectory(directory);

            var filePath = Path.Combine(directory, fileName);
            await File.WriteAllTextAsync(filePath, html);
        }

        /// <summary>
        /// Salva os proxies em um arquivo JSON.
        /// </summary>
        private void SaveToJson(List<Proxy> proxies)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(proxies, options);
            File.WriteAllText(_jsonFilePath, json);
        }

        /// <summary>
        /// Salva as informações da execução no banco de dados SQLite.
        /// </summary>
        private async Task SaveToDatabaseAsync(DateTime endTime, int totalProxies)
        {
            using var connection = new SqliteConnection(_sqliteConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO ExecutionInfo (StartTime, EndTime, TotalPages, TotalProxies) 
                VALUES (@StartTime, @EndTime, @TotalPages, @TotalProxies)
            ";
            command.Parameters.AddWithValue("@StartTime", endTime.AddMinutes(-1).ToString("yyyy-MM-dd HH:mm:ss")); // Subtrai 1 minuto para garantir que o início seja antes do fim.
            command.Parameters.AddWithValue("@EndTime", endTime.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@TotalPages", 0); 
            command.Parameters.AddWithValue("@TotalProxies", totalProxies);
            await command.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Classe que representa um proxy.
    /// </summary>
    public class Proxy
    {
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public string Country { get; set; }
        public string Protocol { get; set; }
    }
}
