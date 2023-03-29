// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 09-07-2020
// ***********************************************************************
// <copyright file="BufferBase.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RecordingBot.Services.Util
{
    /// <summary>
    /// Class BufferBase.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BufferBase<T>
    {
        /// <summary>
        /// The buffer
        /// </summary>
        protected BufferBlock<T> Buffer;
        /// <summary>
        /// The token source
        /// </summary>
        protected CancellationTokenSource TokenSource;
        /// <summary>
        /// The is running
        /// </summary>
        protected bool IsRunning = false;
        /// <summary>
        /// The synchronize lock
        /// </summary>
        private readonly SemaphoreSlim _syncLock = new SemaphoreSlim(1);

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferBase{T}" /> class.

        /// </summary>
        protected BufferBase()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferBase{T}" /> class.

        /// </summary>
        /// <param name="token">The token.</param>
        protected BufferBase(CancellationTokenSource token)
        {
            TokenSource = token;
        }

        /// <summary>
        /// Appends the specified object.
        /// </summary>
        /// <param name="obj">The object.</param>
        public async Task Append(T obj)
        {
            if (!IsRunning)
            {
                await _start();
            }

            try
            {
                await Buffer.SendAsync(obj, TokenSource.Token).ConfigureAwait(false);
            }
            catch (TaskCanceledException e)
            {
                Buffer?.Complete();

                Debug.Write($"Cannot enqueue because queuing operation has been cancelled. Exception: {e}");
            }
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        private async Task _start()
        {
            await this._syncLock.WaitAsync().ConfigureAwait(false);
            if (!IsRunning)
            {
                if (TokenSource == null) { TokenSource = new CancellationTokenSource(); }

                Buffer = new BufferBlock<T>(new DataflowBlockOptions { CancellationToken = this.TokenSource.Token });
                await Task.Factory.StartNew(this._process).ConfigureAwait(false);
                IsRunning = true;
            }
            this._syncLock.Release();
        }

        /// <summary>
        /// Ends this instance.
        /// </summary>
        public virtual async Task End()
        {  
            if (IsRunning)
            {
                await _syncLock.WaitAsync().ConfigureAwait(false);
                if (IsRunning)
                {
                    Buffer.Complete();
                    TokenSource = null;
                    IsRunning = false;
                }
                _syncLock.Release();
            }
        }

        /// <summary>
        /// Processes this instance.
        /// </summary>
        private async Task _process()
        {
            try
            {
                while (await Buffer.OutputAvailableAsync(TokenSource.Token).ConfigureAwait(false))
                {
                    T data = await Buffer.ReceiveAsync(TokenSource.Token).ConfigureAwait(false);

                    await Task.Run(() => Process(data)).ConfigureAwait(false);

                    TokenSource.Token.ThrowIfCancellationRequested();
                }
            }
            catch (TaskCanceledException ex)
            {
                Debug.Write(string.Format("The queue processing task has been cancelled. Exception: {0}", ex));
            }
            catch (ObjectDisposedException ex)
            {
                Debug.Write(string.Format("The queue processing task object has been disposed. Exception: {0}", ex));
            }
            catch (Exception ex)
            {
                // Catch all other exceptions and log
                Debug.Write(string.Format("Caught Exception: {0}", ex));

                // Continue processing elements in the queue
                await _process().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Processes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>Task.</returns>
        protected abstract Task Process(T data);

    }
}
