using System.Threading.Tasks;

namespace OidCredentials.Services
{
    public interface IDatabaseInitializer
    {
        Task Seed();
    }
}
