using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using shortid;
using url_shortener.Models;

namespace url_shortener.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IMongoDatabase _mongoDatabase;
    private const string ServiceUrl = "http://localhost:7184";

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
        var connectionString = "mongodb://localhost:27017/";
        var mongoClient = new MongoClient(connectionString);
        _mongoDatabase = mongoClient.GetDatabase("url-shortener");
    }

    [Route("")]
    public IActionResult Index()
    {
        return View();
    }

    [Route("{url}")]
    public async Task<IActionResult> Index(string url)
    {
        // get shortened url collection
        var shortenedUrlCollection = _mongoDatabase.GetCollection<ShortenedUrl>("shortened-urls");
        // first check if we have the short code
        var filter = Builders<ShortenedUrl>.Filter.Eq("ShortCode", url);
        var shortenedUrl = await shortenedUrlCollection.Find(filter).FirstOrDefaultAsync();

        // if the short code does not exist, send back to home page
        if (shortenedUrl == null)
        {
            return View();
        }

        return Redirect(shortenedUrl.OriginalUrl);
    }


    [HttpPost]
    public async Task<IActionResult> ShortenUrl(string longUrl)
    {
        // get shortened url collection
        var shortenedUrlCollection = _mongoDatabase.GetCollection<ShortenedUrl>("shortened-urls");

        // first we format the long url
        longUrl = formatLongUrl(longUrl);

        // secondly we check if have the url stored
        var filter = Builders<ShortenedUrl>.Filter.Eq("OriginalUrl", longUrl);
        var shortenedUrl = await shortenedUrlCollection.Find(filter).FirstOrDefaultAsync();

        // if the long url has not been shortened
        if (shortenedUrl == null)
        {
            var shortCode = ShortId.Generate(new shortid.Configuration.GenerationOptions(length: 8));
            shortenedUrl = new ShortenedUrl
            {
                CreatedAt = DateTime.UtcNow,
                OriginalUrl = longUrl,
                ShortCode = shortCode,
                ShortUrl = $"{ServiceUrl}/{shortCode}"
            };
            // add to database
            await shortenedUrlCollection.InsertOneAsync(shortenedUrl);
        }

        return View(shortenedUrl);
    }
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private string formatLongUrl(string longUrl)
    {
        if (!longUrl.Contains("https://")) {
            longUrl = "https://" + longUrl;
        }
        return longUrl;
    }
}
