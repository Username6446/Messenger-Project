using System;

namespace Messenger_Project
{
    // ЗАГЛУШКА — модель користувача
    public class UserRecord
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string BirthDate { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public DateTime MemberSince { get; set; } = DateTime.Now;
    }
}
