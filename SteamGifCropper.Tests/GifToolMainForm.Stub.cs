namespace GifProcessorApp;

public class GifToolMainForm
{
    public Label lblStatus { get; } = new();

    public class Label
    {
        public string Text { get; set; } = string.Empty;
    }
}

