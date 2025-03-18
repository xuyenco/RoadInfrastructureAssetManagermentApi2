using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;

namespace Road_Infrastructure_Asset_Management.Interface
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
