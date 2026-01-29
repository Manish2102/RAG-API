using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace BusinessLogicLayer.Models
{
    public class SearchIndexModel
    {
        [SimpleField(IsKey = true, IsFilterable = true)]

        public required string Id { get; set; }

        [SearchableField(IsFilterable = true)]
        public required string Content { get; set; }

        [SearchableField(IsFilterable = false, IsSortable = false, IsFacetable = false)]
        public required float[] ContentVector { get; set; }

    }
}
