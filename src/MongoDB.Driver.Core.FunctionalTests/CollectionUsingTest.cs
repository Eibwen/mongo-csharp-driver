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

using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NUnit.Framework;

namespace MongoDB.Driver
{
    public abstract class CollectionUsingTest : DatabaseUsingTest
    {
        // fields
        private CollectionNamespace _collectionNamespace;

        // properties
        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        // methods
        protected virtual void CreateCollection()
        {
            // override if you need to create the collection a certain way
        }

        protected virtual void DropCollection()
        {
            var operation = new DropCollectionOperation(CollectionNamespace, MessageEncoderSettings);
            ExecuteOperation(operation);
        }

        protected virtual string GetCollectionName()
        {
            var type = GetType();
            var specificationFolder = type.Namespace.Split('.').Last();
            var specificationName = type.Name;
            var collectionName = string.Format("{0}_{1}", specificationFolder, specificationName);
            var namespaceLength = DatabaseNamespace.DatabaseName.Length + collectionName.Length + 1;
            const int maxNamespaceLength = 120;
            if (namespaceLength > maxNamespaceLength)
            {
                var excessLength = namespaceLength - maxNamespaceLength;
                var trimmedCollectionNameLength = collectionName.Length - excessLength;
                collectionName = collectionName.Substring(0, trimmedCollectionNameLength);
            }
            return collectionName;
        }

        [TestFixtureSetUp]
        public void CollectionUsingTestSetUp()
        {
            _collectionNamespace = new CollectionNamespace(DatabaseNamespace, GetCollectionName());
            CreateCollection();
        }

        [TestFixtureTearDown]
        public void CollectionUsingTestTearDown()
        {
            DropCollection();
        }

        protected void Insert<T>(IEnumerable<T> documents, IBsonSerializer<T> serializer, MessageEncoderSettings messageEncoderSettings = null)
        {
            var requests = documents.Select(d => new InsertRequest(new BsonDocumentWrapper(d, serializer)));
            var operation = new BulkInsertOperation(_collectionNamespace, requests, messageEncoderSettings ?? MessageEncoderSettings);
            ExecuteOperationAsync(operation).GetAwaiter().GetResult();
        }

        protected void Insert(IEnumerable<BsonDocument> documents, MessageEncoderSettings messageEncoderSettings = null)
        {
            Insert(documents, BsonDocumentSerializer.Instance, messageEncoderSettings);
        }

        protected List<T> ReadAll<T>(IBsonSerializer<T> serializer = null, CollectionNamespace collectionNamespace = null, MessageEncoderSettings messageEncoderSettings = null)
        {
            var operation = new FindOperation<T>(collectionNamespace ?? _collectionNamespace, serializer ?? BsonSerializer.LookupSerializer<T>(), messageEncoderSettings ?? MessageEncoderSettings);
            var cursor = ExecuteOperationAsync(operation).GetAwaiter().GetResult();
            return ReadCursorToEnd(cursor);
        }

        protected List<BsonDocument> ReadAll(CollectionNamespace collectionNamespace = null, MessageEncoderSettings messageEncoderSettings = null)
        {
            return ReadAll<BsonDocument>(BsonDocumentSerializer.Instance, collectionNamespace, messageEncoderSettings);
        }
    }
}
