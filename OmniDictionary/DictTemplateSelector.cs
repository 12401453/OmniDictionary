
namespace OmniDictionary
{
    public class DictTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DictHeaderTemplate { get; set; }
        public DataTemplate DictHeaderFormattedStringTemplate { get; set; }
        public DataTemplate DictResultSingleOddTemplate { get; set; }
        public DataTemplate DictResultDoubleOddTemplate { get; set; }
        public DataTemplate DictResultSingleEvenTemplate { get; set; }
        public DataTemplate DictResultDoubleEvenTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            if (((DictResult)item).Header_Text != null) return DictHeaderFormattedStringTemplate;
            else if (((DictResult)item).Header) return DictHeaderTemplate;
            else if (((DictResult)item).Column2 == string.Empty)  return ((DictResult)item).EvenNumber ? DictResultSingleEvenTemplate : DictResultSingleOddTemplate;
            else return ((DictResult)item).EvenNumber ? DictResultDoubleEvenTemplate : DictResultDoubleOddTemplate;
  
        }
    }
}
