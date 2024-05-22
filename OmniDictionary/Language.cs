using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniDictionary
{
    public class Language
    {
        public Language(int lang_id, string name, List<string> allowable_dicts, List<string> logo_urls)
        {
            _lang_id = lang_id;
            _name = name;
            _allowable_dicts = allowable_dicts;
            _logo_urls = logo_urls;
        }
        readonly int _lang_id;
        readonly string _name;
        readonly List<string> _allowable_dicts;
        readonly List<string> _logo_urls;

        public int LangId { get => _lang_id; }
        public string Name { get => _name; }
        public List<string> AllowableDicts { get => _allowable_dicts; }
        public List<string> LogoURLs { get => _logo_urls; }
    }
}
