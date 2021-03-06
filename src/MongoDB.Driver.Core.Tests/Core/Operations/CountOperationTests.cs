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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.SyncExtensionMethods;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class CountOperationTests
    {
        private CollectionNamespace _collectionNamespace;
        private MessageEncoderSettings _messageEncoderSettings;
        private bool _testDataHasBeenCreated;

        [SetUp]
        public void Setup()
        {
            _collectionNamespace = SuiteConfiguration.GetCollectionNamespaceForTestFixture();
            _messageEncoderSettings = SuiteConfiguration.MessageEncoderSettings;
        }

        [Test]
        public void Constructor_should_throw_when_collection_namespace_is_null()
        {
            Action act = () => new CountOperation(null, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action act = () => new CountOperation(_collectionNamespace, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void CreateCommand_should_create_the_correct_command()
        {
            var criteria = new BsonDocument("x", 1);
            var hint = "funny";
            var limit = 10;
            var skip = 30;
            var maxTime = TimeSpan.FromSeconds(20);
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Criteria = criteria,
                Hint = hint,
                Limit = limit,
                MaxTime = maxTime,
                Skip = skip
            };
            var expectedResult = new BsonDocument
            {
                { "count", _collectionNamespace.CollectionName },
                { "query", criteria },
                { "limit", limit },
                { "skip", skip },
                { "hint", hint },
                { "maxTimeMS", maxTime.TotalMilliseconds }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_should_return_expected_result()
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings);

            long result;
            using (var binding = SuiteConfiguration.GetReadBinding())
            {
                result = await subject.ExecuteAsync(binding);
            }

            result.Should().Be(5);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_should_return_expected_result_when_criteria_is_provided()
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings);
            subject.Criteria = BsonDocument.Parse("{ _id : { $gt : 1 } }");

            long result;
            using (var binding = SuiteConfiguration.GetReadBinding())
            {
                result = await subject.ExecuteAsync(binding);
            }

            result.Should().Be(4);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_should_return_expected_result_when_hint_is_provided()
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings);
            subject.Hint = BsonDocument.Parse("{ _id : 1 }");

            long result;
            using (var binding = SuiteConfiguration.GetReadBinding())
            {
                result = await subject.ExecuteAsync(binding);
            }

            result.Should().Be(5);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_should_return_expected_result_when_limit_is_provided()
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings);
            subject.Limit = 3;

            long result;
            using (var binding = SuiteConfiguration.GetReadBinding())
            {
                result = await subject.ExecuteAsync(binding);
            }

            result.Should().Be(3);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public void ExecuteAsync_should_return_expected_result_when_maxTime_is_provided()
        {
            if (SuiteConfiguration.ServerVersion >= new SemanticVersion(2, 4, 0))
            {
                // TODO: port FailPoint infrastructure from Driver.Tests to Core.Tests
            }
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_should_return_expected_result_when_skip_is_provided()
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings);
            subject.Skip = 3;

            long result;
            using (var binding = SuiteConfiguration.GetReadBinding())
            {
                result = await subject.ExecuteAsync(binding);
            }

            result.Should().Be(2);
        }

        // helper methods
        private void EnsureTestData()
        {
            if (_testDataHasBeenCreated)
            {
                return;
            }

            using (var binding = SuiteConfiguration.GetReadWriteBinding())
            {
                var dropCollectionOperation = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);
                dropCollectionOperation.Execute(binding);

                var requests = Enumerable.Range(1, 5)
                    .Select(id => new BsonDocument("_id", id))
                    .Select(document => new InsertRequest(document));
                var insertOperation = new BulkInsertOperation(_collectionNamespace, requests, _messageEncoderSettings);
                insertOperation.Execute(binding);
            }

            _testDataHasBeenCreated = true;
        }
    }
}
