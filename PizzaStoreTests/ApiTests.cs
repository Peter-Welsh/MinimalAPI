using System.Net;
using Microsoft.Playwright;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace PizzaStoreTests;

public class ApiTests
{
    private const int waitTimeBetweenStepsMs = 0; // increase to slow down the tests
    private static readonly BrowserTypeLaunchOptions browserTypeLaunchOptions = new()
    {
        Headless = true, // set to false to watch the tests run
        SlowMo = waitTimeBetweenStepsMs
    };
    
    private IPlaywright playwright { get; set; } = null!;
    private IBrowser browser { get; set; } = null!;
    private IPage page { get; set; } = null!;
    private static readonly string postPizzaRoute = ApiTestHelper.Sanitize("post/pizza");
    private static readonly string getPizzaRoute = ApiTestHelper.Sanitize("get/pizza/{id}");
    private static readonly string getPizzasRoute = ApiTestHelper.Sanitize("get/pizzas");
    private static readonly string putPizzaRoute = ApiTestHelper.Sanitize("put/pizza/{id}");
    private static readonly string deletePizzaRoute = ApiTestHelper.Sanitize("delete/pizza/{id}");
    private const string pizzaGroup = "PizzaStore";
    private static readonly string postUserRoute = ApiTestHelper.Sanitize("post/user");
    private static readonly string getUserRoute = ApiTestHelper.Sanitize("get/user/{username}");
    private static readonly string deleteUserRoute = ApiTestHelper.Sanitize("delete/user/{username}");
    private const string userGroup = "User_Management";

    [OneTimeSetUp]
    public async Task Setup()
    {
        #region First-time setup
        // uncomment the following code block if this your first time running Playwright on your machine,
        // and you are getting an error about missing browsers when attempting to run a test
        //
        // if you are getting an error about "connection refused",
        // then ensure that the app is running before attempting to run a test
        //
        // var exitCode = Program.Main(["install"]);
        // if (exitCode != 0)
        // {
        //     throw new Exception($"Playwright exited with code {exitCode}");
        // }
        #endregion
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Firefox.LaunchAsync(browserTypeLaunchOptions);
        page = await browser.NewPageAsync();
        await page.GoToSwaggerPageAsync();
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await browser.CloseAsync();
        playwright.Dispose();
    }
    
    [Test]
    public async Task Pizza_CRUD_HappyPath()
    {
        await page.AuthorizeAsAdminAsync();
        
        await CreatePizza();
        await page.ExpectResponseCodeToBe(HttpStatusCode.Created);
        var pizzaID = await GetPizzaId();
        await page.CollapseEndpoint(pizzaGroup, postPizzaRoute);

        await GetPizza(pizzaID);
        await page.ExpectResponseCodeToBe(HttpStatusCode.OK);
        await page.CollapseEndpoint(pizzaGroup, getPizzaRoute);

        await UpdatePizza(pizzaID);
        await page.ExpectResponseCodeToBe(HttpStatusCode.NoContent);
        await page.CollapseEndpoint(pizzaGroup, putPizzaRoute);

        await DeletePizza(pizzaID);
        await page.ExpectResponseCodeToBe(HttpStatusCode.OK);
        await page.CollapseEndpoint(pizzaGroup, deletePizzaRoute);
    }
    
    [Test]
    public async Task Pizza_AllEndpointsReturn401_WhenNotLoggedIn()
    {
        await page.LogOut();
        
        await CreatePizza();
        await page.ExpectResponseCodeToBe(HttpStatusCode.Unauthorized);
        await page.CollapseEndpoint(pizzaGroup, postPizzaRoute);
        
        await GetPizzas();
        await page.ExpectResponseCodeToBe(HttpStatusCode.Unauthorized);
        await page.CollapseEndpoint(pizzaGroup, getPizzasRoute);

        const string pizzaID = "1";
        await GetPizza(pizzaID);
        await page.ExpectResponseCodeToBe(HttpStatusCode.Unauthorized);
        await page.CollapseEndpoint(pizzaGroup, getPizzaRoute);

        await UpdatePizza(pizzaID);
        await page.ExpectResponseCodeToBe(HttpStatusCode.Unauthorized);
        await page.CollapseEndpoint(pizzaGroup, putPizzaRoute);

        await DeletePizza(pizzaID);
        await page.ExpectResponseCodeToBe(HttpStatusCode.Unauthorized);
        await page.CollapseEndpoint(pizzaGroup, deletePizzaRoute);
    }
    
