using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using URMS.Blazor;
using URMS.Blazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5000/")
});

builder.Services.AddBlazoredLocalStorage();

// App services
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<ExaminerApiService>();
builder.Services.AddScoped<HODApiService>();
builder.Services.AddScoped<TeacherApiService>();
builder.Services.AddScoped<StudentApiService>();
builder.Services.AddScoped<NotificationStateService>();
builder.Services.AddScoped<JsInteropService>();

await builder.Build().RunAsync();