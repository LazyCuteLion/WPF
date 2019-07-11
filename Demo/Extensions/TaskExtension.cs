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
            if (!await Task.Run(() => { return task.Wait(timeout); }))
                throw new TimeoutException();
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
            if (await Task.Run(() => { return task.Wait(timeout); }))
                return task.Result;
            throw new TimeoutException();
        }

        /// <summary>
        /// 给“无法取消”的任务传入CancellationToken
        /// </summary>
        /// <param name="task">无法传入CancellationToken的任务</param>
        /// <param name="token">CancellationToken</param>
        /// <returns></returns>
        public static async Task WaitAsync(this Task task, CancellationToken token)
        {
            if (!await Task.Run(() => { try { return task.Wait(-1, token); } catch { return false; } }))
                throw new TaskCanceledException();
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
            if (await Task.Run(() => { try { return task.Wait(-1, token); } catch { return false; } }))
                return task.Result;
            throw new TaskCanceledException();
        }

        /// <summary>
        /// 封装“无法取消”的任务
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="task">无法传入CancellationToken的任务</param>
        /// <param name="watcher">可取消的无限等待任务[Task.Delay(-1,CancellationToken)]</param>
        /// <returns></returns>
        public static async Task<T> WaitAsync<T>(this Task<T> task, Task watcher)
        {
            var r = await Task.WhenAny(task, watcher);
            if (r == task)
            {
                return task.Result;
            }
            throw new TaskCanceledException();
        }


        public static CancellationTokenRegistration RegisterWidth<T>(this CancellationToken token, T state, Action<T> action)
        {
            return token.Register((p) => { action((T)p); }, state);
        }
    }
}
