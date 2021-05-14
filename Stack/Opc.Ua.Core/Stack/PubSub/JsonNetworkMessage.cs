/* Copyright (c) 1996-2020 The OPC Foundation. All rights reserved.
   The source code in this file is covered under a dual-license scenario:
     - RCL: for OPC Foundation members in good-standing
     - GPL V2: everybody else
   RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/
   GNU General Public License as published by the Free Software Foundation;
   version 2 of the License are accompanied with this source code. See http://opcfoundation.org/License/GPLv2
   This source code is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
*/



using System.Collections.Generic;
using System.IO;

namespace Opc.Ua.PubSub
{
    /// <summary>
    /// 
    /// </summary>
    public class JsonNetworkMessage
    {
        /// <summary>
        /// 
        /// </summary>
        public JsonNetworkMessage()
        {
            MessageContentMask =
                JsonNetworkMessageContentMask.NetworkMessageHeader |
                JsonNetworkMessageContentMask.DataSetMessageHeader |
                JsonNetworkMessageContentMask.PublisherId |
                JsonNetworkMessageContentMask.DataSetClassId;

            Messages = new List<JsonDataSetMessage>();
            MessageType = "ua-data";
        }
        /// <summary>
        /// 
        /// </summary>
        public JsonNetworkMessageContentMask MessageContentMask { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string MessageId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string MessageType { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public string PublisherId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string DataSetClassId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ReplyTo { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<JsonDataSetMessage> Messages { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public virtual IJsonEncoder CreateEncoder(
            ServiceMessageContext context,
            bool useReversibleEncoding,
            StreamWriter writer,
            bool topLevelIsArray)
        {
            return new JsonEncoder(context, true, writer, topLevelIsArray) { IncludeDefaultNumberValues = false };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="writer"></param>
        public void Encode(ServiceMessageContext context, StreamWriter writer)
        {
            bool topLevelIsArray = false;

            if ((MessageContentMask & JsonNetworkMessageContentMask.NetworkMessageHeader) == 0 &&
                (MessageContentMask & JsonNetworkMessageContentMask.SingleDataSetMessage) == 0)
            {
                topLevelIsArray = true;
            }

            using (IJsonEncoder encoder = CreateEncoder(context, true, writer, topLevelIsArray))
            {
                if ((MessageContentMask & JsonNetworkMessageContentMask.NetworkMessageHeader) != 0)
                {
                    Encode(encoder);
                }
                else if (Messages != null && Messages.Count > 0)
                {
                    if ((MessageContentMask & JsonNetworkMessageContentMask.SingleDataSetMessage) != 0)
                    {
                        encoder.PushStructure(null);
                        Messages[0].Encode(encoder, MessageContentMask);
                        encoder.PopStructure();
                    }
                    else
                    {
                        if (!topLevelIsArray)
                        {
                            encoder.PushArray(null);
                        }
                        foreach (var message in Messages)
                        {
                            message.Encode(encoder, MessageContentMask);
                        }
                        if (!topLevelIsArray)
                        {
                            encoder.PopArray();
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="encoder"></param>
        protected void Encode(IJsonEncoder encoder)
        {
            bool networkHeader = (MessageContentMask & JsonNetworkMessageContentMask.NetworkMessageHeader) != 0;
            if (networkHeader)
            {
                encoder.PushStructure(null);

                encoder.WriteString("MessageId", MessageId);
                encoder.WriteString("MessageType", MessageType);

                bool hasPublisherId = (MessageContentMask & JsonNetworkMessageContentMask.PublisherId) != 0;
                var publisherId = hasPublisherId ? PublisherId : null;

                encoder.WriteString("PublisherId", publisherId);

                bool hasDataSetClassId = (MessageContentMask & JsonNetworkMessageContentMask.DataSetClassId) != 0;
                var dataSetClassId = hasDataSetClassId ? DataSetClassId : null;
                encoder.WriteString(nameof(DataSetClassId), dataSetClassId);
            }

            if (Messages != null && Messages.Count > 0)
            {
                var fieldName = networkHeader ? "Messages" : null;
                bool singleDataSetMessage = (MessageContentMask & JsonNetworkMessageContentMask.SingleDataSetMessage) != 0;
                if ((MessageContentMask & JsonNetworkMessageContentMask.DataSetMessageHeader) != 0)
                {
                    if (singleDataSetMessage)
                    {
                        encoder.WriteEncodeable(fieldName, Messages[0], typeof(JsonDataSetMessage));
                    }
                    else
                    {
                        encoder.WriteEncodeableArray(fieldName, Messages.ToArray(), typeof(JsonDataSetMessage));
                    }
                }
                else
                {
                    if (singleDataSetMessage)
                    {
                        encoder.PushStructure(fieldName);
                        Messages[0].Encode(encoder, MessageContentMask);
                        encoder.PopStructure();
                    }
                    else
                    {
                        encoder.PushArray(fieldName);
                        foreach (var message in Messages)
                        {
                            message.Encode(encoder, MessageContentMask);
                        }
                        encoder.PopArray();
                    }
                }
            }

            if (networkHeader)
            {
                encoder.PopStructure();
            }
        }
    }

}
