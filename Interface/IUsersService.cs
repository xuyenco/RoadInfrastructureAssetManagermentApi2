using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;

namespace Road_Infrastructure_Asset_Management_2.Interface
{
    public interface IUsersService
    {
        Task<IEnumerable<UsersResponse>> GetAllUsers();
        Task<UsersResponse?> GetUserById(int id);
        Task<(IEnumerable<UsersResponse> Users, int TotalCount)> GetUsersPagination(int page, int pageSize, string searchTerm, int searchField);
        Task<UsersResponse?> CreateUser(UsersRequest entity);
        Task<UsersResponse?> UpdateUser(int id, UsersRequest entity);
        Task<bool> DeleteUser(int id);
        Task<UsersResponse?> Login(LoginRequest user);
        Task<UsersResponse?> RefreshToken(string refreshToken);
    }
}
