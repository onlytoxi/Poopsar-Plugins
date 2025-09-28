using System;
using System.Threading;

namespace Pulsar.Common.Extensions
{
    /// <summary>
    /// Provides helper extension methods for working safely with <see cref="CancellationTokenSource"/> instances.
    /// </summary>
    public static class CancellationTokenSourceExtensions
    {
        /// <summary>
        /// Attempts to cancel the <see cref="CancellationTokenSource"/> and swallows <see cref="ObjectDisposedException"/>.
        /// </summary>
        /// <param name="source">The token source to cancel.</param>
        public static void CancelSafe(this CancellationTokenSource source)
        {
            if (source == null)
                return;

            try
            {
                source.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The token source was already disposed; nothing more to do.
            }
        }

        /// <summary>
        /// Attempts to dispose the <see cref="CancellationTokenSource"/> and swallows <see cref="ObjectDisposedException"/>.
        /// </summary>
        /// <param name="source">The token source to dispose.</param>
        public static void DisposeSafe(this CancellationTokenSource source)
        {
            if (source == null)
                return;

            try
            {
                source.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // The token source was already disposed; nothing more to do.
            }
        }
    }
}
