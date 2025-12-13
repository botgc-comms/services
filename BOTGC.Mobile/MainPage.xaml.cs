namespace BOTGC.Mobile;

public partial class MainPage : ContentPage
{
	int count = 0;

	public MainPage()
	{
		InitializeComponent();
	}

	private void OnCounterClicked(object? sender, EventArgs e)
	{
		count++;

		if (count == 1)
			CounterBtn.Text = $"Clicked {count} time";
		else
			CounterBtn.Text = $"Clicked {count} times";

		SemanticScreenReader.Announce(CounterBtn.Text);
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Delay to simulate splash screen display for 3 seconds
        Task.Delay(3000).ContinueWith(t =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Navigate to the login page after splash screen
                Application.Current.MainPage = new LoginPage();
            });
        });
    }
}
