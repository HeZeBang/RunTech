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

        // 启动异步线程进行刷新
        refreshTask = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // 调用func()刷新数据

                while (progress > 0)
                {
                    progress -= 0.01;
                    OnPropertyChanged(nameof(progress));
                    await Task.Delay(10, cancellationToken);
                }
                progress = 1;
                Refresh();
            }
        }, cancellationToken);
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        cancellationTokenSource?.Cancel(); // 取消刷新任务
    }

    private async void Refresh()
    {
        response = await GetData($"http://{IPBar.Text}:8088/");
        OnPropertyChanged(nameof(response));
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
        catch {
            return "???";
        }
    }

    private void SearchBar_SearchButtonPressed(object sender, EventArgs e)
    {

    }
}