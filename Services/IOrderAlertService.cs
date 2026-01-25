using System.Threading.Tasks;

namespace SaintHub.Services
{
    public interface IOrderAlertService
    {
        Task TryNotifyNewOrderAsync(int orderId);
    }
}
