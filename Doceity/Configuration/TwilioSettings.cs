// File: Configuration/TwilioSettings.cs
namespace Doceity.Configuration // O il tuo namespace preferito
{
    public class TwilioSettings
    {
        public const string SectionName = "Twilio"; // Corrisponde alla sezione in secrets.json

        public string AccountSid { get; set; } = string.Empty;
        public string ApiKeySid { get; set; } = string.Empty;
        public string ApiKeySecret { get; set; } = string.Empty;
    }
}