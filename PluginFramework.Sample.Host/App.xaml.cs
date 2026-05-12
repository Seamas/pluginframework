using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PluginFramework.Core.Abstractions;
using PluginFramework.Loader;
using PluginFramework.Loader.Metadata;
using PluginFramework.Sample.Host.Controls;
using PluginFramework.Sample.Host.ViewModels;

namespace PluginFramework.Sample.Host;

public partial class App
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
        // 注册 Host 自身的服务
        services.AddTransient<MainWindow>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<InnerControl>();
        services.AddTransient<MyDefaultControl>();
        services.AddTransient<LoginWindow>();
        services.AddTransient<LoginViewModel>();

        // 注册框架基础设施
        services.AddSingleton<DynamicServiceContainer>();
        services.AddSingleton<ModuleScanner>(sp =>
        {
            return new ModuleScanner
            {
                ViewResolver = WpfViewResolver.Resolve
            };
        });
        services.AddSingleton<IModuleLoader, DefaultModuleLoader>();
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
            var container = Host.Services.GetRequiredService<DynamicServiceContainer>();

            // 1. 注册当前用户上下文
            var userContext = new UserContext
            {
                UserId = "123",
                UserName = "admin",
                Roles = new[] { "Admin", "User" }
            };
            container.RegisterSingleton<IUserContext>(userContext);

            // 2. 加载外部模块
            var moduleLoader = Host.Services.GetRequiredService<IModuleLoader>();
            var modulesDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules");

            if (!System.IO.Directory.Exists(modulesDir))
                System.IO.Directory.CreateDirectory(modulesDir);

            var loadedModules = moduleLoader.LoadModulesAsync(modulesDir, userContext.Roles)
                .GetAwaiter().GetResult();

            // 3. 将内置功能包装为伪模块，合并到列表头部
            var allModules = new List<ModuleMeta>(loadedModules);
            allModules.Insert(0, CreateBuiltInModule());

            // 4. 初始化主窗口的 Toolbar
            var viewModel = (MainWindowViewModel)mainWindow.DataContext;
            viewModel.InitializeFromModules(allModules);

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

    /// <summary>
    /// 创建内置功能模块（不需要外部 DLL 的默认功能）
    /// </summary>
    private static ModuleMeta CreateBuiltInModule()
    {
        var module = new ModuleMeta
        {
            Code = "__builtin__",
            Name = "系统",
            Assembly = typeof(App).Assembly,
            ConfigureType = typeof(App)
        };

        module.Features.Add(new FeatureMeta
        {
            Code = "default",
            Name = "默认界面",
            ViewModelType = typeof(object),
            ViewType = typeof(MyDefaultControl)
        });

        module.Features.Add(new FeatureMeta
        {
            Code = "inner",
            Name = "管理界面",
            ViewModelType = typeof(object),
            ViewType = typeof(InnerControl),
            RequiredRoles = new[] { "Admin" }
        });

        return module;
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
