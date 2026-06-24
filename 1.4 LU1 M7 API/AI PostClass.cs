namespace API_Data.Controllers
{
    public class TrashPostModel
    {
        public int CameraId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public string StartDate { get; set; }

        public double Confidence { get; set; }
        public int GarbageAmount { get; set; }
        public double DistanceToStadiumKm { get; set; }
        public int IsNACMatchDay { get; set; }
        public int IsHomeMatch { get; set; }
        public string GarbageType { get; set; }
        public string ExpectedCrowdLevel { get; set; }
    }
}