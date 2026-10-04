namespace SyntaxAlter;

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
    }
    private void OnClearJqueryClicked(object? sender, EventArgs e)
    {
        string cleared = JQueryDependencyRemover.RemoveJQueryDependencies(JsBox.Text);
        NoJquery.Text = cleared;
    }
}