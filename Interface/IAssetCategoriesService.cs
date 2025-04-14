using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;
namespace Road_Infrastructure_Asset_Management_2.Interface
{
    public interface IAssetCategoriesService
    {
        Task<IEnumerable<AssetCategoriesResponse>> GetAllAssetCategories();
        Task<AssetCategoriesResponse?> GetAssetCategoriesById(int id);
        Task<AssetCategoriesResponse?> CreateAssetCategories(AssetCategoriesRequest entity);
        Task<AssetCategoriesResponse?> UpdateAssetCategories(int id, AssetCategoriesRequest entity);
        Task<bool> DeleteAssetCategories(int id);
    }
}
