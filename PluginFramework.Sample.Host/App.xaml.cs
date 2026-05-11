using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PluginFramework.Sample.Host.Controls;
using PluginFramework.Sample.Host.ViewModels;

namespace PluginFramework.Sample.Host;

public partial class App : Application
{
    public static IHost? Host { get; private set; }

    public App()
    {
        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services);
            })
            .Build();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<MainWindow>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<InnerControl>();
        services.AddTransient<MyDefaultControl>();
        
        services.AddTransient<LoginWindow>();
        services.AddTransient<LoginViewModel>();
        
        services.AddSingleton<DynamicServiceContainer>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        Host!.StartAsync().GetAwaiter().GetResult();
        base.OnStartup(e);
        
        var loginWindow = Host.Services.GetRequiredService<LoginWindow>();
        
        var mainWindow = Host.Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        
        var loginResult = loginWindow.ShowDialog();
        
        if (loginResult == true)
        {
            var dynamicContainer = Host.Services.GetRequiredService<DynamicServiceContainer>();
            
            dynamicContainer.RegisterSingleton<IUserContext, UserContext>();
            
            var userContext = dynamicContainer.GetRequiredService<IUserContext>();
            userContext.UserId = "123";
            userContext.UserName = "admin";
            userContext.Roles = new[] { "Admin", "User" };
            
            dynamicContainer.RegisterSingleton<FeatureMetaData>(new FeatureMetaData 
            { 
                Name = "默认界面", 
                ControlType = typeof(MyDefaultControl) 
            });
            
            if (userContext.Roles.Contains("Admin"))
            {
                dynamicContainer.RegisterSingleton<FeatureMetaData>(new FeatureMetaData 
                { 
                    Name = "管理界面", 
                    ControlType = typeof(InnerControl) 
                });
            }
            
            var viewModel = (MainWindowViewModel)mainWindow.DataContext;
            viewModel.InitializeFeatures();
            
            mainWindow.Show();
        }
        else
        {
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Host!.StopAsync().GetAwaiter().GetResult();
        Host.Dispose();
        base.OnExit(e);
    }
}

public interface IUserContext
{
    string UserId { get; set; }
    string UserName { get; set; }
    string[] Roles { get; set; }
}

public class UserContext : IUserContext
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
}

public class AdminService
{
    public void DoAdminTask()
    {
        // 管理员专属操作
    }
}
