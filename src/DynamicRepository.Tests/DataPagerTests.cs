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

            settings.Sorting.Add(new SortingSettings()
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

            settings.Sorting.Add(new SortingSettings()
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

        /// <summary>
        /// Identifies if the advanced search engine can do post query filters and data removal to child collections of a result set.
        /// </summary>
        [Fact]
        public void CanFilterPostQueryPaths()
        {
            // Arrange
            var items = MockModel.ArrangeFixture();
            var pagedSize = 5;

            var settings = new PagedDataSettings()
            {
                TotalPerPage = pagedSize
            };

            settings.Filter.Add(new FilterSettings()
            {
                PostQueryFilterPath = "ChildCollection.Label",
                Property = "ChildCollection.Label",
                Value = "First"
            });

            // Act
            var result = _dataPager.GetPagedData(items.AsQueryable(), settings);

            // Assert
            Assert.True(result.TotalRecords == 1);
            Assert.True(result.Result.Count() == 1);
            Assert.True(result.Result.First().Label == "Eigth Label");
        }

        /// <summary>
        /// Identifies if the advanced search engine can do post query filters and data removal 
        /// to child collections of a result set by exact match filters.
        /// </summary>
        [Fact]
        public void CanFilterPostQueryPathsByExactMatch()
        {
            // Arrange
            var items = MockModel.ArrangeFixture();
            var pagedSize = 5;

            var settings = new PagedDataSettings()
            {
                TotalPerPage = pagedSize
            };

            settings.Filter.Add(new FilterSettings()
            {
                Property = "ChildCollection.Label",
                Value = "First",
                IsExactMatch = true
            });

            // Act
            var result = _dataPager.GetPagedData(items.AsQueryable(), settings);

            // Assert
            Assert.True(result.TotalRecords == 0);
            Assert.True(result.Result.Count() == 0);
        }

        [Fact]
        public void CanSortPostQueryPathsByAscending()
        {
            // Arrange
            var items = MockModel.ArrangeFixture();
            var pagedSize = 10;

            var settings = new PagedDataSettings()
            {
                TotalPerPage = pagedSize
            };

            settings.Sorting.Add(new SortingSettings()
            {
                Property = "ChildCollection.Label",
                Order = SortOrderEnum.ASC
            });

            // Act
            var result = _dataPager.GetPagedData(items.AsQueryable(), settings);

            // Assert
            Assert.True(result.Result[7].ChildCollection[0].Label.Contains("First"));
            Assert.True(result.Result[7].ChildCollection[1].Label.Contains("Second"));
            Assert.True(result.Result[7].ChildCollection[2].Label.Contains("Third"));
        }

        [Fact]
        public void CanSortPostQueryPathsByDescending()
        {
            // Arrange
            var items = MockModel.ArrangeFixture();
            var pagedSize = 10;

            var settings = new PagedDataSettings()
            {
                TotalPerPage = pagedSize
            };

            settings.Sorting.Add(new SortingSettings()
            {
                Property = "ChildCollection.Label",
                Order = SortOrderEnum.DESC
            });

            // Act
            var result = _dataPager.GetPagedData(items.AsQueryable(), settings);

            // Assert
            Assert.True(result.Result[7].ChildCollection[0].Label.Contains("Third"));
            Assert.True(result.Result[7].ChildCollection[1].Label.Contains("Second"));
            Assert.True(result.Result[7].ChildCollection[2].Label.Contains("First"));  
        }

        [Fact]
        public void CanFilterAndSortPostQueryPathsByAscending()
        {
            // Arrange
            var items = MockModel.ArrangeFixture();
            var pagedSize = 5;

            var settings = new PagedDataSettings()
            {
                TotalPerPage = pagedSize
            };

            settings.Filter.Add(new FilterSettings()
            {
                Property = "ChildCollection.Label",
                Value = "First"
            });

            settings.Sorting.Add(new SortingSettings()
            {
                Property = "ChildCollection.Label",
                Order = SortOrderEnum.ASC
            });

            // Act
            var result = _dataPager.GetPagedData(items.AsQueryable(), settings);

            // Assert
            Assert.True(result.Result[0].ChildCollection[0].Label.Contains("First"));
            Assert.True(result.Result[0].ChildCollection[1].Label.Contains("Second"));
            Assert.True(result.Result[0].ChildCollection[2].Label.Contains("Third"));
        }

        [Fact]
        public void CanFilterAndSortPostQueryPathsByDescending()
        {
            // Arrange
            var items = MockModel.ArrangeFixture();
            var pagedSize = 5;

            var settings = new PagedDataSettings()
            {
                TotalPerPage = pagedSize
            };

            settings.Filter.Add(new FilterSettings()
            {
                Property = "ChildCollection.Label",
                Value = "First"
            });

            settings.Sorting.Add(new SortingSettings()
            {
                Property = "ChildCollection.Label",
                Order = SortOrderEnum.DESC
            });

            // Act
            var result = _dataPager.GetPagedData(items.AsQueryable(), settings);

            // Assert
            Assert.True(result.Result[0].ChildCollection[0].Label.Contains("Third"));
            Assert.True(result.Result[0].ChildCollection[1].Label.Contains("Second"));
            Assert.True(result.Result[0].ChildCollection[2].Label.Contains("First"));
        }
    }
}
