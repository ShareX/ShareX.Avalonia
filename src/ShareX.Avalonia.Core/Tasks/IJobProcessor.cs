using System;
using System.Threading.Tasks;
using System.Threading;
using ShareX.Ava.Core;

namespace ShareX.Ava.Core.Tasks
{
    public interface IJobProcessor
    {
        Task ProcessAsync(TaskInfo info, CancellationToken token);
    }
}
