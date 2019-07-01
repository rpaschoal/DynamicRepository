using System;
using System.Text;
using System.Collections.Generic;
using DynamicRepository.Tests.Support;
using DynamicRepository;
using Xunit;
using DynamicRepository.Filter;
using System.Linq;
using DynamicRepository.Annotation;
using Microsoft.EntityFrameworkCore;
using DynamicRepository.EFCore;
using FluentAssertions;
using System.Linq.Expressions;

namespace DynamicRepository.Tests.EFCore
{
    public class InMemoryTests
    {
        private readonly Guid FirstTenantId = Guid.NewGuid();
        private readonly Guid SecondTenantId = Guid.NewGuid();

        private readonly DbContextOptions<TestDbContext> _inMemoryDbOptions;

        private readonly IEnumerable<TestDbContext.TestEntityClass> TestData;

        private class TestableRepository : Repository<int, TestDbContext.TestEntityClass>, IRepository<int, TestDbContext.TestEntityClass>
        {
            public TestableRepository(DbContext context) : base(context)
            {
            }

            internal void SetGlobalFilter(Expression<Func<TestDbContext.TestEntityClass, bool>> filter)
            {
                this.HasGlobalFilter(filter);
            }
        }

        public InMemoryTests()
        {
            TestData = new[] {
                new TestDbContext.TestEntityClass()
                {
                    Id = 1,
                    TenantId = FirstTenantId,
                    Title = "Test 1"
                },
                new TestDbContext.TestEntityClass()
                {
                    Id = 2,
                    TenantId = FirstTenantId,
                    Title = "Test 2"
                },
                new TestDbContext.TestEntityClass()
                {
                    Id = 3,
                    TenantId = SecondTenantId,
                    Title = "Test 3"
                },
            };

            _inMemoryDbOptions = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: "DynamicRepositoryIntegrationTests")
                .Options;

            // Run the test against one instance of the context
            using (var context = new TestDbContext(_inMemoryDbOptions))
            {
                context.Database.EnsureDeleted();

                context.TestEntityClasses.AddRange(TestData);

                context.SaveChanges();
            }
        }

        [Fact]
        public void ShouldGetById()
        {
            using (var context = new TestDbContext(_inMemoryDbOptions))
            {
                var subject = new TestableRepository(context);

                var result = subject.Get(TestData.ElementAt(0).Id);

                result.Id.Should().Be(TestData.ElementAt(0).Id);
                result.TenantId.Should().Be(TestData.ElementAt(0).TenantId);
                result.Title.Should().Be(TestData.ElementAt(0).Title);
            }
        }

        [Fact]
        public async void ShouldGetByIdAsync()
        {
            using (var context = new TestDbContext(_inMemoryDbOptions))
            {
                var subject = new TestableRepository(context);

                var result = await subject.GetAsync(TestData.ElementAt(0).Id);

                result.Id.Should().Be(TestData.ElementAt(0).Id);
                result.TenantId.Should().Be(TestData.ElementAt(0).TenantId);
                result.Title.Should().Be(TestData.ElementAt(0).Title);
            }
        }

        [Fact]
        public  void ShouldInsertEntity()
        {
            using (var context = new TestDbContext(_inMemoryDbOptions))
            {
                var subject = new TestableRepository(context);

                subject.Insert(new TestDbContext.TestEntityClass() { Id = 999 });

                context.SaveChanges();

                context.TestEntityClasses.Count().Should().Be(TestData.Count() + 1);
            }
        }

        [Fact]
        public async void ShouldInsertEntityAsync()
        {
            using (var context = new TestDbContext(_inMemoryDbOptions))
            {
                var subject = new TestableRepository(context);

                await subject.InsertAsync(new TestDbContext.TestEntityClass() { Id = 999 });

                await context.SaveChangesAsync();

                context.TestEntityClasses.Count().Should().Be(TestData.Count() + 1);
            }
        }

