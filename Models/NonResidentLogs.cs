namespace OCR_AccessControl.Models
{
    public class NonResidentLogs
    {
        public int id { get; set; }
        public string? full_name { get; set; }
        public string? id_type { get; set; }
        public string? id_number { get; set; }
        public DateTime? entry_time { get; set; } // Changed to DateTime
        public DateTime? exit_time { get; set; } // Changed to DateTime?
        public string? qr_code { get; set; }
    }
}