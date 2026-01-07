using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JournalApp.Models;

namespace JournalApp.Services
{
    public class UserService
    {
        private readonly List<User> _users = new();

        private User? _currentUser;

        public Task<bool> RegisterAsync(string username, string email, string password)
        {
            if (_users.Any(u => u.Email == email))
                return Task.FromResult(false); // email already exists

            var user = new User
            {
                Username = username,
                Email = email,
                Password = password // NOTE: hash in production
            };

            _users.Add(user);
            _currentUser = user;
            return Task.FromResult(true);
        }

        public Task<bool> LoginAsync(string email, string password)
        {
            var user = _users.FirstOrDefault(u => u.Email == email && u.Password == password);
            if (user != null)
            {
                _currentUser = user;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public void Logout()
        {
            _currentUser = null;
        }

        public User? GetCurrentUser() => _currentUser;
    }
}
