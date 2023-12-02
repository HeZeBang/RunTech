using System.Text.Json;

namespace RunTech;

public partial class ClientPage : ContentPage
{
    public string response { get; set; } = "";
    public string detail { get; set; } = "Wating for response...";
    public double progress { get; set; } = 0.5;
    public ClientPage()
    {
        InitializeComponent();
        OnPropertyChanged(nameof(response));
        OnPropertyChanged(nameof(detail));
        this.Disappearing += ClientPage_Disappearing;
    }

    private void ClientPage_Disappearing(object? sender, EventArgs e)
    {
        try
        {
            cancellationTokenSource?.Cancel();
        }
        catch { cancellationTokenSource?.Dispose(); }
    }

    private CancellationTokenSource cancellationTokenSource;
    private Task refreshTask;
    private void GetButton_Clicked(object sender, EventArgs e)
    {
        if (refreshTask != null && !refreshTask.IsCompleted)
        {
            return;
        }

        cancellationTokenSource?.Cancel();

        cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        refreshTask = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {

                while (progress > 0)
                {
                    progress -= 0.01;
                    OnPropertyChanged(nameof(progress));
                    await Task.Delay(5, cancellationToken);
                }
                progress = 1;
                Refresh();
            }
        }, cancellationToken);
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        cancellationTokenSource?.Cancel();
    }

    public class CodeData
    {
        public string code { get; set; }
        public string time { get; set; }
        public string version { get; set; }
    }

    private void Refresh()
    {
        bool flag = false;
        new System.Threading.Tasks.TaskFactory().StartNew(async () =>
        {
            try
            {
                response = await GetData($"http://{IPBar.Text}:8088/code");
                flag = true;
                var items = JsonSerializer.Deserialize<CodeData>(response);
                response = items.code;
                OnPropertyChanged(nameof(response));
                detail = $"Response: {items.code}\nAt: {items.time}\nServer version: {items.version}";
                OnPropertyChanged(nameof(detail));
            }
            catch { }
        }).Wait(1000);

        if (!flag)
        {
            response = "TIMEOUT";
            flag = true;
        }
    }

    public async Task<string> GetData(string url)
    {
        try
        {
            using HttpClient client = new();
            var res = await client.GetAsync(url);
            string result = await res.Content.ReadAsStringAsync();
            return result;
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }

}