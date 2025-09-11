using System.Text.Json.Serialization;

namespace SharedKernel.Models
{
    public class MeetingRequest
    {
        public string UID { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public string CreatedDateTime { get; set; } = null!;
        public string StartDateTime { get; set; } = null!;
        public string EndDateTime { get; set; } = null!;
        public string? Location { get; set; }
        public int? AlarmMinutesBeforeMeeting { get; set; } = 15;
        public string? Description { get; set; }
        public bool? IsDescriptionHtml { get; set; } = false;
        public List<MailAttendee> To { get; set; } = null!;
        public List<MailAttendee>? Cc { get; set; }
        public bool IsMeetingCancel { get; set; }
        public string? OrganizerEmail { get; set; }

        [JsonIgnore]
        public string Method
        {
            get
            { return IsMeetingCancel ? "CANCEL" : "REQUEST"; }
        }

        [JsonIgnore]
        public string Status
        {
            get { return IsMeetingCancel ? "CANCELLED" : "CONFIRMED"; }
        }

        [JsonIgnore]
        public string? ICSContent
        {
            get
            {
                return IsMeetingCancel ?
@$"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//YourOrganization//NONSGML v1.0//EN
CALSCALE:GREGORIAN
METHOD:{Method}
BEGIN:VEVENT
UID:{UID}
SEQUENCE:1
STATUS:{Status}
DTSTAMP:{Convert.ToDateTime(CreatedDateTime):yyyyMMddTHHmmssZ}
DTSTART:{Convert.ToDateTime(StartDateTime):yyyyMMddTHHmmssZ} 
SUMMARY:{Subject}
DESCRIPTION:{Description}
{(Location != null ? $"LOCATION:{Location}" : string.Empty)}
{(OrganizerEmail != null ? $"ORGANIZER;CN=Organizer Name:mailto:{OrganizerEmail}" : string.Empty)}
ATTENDEE;RSVP=TRUE:ROLE=REQ-PARTICIPANT:mailto:{string.Join(",", To.Select(a => a.Email))}
{(Cc?.Count > 0 ? $"ATTENDEE;RSVP=TRUE;ROLE=OPT-PARTICIPANT:mailto:{string.Join(",", Cc.Select(a => a.Email))}" : string.Empty)}
END:VEVENT
END:VCALENDAR"
    :
@$"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//YourOrganization//NONSGML v1.0//EN
CALSCALE:GREGORIAN
METHOD:REQUEST
BEGIN:VEVENT
UID:{UID}
SEQUENCE:1
STATUS:CONFIRMED
DTSTAMP:{Convert.ToDateTime(CreatedDateTime):yyyyMMddTHHmmssZ}
DTSTART:{Convert.ToDateTime(StartDateTime):yyyyMMddTHHmmssZ} 
DTEND:{Convert.ToDateTime(EndDateTime):yyyyMMddTHHmmssZ}
SUMMARY:{Subject}
DESCRIPTION:{Description}
{(Location != null ? $"LOCATION:{Location}" : string.Empty)}
ORGANIZER;CN=Organizer Name:mailto:{OrganizerEmail}
ATTENDEE;RSVP=TRUE:mailto:{string.Join(",", To.Select(a => a.Email))}
{(Cc?.Count > 0 ? $"ATTENDEE;RSVP=TRUE;ROLE=OPT-PARTICIPANT:mailto:{string.Join(",", Cc.Select(a => a.Email))}" : string.Empty)}
BEGIN:VALARM
TRIGGER:-PT{AlarmMinutesBeforeMeeting}M
ACTION:DISPLAY
DESCRIPTION:Reminder: The meeting is starting in {AlarmMinutesBeforeMeeting} minutes.
END:VALARM
END:VEVENT
END:VCALENDAR";
            }
        }

    }

    public class MailAttendee
    {
        public string DisplayName { get; set; } = null!;
        public string Email { get; set; } = null!;
    }
}
