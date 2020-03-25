using DynamicRepository.AddOn;
using DynamicRepository.AddOn.Resiliency;
using DynamicRepository.Tests.Support;
using FluentAssertions;
using Moq;
using System;
using Xunit;

namespace DynamicRepository.Tests.AddOn
{
    public class RepositoryAddOnBuilderTests
    {
        private Mock<IRepository<long, MockModel>> _repositoryInternalsMock;

        public RepositoryAddOnBuilderTests()
        {
            _repositoryInternalsMock = new Mock<IRepository<long, MockModel>>();
        }

        [Fact]
        public void ShouldAddResiliencyDecoratorWhenRequested()
        {
            // Act
            var builtRepository = new RepositoryAddOnBuilder<long, MockModel>(_repositoryInternalsMock.Object)
                .AddResiliency<Exception>()
                .Build();

            // Assert
            builtRepository.Should().NotBeNull();
            builtRepository.Should().BeOfType<ResilientRepositoryDecorator<long, MockModel, Exception>>();
        }
    }
}
