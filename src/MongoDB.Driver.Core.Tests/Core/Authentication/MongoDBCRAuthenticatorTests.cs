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
using System.Net;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Authentication;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Authentication
{
    [TestFixture]
    public class MongoDBXCRAuthenticatorTests
    {
        private static readonly UsernamePasswordCredential __credential = new UsernamePasswordCredential("source", "user", "pencil");
        private static readonly ClusterId __clusterId = new ClusterId();
        private static readonly ServerId __serverId = new ServerId(__clusterId, new DnsEndPoint("localhost", 27017));

        [Test]
        public void Constructor_should_throw_an_ArgumentNullException_when_credential_is_null()
        {
            Action act = () => new MongoDBCRAuthenticator(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void AuthenticateAsync_should_throw_an_AuthenticationException_when_authentication_fails()
        {
            var subject = new MongoDBCRAuthenticator(__credential);

            var reply = MessageHelper.BuildNoDocumentsReturnedReply<RawBsonDocument>();
            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(reply);

            Action act = () => subject.AuthenticateAsync(connection, Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();

            act.ShouldThrow<MongoAuthenticationException>();
        }

        [Test]
        public void AuthenticateAsync_should_not_throw_when_authentication_succeeds()
        {
            var subject = new MongoDBCRAuthenticator(__credential);

            var getNonceReply = MessageHelper.BuildSuccessReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{nonce: \"2375531c32080ae8\", ok: 1}"));
            var authenticateReply = MessageHelper.BuildSuccessReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ok: 1}"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(getNonceReply);
            connection.EnqueueReplyMessage(authenticateReply);

            var currentRequestId = RequestMessage.CurrentGlobalRequestId;
            Action act = () => subject.AuthenticateAsync(connection, Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();
            act.ShouldNotThrow();

            var sentMessages = MessageHelper.TranslateMessagesToBsonDocuments(connection.GetSentMessages());
            sentMessages.Count.Should().Be(2);

            sentMessages[0].Should().Be("{opcode: \"query\", requestId: " + (currentRequestId + 1) + ", database: \"source\", collection: \"$cmd\", batchSize: -1, slaveOk: true, query: {getnonce: 1}}");
            sentMessages[1].Should().Be("{opcode: \"query\", requestId: " + (currentRequestId + 2) + ", database: \"source\", collection: \"$cmd\", batchSize: -1, slaveOk: true, query: {authenticate: 1, user: \"user\", nonce: \"2375531c32080ae8\", key: \"21742f26431831d5cfca035a08c5bdf6\"}}");
        }
    }
}