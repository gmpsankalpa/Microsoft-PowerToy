// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace Peek.UI.Services
{
    public class NamedPipeServer : IDisposable
    {
        private const string ToggleAction = "PowerToys-Peek-Pipe-Toggle-Message";
        private const string PreviewAction = "PowerToys-Peek-Pipe-Preview-Message";
        private readonly string _pipeName = $"PowerToys-Peek-Pipe-{WindowsIdentity.GetCurrent().User?.Value}";
        private readonly App _app;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly NamedPipeServerStream _server;
        private bool _disposed;

        public NamedPipeServer()
        {
            _app = (App.Current as App)!;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _server = new NamedPipeServerStream(_pipeName, PipeDirection.In, -1, PipeTransmissionMode.Message);
            var runningThread = new Thread(async () => await RunAsync());
            runningThread.IsBackground = true;
            runningThread.Start();
        }

        private async Task RunAsync()
        {
            using var streamReader = new StreamReader(_server);
            while (true)
            {
                _server.WaitForConnection();

                var message = await streamReader.ReadLineAsync();
                if (message != null)
                {
                    var messagePart = message.Split('|');
                    if (messagePart?.Length == 2)
                    {
                        var action = messagePart[0];
                        var path = messagePart[1];

                        if (action.Equals(ToggleAction, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(path))
                        {
                            _ = _dispatcherQueue.TryEnqueue(() => _app.OnToggleMessage(path));
                        }
                        else if (action.Equals(PreviewAction, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(path))
                        {
                            _ = _dispatcherQueue.TryEnqueue(() => _app.OnPreviewMessage(path));
                        }
                    }
                }

                _server.Disconnect();
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _server?.Dispose();
            }

            _disposed = true;
        }
    }
}
