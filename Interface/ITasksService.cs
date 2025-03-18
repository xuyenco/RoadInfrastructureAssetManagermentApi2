using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;

namespace Road_Infrastructure_Asset_Management.Interface
{
    public interface ITasksService
    {
        Task<IEnumerable<TasksResponse>> GetAllTasks();
        Task<TasksResponse?> GetTaskById(int id);
        Task<TasksResponse?> CreateTask(TasksRequest entity);
        Task<TasksResponse?> UpdateTask(int id, TasksRequest entity);
        Task<bool> DeleteTask(int id);
    }
}
