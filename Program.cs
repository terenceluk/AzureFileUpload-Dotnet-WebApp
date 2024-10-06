// https://dotnet.microsoft.com/en-us/download
// dotnet --version
// dotnet new webapp -n AzureFileUpload
using Microsoft.AspNetCore.Authentication.Cookies;
// dotnet add package Microsoft.AspNetCore.Authentication.OpenIdConnect
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
// dotnet add package Azure.Storage.Blobs
using Azure.Storage.Blobs;
// dotnet add package Azure.Identity
using Azure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages()
    .AddMvcOptions(options =>
    {
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
        options.Filters.Add(new AuthorizeFilter(policy));
    });

// Add authentication services.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie()
.AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration["OpenIdConnect:Authority"];
    options.ClientId = builder.Configuration["OpenIdConnect:ClientId"];
    options.ClientSecret = builder.Configuration["OpenIdConnect:ClientSecret"];
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.SaveTokens = true;
});

// Add Azure Blob Storage service
var defaultAzureCredential = new DefaultAzureCredential();
string storageAccountName = builder.Configuration["StorageAccount:Name"];
builder.Services.AddSingleton(x => new BlobServiceClient(new Uri($"https://{storageAccountName}.blob.core.windows.net"), defaultAzureCredential));

var app = builder.Build();

// Use authentication middleware
app.UseAuthentication();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapRazorPages();
app.Run();