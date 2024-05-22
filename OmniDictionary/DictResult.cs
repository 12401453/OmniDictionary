
using static System.Net.Mime.MediaTypeNames;

namespace OmniDictionary
{
    public class DictResult
    {
        public DictResult(bool even_number, bool header=false, string column1="", string column2="", FormattedString? header_text = null) {
            this.Header = header;
            this.Column1 = column1;
            this.Column2 = column2;
            Header_Text = header_text;
            this.EvenNumber = even_number;
        }

        public bool Header { get; } = false;
        public string Column1 { get;} = string.Empty;
        public string Column2 { get; } = string.Empty;
        public FormattedString? Header_Text { get; }
        public bool EvenNumber { get; } = false;
    }
}
