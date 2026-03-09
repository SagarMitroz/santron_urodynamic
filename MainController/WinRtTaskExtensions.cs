using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace SantronWinApp
{
    internal static class WinRtTaskExtensions
    {
        public static Task<T> ToTask<T>(this Windows.Foundation.IAsyncOperation<T> op)
        {
            if (op == null) throw new ArgumentNullException(nameof(op));

            var tcs = new TaskCompletionSource<T>();

            op.Completed = (asyncInfo, status) =>
            {
                try
                {
                    switch (status)
                    {
                        case AsyncStatus.Completed:
                            tcs.TrySetResult(asyncInfo.GetResults());
                            break;
                        case AsyncStatus.Error:
                            tcs.TrySetException(asyncInfo.ErrorCode ?? new Exception("WinRT async error"));
                            break;
                        case AsyncStatus.Canceled:
                            tcs.TrySetCanceled();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            };

            return tcs.Task;
        }

        public static Task ToTask(this Windows.Foundation.IAsyncAction op)
        {
            if (op == null) throw new ArgumentNullException(nameof(op));

            var tcs = new TaskCompletionSource<object>();

            op.Completed = (asyncInfo, status) =>
            {
                try
                {
                    switch (status)
                    {
                        case AsyncStatus.Completed:
                            tcs.TrySetResult(null);
                            break;
                        case AsyncStatus.Error:
                            tcs.TrySetException(asyncInfo.ErrorCode ?? new Exception("WinRT async error"));
                            break;
                        case AsyncStatus.Canceled:
                            tcs.TrySetCanceled();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            };

            return tcs.Task;
        }
    }
}
