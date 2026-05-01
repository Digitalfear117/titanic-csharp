using System;
using System.Collections.Generic;
using System.Text;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class GetNotificationsRequest : APIRequest<List<NotificationModel>>
    {
        protected override List<NotificationModel> Execute(TitanicAPI api)
        {
            return api.GetList<NotificationModel>("/account/notifications");
        }
    }

    public class ConfirmAllNotificationsRequest : APIRequest<List<NotificationModel>>
    {
        protected override List<NotificationModel> Execute(TitanicAPI api)
        {
            return api.DeleteList<NotificationModel>("/account/notifications");
        }
    }

    public class ConfirmNotificationRequest : APIRequest<List<NotificationModel>>
    {
        public int NotificationId { get; set; }

        public ConfirmNotificationRequest(int notificationId)
        {
            NotificationId = notificationId;
        }

        protected override List<NotificationModel> Execute(TitanicAPI api)
        {
            return api.DeleteList<NotificationModel>($"/account/notifications/{NotificationId}");
        }
    }
}
