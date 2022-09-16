// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using Hosts.Helpers;
using Hosts.Settings;
using Hosts.ViewModels;
using Hosts.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

namespace Hosts
{
    public partial class App : Application
    {
        private Window _window;

        public IHost Host
        {
            get;
        }

        public static T GetService<T>()
            where T : class
        {
            if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
            {
                throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
            }

            return service;
        }

        public App()
        {
            InitializeComponent();

            Host = Microsoft.Extensions.Hosting.Host.
                CreateDefaultBuilder().
                UseContentRoot(AppContext.BaseDirectory).
                ConfigureServices((context, services) =>
                {
                    // Core Services
                    services.AddSingleton<IFileSystem, FileSystem>();
                    services.AddSingleton<IHostsService, HostsService>();
                    services.AddSingleton<IUserSettings, UserSettings>();

                    // Views and ViewModels
                    services.AddTransient<MainPage>();
                    services.AddTransient<MainViewModel>();
                }).
                Build();

            UnhandledException += App_UnhandledException;

            new Thread(() =>
            {
                try
                {
                    Directory.GetFiles(Path.GetDirectoryName(HostsService.HostsFilePath), $"*{HostsService.BackupSuffix}*")
                        .Select(f => new FileInfo(f))
                        .Where(f => f.CreationTime < DateTime.Now.AddDays(-15))
                        .ToList()
                        .ForEach(f => f.Delete());
                }
                catch
                {
                }
            }).Start();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // TODO: Log and handle exceptions as appropriate.
        }
    }
}