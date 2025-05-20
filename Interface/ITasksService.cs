using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;

namespace Road_Infrastructure_Asset_Management_2.Interface
{
    public interface ITasksService
    {
        Task<IEnumerable<TasksResponse>> GetAllTasks();
        Task<TasksResponse?> GetTaskById(int id);
        Task<(IEnumerable<TasksResponse> Tasks, int TotalCount)> GetTasksPagination(int page, int pageSize, string searchTerm, int searchField);
        Task<TasksResponse?> CreateTask(TasksRequest entity);
        Task<TasksResponse?> UpdateTask(int id, TasksRequest entity);
        Task<bool> DeleteTask(int id);
    }
}
