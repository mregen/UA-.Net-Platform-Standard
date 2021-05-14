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
    public class JsonDataSetMetaData
    {
        /// <summary>
        /// 
        /// </summary>
        public string MessageId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string MessageType { get; set; }
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
        public DataSetMetaDataType MetaData { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public void Encode(IEncoder encoder)
        {
            encoder.WriteString("MessageId", MessageId);
            encoder.WriteString("MessageType", "ua-metadata");
            encoder.WriteString("PublisherId", PublisherId);
            encoder.WriteString("DataSetClassId", DataSetClassId);
            encoder.WriteEncodeable("MetaData", MetaData, typeof(DataSetMetaDataType));
        }
        /// <summary>
        /// 
        /// </summary>
        public static JsonDataSetMetaData Decode(IDecoder decoder)
        {
            JsonDataSetMetaData metaData = new JsonDataSetMetaData();
            metaData.MessageId = decoder.ReadString(nameof(metaData.MessageId));
            metaData.MessageType = decoder.ReadString("MessageType");
            metaData.PublisherId = decoder.ReadString("PublisherId");
            metaData.DataSetClassId = decoder.ReadString("DataSetClassId");
            metaData.MetaData = (DataSetMetaDataType)decoder.ReadEncodeable("MetaData", typeof(DataSetMetaDataType));

            return metaData;
        }
    }
}
