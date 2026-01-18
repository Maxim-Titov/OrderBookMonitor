namespace OrderBookMonitor.Alerts
{
    public interface IAlertChannel
    {
        Task SendAsync(Alert alert);
    }
}