using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Threading.Tasks
{
    public static class TaskExtension
    {
        /// <summary>
        /// 异步等待
        /// 任务完成立即返回，否则在指定时间后抛出TimeoutException
        /// </summary>
        /// <param name="task"></param>
        /// <param name="timeout">等待时间（毫秒）</param>
        /// <returns></returns>
        public static async Task WaitAsync(this Task task, int timeout)
        {
            if (timeout <= 0)
                throw new ArgumentException("timeout 必须大于0");

            if (task.IsCompleted)
                await task;

            using (var cts = new CancellationTokenSource())
            {
                var r = await Task.WhenAny(task, Task.Delay(timeout, cts.Token)).ConfigureAwait(false);
                if (r == task)
                {
                    if (!cts.IsCancellationRequested)
                    {
                        cts.Cancel();
                    }
                }
                else
                {
                    throw new TimeoutException();
                }
            }
        }

        /// <summary>
        /// 异步等待
        /// 任务完成立即返回，否则在指定时间后抛出TimeoutException
        /// </summary>
        /// <param name="task"></param>
        /// <param name="timeout">等待时间（毫秒）</param>
        /// <returns></returns>
        public static async Task<T> WaitAsync<T>(this Task<T> task, int timeout)
        {
            if (timeout <= 0)
                throw new ArgumentException("timeout 必须大于0");

            if (task.IsCompleted)
                return task.Result;

            using (var cts = new CancellationTokenSource())
            {
                var r = await Task.WhenAny(task, Task.Delay(timeout, cts.Token)).ConfigureAwait(false);
                if (r == task)
                {
                    if (!cts.IsCancellationRequested)
                    {
                        cts.Cancel();
                    }
                    return task.Result;
                }
                else
                {
                    throw new TimeoutException();
                }
            }
        }

        /// <summary>
        /// 给“无法取消”的任务传入CancellationToken
        /// </summary>
        /// <param name="task">无法传入CancellationToken的任务</param>
        /// <param name="token">CancellationToken</param>
        /// <returns></returns>
        public static async Task WaitAsync(this Task task, CancellationToken token)
        {
            if (!token.CanBeCanceled)
                await task;

            if (token.IsCancellationRequested)
                throw new TimeoutException();

            var tcs = new TaskCompletionSource<bool>();
            var ctr = token.RegisterWidth(tcs, (s) => { s.TrySetResult(false); });
            var r = await Task.WhenAny(task, tcs.Task);
            ctr.Dispose();
            if (r != task)
            {
                throw new TimeoutException();
            }
        }

        /// <summary>
        /// 给“无法取消”的任务传入CancellationToken
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="task">无法传入CancellationToken的任务</param>
        /// <param name="token">CancellationToken</param>
        /// <returns></returns>
        public static async Task<T> WaitAsync<T>(this Task<T> task, CancellationToken token)
        {
            if (!token.CanBeCanceled)
                return await task;
            if (token.IsCancellationRequested)
                throw new TimeoutException();

            var tcs = new TaskCompletionSource<bool>();
            var ctr = token.RegisterWidth(tcs, (s) => { s.TrySetResult(false); });
            var r = await Task.WhenAny(task, tcs.Task);
            ctr.Dispose();
            if (r == task)
            {
                return task.Result;
            }
            else
            {
                throw new TimeoutException();
            }
        }

        public static CancellationTokenRegistration RegisterWidth<T>(this CancellationToken token, T state, Action<T> action)
        {
            return token.Register((p) => { action((T)p); }, state);
        }
    }
}
