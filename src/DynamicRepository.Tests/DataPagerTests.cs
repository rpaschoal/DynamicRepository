using System;
using System.Text;
using System.Collections.Generic;
using DynamicRepository.Tests.Support;
using DynamicRepository;
using Xunit;
using DynamicRepository.Filter;
using System.Linq;

namespace DynamicRepository.Tests
{
    /// <summary>
    /// Tests basic behaviors of the Data Pager class.
    /// </summary>
    public class DataPagerTests
    {
        protected DataPager<long, MockModel> _dataPager { get; set; } = new DataPager<long, MockModel>();

        public DataPagerTests()
        {
        }

        /// <summary>
        /// Evaluates if data can be paged over an IQueryable instance.
        /// </summary>
        [Fact]
        public void CanGetPagedData()
        {
            // Arrange
            var items = MockModel.ArrangeFixture();
            var pagedSize = 5;

            var settings = new PagedDataSettings()
            {
                TotalPerPage = pagedSize
            };

            // Act
            var result = _dataPager.GetPagedData(items.AsQueryable(), settings);

            // Assert
            Assert.True(result.TotalRecords == items.Count, "Total records in the paged result does not match the total in the fixture collection.");
            Assert.True(result.Result.Count == pagedSize, "Total items in first paged batch does not match the configured page size.");
        }

        /// <summary>
        /// Evaluates if data can be filtered by exact match over an IQueryable instance.
        /// </summary>
        [Fact]
        public void CanFilterPagedDataWithExactMatch()
        {
            // Arrange
            var items = MockModel.ArrangeFixture();
            var pagedSize = 5;
            var expectedResults = 1;

            var settings = new PagedDataSettings()
            {
                TotalPerPage = pagedSize
            };

            settings.Filter.Add(new FilterSettings()
            {
                Property =  "Label",
                Value = items.Last().Label,
                IsExactMatch = true
                
            });

            // Act
            var result = _dataPager.GetPagedData(items.AsQueryable(), settings);

            // Assert
            Assert.True(result.TotalRecords == expectedResults, "Results differ of what they should be. Possibly the filter on the DataPager class is not working.");
            Assert.True(result.Result.Count == expectedResults, "Returned results differ than total records.");
        }

        /// <summary>
        /// Evaluates if data can be filtered by partial match over an IQueryable instance.
        /// </summary>
        [Fact]
        public void CanFilterPagedDataWithPartialMatch()
        {
            // Arrange
            var items = MockModel.ArrangeFixture();
            var pagedSize = 5;
            var expectedResults = items.Count - 1;

            var settings = new PagedDataSettings()
            {
                TotalPerPage = pagedSize
            };

            settings.Filter.Add(new FilterSettings()
            {
                Property = "Label",
                Value = "Label",
                IsExactMatch = false

            });

            // Act
            var result = _dataPager.GetPagedData(items.AsQueryable(), settings);

            // Assert
            Assert.True(result.TotalRecords == expectedResults, "Results differ of what they should be. Possibly the filter on the DataPager class is not working.");
            Assert.True(result.Result.Count == pagedSize, "Total items in first paged batch does not match the configured page size.");
        }

        /// <summary>
        /// Evaluates if data can be sorted over an IQueryable instance.
        /// </summary>
        [Fact]
        public void CanSortPagedData()
        {
            // Arrange
            var items = MockModel.ArrangeFixture();
            var pagedSize = 5;

            var settings = new PagedDataSettings()
            {
                TotalPerPage = pagedSize
            };

            settings.Order.Add(new SortingSettings()
            {
                Property = "Label",
                // Order =  SortOrderEnum.ASC // Should default to ASC
            });

            // Act
            var result = _dataPager.GetPagedData(items.AsQueryable(), settings);

            // Assert
            Assert.True(result.Result.First().Label == "Eigth Label");
            Assert.True(result.Result.Last().Label == "Nineth");
        }

        /// <summary>
        /// Evaluates if data can be sorted by descending data over an IQueryable instance.
        /// </summary>
        [Fact]
        public void CanSortPagedDataByDescending()
        {
            // Arrange
            var items = MockModel.ArrangeFixture();
            var pagedSize = 5;

            var settings = new PagedDataSettings()
            {
                TotalPerPage = pagedSize
            };

            settings.Order.Add(new SortingSettings()
            {
                Property = "Label",
                Order =  SortOrderEnum.DESC // Should default to ASC
            });

            // Act
            var result = _dataPager.GetPagedData(items.AsQueryable(), settings);

            // Assert
            Assert.True(result.Result.First().Label == "Third Label");
            Assert.True(result.Result.Last().Label == "Nineth");
        }
    }
}
