﻿/* Copyright 2013-2014 MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.IO;
using System.Text;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    public class KillCursorsMessageBinaryEncoder : MessageBinaryEncoderBase, IMessageEncoder<KillCursorsMessage>
    {
        // constructors
        public KillCursorsMessageBinaryEncoder(Stream stream, MessageEncoderSettings encoderSettings)
            : base(stream, encoderSettings)
        {
        }

        // methods
        public KillCursorsMessage ReadMessage()
        {
            var binaryReader = CreateBinaryReader();
            var streamReader = binaryReader.StreamReader;

            var messageSize = streamReader.ReadInt32();
            var requestId = streamReader.ReadInt32();
            var responseTo = streamReader.ReadInt32();
            var opcode = (Opcode)streamReader.ReadInt32();
            var reserved = streamReader.ReadInt32();
            var count = streamReader.ReadInt32();
            var cursorIds = new long[count];
            for (var i = 0; i < count; i++)
            {
                cursorIds[i] = streamReader.ReadInt64();
            }

            return new KillCursorsMessage(
                requestId,
                cursorIds);
        }

        public void WriteMessage(KillCursorsMessage message)
        {
            Ensure.IsNotNull(message, "message");

            var binaryWriter = CreateBinaryWriter();
            var streamWriter = binaryWriter.StreamWriter;
            var startPosition = streamWriter.Position;

            streamWriter.WriteInt32(0); // messageSize
            streamWriter.WriteInt32(message.RequestId);
            streamWriter.WriteInt32(0); // responseTo
            streamWriter.WriteInt32((int)Opcode.KillCursors);
            streamWriter.WriteInt32(0); // reserved
            streamWriter.WriteInt32(message.CursorIds.Count);
            foreach (var cursorId in message.CursorIds)
            {
                streamWriter.WriteInt64(cursorId);
            }
            streamWriter.BackpatchSize(startPosition);
        }

        // explicit interface implementations
        MongoDBMessage IMessageEncoder.ReadMessage()
        {
            return ReadMessage();
        }

        void IMessageEncoder.WriteMessage(MongoDBMessage message)
        {
            WriteMessage((KillCursorsMessage)message);
        }
    }
}
