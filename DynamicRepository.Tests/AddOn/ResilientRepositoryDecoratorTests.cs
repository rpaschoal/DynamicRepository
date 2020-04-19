using DynamicRepository.AddOn.Resiliency;
using DynamicRepository.Filter;
using DynamicRepository.Tests.Support;
using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DynamicRepository.Tests.AddOn
{
    public class ResilientRepositoryDecoratorTests
    {
        private const int NUMBER_OF_ATTEMPTS = 3; // Includes the first initial try

        private ResilientRepositoryDecorator<long, MockModel, Exception> _subject;
        private Mock<IRepository<long, MockModel>> _repositoryInternalsMock;

        public ResilientRepositoryDecoratorTests()
        {
            _repositoryInternalsMock = new Mock<IRepository<long, MockModel>>();

            _subject = new ResilientRepositoryDecorator<long, MockModel, Exception>(_repositoryInternalsMock.Object);
        }

        [Fact]
        public void ShouldBeResilientOnDeleteById()
        {
            // Arrange
            var testKey = 1;

            _repositoryInternalsMock
                .Setup(x => x.Delete(testKey))
                .Throws<Exception>();

            // Act
            Action action = () => _subject.Delete(testKey);

            // Assert
            action.Should().Throw<Exception>();

            _repositoryInternalsMock.Verify(x => x.Delete(testKey), Times.Exactly(NUMBER_OF_ATTEMPTS));
        }

        [Fact]
        public void ShouldBeResilientOnDeleteByEntity()
        {
            // Arrange
            var entityStub = new MockModel();

            _repositoryInternalsMock
                .Setup(x => x.Delete(entityStub))
                .Throws<Exception>();

            // Act
            Action action = () => _subject.Delete(entityStub);

            // Assert
            action.Should().Throw<Exception>();

            _repositoryInternalsMock.Verify(x => x.Delete(entityStub), Times.Exactly(NUMBER_OF_ATTEMPTS));
        }

        [Fact]
        public void ShouldBeResilientOnDeleteByIdAsync()
        {
            // Arrange
            var testKey = 1;

            _repositoryInternalsMock
                .Setup(x => x.DeleteAsync(testKey, CancellationToken.None))
                .Throws<Exception>();

            // Act
            Func<Task> action = async () => await _subject.DeleteAsync(testKey);

            // Assert
            action.Should().Throw<Exception>();

            _repositoryInternalsMock.Verify(x => x.DeleteAsync(testKey, CancellationToken.None), Times.Exactly(NUMBER_OF_ATTEMPTS));
        }

        [Fact]
        public void ShouldBeResilientOnDeleteByEntityAsync()
        {
            // Arrange
            var entityStub = new MockModel();

            _repositoryInternalsMock
                .Setup(x => x.DeleteAsync(entityStub, CancellationToken.None))
                .Throws<Exception>();

            // Act
            Func<Task> action = async () => await _subject.DeleteAsync(entityStub);

            // Assert
            action.Should().Throw<Exception>();

            _repositoryInternalsMock.Verify(x => x.DeleteAsync(entityStub, CancellationToken.None), Times.Exactly(NUMBER_OF_ATTEMPTS));
        }

        [Fact]
        public void ShouldBeResilientOnGet()
        {
            // Arrange
            var testKey = 1;

            _repositoryInternalsMock
                .Setup(x => x.Get(testKey))
                .Throws<Exception>();

            // Act
            Action action = () => _subject.Get(testKey);

            // Assert
            action.Should().Throw<Exception>();

            _repositoryInternalsMock.Verify(x => x.Get(testKey), Times.Exactly(NUMBER_OF_ATTEMPTS));
        }

        [Fact]
        public void ShouldBeResilientOnGetAsync()
        {
            // Arrange
            var testKey = 1;

            _repositoryInternalsMock
                .Setup(x => x.GetAsync(testKey, CancellationToken.None))
                .Throws<Exception>();

            // Act
            Func<Task> action = async () => await _subject.GetAsync(testKey);

            // Assert
            action.Should().Throw<Exception>();

            _repositoryInternalsMock.Verify(x => x.GetAsync(testKey, CancellationToken.None), Times.Exactly(NUMBER_OF_ATTEMPTS));
        }

        [Fact]
        public void ShouldBeResilientOnGetPagedData()
        {
            // Arrange
            var settingsStub = new PagedDataSettings();

            _repositoryInternalsMock
                .Setup(x => x.GetPagedData(settingsStub))
                .Throws<Exception>();

            // Act
            Action action = () => _subject.GetPagedData(settingsStub);

            // Assert
            action.Should().Throw<Exception>();

            _repositoryInternalsMock.Verify(x => x.GetPagedData(settingsStub), Times.Exactly(NUMBER_OF_ATTEMPTS));
        }

        [Fact]
        public void ShouldBeResilientOnInsert()
        {
            // Arrange
            var modelStub = new MockModel();

            _repositoryInternalsMock
                .Setup(x => x.Insert(modelStub))
                .Throws<Exception>();

            // Act
            Action action = () => _subject.Insert(modelStub);

            // Assert
            action.Should().Throw<Exception>();

            _repositoryInternalsMock.Verify(x => x.Insert(modelStub), Times.Exactly(NUMBER_OF_ATTEMPTS));
        }

        [Fact]
        public void ShouldBeResilientOnInsertAsync()
        {
            // Arrange
            var entityStub = new MockModel();

            _repositoryInternalsMock
                .Setup(x => x.InsertAsync(entityStub, CancellationToken.None))
                .Throws<Exception>();

            // Act
            Func<Task> action = async () => await _subject.InsertAsync(entityStub);

            // Assert
            action.Should().Throw<Exception>();

            _repositoryInternalsMock.Verify(x => x.InsertAsync(entityStub, CancellationToken.None), Times.Exactly(NUMBER_OF_ATTEMPTS));
        }

        [Fact]
        public void ShouldBeResilientOnList()
        {
            // Arrange
            var settingsStub = new PagedDataSettings();

            _repositoryInternalsMock
                .Setup(x => x.List(x => x.Id == 1, null, new[] { nameof(MockModel.ChildCollection) } ))
                .Throws<Exception>();

            // Act
            Action action = () => _subject.List(x => x.Id == 1, null, new[] { nameof(MockModel.ChildCollection) });

            // Assert
            action.Should().Throw<Exception>();

            _repositoryInternalsMock.Verify(x => x.List(x => x.Id == 1, null, new[] { nameof(MockModel.ChildCollection) }), Times.Exactly(NUMBER_OF_ATTEMPTS));
        }

        [Fact]
        public void ShouldBeResilientOnListAll()
        {
            // Arrange
            var settingsStub = new PagedDataSettings();

            _repositoryInternalsMock
                .Setup(x => x.ListAll())
                .Throws<Exception>();

            // Act
            Action action = () => _subject.ListAll();

            // Assert
            action.Should().Throw<Exception>();

            _repositoryInternalsMock.Verify(x => x.ListAll(), Times.Exactly(NUMBER_OF_ATTEMPTS));
        }

        [Fact]
        public void ShouldBeResilientOnUpdate()
        {
            // Arrange
            var modelStub = new MockModel();

            _repositoryInternalsMock
                .Setup(x => x.Update(modelStub))
                .Throws<Exception>();

            // Act
            Action action = () => _subject.Update(modelStub);

            // Assert
            action.Should().Throw<Exception>();

            _repositoryInternalsMock.Verify(x => x.Update(modelStub), Times.Exactly(NUMBER_OF_ATTEMPTS));
        }

        [Fact]
        public void ShouldBeResilientOnUpdateAsync()
        {
            // Arrange
            var entityStub = new MockModel();

            _repositoryInternalsMock
                .Setup(x => x.UpdateAsync(entityStub, CancellationToken.None))
                .Throws<Exception>();

            // Act
            Func<Task> action = async () => await _subject.UpdateAsync(entityStub);

            // Assert
            action.Should().Throw<Exception>();

            _repositoryInternalsMock.Verify(x => x.UpdateAsync(entityStub, CancellationToken.None), Times.Exactly(NUMBER_OF_ATTEMPTS));
        }
    }
}
