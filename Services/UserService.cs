public class UserService
{
    public User? CurrentUser { get; private set; }
    public bool IsLoggedIn => CurrentUser != null;

    private readonly List<User> users = new();

    // REGISTER (NO AUTO LOGIN)
    public async Task<bool> RegisterAsync(string name, string email, string password)
    {
        // Check if email already exists
        if (users.Any(u => u.Email == email))
            return false;

        users.Add(new User
        {
            Name = name,
            Email = email,
            Password = password
        });

        return true; // ONLY register
    }

    // LOGIN
    public async Task<bool> LoginAsync(string email, string password)
    {
        var user = users.FirstOrDefault(u =>
            u.Email == email && u.Password == password);

        if (user == null)
            return false;

        CurrentUser = user; //  login happens HERE
        return true;
    }

    // LOGOUT
    public void Logout()
    {
        CurrentUser = null;
    }
}

// Simple User model
public class User
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}
