using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
namespace Road_Infrastructure_Asset_Management.Interface
{
    public interface IAssetCagetoriesService
    {
        Task<IEnumerable<AssetCagetoriesResponse>> GetAllAssetCagetories();
        Task<AssetCagetoriesResponse?> GetAssetCagetoriesByid(int id);
        Task<AssetCagetoriesResponse?> CreateAssetCagetories(AssetCagetoriesRequest entity);
        Task<AssetCagetoriesResponse?> UpdateAssetCagetories(int id, AssetCagetoriesRequest entity);
        Task<bool> DeleteAssetCagetories(int id);
    }
}
