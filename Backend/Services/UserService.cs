using System;
using System.Collections.Generic;
using System.Linq;
using WebApi.Entities;
using WebApi.Helpers;

namespace WebApi.Services
{
    public interface IUserService
    {
        User Authenticate(string username, string password);
        IEnumerable<User> GetAll();
        User GetById(int id);
        User Create(User user, string password);
        void Update(User user, string password = null);
        void Delete(int id);
        IEnumerable<User> GetUsersFromGuild(int GuildId);
        IEnumerable<User> GetuserRanking();
        void UpdateGuildWithTotalXP(User userParam);
        int? getUserGuildIdFromDB(User userParam);
    }

    public class UserService : IUserService
    {
        private DataContext _context;

        public UserService(DataContext context)
        {
            _context = context;
        }

        public User Authenticate(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            var user = _context.Users.SingleOrDefault(x => x.Username == username);

            // check if username exists
            if (user == null)
                return null;

            // check if password is correct
            if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                return null;

            // authentication successful
            return user;
        }

        public IEnumerable<User> GetAll()
        {
            return _context.Users;
        }

        public User GetById(int id)
        {
            return _context.Users.Find(id);
        }

        public User Create(User user, string password)
        {
            // validation
            if (string.IsNullOrWhiteSpace(password))
                throw new AppException("Password is required");

            if (_context.Users.Any(x => x.Username == user.Username))
                throw new AppException("Username \"" + user.Username + "\" is already taken");

            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(password, out passwordHash, out passwordSalt);

            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            _context.Users.Add(user);
            _context.SaveChanges();

            return user;
        }

        public void Update(User userParam, string password = null)
        {
            var user = _context.Users.Find(userParam.Id);

            if (user == null)
                throw new AppException("User not found");

            // update username if it has changed
            if (!string.IsNullOrWhiteSpace(userParam.Username) && userParam.Username != user.Username)
            {
                // throw error if the new username is already taken
                if (_context.Users.Any(x => x.Username == userParam.Username))
                    throw new AppException("Username " + userParam.Username + " is already taken");

                user.Username = userParam.Username;
            }

            // update user properties if provided
            if (!string.IsNullOrWhiteSpace(userParam.Email))
                user.Email = userParam.Email;

            if (!string.IsNullOrWhiteSpace(userParam.Country))
                user.Country = userParam.Country;

            if (!string.IsNullOrWhiteSpace(userParam.City))
                user.City = userParam.City;

            if (!string.IsNullOrWhiteSpace(userParam.PhoneNumber))
                user.PhoneNumber = userParam.PhoneNumber;

            if (!string.IsNullOrWhiteSpace(userParam.Description))
                user.Description = userParam.Description;

            // update password if provided
            if (!string.IsNullOrWhiteSpace(password))
            {
                byte[] passwordHash, passwordSalt;
                CreatePasswordHash(password, out passwordHash, out passwordSalt);

                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;
            }

            if (userParam.GuildId.HasValue)
            {
                user.GuildId = userParam.GuildId;
            }

            _context.Users.Update(user);
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }
        }

        public IEnumerable<User> GetUsersFromGuild(int GuildId)
        {
            return _context.Users.Where(u => u.GuildId.Equals(GuildId));
        }

        public IEnumerable<User> GetuserRanking()
        {
            return _context.Users.OrderByDescending(x => x.TotalXP);
        }


        // private helper methods

        private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");

            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
            if (storedHash.Length != 64) throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "passwordHash");
            if (storedSalt.Length != 128) throw new ArgumentException("Invalid length of password salt (128 bytes expected).", "passwordHash");

            using (var hmac = new System.Security.Cryptography.HMACSHA512(storedSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != storedHash[i]) return false;
                }
            }

            return true;
        }

        public int? GetGuildXPTotal(int guildId)
        {
            int? sum = 0;
            var usersInGuild = _context.Users.Where(u => u.GuildId.Equals(guildId));
            foreach (var user in usersInGuild)
            {
                var workouts =  _context.Workouts.Where(w => w.userId.Equals(user.Id));
                foreach (var workout in workouts)
                    sum += workout.XP;
            }

            return sum;
        }

        public void UpdateGuildWithTotalXP(User userParam)
        {
            var guild = _context.Guilds.Find(userParam.GuildId);

            if (guild == null)
                throw new AppException("guild not found");

            // update guild total xp
            guild.TotalXP = GetGuildXPTotal(guild.Id);


            _context.Guilds.Update(guild);
            _context.SaveChanges();
        }

        public int? getUserGuildIdFromDB(User userParam)
        {
            var user = _context.Users.Find(userParam.Id);
            if (user != null)
            {
                return user.GuildId;
            }
            else
            {
                return -1;
            }
        }
    }
}