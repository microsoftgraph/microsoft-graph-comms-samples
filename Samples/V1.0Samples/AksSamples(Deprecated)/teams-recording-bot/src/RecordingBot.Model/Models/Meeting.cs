// ***********************************************************************
// Assembly         : RecordingBot.Model
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 08-17-2020
// ***********************************************************************
// <copyright file="Meeting.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using System.Runtime.Serialization;

namespace RecordingBot.Model.Models
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