    [Test]
    public async Task Pizza_EndpointsReturn404_WhenResourceIsNotFound()
    {
        await page.AuthorizeAsAdminAsync();
        
        var pizzaID = $"{int.MaxValue}";
        await GetPizza(pizzaID);
        await page.ExpectResponseCodeToBe(HttpStatusCode.NotFound);
        await page.CollapseEndpoint(pizzaGroup, getPizzaRoute);

        await UpdatePizza(pizzaID);
        await page.ExpectResponseCodeToBe(HttpStatusCode.NotFound);
        await page.CollapseEndpoint(pizzaGroup, putPizzaRoute);

        await DeletePizza(pizzaID);
        await page.ExpectResponseCodeToBe(HttpStatusCode.NotFound);
        await page.CollapseEndpoint(pizzaGroup, deletePizzaRoute);
    }
    
    [Test]
    public async Task User_AllEndpointsReturn401_WhenNotLoggedIn()
    {
        await page.LogOut();
        const string username = "Tester";
        await CreateUser(username);
        await page.ExpectResponseCodeToBe(HttpStatusCode.Unauthorized);
        await page.CollapseEndpoint(userGroup, postUserRoute);

        await GetUser(username);
        await page.ExpectResponseCodeToBe(HttpStatusCode.Unauthorized);
        await page.CollapseEndpoint(userGroup, getUserRoute);

        await DeleteUser(username);
        await page.ExpectResponseCodeToBe(HttpStatusCode.Unauthorized);
        await page.CollapseEndpoint(userGroup, deleteUserRoute);
    }
    
    [Test]
    public async Task User_EndpointsReturn404_WhenResourceIsNotFound()
    {
        await page.AuthorizeAsAdminAsync();
        
        const string username = "Tester";
        await GetUser(username);
        await page.ExpectResponseCodeToBe(HttpStatusCode.NotFound);
        await page.CollapseEndpoint(userGroup, getUserRoute);

        await DeleteUser(username);
        await page.ExpectResponseCodeToBe(HttpStatusCode.NotFound);
        await page.CollapseEndpoint(userGroup, deleteUserRoute);
    }

    private async Task<dynamic> GetPizzaId()
    {
        var postResponseBody = $"{await page.Locator(".highlight-code").TextContentAsync()}";
        postResponseBody = postResponseBody[postResponseBody.IndexOf('{')..]; // text content is like 'Download{...}' 
        dynamic data = JObject.Parse(postResponseBody);
        var pizzaID = data.id.ToString();
        return pizzaID;
    }

    private async Task CreatePizza()
    {
        await page.ExpandEndpoint(pizzaGroup, postPizzaRoute);
        await page.ClickTryItOut();
        await page.FillRequestBody("""
                                   {
                                     "name": "The Big One",
                                     "description": "Description of The Big One"
                                   }
                                   """);
        await page.ClickExecute();
    }

    private async Task GetPizzas()
    {
        await page.ExpandEndpoint(pizzaGroup, getPizzasRoute);
        await page.ClickTryItOut();
        await page.ClickExecute();
    }

    private async Task GetPizza(string pizzaID)
    {
        await page.ExpandEndpoint(pizzaGroup, getPizzaRoute);
        await page.ClickTryItOut();
        await page.GetByPlaceholder("id").FillAsync(pizzaID);
        await page.ClickExecute();
    }

    private async Task UpdatePizza(string pizzaID)
    {
        await page.ExpandEndpoint(pizzaGroup, putPizzaRoute);
        await page.ClickTryItOut();
        await page.GetByPlaceholder("id").FillAsync(pizzaID);
        await page.FillRequestBody("""
                                   {
                                     "name": "The New and Improved One",
                                     "description": "Description of The New and Improved One"
                                   }
                                   """);
        await page.ClickExecute();
    }

    private async Task DeletePizza(string pizzaID)
    {
        await page.ExpandEndpoint(pizzaGroup, deletePizzaRoute);
        await page.ClickTryItOut();
        await page.GetByPlaceholder("id").FillAsync(pizzaID);
        await page.ClickExecute();
    }

    private async Task CreateUser(string username, string password = "password")
    {
        await page.ExpandEndpoint(userGroup, postUserRoute);
        await page.ClickTryItOut();
        await page.FillRequestBody("""
                                   {
                                     "username": "{1}",
                                     "password": "{2}"
                                   }
                                   """
            .Replace("{1}", username)
            .Replace("{2}", password));
        await page.ClickExecute();
    }

    private async Task GetUser(string username)
    {
        await page.ExpandEndpoint(userGroup, getUserRoute);
        await page.ClickTryItOut();
        await page.GetByPlaceholder("username").FillAsync(username);
        await page.ClickExecute();
    }

    private async Task DeleteUser(string username)
    {
        await page.ExpandEndpoint(userGroup, deleteUserRoute);
        await page.ClickTryItOut();
        await page.GetByPlaceholder("username").FillAsync(username);
        await page.ClickExecute();
    }
}