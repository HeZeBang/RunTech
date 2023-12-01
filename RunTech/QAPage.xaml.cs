namespace RunTech;

public partial class QAPage : ContentPage
{
	public QAPage()
	{
		InitializeComponent();
		versionLabel.Text = $"Current version: {VersionTracking.CurrentVersion}";

    }
}