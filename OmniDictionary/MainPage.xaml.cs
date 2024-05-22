namespace OmniDictionary
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = new DictViewModel();
            //SetPickerDefault();
            //searchBox.Focused += (sender, args) => searchBoxBorder.Stroke = Color.FromArgb("#cbd9f4");
            //searchBox.Unfocused += (sender, args) => searchBoxBorder.Stroke = Color.FromArgb("#000000");
  
        }

        public void SetPickerDefault()
        {
            langPicker.SelectedIndex = 0;
            dictPicker.SelectedIndex = 0;
        }


    }

}
