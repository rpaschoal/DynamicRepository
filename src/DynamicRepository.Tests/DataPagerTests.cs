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
    }
}
