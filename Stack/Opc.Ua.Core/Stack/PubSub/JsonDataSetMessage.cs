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



using System;
using System.Collections.Generic;
using System.IO;
using Opc.Ua;

namespace Opc.Ua.PubSub
{
    /// <summary>
    /// 
    /// </summary>
    public class JsonDataSetMessage : IEncodeable
    {
        /// <summary>
        /// 
        /// </summary>
        public JsonDataSetMessage()
        {
            MessageContentMask =
                JsonDataSetMessageContentMask.DataSetWriterId |
                JsonDataSetMessageContentMask.MetaDataVersion |
                JsonDataSetMessageContentMask.SequenceNumber;

            FieldContentMask = DataSetFieldContentMask.None;

            Payload = new Dictionary<string, DataValue>();
        }
        /// <summary>
        /// 
        /// </summary>
        public JsonDataSetMessageContentMask MessageContentMask { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public UInt16 DataSetWriterId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public uint SequenceNumber { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ConfigurationVersionDataType MetaDataVersion { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime Timestamp { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public StatusCode Status { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DataSetFieldContentMask FieldContentMask { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, DataValue> Payload { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ExpandedNodeId TypeId { get { return ExpandedNodeId.Null; } }
        /// <summary>
        /// 
        /// </summary>
        public ExpandedNodeId BinaryEncodingId { get { return ExpandedNodeId.Null; } }
        /// <summary>
        /// 
        /// </summary>
        public ExpandedNodeId XmlEncodingId { get { return ExpandedNodeId.Null; } }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        private void EncodeField(IJsonEncoder encoder, string fieldName, DataValue value)
        {
            if (FieldContentMask == 0)
            {
                // value encoded as reversible
                encoder.WriteVariant(fieldName, value.WrappedValue, true);
                return;
            }

            if ((FieldContentMask & DataSetFieldContentMask.RawData) != 0)
            {
                // switch to non reversible here ?
                // https://reference.opcfoundation.org/v104/Core/docs/Part14/7.2.3/

                var variant = value.WrappedValue;

                if (variant.TypeInfo == null || variant.TypeInfo.BuiltInType == BuiltInType.Null)
                {
                    return;
                }

                encoder.WriteVariant(fieldName, variant, false);
                return;
            }

            bool reversible = (FieldContentMask & DataSetFieldContentMask.Reversible) != 0;

            DataValue dv = new DataValue();
            dv.WrappedValue = value.WrappedValue;

            if ((FieldContentMask & DataSetFieldContentMask.StatusCode) != 0)
            {
                dv.StatusCode = value.StatusCode;
            }

            if ((FieldContentMask & DataSetFieldContentMask.SourceTimestamp) != 0)
            {
                dv.SourceTimestamp = value.SourceTimestamp;
            }

            if ((FieldContentMask & DataSetFieldContentMask.SourcePicoSeconds) != 0)
            {
                dv.SourcePicoseconds = value.SourcePicoseconds;
            }

            if ((FieldContentMask & DataSetFieldContentMask.ServerTimestamp) != 0)
            {
                dv.ServerTimestamp = value.ServerTimestamp;
            }

            if ((FieldContentMask & DataSetFieldContentMask.ServerPicoSeconds) != 0)
            {
                dv.ServerPicoseconds = value.ServerPicoseconds;
            }

            // DataValue is non reversible
            // https://reference.opcfoundation.org/v104/Core/docs/Part14/7.2.3/
            encoder.WriteDataValue(fieldName, dv, reversible);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="messageContentMask"></param>
        public void Encode(IJsonEncoder encoder, JsonNetworkMessageContentMask messageContentMask)
        {
            bool singleDataSetMessage = (messageContentMask & JsonNetworkMessageContentMask.SingleDataSetMessage) != 0;
            if ((messageContentMask & JsonNetworkMessageContentMask.DataSetMessageHeader) != 0)
            {
                bool networkMessageHeader = (messageContentMask & JsonNetworkMessageContentMask.NetworkMessageHeader) != 0;
                if (!networkMessageHeader && !singleDataSetMessage)
                {
                    encoder.PushStructure(null);
                }
                Encode(encoder);
                if (!networkMessageHeader && !singleDataSetMessage)
                {
                    encoder.PopStructure();
                }
                return;
            }

            if (!singleDataSetMessage)
            {
                encoder.PushStructure(null);
            }
            if (Payload != null)
            {
                foreach (var ii in Payload)
                {
                    EncodeField(encoder, ii.Key, ii.Value);
                }
            }
            else
            {
                // write null
            }
            if (!singleDataSetMessage)
            {
                encoder.PopStructure();
            }
        }

        void IEncodeable.Encode(IEncoder encoder)
        {
            Encode(encoder as IJsonEncoder);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="encoder"></param>
        public void Encode(IJsonEncoder encoder)
        {
            if ((MessageContentMask & JsonDataSetMessageContentMask.DataSetWriterId) != 0)
            {
                encoder.WriteUInt16(nameof(DataSetWriterId), DataSetWriterId);
            }

            if ((MessageContentMask & JsonDataSetMessageContentMask.SequenceNumber) != 0)
            {
                encoder.WriteUInt32(nameof(SequenceNumber), SequenceNumber);
            }

            if ((MessageContentMask & JsonDataSetMessageContentMask.MetaDataVersion) != 0)
            {
                encoder.WriteEncodeable(nameof(MetaDataVersion), MetaDataVersion, typeof(ConfigurationVersionDataType));
            }

            if ((MessageContentMask & JsonDataSetMessageContentMask.Timestamp) != 0)
            {
                encoder.WriteDateTime(nameof(Timestamp), Timestamp);
            }

            if ((MessageContentMask & JsonDataSetMessageContentMask.Status) != 0)
            {
                encoder.WriteStatusCode(nameof(Status), Status);
            }

            if (Payload != null)
            {
                ((IJsonEncoder)encoder).PushStructure("Payload");

                foreach (var ii in Payload)
                {
                    EncodeField(encoder, ii.Key, ii.Value);
                }

                ((IJsonEncoder)encoder).PopStructure();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="decoder"></param>
        public void Decode(IDecoder decoder)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="encodeable"></param>
        /// <returns></returns>
        public bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }

            // TODO

            return false;
        }
    }


}
