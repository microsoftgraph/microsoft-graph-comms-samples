using System.Runtime.Serialization;

namespace EchoBot.Models
{
    /// <summary>
    /// Join URL context.
    /// </summary>
    [DataContract]
    public class Meeting
    {
        /// <summary>
        /// Gets or sets the Tenant Id.
        /// </summary>
        /// <value>The tid.</value>
        [DataMember]
        public string Tid { get; set; }

        /// <summary>
        /// Gets or sets the AAD object id of the user.
        /// </summary>
        /// <value>The oid.</value>
        [DataMember]
        public string Oid { get; set; }

        /// <summary>
        /// Gets or sets the chat message id.
        /// </summary>
        /// <value>The message identifier.</value>
        [DataMember]
        public string MessageId { get; set; }
    }
}
