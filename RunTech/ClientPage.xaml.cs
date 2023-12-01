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
            // ˢ����������������
            return;
        }

        // ȡ��֮ǰ������������ڣ�
        cancellationTokenSource?.Cancel();

        // �����µ�ȡ������
        cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        // �����첽�߳̽���ˢ��
        refreshTask = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // ����func()ˢ������

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
        cancellationTokenSource?.Cancel(); // ȡ��ˢ������
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