using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using FurEver_Home.Models;

namespace FurEver_Home.Services
{
	public class RoleService : IDisposable
	{
		private readonly FurEverHomeContext _db;
		private readonly bool _disposeContext;
		private bool _disposed;

		/// <summary>
		/// Constructor for use with dependency injection or when you want RoleService to manage its own context
		/// </summary>
		public RoleService()
		{
			_db = new FurEverHomeContext();
			_disposeContext = true; // We created it, so we'll dispose it                                                                   
		}

		/// <summary>
		/// Constructor for sharing an existing DbContext (recommended for controllers)
		/// </summary>
		public RoleService(FurEverHomeContext db)
		{
			_db = db ?? throw new ArgumentNullException(nameof(db));
			_disposeContext = false; // Context is managed externally
		}

		/// <summary>
		/// Get all active roles for a user with proper navigation property loading.
		/// Returns role names (case preserved).
		/// </summary>
		public List<string> GetUserRoles(int userId)
		{
			return _db.UserRoles
				.Include(ur => ur.Role)
				.Where(ur => ur.UserId == userId && ur.IsActive && ur.Role != null)
				.Select(ur => ur.Role.RoleName)
				.ToList();
		}

		/// <summary>
		/// Check if user has a specific role (case-insensitive).
		/// </summary>
		public bool HasRole(int userId, string roleName)
		{
			if (string.IsNullOrWhiteSpace(roleName))
				return false;

			return _db.UserRoles
				.Include(ur => ur.Role)
				.Any(ur => ur.UserId == userId
						   && ur.IsActive
						   && ur.Role != null
						   && ur.Role.RoleName.Equals(roleName.Trim(), StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Check if user has any of the specified roles (case-insensitive).
		/// </summary>
		public bool HasAnyRole(int userId, params string[] roleNames)
		{
			if (roleNames == null || roleNames.Length == 0)
				return false;

			var normalized = roleNames
				.Where(r => !string.IsNullOrWhiteSpace(r))
				.Select(r => r.Trim())
				.ToArray();

			if (!normalized.Any())
				return false;

			var userRoles = GetUserRoles(userId);
			return userRoles.Any(ur => normalized.Any(rn => string.Equals(ur, rn, StringComparison.OrdinalIgnoreCase)));
		}

		public bool IsSuperAdmin(int userId) => HasRole(userId, "Super Admin");
		public bool IsModerator(int userId) => HasRole(userId, "Moderator");
		public bool IsSupport(int userId) => HasRole(userId, "Support");

		/// <summary>
		/// Assign a role to a user with validation (by role id).
		/// Caller should ensure assigner has permission to assign roles.
		/// </summary>
		public RoleAssignmentResult AssignRole(int userId, int roleId, int assignedBy)
		{
			try
			{
				var user = _db.Users.Find(userId);
				if (user == null) return RoleAssignmentResult.Failed("User not found");

				var role = _db.Roles.Find(roleId);
				if (role == null) return RoleAssignmentResult.Failed("Role not found");
				if (!role.IsActive) return RoleAssignmentResult.Failed("Role is inactive and cannot be assigned");

				var assigner = _db.Users.Find(assignedBy);
				if (assigner == null) return RoleAssignmentResult.Failed("Assigner user not found");

				var existing = _db.UserRoles.FirstOrDefault(ur => ur.UserId == userId && ur.RoleId == roleId);

				if (existing != null)
				{
					if (existing.IsActive) return RoleAssignmentResult.Failed("User already has this role assigned");

					existing.IsActive = true;
					existing.AssignedAt = DateTime.Now;
					existing.AssignedBy = assignedBy;
				}
				else
				{
					var userRole = new UserRoles
					{
						UserId = userId,
						RoleId = roleId,
						AssignedAt = DateTime.Now,
						AssignedBy = assignedBy,
						IsActive = true
					};
					_db.UserRoles.Add(userRole);
				}

				_db.SaveChanges();
				return RoleAssignmentResult.Success($"Role '{role.RoleName}' assigned successfully");
			}
			catch (Exception ex)
			{
				// Consider logging ex
				return RoleAssignmentResult.Failed($"Error assigning role: {ex.Message}");
			}
		}

		/// <summary>
		/// Assign role by role name (case-insensitive).
		/// </summary>
		public RoleAssignmentResult AssignRole(int userId, string roleName, int assignedBy)
		{
			if (string.IsNullOrWhiteSpace(roleName))
				return RoleAssignmentResult.Failed("Role name required");

			var role = _db.Roles.FirstOrDefault(r => r.IsActive && r.RoleName.Equals(roleName.Trim(), StringComparison.OrdinalIgnoreCase));
			if (role == null) return RoleAssignmentResult.Failed("Role not found");
			return AssignRole(userId, role.RoleId, assignedBy);
		}

		/// <summary>
		/// Remove a role from a user (by id).
		/// </summary>
		public RoleAssignmentResult RemoveRole(int userId, int roleId)
		{
			try
			{
				var userRole = _db.UserRoles
					.Include(ur => ur.Role)
					.FirstOrDefault(ur => ur.UserId == userId && ur.RoleId == roleId);

				if (userRole == null) return RoleAssignmentResult.Failed("Role assignment not found");
				if (!userRole.IsActive) return RoleAssignmentResult.Failed("Role is already inactive");

				userRole.IsActive = false;
				_db.SaveChanges();

				var roleName = userRole.Role?.RoleName ?? "Unknown";
				return RoleAssignmentResult.Success($"Role '{roleName}' removed successfully");
			}
			catch (Exception ex)
			{
				// Consider logging ex
				return RoleAssignmentResult.Failed($"Error removing role: {ex.Message}");
			}
		}

		/// <summary>
		/// Remove a role from a user by role name (case-insensitive).
		/// </summary>
		public RoleAssignmentResult RemoveRole(int userId, string roleName)
		{
			if (string.IsNullOrWhiteSpace(roleName))
				return RoleAssignmentResult.Failed("Role name required");

			var role = _db.Roles.FirstOrDefault(r => r.IsActive && r.RoleName.Equals(roleName.Trim(), StringComparison.OrdinalIgnoreCase));
			if (role == null) return RoleAssignmentResult.Failed("Role not found");
			return RemoveRole(userId, role.RoleId);
		}

		/// <summary>
		/// Get all available roles
		/// </summary>
		public List<Role> GetAllRoles()
		{
			return _db.Roles
				.Where(r => r.IsActive)
				.OrderBy(r => r.RoleName)
				.ToList();
		}

		/// <summary>
		/// Get structured role permissions (instead of just string description)
		/// </summary>
		public RolePermissions GetRolePermissions(string roleName)
		{
			if (string.IsNullOrWhiteSpace(roleName))
			{
				return new RolePermissions();
			}

			switch (roleName.Trim())
			{
				case "Super Admin":
					return new RolePermissions
					{
						RoleName = "Super Admin",
						Description = "Full system access - Can manage users, pets, applications, and assign roles",
						CanManageUsers = true,
						CanToggleUserStatus = true,
						CanAssignRoles = true,
						CanManagePets = true,
						CanAddPets = true,
						CanEditPets = true,
						CanDeletePets = true,
						CanVerifyIDs = true,
						CanApprovePetPosts = true,
						CanManageApplications = true,
						CanApproveApplications = true,
						CanRejectApplications = true,
						CanViewReports = true,
						CanViewHistory = true,
						CanAccessAllFeatures = true
					};

				case "Moderator":
					return new RolePermissions
					{
						RoleName = "Moderator",
						Description = "Can verify IDs, approve pet posts, and manage adoption applications",
						CanManageUsers = false,
						CanToggleUserStatus = false,
						CanAssignRoles = false,
						CanManagePets = false,
						CanAddPets = false,
						CanEditPets = false,
						CanDeletePets = false,
						CanVerifyIDs = true,
						CanApprovePetPosts = true,
						CanManageApplications = true,
						CanApproveApplications = true,
						CanRejectApplications = true,
						CanViewReports = true,
						CanViewHistory = true,
						CanAccessAllFeatures = false
					};

				case "Support":
					return new RolePermissions
					{
						RoleName = "Support",
						Description = "Read-only access - Can view users and applications but cannot modify",
						CanManageUsers = false,
						CanToggleUserStatus = false,
						CanAssignRoles = false,
						CanManagePets = false,
						CanAddPets = false,
						CanEditPets = false,
						CanDeletePets = false,
						CanVerifyIDs = false,
						CanApprovePetPosts = false,
						CanManageApplications = false,
						CanApproveApplications = false,
						CanRejectApplications = false,
						CanViewReports = true,
						CanViewHistory = true,
						CanAccessAllFeatures = false
					};

				default:
					return new RolePermissions
					{
						RoleName = roleName,
						Description = "No administrative permissions"
					};
			}
		}

		public string GetRoleDescription(string roleName)
		{
			return GetRolePermissions(roleName).Description;
		}

		public List<User> GetUsersWithRole(string roleName)
		{
			if (string.IsNullOrWhiteSpace(roleName)) return new List<User>();

			return _db.UserRoles
				.Include(ur => ur.Role)
				.Include(ur => ur.User)
				.Where(ur => ur.IsActive && ur.Role != null && ur.Role.RoleName.Equals(roleName.Trim(), StringComparison.OrdinalIgnoreCase))
				.Select(ur => ur.User)
				.Distinct()
				.ToList();
		}

		public List<UserRoles> GetUserRoleAssignments(int userId, bool includeInactive = false)
		{
			var query = _db.UserRoles
				.Include(ur => ur.Role)
				.Where(ur => ur.UserId == userId);

			if (!includeInactive)
			{
				query = query.Where(ur => ur.IsActive);
			}

			return query.OrderByDescending(ur => ur.AssignedAt).ToList();
		}

		#region IDisposable
		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing && _disposeContext)
				{
					_db?.Dispose();
				}
				_disposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}

	public class RoleAssignmentResult
	{
		public bool IsSuccess { get; set; }
		public string Message { get; set; }

		public static RoleAssignmentResult Success(string message)
		{
			return new RoleAssignmentResult
			{
				IsSuccess = true,
				Message = message
			};
		}

		public static RoleAssignmentResult Failed(string message)
		{
			return new RoleAssignmentResult
			{
				IsSuccess = false,
				Message = message
			};
		}
	}

	public class RolePermissions
	{
		public string RoleName { get; set; }
		public string Description { get; set; }

		// User Management
		public bool CanManageUsers { get; set; }
		public bool CanToggleUserStatus { get; set; }
		public bool CanAssignRoles { get; set; }

		// Pet Management
		public bool CanManagePets { get; set; }
		public bool CanAddPets { get; set; }
		public bool CanEditPets { get; set; }
		public bool CanDeletePets { get; set; }

		// Verification & Approval
		public bool CanVerifyIDs { get; set; }
		public bool CanApprovePetPosts { get; set; }

		// Application Management
		public bool CanManageApplications { get; set; }
		public bool CanApproveApplications { get; set; }
		public bool CanRejectApplications { get; set; }

		// Reporting & History
		public bool CanViewReports { get; set; }
		public bool CanViewHistory { get; set; }

		// Global
		public bool CanAccessAllFeatures { get; set; }
	}
}