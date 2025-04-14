using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;

namespace Road_Infrastructure_Asset_Management.Interface
{
    public interface IUsersService
    {
        Task<IEnumerable<UsersResponse>> GetAllUsers();
        Task<UsersResponse?> GetUserById(int id);
        Task<UsersResponse?> CreateUser(UsersRequest entity);
        Task<UsersResponse?> UpdateUser(int id, UsersRequest entity);
        Task<bool> DeleteUser(int id);
        Task<UsersResponse?> Login(LoginRequest user);
        Task<UsersResponse?> RefreshToken(string refreshToken);
    }
}
