using Camera.MAUI;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using EmbedIO;
using EmbedIO.WebApi;
using EmbedIO.Actions;
using EmbedIO.Routing;
using System.Net.Http;
using System.Text;

namespace RunTech;

public partial class ServerPage : ContentPage
{
    public string BarcodeText { get; set; } = "No barcode detected";

    public string Barcode { get; set; } = "";

    public bool playing { get; set; } = false;
    public ServerPage()
    {
        InitializeComponent();

        OnPropertyChanged(nameof(BarcodeText));
        Barcode = "Please wait";
        OnPropertyChanged(nameof(Barcode));
        IPLbl.Text = GetIP();
        cameraView.CamerasLoaded += CameraView_CamerasLoaded;
        cameraView.BarcodeDetected += CameraView_BarcodeDetected;

        cameraView.BarCodeOptions = new Camera.MAUI.ZXingHelper.BarcodeDecodeOptions
        {
            AutoRotate = true,
            PossibleFormats = { ZXing.BarcodeFormat.QR_CODE },
            ReadMultipleCodes = false,
            TryHarder = true,
            TryInverted = true
        };
        cameraView.BarCodeDetectionFrameRate = 10;
        cameraView.BarCodeDetectionMaxThreads = 5;
        cameraView.ControlBarcodeResultDuplicate = true;
        cameraView.BarCodeDetectionEnabled = true;

        var server = new WebServer(8088);
        server.HandleHttpException(async (context, exception) =>
        {
            context.Response.StatusCode = exception.StatusCode;

            switch (exception.StatusCode)
            {
                case 404:
                    await context.SendStringAsync(Barcode, "text/html", Encoding.UTF8);
                    break;
                default:
                    await HttpExceptionHandler.Default(context, exception);
                    break;
            }
        });
        server.Start();
    }

    [Route(HttpVerbs.Get, "/ping")]
    public string TableTennis()
    {
        return "pong";
    }

    [Route(HttpVerbs.Get, "/qrcode")]
    public string WebGetQrcode()
    {
        // You will probably want to do something more useful than this.

        return Barcode;
    }
    private void Stepper_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (cameraView != null) cameraView.ZoomFactor = (float)e.NewValue;
        cameraView.Focus();
    }
    private void CameraView_BarcodeDetected(object sender, Camera.MAUI.ZXingHelper.BarcodeEventArgs args)
    {
        BarcodeText = $"Barcode at {DateTime.Now}: {args.Result[0].Text}";
        OnPropertyChanged(nameof(BarcodeText));
        Barcode = args.Result[0].Text;
        OnPropertyChanged(nameof(Barcode));
        Debug.WriteLine("BarcodeText=" + args.Result[0].Text);
    }

    private void CameraView_CamerasLoaded(object sender, EventArgs e)
    {
        cameraPicker.ItemsSource = cameraView.Cameras;
        cameraPicker.SelectedIndex = 0;
        if (cameraView.NumCamerasDetected > 0)
        {
            if (cameraView.NumMicrophonesDetected > 0)
                cameraView.Microphone = cameraView.Microphones.First();
            cameraView.Camera = cameraView.Cameras.First();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (!playing && await cameraView.StartCameraAsync() == CameraResult.Success)
                {
                    controlButton.Text = "Stop Camera";
                    playing = true;
                }
            });
        }
    }

    private void CameraView_ToggleStatus(object sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (!playing && await cameraView.StartCameraAsync() == CameraResult.Success)
            {
                controlButton.Text = "Stop Camera";
                cameraView.IsVisible = true;
                playing = true;
            }
            else if (playing && await cameraView.StopCameraAsync() == CameraResult.Success)
            {
                controlButton.Text = "Start Camera";
                cameraView.IsVisible = false;
                playing = false;
            }
        });
    }


    private void CopyIP(object sender, EventArgs e)
    {
        CopyButton.Text = "✅ 已复制";
        Clipboard.SetTextAsync(GetIP());
    }

	private string GetIP()
	{
        var addressList = Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList;
        string nativeIp = addressList.FirstOrDefault(address => address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString();
		if (nativeIp == null || nativeIp == "127.0.0.1")
            return "未能获取到有效IP地址\n请尝试在系统设置中查询";
		
        return nativeIp;
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        cameraView.Focus();
    }

    private void cameraPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (cameraPicker.SelectedItem != null && cameraPicker.SelectedItem is CameraInfo camera)
        {
            if (camera.MaxZoomFactor > 1)
            {
                zoomStepper.IsEnabled = true;
                zoomStepper.Maximum = camera.MaxZoomFactor;
            }
            else
                zoomStepper.IsEnabled = true;
            cameraView.Camera = camera;
        }
    }
}