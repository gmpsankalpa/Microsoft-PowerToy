// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCommon;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using Peek.UI.Helpers;

namespace Peek.UI
{
    public class Program
    {
        private static App? _app;
        private static DispatcherQueue? _dispatcherQueue;

        [STAThread]
        public static void Main(string[] args)
        {
            Logger.InitializeLogger("\\Peek\\Logs");

            WinRT.ComWrappersSupport.InitializeComWrappers();

            if (PowerToys.GPOWrapper.GPOWrapper.GetConfiguredPeekEnabledValue() == PowerToys.GPOWrapper.GpoRuleConfigured.Disabled)
            {
                Logger.LogWarning("Tried to start with a GPO policy setting the utility to always be disabled. Please contact your systems administrator.");
                Environment.Exit(0); // Current.Exit won't work until there's a window opened.
                return;
            }

            var isRedirect = DecideRedirection().GetAwaiter().GetResult();

            if (!isRedirect)
            {
                Microsoft.UI.Xaml.Application.Start((p) =>
                {
                    _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
                    var context = new DispatcherQueueSynchronizationContext(_dispatcherQueue);
                    SynchronizationContext.SetSynchronizationContext(context);
                    _app = new App();
                });
            }
        }

        private static async Task<bool> DecideRedirection()
        {
            var runningInstance = AppInstance.FindOrRegisterForKey("PowerToys_Peek_Instance");
            var activatedEventArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

            var isRedirect = false;
            if (runningInstance.IsCurrent)
            {
                runningInstance.Activated += OnActivated;
            }
            else
            {
                if (CommandLineHelper.SerializeCommandLine())
                {
                    await runningInstance.RedirectActivationToAsync(activatedEventArgs);
                }

                isRedirect = true;
            }

            return isRedirect;
        }

        private static void OnActivated(object? sender, AppActivationArguments e)
        {
            _dispatcherQueue?.TryEnqueue(() => _app?.PreviewCommandLine());
        }
    }
}
