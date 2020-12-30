namespace WebApi.Models.Users
{
  public class UserModel
    {
        public int Id { get; set; }

        public string Username { get; set; }

        public string Email { get; set; }

        public string Country { get; set; }

        public string City { get; set; }

        public string PhoneNumber { get; set; }

        public string Description { get; set; }

        public int? GuildId { get; set; }
    }
}