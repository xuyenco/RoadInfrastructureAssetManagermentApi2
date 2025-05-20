using Road_Infrastructure_Asset_Management_2.Model.Response;
using Road_Infrastructure_Asset_Management_2.Model.Request;
using Road_Infrastructure_Asset_Management_2.Model.Response;

namespace Road_Infrastructure_Asset_Management_2.Interface
{
    public interface INotificationsService
    {
        Task<IEnumerable<NotificationResponse>> GetAllNotifications();
        Task<NotificationResponse?> GetNotificationById(int id);
        Task<IEnumerable<NotificationResponse>> GetAllNotificationsByUserId(int id);
        Task<NotificationResponse?> CreateNotification(NotificationRequest entity);
        Task<NotificationResponse?> UpdateNotification(int id, NotificationRequest entity);
        Task<bool> DeleteNotification(int id);
    }
}
