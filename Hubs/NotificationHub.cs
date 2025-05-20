using Microsoft.AspNetCore.SignalR;

namespace Road_Infrastructure_Asset_Management_2.Hubs
{
    public class NotificationHub : Hub
    {
        // Phương thức để client tham gia nhóm theo userId
        public async Task JoinGroup(int userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());
        }
        // Phương thức để client rời nhóm
        public async Task LeaveGroup(int userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId.ToString());
        }
        // Thêm phương thức Trigger
        public async Task Trigger(string eventName)
        {
            // Gửi sự kiện đến tất cả client
            await Clients.All.SendAsync(eventName);
        }
    }
}
