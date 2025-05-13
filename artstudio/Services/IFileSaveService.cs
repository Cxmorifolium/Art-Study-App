
using System.Threading.Tasks;

namespace artstudio.Services
{
    // Create abstract class
    public interface IFileSaveService
    {
        Task SaveFileAsync(byte[] imageBytes, string fileName);
    }
}
