using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace PizzaStoreTests;

public static partial class ApiTestHelper
{
    private static readonly string postLoginRoute = Sanitize("post/login");
    private const string slogan = "Making the Pizzas you love";
    public static async Task GoToSwaggerPageAsync(this IPage page)
    {
        await page.GotoAsync("http://localhost:5024/swagger/index.html");
    }
    
    public static async Task AuthorizeAsAdminAsync(this IPage page)
    {
        await WaitForPageToFullyLoad(page);
        var isAuthorized = await page.Locator(".unlocked").CountAsync() == 0;
        if (isAuthorized) { return; }
        await page.ExpandEndpoint("Authentication", postLoginRoute);
        await page.ClickTryItOut();
        await page.FillRequestBody("""{"username":"admin","password":"admin"}""");
        await page.ClickExecute();
        await page.ExpectResponseCodeToBe(HttpStatusCode.OK);
        await page.CopyResponseBody();
        await page.Authorize();
        await page.CollapseEndpoint("Authentication", postLoginRoute);
    }

    internal static async Task ExpectResponseCodeToBe(this IPage page, HttpStatusCode code)
    {
        var responseCode = page.GetByText($"{(int)code}", new PageGetByTextOptions { Exact = true });
        const int numActualResponseCodes = 1;
        const int numExampleResponseCodes = 1;
        await Assertions.Expect(responseCode).ToHaveCountAsync(numActualResponseCodes + numExampleResponseCodes);
        // note: the above assertion will fail if multiple endpoints are expanded
        // so it is recommended to collapse each endpoint when you are done testing it. 
    }

    private static async Task WaitForPageToFullyLoad(IPage page)
    {
        await Assertions.Expect(page.GetByText(slogan, new PageGetByTextOptions { Exact = true })).ToBeVisibleAsync();
    }

    private static async Task Authorize(this IPage page)
    {
        await GetAuthorizeButton(page).ClickAsync();
        await page.Locator("#auth-bearer-value").ClickAsync();
        await page.Keyboard.PressAsync($"Control+KeyV");
        await page.Locator("button[aria-label='Apply credentials']").ClickAsync();
        await GetCloseButton(page).ClickAsync();
    }

    internal static async Task LogOut(this IPage page)
    {
        await WaitForPageToFullyLoad(page);
        var isLoggedOut = await page.Locator(".unlocked").CountAsync() > 0;
        if (isLoggedOut) { return; }
        await GetAuthorizeButton(page).ClickAsync();
        await page.Locator("button[aria-label='Remove authorization']").ClickAsync();
        await GetCloseButton(page).ClickAsync();
    }

    private static ILocator GetAuthorizeButton(IPage page)
    {
        return page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Authorize", Exact = true });
    }

    private static ILocator GetCloseButton(IPage page)
    {
        return page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Close", Exact = true });
    }

    private static async Task CopyResponseBody(this IPage page)
    {
        await page.Locator(".copy-to-clipboard").Nth(2).ClickAsync();
    }

    internal static async Task ClickExecute(this IPage page)
    {
        var button = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Execute", Exact = true });
        await button.ClickAsync();
    }

    internal static async Task FillRequestBody(this IPage page, string requestBody)
    {
        await page.Locator(".body-param__text").FillAsync(requestBody);
    }

    internal static async Task ClickTryItOut(this IPage page)
    {
        var cancelButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Cancel", Exact = true });
        if (await cancelButton.IsVisibleAsync()) { return; }
        var tryItOutButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Try it out", Exact = true });
        await tryItOutButton.ClickAsync();
    }

    internal static async Task ExpandEndpoint(this IPage page, string group, string route)
        => await ClickEndpointTitleBar(page, group, route);

    internal static async Task CollapseEndpoint(this IPage page, string group, string route)
        => await ClickEndpointTitleBar(page, group, route);
    
    private static async Task ClickEndpointTitleBar(this IPage page, string group, string route)
    {
        var titleBar = page.Locator($"#operations-{group}-{route} > div:nth-child(1)");
        await titleBar.ClickAsync();
    }

    internal static string Sanitize(string route)
    {
        return MyRegex().Replace(route, "_");
    }

    [GeneratedRegex(@"{|}|/")]
    private static partial Regex MyRegex();
}