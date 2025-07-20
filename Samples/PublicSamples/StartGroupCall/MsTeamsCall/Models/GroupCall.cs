namespace CseSample.Models
{
    public class GroupCall
    {
        public string TenantId { get; set; }
        public string[] ParticipantEmails { get; set; }

        public bool IsValid()
        {
            if(string.IsNullOrEmpty(TenantId)) return false;
            if(ParticipantEmails == null || ParticipantEmails.Length == 0) return false;
            
            return true;
        }
    }
}