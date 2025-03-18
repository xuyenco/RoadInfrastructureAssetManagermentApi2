using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;

namespace Road_Infrastructure_Asset_Management.Interface
{
    public interface IAssetsService
    {
        Task<IEnumerable<AssetsResponse>> GetAllAssets();
        Task<AssetsResponse?> GetAssetById(int id);
        Task<AssetsResponse?> CreateAsset(AssetsRequest entity);
        Task<AssetsResponse?> UpdateAsset(int id, AssetsRequest entity);
        Task<bool> DeleteAsset(int id);
    }
}
