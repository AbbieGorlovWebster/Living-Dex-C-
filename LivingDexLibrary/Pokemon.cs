using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LivingDexLibrary
{
    public class Pokemon
    {

        [Key]
        public string name { get; set; }
        public string species { get; set; }
        public string? variantSuffix { get; set; }
        public string? firstType { get; set; }
        public string? SecondType { get; set; }

        public int speciesID { get; set; }
        public int variantID { get; set; }
        public int? displayID { get; set; }

        public bool? caught { get; set; } = false;
        public bool? canBeMale { get; set; } = false;
        public bool? canBeFemale { get; set; } = false;
        public bool? hasGenderDifferences { get; set; } = false;
        public bool? displayStatus { get; set; } = false;
    }
}