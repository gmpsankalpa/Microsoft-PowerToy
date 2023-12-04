// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using interop;
using ManagedCommon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.PowerToys.Telemetry;
using Microsoft.UI.Xaml;
using Peek.Common;
using Peek.Common.Helpers;
using Peek.FilePreviewer;
using Peek.FilePreviewer.Models;
using Peek.UI.Native;
using Peek.UI.Services;
using Peek.UI.Telemetry.Events;
using Peek.UI.Views;

namespace Peek.UI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application, IApp
    {
        public static int PowerToysPID { get; set; }

        public IHost Host
        {
            get;
        }

        private MainWindow? Window { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();

            Host = Microsoft.Extensions.Hosting.Host.
            CreateDefaultBuilder().
            UseContentRoot(AppContext.BaseDirectory).
            ConfigureServices((context, services) =>
            {
                // Core Services
                services.AddTransient<NeighboringItemsQuery>();
                services.AddSingleton<IUserSettings, UserSettings>();
                services.AddSingleton<IPreviewSettings, PreviewSettings>();
                services.AddSingleton<NamedPipeServer>();

                // Views and ViewModels
                services.AddTransient<TitleBar>();
                services.AddTransient<FilePreview>();
                services.AddTransient<MainWindowViewModel>();
            }).
            Build();

            UnhandledException += App_UnhandledException;
        }

        public T GetService<T>()
            where T : class
        {
            if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
            {
                throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
            }

            return service;
        }

        public void PreviewCommandLine()
        {
            var firstActivation = EnsureWindowInitialized();
            Window!.PreviewCommandLine(firstActivation);
        }

        public void OnPreviewMessage(string path)
        {
            var firstActivation = EnsureWindowInitialized();
            Window!.PreviewMessage(firstActivation, path);
        }

        public void OnToggleMessage(string path)
        {
            var firstActivation = EnsureWindowInitialized();
            Window!.ToggleMessage(firstActivation, path);
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            int powerToysRunnerPid = GetPowerToysPid();
            if (powerToysRunnerPid != 0 && Environment.GetCommandLineArgs().Contains("--started-from-runner"))
            {
                RunnerHelper.WaitForPowerToysRunner(powerToysRunnerPid, () =>
                {
                    Environment.Exit(0);
                });

                NativeEventWaiter.WaitForEventLoop(Constants.ShowPeekEvent(), OnPeekHotkey);
                GetService<NamedPipeServer>(); // Start the named pipe server
            }
            else
            {
                System.Windows.MessageBox.Show(
                    ResourceLoaderInstance.ResourceLoader.GetString("Startup_Detached_Error"),
                    ResourceLoaderInstance.ResourceLoader.GetString("AppTitle\\Title"),
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);

                Environment.Exit(0);
            }
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            PowerToysTelemetry.Log.WriteEvent(new ErrorEvent() { HResult = (Common.Models.HResult)e.Exception.HResult, Failure = ErrorEvent.FailureType.AppCrash });
        }

        /// <summary>
        /// Handle Peek hotkey
        /// </summary>
        private void OnPeekHotkey()
        {
            // Need to read the foreground HWND before activating Peek to avoid focus stealing
            // Foreground HWND must always be Explorer or Desktop
            var foregroundWindowHandle = Windows.Win32.PInvoke.GetForegroundWindow();

            bool firstActivation = false;

            if (Window == null)
            {
                firstActivation = true;
                Window = new MainWindow();
            }

            Window.Toggle(firstActivation, foregroundWindowHandle);
        }

        /// <summary>
        /// Ensure the main window is initialized
        /// </summary>
        /// <returns>Whether the window has been initialized for the first time</returns>
        private bool EnsureWindowInitialized()
        {
            if (Window == null)
            {
                Window = new MainWindow();
                return true;
            }
            else
            {
                return false;
            }
        }

        private static int GetPowerToysPid()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (args[i] == "-powerToysPid")
                {
                    int powerToysPid;
                    if (int.TryParse(args[i + 1], out powerToysPid))
                    {
                        return powerToysPid;
                    }

                    break;
                }
            }

            return 0;
        }
    }
}
