using DynamicRepository.Filter;
using DynamicRepository.MongoDB;
using DynamicRepository.Tests.Support;
using FluentAssertions;
using MongoDB.Driver;
using Moq;
using System;
using System.Linq.Expressions;
using Xunit;

namespace DynamicRepository.Tests.MongoDB
{
    public class RepositoryTests
    {
        private Mock<IMongoDatabase> _mongoDatabaseMock;
        private Mock<IMongoCollection<MockModel>> _mongoCollectionMock;
        private string _testCollectionName;
        private TestableRepository _subject;

        public RepositoryTests()
        {
            _testCollectionName = "TestCollection";

            _mongoDatabaseMock = new Mock<IMongoDatabase>();
            _mongoCollectionMock = new Mock<IMongoCollection<MockModel>>();

            _mongoDatabaseMock
                .Setup(x => x.GetCollection<MockModel>(_testCollectionName, null))
                .Returns(_mongoCollectionMock.Object);

            _subject = new TestableRepository(_mongoDatabaseMock.Object, _testCollectionName);
        }

        [Fact]
        public void ShouldBeDecoratedByResiliencyAddOn()
        {
            // Arrange
            var mockModelStub = new MockModel();
            var retryAttempts = 3;

            _mongoCollectionMock
                .Setup(x => x.InsertOne(mockModelStub, null, default))
                .Throws<TestableException>();

            // Act
            // Picked up inserts as a "smoke test"
            Action action = () => _subject.Insert(mockModelStub);

            // Assert
            action.Should().Throw<MongoException>();
            _mongoCollectionMock.Verify(x => x.InsertOne(mockModelStub, null, default), Times.Exactly(retryAttempts));
        }

        [Fact]
        public void ShouldUseAbstractDelegatesWhenPagingData()
        {
            // Act
            // HACK as "AsQueryable" can't be mocked
            try
            {
                _subject.GetPagedData(new PagedDataSettings());
            }
            catch (Exception){}

            // Assert
            _subject.HasAddPreConditionsPagedDataFilterBeenInvoked.Should().BeTrue();
            _subject.HasAddExtraPagedDataFilterBeenInvoked.Should().BeTrue();
        }

        private class TestableException : MongoException
        {
            public TestableException() : base("")
            {
            }
        }

        private class TestableRepository : Repository<long, MockModel>, IRepository<long, MockModel>
        {
            internal bool HasAddPreConditionsPagedDataFilterBeenInvoked { get; set; }
            internal bool HasAddExtraPagedDataFilterBeenInvoked { get; set; }

            public TestableRepository(IMongoDatabase mongoDatabase, string collectionName) : base(mongoDatabase, collectionName)
            {
            }

            protected override Expression<Func<MockModel, bool>> AddPreConditionsPagedDataFilter(PagedDataSettings settings)
            {
                HasAddPreConditionsPagedDataFilterBeenInvoked = true;

                return base.AddPreConditionsPagedDataFilter(settings);
            }

            protected override Expression<Func<MockModel, bool>> AddExtraPagedDataFilter(PagedDataSettings settings)
            {
                HasAddExtraPagedDataFilterBeenInvoked = true;

                return base.AddExtraPagedDataFilter(settings);
            }
        }
    }
}
