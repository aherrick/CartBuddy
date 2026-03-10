namespace CartBuddy;

public class LaunchCheckPage : ContentPage
{
    public LaunchCheckPage()
    {
        Title = "CartBuddy";
        BackgroundColor = Colors.White;
        Content = new Grid
        {
            Children =
            {
                new Label
                {
                    Text = "CartBuddy launched via Shell",
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = Colors.Black,
                    FontSize = 24,
                },
            },
        };
    }
}
