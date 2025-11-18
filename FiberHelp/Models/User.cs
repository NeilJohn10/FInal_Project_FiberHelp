using System;

namespace FiberHelp.Models
{
 public class User
 {
 public string Id { get; set; } = Guid.NewGuid().ToString();
 public string Email { get; set; } = string.Empty;
 public string PasswordHash { get; set; } = string.Empty; // store hash
 public string Role { get; set; } = "Agent"; // Agent, Administrator, CSR
 public string FullName { get; set; } = string.Empty;
 public bool IsActive { get; set; } = true;
 }
}
