using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RecordingBot.Services.Util
{
    public abstract class BufferBase<T>
    {
        protected BufferBlock<T> Buffer;
        protected CancellationTokenSource TokenSource;
        protected bool IsRunning = false;
        private readonly SemaphoreSlim _syncLock = new(1);

        protected BufferBase()
        { }

        protected BufferBase(CancellationTokenSource token)
        {
            TokenSource = token;
        }

        public async Task Append(T obj)
        {
            if (!IsRunning)
            {
                await Start();
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

        private async Task Start()
        {
            await _syncLock.WaitAsync().ConfigureAwait(false);

            if (!IsRunning)
            {
                TokenSource ??= new CancellationTokenSource();

                Buffer = new BufferBlock<T>(new DataflowBlockOptions { CancellationToken = TokenSource.Token });
                await Task.Factory.StartNew(Process).ConfigureAwait(false);
                IsRunning = true;
            }

            _syncLock.Release();
        }

        private async Task Process()
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
                await Process().ConfigureAwait(false);
            }
        }

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

        protected abstract Task Process(T data);
    }
}
