using Camera.MAUI;
using CommunityToolkit.Maui.Behaviors;
using EmbedIO;
using EmbedIO.Actions;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;




#if ANDROID
using Android.App;
using Android.Net.Wifi;
#endif

namespace RunTech;

public partial class ServerPage : ContentPage
{
    public string BarcodeText { get; set; } = "No barcode detected";

    public string Barcode { get; set; } = "";

    public bool playing { get; set; } = false;

    public ServerPage()
    {
        InitializeComponent();

#if ANDROID
        this.Behaviors.Add(new StatusBarBehavior
        {
            StatusBarColor = Color.FromHex("#d32f2f")
        });
#endif

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

        //var server = new WebServer(8088);

        var server = new WebServer(8088)//.WithMode(HttpListenerMode.EmbedIO))
                .WithModule(new ActionModule("/code", HttpVerbs.Any, ctx => ctx.SendDataAsync(new { code = Barcode, time = DateTime.Now.ToString(), version = VersionTracking.CurrentVersion })))
                .WithModule(new ActionModule("/", HttpVerbs.Any, ctx => ctx.SendStringAsync("<!DOCTYPE html><html><head><title>RunTech Web</title><script src='https://cdn.jsdelivr.net/npm/qrious@4.0.2/dist/qrious.min.js'></script></head><body><h1 style='text-align: center;'>RunTech</h1><div style=\"justify-content: center;align-items: center;display: flex;padding: 5%;flex-direction: column;\"><canvas style=\"background-color:#f2f2f2;border-radius:10px; padding:5px\"width='100px'id='qr'></canvas><p id='info'></p></div><div class='progress-bar'style=\"width:100%;height:30px;background-color:#f2f2f2;border-radius:5px;overflow:hidden;position:relative\"><div class='progress'style=\"width:100%;height:100%;background-color:#4caf50;position:absolute;left:0;top:0;transition:width 0.3s ease-in-out\"></div></div><script>function startProgressBar(){var progressBar=document.querySelector('.progress');var width=100;var interval=setInterval(decreaseProgress,500);function decreaseProgress(){width-=50;if(width<0){width=0;clearInterval(interval);try{fetch('/code').then(response=>response.json()).then(data=>{var qr=new QRious({element:document.getElementById('qr'),value:(data.code=='Please wait'?'':data.code)});document.getElementById('info').innerText=`Response:${data.code}\\nUpdated at ${data.time}\\nRunTech Version:${data.version}`})}catch(error){document.getElementById('info').innerText=`Error:${error.message}`}setTimeout(resetProgressBar,100)}progressBar.style.width=width+'%'}function resetProgressBar(){width=100;progressBar.style.width=width+'%';startProgressBar()}}startProgressBar();</script></body></html>"
                , "text/html", Encoding.UTF8)));
        server.HandleHttpException(async (context, exception) =>
        {
            context.Response.StatusCode = exception.StatusCode;

            switch (exception.StatusCode)
            {
                case 404:
                    await context.SendStringAsync("<h1>404 Not Found - From RunTech<h1/>", "text/html", Encoding.UTF8);
                    break;
                default:
                    await HttpExceptionHandler.Default(context, exception);
                    break;
            }
        });

        server.Start();
        this.Disappearing += (sender, e) =>
        {
            server.Dispose();
        };
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

#if ANDROID
        this.Behaviors.Add(new StatusBarBehavior
        {
            StatusBarColor = Color.FromHex("#00af6b")
        });
#endif

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
        string? nativeIp = addressList.FirstOrDefault(address => address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString();

#if ANDROID
        try
        {
            WifiManager wifiManager = (WifiManager)Android.App.Application.Context.GetSystemService(Service.WifiService);
            int ipaddress = wifiManager.ConnectionInfo.IpAddress; // Out of date in Android 12
            IPAddress ipAddr = new IPAddress(ipaddress);
            nativeIp = ipAddr.ToString();
        }
        catch (Exception ex)
        {
            return $"{nativeIp}\n获取IP出现错误\n在 Android 12 后的IP地址请求被废弃\n请尝试在系统设置中查询\n\n{ex.Message}";
        }
        
#endif
        if (nativeIp == null || nativeIp == "127.0.0.1")
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            string dhcpAddress = "127.0.0.1";
            foreach (var networkInterface in networkInterfaces)
            {
                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                    networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                {
                    var ipProperties = networkInterface.GetIPProperties();
                    var unicastAddresses = ipProperties.UnicastAddresses;
                    foreach (var unicastAddress in unicastAddresses)
                    {
                        if (unicastAddress.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                                return unicastAddress.Address.ToString(); // 优先级高
                            dhcpAddress = unicastAddress.Address.ToString();
                        }
                    }
                }
            }
            if (dhcpAddress == "127.0.0.1")
                return "未能获取到有效IP地址\n请尝试在系统设置中查询";
            else
                return dhcpAddress;
        }


        return nativeIp;
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        cameraView.Focus();
    }

    private async void cameraPicker_SelectedIndexChanged(object sender, EventArgs e)
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

            /*if (await cameraView.StopCameraAsync() == CameraResult.Success &&
                await cameraView.StartCameraAsync() == CameraResult.Success)
            {
                controlButton.Text = "Stop Camera";
                playing = true;
                return;
            }*/
        }
    }

}