using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;

namespace MinimalRazor.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    [Phone]
    public string? Phone { get; set; }
    [EmailAddress]
    public string? Email { get; set; }
    public string? Password { get; set; } 
    public string? Salt { get; set; }
    public string? Token { get; set; }
    public bool IsActive { get; set; }
    public DateTime? SignInDate { get; set; }
    public DateTime? SignOutDate { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    public int CreatedById { get; set; }
    public int UpdatedById { get; set; }

    public List<Role>? Roles { get; set; }
    [NotMapped] public bool IsAdmin => Roles?.Where(w => w.Id == 1).Count() > 0;
    [NotMapped] public string? CreatedBy { get; set; }
    [NotMapped] public string? UpdatedBy { get; set; }

    public override string ToString() => $"{Name}";
}

public class Role
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public List<User>? Users { get; set; }
    [NotMapped] public bool IsActive { get; set; }
}

public static class UserExtensions
{
    public static bool IsNullOrEmpty(this string? v) => string.IsNullOrEmpty(v);

    public static bool IsNullOrWhiteSpace(this string v) => string.IsNullOrWhiteSpace(v);

    public static bool IsAdmin(this IPrincipal? user) => user is not null && user.IsInRole("Admin");

    public static bool IsLogged(this IPrincipal? user) => user?.Identity?.IsAuthenticated ?? false;

    public static int GetId(this IPrincipal? user) => int.TryParse(user?.Identity?.Name, out int id) ? id : 0;

    public static string GetName(this ClaimsPrincipal? user)
        => user?.FindFirst(ClaimTypes.GivenName)?.Value ?? "Anonymous User";
    public static string? GetEmail(this ClaimsPrincipal? user)
        => user?.FindFirst(ClaimTypes.Email)?.Value;
    public static string? GetPhone(this ClaimsPrincipal? user)
        => user?.FindFirst(ClaimTypes.MobilePhone)?.Value;
    public static int GetMemberId(this ClaimsPrincipal? user)
        => int.TryParse(user?.FindFirst(ClaimTypes.Sid)?.Value, out var id) ? id : 0;
    public static User GetDetails(this ClaimsPrincipal? user) => new()
    {
        Id = user.GetId(),
        Name = user.GetName() ?? "Anonymous",
        Email = user.GetEmail(),
        Phone = user.GetPhone(),
    };

    public static List<Claim> ToClaims(this User user)
    {
		List<Claim> claims = [new(ClaimTypes.Name, user.Id.ToString()), new(ClaimTypes.GivenName, user.Name)];
		if (user.Email is not null)
			claims.Add(new(ClaimTypes.Email, user.Email));
		if (user.Phone is not null)
			claims.Add(new(ClaimTypes.MobilePhone, user.Phone));
		if (user.Roles?.Count > 0)
			foreach (var role in user.Roles)
				claims.Add(new Claim(ClaimTypes.Role, role.Name ?? role.Id.ToString()));
        return claims;
	}

    public static (string Password, string Salt) Encrypt(this string password, string? salt = null)
    {
        Span<byte> salted = salt is null ? stackalloc byte[16] : Convert.FromBase64String(salt);
        // generate a 128-bit salt using a secure PRNG
        if (string.IsNullOrEmpty(salt))
        {
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salted);
            salt = Convert.ToBase64String(salted);
        }
        // derive a 256-bit subkey (use HMACSHA1 with 10,000 iterations)
        var hashed = Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(password, salted.ToArray(),
            10000, HashAlgorithmName.SHA1, 32));

        return (hashed, salt);
    }
    
}