using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniDictionary
{
    //This class exists purely to hold the relevant data from the JSON result which I get back from sozdik.kz, and thus its property-names need to match those of the JSON
    public class SozdikJSONResult
    {
        public DataJSON data {  get; set; }
        public string message { get; set; }
    }
    public class DataJSON
    {
        public string? translation { get; set; }
        //public string? synonyms { get; set; }
    }

}
