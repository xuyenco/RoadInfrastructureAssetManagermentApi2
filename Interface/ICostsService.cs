using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;

namespace Road_Infrastructure_Asset_Management_2.Interface
{
    public interface ICostsService
    {
        Task<IEnumerable<CostsResponse>> GetAllCosts();
        Task<CostsResponse?> GetCostById(int id);
        Task<CostsResponse?> CreateCost(CostsRequest entity);
        Task<CostsResponse?> UpdateCost(int id, CostsRequest entity);
        Task<bool> DeleteCost(int id);
    }
}
