using Microsoft.AspNetCore.Identity;

// Generates an ASP.NET Core Identity PBKDF2 password hash for "Admin@123"
// so it can be embedded in the SQL seed-data script.
var hasher = new PasswordHasher<object>();
var hash = hasher.HashPassword(new object(), "Admin@123");
Console.WriteLine(hash);
