//using CommunityToolkit.Mvvm.ComponentModel;

using System.ComponentModel;
using System.Windows.Input;

namespace OmniDictionary
{
    public partial class DictViewModel : INotifyPropertyChanged/* : ObservableObject */
    {  
        public static readonly Dictionary<int, string> languages = new()
	    {
		    {1, "Russian"},
		    {2, "Kazakh"},
		    {3, "Polish"},
		    {4, "Bulgarian"},
		    {5, "German"},
		    {6, "Swedish"},
		    {7, "Turkish"},
		    {8, "Danish"},
		    {10, "Old English"}
	    };

        static readonly Dictionary<int, int[]> allowable_dict_list = new()
        {
            {1, [1, 0] },
            {2, [2, 1] },
            {3, [0, 1] },
            {4, [0, 1] },
            {5, [3, 0, 1] },
            {6, [0, 1] },
            {7, [0, 1] },
            {8, [0, 1, 3] },
            {10, [4, 1] }
        };

        int _lang_id = 5;
        string _name = "German";
        List<string> _allowable_dicts = ["dict.cc", "PONS.com", "Wiktionary"];

        List<string> language_names = new();
        public string Name
        {
            get => _name;
        }

        public List<string> LanguageNames
        {
            get => languages.Values.ToList();
        }

        static List<Language> _langs = new List<Language>() {
            new Language(1, "Russian", ["Wiktionary", "PONS.com"], ["enwiktionary_grey.png", "pons.png"]),
            new Language(2, "Kazakh", ["sozdik.kz", "Wiktionary"], ["sozdik.png", "enwiktionary_grey.png"]),    
            new Language(3, "Polish", ["PONS.com", "Wiktionary"], ["pons.png", "enwiktionary_grey.png"]),
            new Language(4, "Bulgarian", ["PONS.com", "Wiktionary"], ["pons.png", "enwiktionary_grey.png"]),
            new Language(5, "German", ["dict.cc", "PONS.com", "Wiktionary"], ["dictcc.png", "pons.png", "enwiktionary_grey.png"]),
            new Language(6, "Swedish", ["PONS.com", "Wiktionary"], ["pons.png", "enwiktionary_grey.png"]),
            new Language(7, "Turkish", ["PONS.com", "Wiktionary"], ["pons.png", "enwiktionary_grey.png"]),
            new Language(8, "Danish", ["PONS.com", "Wiktionary"], ["pons.png", "enwiktionary_grey.png"]),
            new Language(10, "Old English", ["Mitchell&Robinson Glossary", "Wiktionary"], ["mr_glossary.jpg", "enwiktionary_grey.png"])
        };

        public List<Language> Langs { get => _langs; }

        Language _selected_lang = _langs[4];
        public Language SelectedLanguage { 
            get => _selected_lang; 
            set 
            {
                _selected_lang = _langs[_selected_lang_index];
                OnPropertyChanged(nameof(SelectedLanguage));
            } 
        }
        int _selected_lang_index = 4;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int SelectedLangIndex { 
            get => _selected_lang_index;
            set
            {
                _selected_lang_index = value;
                Console.WriteLine("selected lang index changed");
                OnPropertyChanged(nameof(SelectedLangIndex));
         
            }
        }
        int _selected_dict_index = 0;
        public int SelectedDictIndex
        {
            get => _selected_dict_index;
            set
            {
                if(value > -1) _selected_dict_index = value;
                else _selected_dict_index = 0;
                OnPropertyChanged(nameof(SelectedDictIndex));
            }
        }
        string _selected_dict = _langs[4].AllowableDicts[0];
        public string SelectedDict
        {
            get => _selected_dict;
            set 
            {
                _selected_dict = SelectedLanguage.AllowableDicts[SelectedDictIndex];
                SelectedLogo = SelectedLanguage.LogoURLs[SelectedDictIndex];
                OnPropertyChanged(nameof(SelectedDict));
            }
        }

        string _selected_logo = _langs[4].LogoURLs[0];
        public string SelectedLogo
        {
            get => _selected_logo;
            set
            {
                _selected_logo = SelectedLanguage.LogoURLs[SelectedDictIndex]; //I'm setting the SelectedLogo property in the setter of the above property so I have no clue why these two lines are also needed for the logo to update
                OnPropertyChanged(nameof(SelectedLogo));
            }
        }

        List<DictResult> dict_results = new List<DictResult>() /*{ new DictResult(false, true, "<b>прицѐлвам се , прицѐля се</b> perf VERB intr", ""), new DictResult(true, false, "прицелвам се", "zielen"), new DictResult(false, false, "прицелвам се в нкг/нщ", "auf jdn/etw zielen"), new DictResult(true, false, "прицелвам се", "zielen"), new DictResult(false, false, "прицелвам се в нкг/нщ", "auf jdn/etw zielen"), new DictResult(true, false, "прицелвам се", "zielen"), new DictResult(false, false, "прицелвам се в нкг/нщ", "auf jdn/etw zielen"), new DictResult(true, false, "прицелвам се", "zielen"), new DictResult(false, false, "прицелвам се в нкг/нщ", "auf jdn/etw zielen"), new DictResult(true, false, "прицелвам се", "zielen"), new DictResult(false, false, "прицелвам се в нкг/нщ", "auf jdn/etw zielen"), new DictResult(true, false, "прицелвам се", "zielen"), new DictResult(false, false, "прицелвам се в нкг/нщ", "auf jdn/etw zielen"), new DictResult(true, false, "прицелвам се", "zielen"), new DictResult(false, false, "прицелвам се в нкг/нщ", "auf jdn/etw zielen"), new DictResult(true, false, "прицелвам се", "zielen"), new DictResult(true, false, "прицелвам се в нкг/нщ", "auf jdn/etw zielen"), new DictResult(false, false, "прицелвам се", "zielen"), new DictResult(true, false, "прицелвам се в нкг/нщ", "auf jdn/etw zielen"), new DictResult(false, false, "прицелвам се", "zielen"), new DictResult(true, false, "прицелвам се в нкг/нщ", "auf jdn/etw zielen"), new DictResult(false, false, "прицелвам се", "zielen"), new DictResult(false, false, "прицелвам се в нкг/нщ", "auf jdn/etw zielen"), new DictResult(true, false, "прицелвам се", "zielen"), new DictResult(false, false, "прицелвам се в нкг/нщ", "auf jdn/etw zielen"), new DictResult(true, false, "прицелвам се", "zielen"), new DictResult(false, false, "прицелвам се в нкг/нщ", "auf jdn/etw zielen"), new DictResult(true, false, "прицелвам се", "zielen"), new DictResult(false, false, "прицелвам се в нкг/нщ", "auf jdn/etw zielen")} */;
        public List<DictResult> Results
        { 
            get => dict_results; 
            set {
                dict_results = value;
                OnPropertyChanged(nameof(Results));
            } 
        }

        private DictScraper dictScraper = new();
        public ICommand PerformSearch => new Command<string>(async (string dictQuery) => await FetchDictResultsAsync(dictQuery));

        private async Task FetchDictResultsAsync(string dict_query)
        {
            dictScraper.UrlMaker(SelectedLanguage.LangId, allowable_dict_list[SelectedLanguage.LangId][SelectedDictIndex], dict_query);
            Results = new List<DictResult>() { new DictResult(false, false, "Performing query...", "")};
            Results = await dictScraper.GetDictResultsAsync();
        }


        void OnPropertyChanged(string name)
        { 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        
    }
}
