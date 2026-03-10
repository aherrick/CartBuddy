namespace CartBuddy;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState activationState) =>
        new(
            new ContentPage
            {
                Title = "CartBuddy",
                BackgroundColor = Colors.White,
                Content = new Grid
                {
                    Children =
                    {
                        new Label
                        {
                            Text = "CartBuddy launched",
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center,
                            TextColor = Colors.Black,
                            FontSize = 24,
                        },
                    },
                },
            }
        );
}
