using AspNetCore.Identity.MongoDbCore.Models;
using System;

namespace OidCredentials.Models
{
    public class ApplicationRole : MongoIdentityRole<Guid>
    {
        public ApplicationRole()
        {
        }

        public ApplicationRole(string roleName) : base(roleName)
        {
        }
    }
}
