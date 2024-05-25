//using CommunityToolkit.Mvvm.ComponentModel;

using System.ComponentModel;
using System.Runtime.CompilerServices;
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
    
        /* -----------------------------------------------------------------------------------------------------------------------------------------*/
        //This entire section only exists so that I can access the value of the IsEnabled property of the searchbox element from within this class, which is only necessary because the IsEnabled property doesn't work right on Android and thus I can't rely just on disabling the searchbox directly with an event-handler in the MainPage.xaml.cs file (because on Android you can still submit searches on an IsEnabled=false SearchBox, because MAUI controls are half-baked crap that don't correctly map to their native equivalents)
        public DictViewModel()
        {
            Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
        }
        private void Connectivity_ConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            SearchBoxEnabled = (e.NetworkAccess == NetworkAccess.Internet);
            SearchBoxPlaceholder = SearchBoxEnabled ? "Search..." : "No internet - search unavailable";
        }

        private bool _searchBoxEnabled = (Connectivity.Current.NetworkAccess == NetworkAccess.Internet);
        public bool SearchBoxEnabled {
            get => _searchBoxEnabled;
            set 
            {
                _searchBoxEnabled = value;
                OnPropertyChanged(nameof(SearchBoxEnabled));
            } 
        }
        private string _searchBoxPlaceholder = (Connectivity.Current.NetworkAccess == NetworkAccess.Internet) ? "Search..." : "No internet - search unavailable";
        public string SearchBoxPlaceholder
        {
            get => _searchBoxPlaceholder;
            set
            {
                _searchBoxPlaceholder = value;
                OnPropertyChanged(nameof(SearchBoxPlaceholder));
            }
        }
        /* -----------------------------------------------------------------------------------------------------------------------------------------*/


        public List<string> LanguageNames
        {
            get => languages.Values.ToList();
        }

        static readonly List<Language> _langs = new List<Language>() {
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
                dictScraper.LangName = SelectedLanguage.Name;
                dictScraper.LangId = SelectedLanguage.LangId;
                
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
                dictScraper.DictIndex = allowable_dict_list[SelectedLanguage.LangId][SelectedDictIndex];
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

        List<DictResult> dict_results = new List<DictResult>();
        public List<DictResult> Results
        { 
            get => dict_results; 
            set {
                dict_results = value;
                OnPropertyChanged(nameof(Results));
            } 
        }

        private DictScraper dictScraper = new();
        public ICommand PerformSearch => new Command<string>(async (string dictQuery) => { 
            if(dictQuery.Trim() != string.Empty && SearchBoxEnabled) await FetchDictResultsAsync(dictQuery.Trim()); 
        });

        private async Task FetchDictResultsAsync(string dict_query)
        {
            dictScraper.UrlMaker(dict_query);
            Results = new List<DictResult>() { new DictResult(false, false, "Performing query...", "")};
            Results = await dictScraper.GetDictResultsAsync();
        }


        void OnPropertyChanged(string name)
        { 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        
    }
}
