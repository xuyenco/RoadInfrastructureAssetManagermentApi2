using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;

namespace Road_Infrastructure_Asset_Management.Interface
{
    public interface IBudgetsService
    {
        Task<IEnumerable<BudgetsResponse>> GetAllBudgets();
        Task<BudgetsResponse?> GetBudgetById(int id);
        Task<BudgetsResponse?> CreateBudget(BudgetsRequest entity);
        Task<BudgetsResponse?> UpdateBudget(int id, BudgetsRequest entity);
        Task<bool> DeleteBudget(int id);
    }
}
