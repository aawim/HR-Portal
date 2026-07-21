namespace HRM.DTOs
{
    // This model handles the search bar inputs for ANY search page
    public class SearchModel
    {
        public string SearchTerm { get; set; } = string.Empty;
        public string SearchType { get; set; } = "Name";

        // This is specifically used by the Staff Search page to filter by Org
        public int? OrganisationId { get; set; }
    }

    // This handles any generic dropdown list (like Countries, Types, Organisations)
    public class DropdownItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }


}