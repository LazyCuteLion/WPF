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
        /// 等待指定时间
        /// 任务完成立即返回，否则在指定时间后抛出错误
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
        /// 等待指定时间
        /// 任务完成立即返回，否则在指定时间后抛出错误
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

        public static async Task WhenAll(this IEnumerable<Task> tasks, int timeout, CancellationToken token)
        {
            var r = await Task.Run(() => { return Task.WaitAll(tasks.ToArray(), timeout, token); });
            if (!r)
                throw new Exception("某个task超时或被取消！");
        }
    }
}
