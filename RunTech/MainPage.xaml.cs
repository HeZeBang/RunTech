using CommunityToolkit.Maui.Behaviors;

namespace RunTech
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
            this.Behaviors.Add(new StatusBarBehavior
            {
                StatusBarColor = Color.FromHex("#00af6b")
            });
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }

        private async void OnServerBtnClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ServerPage());
        }

        private async void OnClientBtnClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ClientPage());
        }

        private async void QABtn_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new QAPage());
        }
    }

}
