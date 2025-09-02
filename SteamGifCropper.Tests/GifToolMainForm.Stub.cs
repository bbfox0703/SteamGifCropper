using System.Collections.Generic;

namespace GifProcessorApp;

public class GifToolMainForm
{
    public Label lblStatus { get; } = new();
    public ProgressBar pBarTaskStatus { get; } = new();

    public class Label
    {
        public string Text { get; set; } = string.Empty;
    }

    public class ProgressBar
    {
        private int _value;
        public int Value
        {
            get => _value;
            set { _value = value; Values.Add(value); }
        }
        public int Minimum { get; set; }
        public int Maximum { get; set; }
        public List<int> Values { get; } = new();
    }
}

