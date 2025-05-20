using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;

namespace Road_Infrastructure_Asset_Management_2.Interface
{
    public interface IAssetsService
    {
        Task<IEnumerable<AssetsResponse>> GetAllAssets();
        Task<AssetsResponse?> GetAssetById(int id);
        Task<(IEnumerable<AssetsResponse> Assets, int TotalCount)> GetAssetsPagination(int page, int pageSize, string searchTerm, int searchField);
        Task<AssetsResponse?> CreateAsset(AssetsRequest entity);
        Task<AssetsResponse?> UpdateAsset(int id, AssetsRequest entity);
        Task<bool> DeleteAsset(int id);
    }
}
