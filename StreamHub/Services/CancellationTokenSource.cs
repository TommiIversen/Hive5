namespace StreamHub.Services;

public class CancellationService
{
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    public CancellationToken Token => _cancellationTokenSource.Token;

    public void CancelOperations()
    {
        _cancellationTokenSource.Cancel();
    }

    public void ResetToken()
    {
        _cancellationTokenSource.Dispose();
        // Genopretter en ny token kilde hvis nødvendigt
    }
}