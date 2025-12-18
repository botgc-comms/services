namespace BOTGC.Mobile.Controls;

public partial class BottomNavBar : ContentView
{
    public event Action<string>? TabTapped;

    public static readonly BindableProperty SelectedKeyProperty =
        BindableProperty.Create(
            nameof(SelectedKey),
            typeof(string),
            typeof(BottomNavBar),
            "home",
            propertyChanged: (b, _, n) => ((BottomNavBar)b).ApplySelection((string)n));

    public string SelectedKey
    {
        get => (string)GetValue(SelectedKeyProperty);
        set => SetValue(SelectedKeyProperty, value);
    }

    public BottomNavBar()
    {
        InitializeComponent();
        ApplySelection(SelectedKey);
    }

    void OnHomeTapped(object sender, TappedEventArgs e) => SelectAndRaise("home");
    void OnProgressTapped(object sender, TappedEventArgs e) => SelectAndRaise("progress");
    void OnVouchersTapped(object sender, TappedEventArgs e) => SelectAndRaise("vouchers");
    void OnMentorTapped(object sender, TappedEventArgs e) => SelectAndRaise("mentor");
    void OnPlayTapped(object sender, TappedEventArgs e) => SelectAndRaise("play");

    void SelectAndRaise(string key)
    {
        SelectedKey = key;
        TabTapped?.Invoke(key);
    }

    void ApplySelection(string key)
    {
        var selectedText = Color.FromArgb("#434343");
        var unselectedText = Color.FromArgb("#434343");

        SetTab(HomeLabel, HomeIcon, key == "home", selectedText, unselectedText, "nav_home_orange.png", "nav_home.png", isBold: true);
        SetTab(ProgressLabel, ProgressIcon, key == "progress", selectedText, unselectedText, "nav_progress_orange.png", "nav_progress.png", isBold: false);
        SetTab(VouchersLabel, VouchersIcon, key == "vouchers", selectedText, unselectedText, "nav_vouchers_orange.png", "nav_vouchers.png", isBold: false);
        SetTab(MentorLabel, MentorIcon, key == "mentor", selectedText, unselectedText, "nav_mentor_orange.png", "nav_mentor.png", isBold: false);
        SetTab(PlayLabel, PlayIcon, key == "play", selectedText, unselectedText, "nav_play_orange.png", "nav_play.png", isBold: false);
    }

    static void SetTab(Label label, Image icon, bool selected, Color selectedText, Color unselectedText, string selectedSource, string unselectedSource, bool isBold)
    {
        label.TextColor = selected ? selectedText : unselectedText;
        label.FontAttributes = selected && isBold ? FontAttributes.Bold : FontAttributes.None;
        icon.Source = selected ? selectedSource : unselectedSource;
    }
}