        [Fact]
        public void ShouldUpdateEntity()
        {
            using (var context = new TestDbContext(_inMemoryDbOptions))
            {
                var subject = new TestableRepository(context);

                var entity = context.TestEntityClasses.FirstOrDefault();

                entity.Title = "Update Test";

                subject.Update(entity);

                context.SaveChanges();

                context.TestEntityClasses.FirstOrDefault().Title.Should().Be("Update Test");
            }
        }

        [Fact]
        public async void ShouldUpdateEntityAsync()
        {
            using (var context = new TestDbContext(_inMemoryDbOptions))
            {
                var subject = new TestableRepository(context);

                var entity = context.TestEntityClasses.FirstOrDefault();

                entity.Title = "Update Test";

                await subject.UpdateAsync(entity);

                await context.SaveChangesAsync();

                context.TestEntityClasses.FirstOrDefault().Title.Should().Be("Update Test");
            }
        }

        [Fact]
        public void ShouldRemoveEntity()
        {
            using (var context = new TestDbContext(_inMemoryDbOptions))
            {
                var subject = new TestableRepository(context);

                var entity = context.TestEntityClasses.FirstOrDefault();

                subject.Delete(entity);

                context.SaveChanges();

                context.TestEntityClasses.Count().Should().Be(TestData.Count() - 1);
                context.TestEntityClasses.Any(x => x.Id == entity.Id).Should().BeFalse();
            }
        }

        [Fact]
        public async void ShouldRemoveEntityAsync()
        {
            using (var context = new TestDbContext(_inMemoryDbOptions))
            {
                var subject = new TestableRepository(context);

                var entity = context.TestEntityClasses.FirstOrDefault();

                await subject.DeleteAsync(entity);

                await context.SaveChangesAsync();

                context.TestEntityClasses.Count().Should().Be(TestData.Count() - 1);
                context.TestEntityClasses.Any(x => x.Id == entity.Id).Should().BeFalse();
            }
        }

        [Fact]
        public void ShouldRemoveById()
        {
            using (var context = new TestDbContext(_inMemoryDbOptions))
            {
                var subject = new TestableRepository(context);

                var entity = context.TestEntityClasses.FirstOrDefault();

                subject.Delete(entity.Id);

                context.SaveChanges();

                context.TestEntityClasses.Count().Should().Be(TestData.Count() - 1);
                context.TestEntityClasses.Any(x => x.Id == entity.Id).Should().BeFalse();
            }
        }

        [Fact]
        public async void ShouldRemoveByIdAsync()
        {
            using (var context = new TestDbContext(_inMemoryDbOptions))
            {
                var subject = new TestableRepository(context);

                var entity = context.TestEntityClasses.FirstOrDefault();

                await subject.DeleteAsync(entity.Id);

                await context.SaveChangesAsync();

                context.TestEntityClasses.Count().Should().Be(TestData.Count() - 1);
                context.TestEntityClasses.Any(x => x.Id == entity.Id).Should().BeFalse();
            }
        }

        [Fact]
        public void ShouldReturnAllDataOnListAll()
        {
            using (var context = new TestDbContext(_inMemoryDbOptions))
            {
                var subject = new TestableRepository(context);

                var result = subject.ListAll();

                result.Should().BeEquivalentTo(TestData);
            }
        }

        [Fact]
        public void ShouldReturnAllDataOnGetQueryable()
        {
            using (var context = new TestDbContext(_inMemoryDbOptions))
            {
                var subject = new TestableRepository(context);

                var result = subject.GetQueryable();

                result.Should().BeEquivalentTo(TestData);
            }
        }

        [Fact]
        public void ShouldReturnFilteredDataWhenGlobalFilterIsSet()
        {
            using (var context = new TestDbContext(_inMemoryDbOptions))
            {
                var subject = new TestableRepository(context);

                subject.SetGlobalFilter(t => t.TenantId == FirstTenantId);

                var result = subject.GetQueryable();

                result.Count().Should().Be(TestData.Count(t => t.TenantId == FirstTenantId));
            }
        }
    }
}
