namespace ShareX.Ava.Core.Tasks
{
    public interface IJobProcessor
    {
        Task ProcessAsync(TaskInfo info, CancellationToken token);
    }
}
