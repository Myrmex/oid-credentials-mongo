using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using OidCredentials.Models;

namespace OidCredentials.Services
{
    public sealed class MongoDatabaseInitializer : IDatabaseInitializer
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public MongoDatabaseInitializer(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task Seed()
        {
            Serilog.Log.Information("Seeding users");

            const string email = "fake@nowhere.com";
            ApplicationUser user;
            if (await _userManager.FindByEmailAsync(email) == null)
            {
                // use the create rather than addorupdate so can set password
                user = new ApplicationUser
                {
                    UserName = "zeus",
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = "John",
                    LastName = "Doe"
                };
                await _userManager.CreateAsync(user, "P4ssw0rd!");
            }

            user = await _userManager.FindByEmailAsync(email);
            string roleName = "admin";
            if (await _roleManager.FindByNameAsync(roleName) == null)
                await _roleManager.CreateAsync(new ApplicationRole { Name = roleName });

            if (!await _userManager.IsInRoleAsync(user, roleName))
                await _userManager.AddToRoleAsync(user, roleName);
        }
    }
}
