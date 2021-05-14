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

namespace Opc.Ua
{
    /// <summary>
    /// Defines functions specific to encoding of Json
    /// objects in a stream.
    /// </summary>
    public interface IJsonEncoder : IEncoder, IDisposable
    {
        /// <summary>
        /// Push the begin of a structure on the decoder stack.
        /// </summary>
        /// <param name="fieldName">The name of the structure field.</param>
        void PushStructure(string fieldName);

        /// <summary>
        /// Pop the structure from the decoder stack.
        /// </summary>
        void PopStructure();

        /// <summary>
        /// Push the begin of an array on the decoder stack.
        /// </summary>
        /// <param name="fieldName">The name of the array field.</param>
        void PushArray(string fieldName);

        /// <summary>
        /// Pop the array from the decoder stack.
        /// </summary>
        void PopArray();

        /// <summary>
        /// Writes a Variant to the stream with the specified reversible encoding parameter
        /// </summary>
        void WriteVariant(string fieldName, Variant value, bool useReversibleEncoding);

        /// <summary>
        /// Writes an DataValue array to the stream.
        /// </summary>
        void WriteDataValue(string fieldName, DataValue value, bool useReversibleEncoding);

    }
}
