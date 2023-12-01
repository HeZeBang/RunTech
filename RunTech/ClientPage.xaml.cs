using System.Net;
using System.Text;
using System.Threading;

namespace RunTech;

public partial class ClientPage : ContentPage
{
    public string response { get; set; } = "";
    public double progress { get; set; } = 0.5;
    public ClientPage()
	{
		InitializeComponent();
        OnPropertyChanged(nameof(response));
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
            // 刷新任务已在运行中
            return;
        }

        // 取消之前的任务（如果存在）
        cancellationTokenSource?.Cancel();

        // 创建新的取消令牌
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

    private void Refresh()
    {
        bool flag = false;
        new System.Threading.Tasks.TaskFactory().StartNew(async () =>
        {
            response = await GetData($"http://{IPBar.Text}:8088/code");
            flag = true;
            OnPropertyChanged(nameof(response));
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
        catch (Exception ex){
            return ex.ToString();
        }
    }

}