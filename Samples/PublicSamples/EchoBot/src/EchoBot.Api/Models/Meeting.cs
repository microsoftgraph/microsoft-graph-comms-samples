using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace EchoBot.Api.Models
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
