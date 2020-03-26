using DynamicRepository.AddOn;
using DynamicRepository.Filter;
using DynamicRepository.Tests.Support;
using FluentAssertions;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace DynamicRepository.Tests.AddOn
{
    public class RepositoryProxyTests
    {
        private RepositoryProxy<long, MockModel> _subject;
        private Mock<IRepository<long, MockModel>> _repositoryInternalsMock;

        public RepositoryProxyTests()
        {
            _repositoryInternalsMock = new Mock<IRepository<long, MockModel>>();

            _subject = new RepositoryProxy<long, MockModel>();

            _subject.InitializeProxy(_repositoryInternalsMock.Object);
        }

        [Fact]
        public async Task ShouldInvokeInternalsExercise()
        {
            // Arrange
            var testKey = 1;
            var insertStub = new MockModel();
            var updateStub = new MockModel();
            var pagedDataSettingsStub = new PagedDataSettings();

            // Act
            _subject.Get(testKey);
            _subject.Insert(insertStub);
            _subject.Update(updateStub);
            _subject.Delete(testKey);
            _subject.Delete(insertStub);
            
            _subject.GetQueryable();
            _subject.ListAll();
            _subject.List(x => x.Id == testKey, null, new[] { nameof(MockModel.ChildCollection) });
            _subject.GetPagedData(pagedDataSettingsStub);

            await _subject.GetAsync(testKey);
            await _subject.InsertAsync(insertStub);
            await _subject.UpdateAsync(updateStub);
            await _subject.DeleteAsync(testKey);
            await _subject.DeleteAsync(insertStub);

            // Assert
            _repositoryInternalsMock.Verify(x => x.Get(testKey), Times.Once);
            _repositoryInternalsMock.Verify(x => x.Insert(insertStub), Times.Once);
            _repositoryInternalsMock.Verify(x => x.Update(updateStub), Times.Once);
            _repositoryInternalsMock.Verify(x => x.Delete(testKey), Times.Once);
            _repositoryInternalsMock.Verify(x => x.Delete(insertStub), Times.Once);
            _repositoryInternalsMock.Verify(x => x.GetQueryable(), Times.Once);
            _repositoryInternalsMock.Verify(x => x.ListAll(), Times.Once);
            _repositoryInternalsMock.Verify(x => x.List(x => x.Id == testKey, null, new[] { nameof(MockModel.ChildCollection) }), Times.Once);
            _repositoryInternalsMock.Verify(x => x.GetPagedData(pagedDataSettingsStub), Times.Once);

            _repositoryInternalsMock.Verify(x => x.GetAsync(testKey), Times.Once);
            _repositoryInternalsMock.Verify(x => x.InsertAsync(insertStub), Times.Once);
            _repositoryInternalsMock.Verify(x => x.UpdateAsync(updateStub), Times.Once);
            _repositoryInternalsMock.Verify(x => x.DeleteAsync(testKey), Times.Once);
            _repositoryInternalsMock.Verify(x => x.DeleteAsync(insertStub), Times.Once);
        }

        [Fact]
        public void ShouldThrowExceptionOnNonInitializedProxy()
        {
            // Arrange
            _subject = new RepositoryProxy<long, MockModel>();

            // Act
            Action proxyCall = () => _subject.GetQueryable();

            // Assert
            proxyCall.Should().Throw<NullReferenceException>().WithMessage("Proxy internals were not initialized.");
        }
    }
}
