namespace AlgorithmApi.Models
{
    public class SortRequest
    {
        public int[] Numbers { get; set; }
    }

    public class SearchRequest
    {
        public int[] Numbers { get; set; }
        public int Target { get; set; }
    }
}
