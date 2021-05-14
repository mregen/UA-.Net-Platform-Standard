/* ========================================================================
 * Copyright (c) 2005-2018 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Opc.Ua.PubSub;

namespace Opc.Ua.Core.Tests.Types.Encoders
{
    /// <summary>
    /// Tests for the Json encoder and decoder class.
    /// </summary>
    [TestFixture, Category("JsonPubSub")]
    [SetCulture("en-us"), SetUICulture("en-us")]
    [Parallelizable]
    public class JsonPubSubTests : EncoderCommon
    {
        #region DataSource
        #endregion

        #region Setup
        [OneTimeSetUp]
        protected new void OneTimeSetUp()
        {
        }

        [OneTimeTearDown]
        protected new void OneTimeTearDown()
        {
        }


        [SetUp]
        protected new void SetUp()
        {
        }

        [TearDown]
        protected new void TearDown()
        {
        }
        #endregion

        #region Test Methods
        /// <summary>
        /// It is not valid to have a JSON object within another object without fieldname
        /// </summary>
        //[TestCase(JsonNetworkMessageContentMask.None)]
        //[TestCase(JsonNetworkMessageContentMask.NetworkMessageHeader)]
        [Theory]
        public void JsonPubSubEncodeNetworkMessage(
            bool networkMessageHeader,
            bool datasetMessageHeader,
            bool singleDataSetMessage)
        {
            JsonNetworkMessageContentMask messageContentMask =
                JsonNetworkMessageContentMask.PublisherId |
                JsonNetworkMessageContentMask.DataSetClassId |
                JsonNetworkMessageContentMask.ReplyTo;

            if (networkMessageHeader)
            {
                messageContentMask |= JsonNetworkMessageContentMask.NetworkMessageHeader;
            }
            if (singleDataSetMessage)
            {
                messageContentMask |= JsonNetworkMessageContentMask.SingleDataSetMessage;
            }
            if (datasetMessageHeader)
            {
                messageContentMask |= JsonNetworkMessageContentMask.DataSetMessageHeader;
            }

            TestContext.Out.WriteLine("Expected:");
            //_ = PrettifyAndValidateJson(expected);

            var networkMessage = new JsonNetworkMessage() {
                MessageId = Guid.NewGuid().ToString(),
                DataSetClassId = Guid.NewGuid().ToString(),
                PublisherId = Guid.NewGuid().ToString(),
                MessageContentMask = messageContentMask
            };

            for (int i = 0; i < 8; i++)
            {
                DataSetFieldContentMask fieldContentMask = DataSetFieldContentMask.None;
                if (i == 1)
                {
                    fieldContentMask = DataSetFieldContentMask.RawData;
                }
                else if ((i & 1) != 0)
                {
                    fieldContentMask = DataSetFieldContentMask.StatusCode;
                }
                else if ((i & 2) != 0)
                {
                    fieldContentMask = DataSetFieldContentMask.SourceTimestamp;
                }
                else if ((i & 4) != 0)
                {
                    fieldContentMask = DataSetFieldContentMask.ServerTimestamp;
                }

                JsonDataSetMessageContentMask dataSetMessageContentMask =
                    JsonDataSetMessageContentMask.DataSetWriterId |
                    JsonDataSetMessageContentMask.MetaDataVersion |
                    JsonDataSetMessageContentMask.SequenceNumber |
                    JsonDataSetMessageContentMask.Status |
                    JsonDataSetMessageContentMask.Timestamp;

                var dataSetMessage = new JsonDataSetMessage() {
                    SequenceNumber = (uint)i,
                    DataSetWriterId = 5555,
                    MetaDataVersion = new ConfigurationVersionDataType(),
                    Timestamp = DateTime.UtcNow,
                    Status = new StatusCode(((i & 1) == 0) ? StatusCodes.Uncertain : StatusCodes.Good),
                    FieldContentMask = fieldContentMask,
                    MessageContentMask = dataSetMessageContentMask
                };

                dataSetMessage.Payload["#0"] = new DataValue(new Variant(100 + i), StatusCodes.Uncertain, DateTime.UtcNow, DateTime.UtcNow - TimeSpan.FromMilliseconds(12));
                dataSetMessage.Payload["#1"] = new DataValue(new Variant(200 + i), StatusCodes.Good, DateTime.UtcNow, DateTime.UtcNow - TimeSpan.FromSeconds(1));

                networkMessage.Messages.Add(dataSetMessage);
            }

            using (MemoryStream stream = new MemoryStream())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 65535, true))
            {
                networkMessage.Encode(Context, writer);
                var encoded = Encoding.UTF8.GetString(stream.ToArray());

                TestContext.Out.WriteLine("Encoded:");
                TestContext.Out.WriteLine(encoded);

                TestContext.Out.WriteLine("Formatted Encoded:");
                var prettyText = PrettifyAndValidateJson(encoded);

                var fileName = $"NMH{networkMessageHeader}_DSMH{datasetMessageHeader}_SDSM{singleDataSetMessage}.json";
                File.WriteAllText(fileName, prettyText);
            }
        }

        #endregion

        #region Private Methods
        private void RunWriteEncodeableArrayTest(string fieldName, List<FooBarEncodeable> encodeables, string expected, bool topLevelIsArray, bool noExpectedValidation = false)
        {
            try
            {
                if (!noExpectedValidation)
                {
                    TestContext.Out.WriteLine("Expected:");
                    _ = PrettifyAndValidateJson(expected);
                }

                var encoder = new JsonEncoder(Context, true, null, topLevelIsArray);

                encoder.WriteEncodeableArray(
                    fieldName,
                    encodeables.Cast<IEncodeable>().ToList(),
                    typeof(FooBarEncodeable));

                var encoded = encoder.CloseAndReturnText();
                TestContext.Out.WriteLine("Encoded:");
                TestContext.Out.WriteLine(encoded);

                TestContext.Out.WriteLine("Formatted Encoded:");
                _ = PrettifyAndValidateJson(encoded);

                Assert.That(encoded, Is.EqualTo(expected));
            }
            finally
            {
                encodeables.ForEach(e => e.Dispose());
            }
        }

        #endregion

        #region Private Fields
        #endregion
    }

}
