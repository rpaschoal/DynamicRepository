using System;
using System.Text;
using System.Collections.Generic;
using DynamicRepository.Tests.Support;
using DynamicRepository;
using Xunit;
using DynamicRepository.Filter;
using System.Linq;
using DynamicRepository.Annotation;
using System.Reflection;

namespace DynamicRepository.Tests
{
    public class DataPagerAdapterTests
    {
        [Fact]
        [PagedDataFilter("UIFirstProperty", "BackEndFirstProperty")]
        [PagedDataFilter("UISecondProperty", "BackEndSecondProperty")]
        public void MustTranformPagedDataAttributesWhenFiltering()
        {
            // Arrange
            var items = MockModel.ArrangeFixture();

            var settings = new PagedDataSettings()
            {
            };

            settings.Filter.Add(new FilterSettings()
            {
                Property = "UIFirstProperty",
                Value = items.First().Label,
                IsExactMatch = true
            });

            settings.Filter.Add(new FilterSettings()
            {
                Property = "UISecondProperty",
                Value = items.Last().Label,
                IsExactMatch = true
            });

            // Act
            var transformedSettings = DataPagerAdapter.TransformSettings(settings, this.GetType().GetMethod(nameof(MustTranformPagedDataAttributesWhenFiltering)));

            // Assert
            Assert.Equal(transformedSettings.Filter.Count, settings.Filter.Count);
            Assert.Equal(transformedSettings.Sorting.Count, settings.Sorting.Count);
            Assert.True(transformedSettings.Filter.Where(x => x.Property == "BackEndFirstProperty").Any());
            Assert.True(transformedSettings.Filter.Where(x => x.Property == "BackEndSecondProperty").Any());
            Assert.True(transformedSettings.Filter.Where(x => x.Property == "BackEndFirstProperty").FirstOrDefault().Value.Contains("First"));
            Assert.True(transformedSettings.Filter.Where(x => x.Property == "BackEndSecondProperty").FirstOrDefault().Value.Contains("Nineth"));
        }

        [Fact]
        [PagedDataFilter("UIFirstProperty", "BackEndFirstProperty")]
        [PagedDataFilter("UISecondProperty", "BackEndSecondProperty")]
        public void MustTranformPagedDataAttributesWhenSorting()
        {
            // Arrange
            var items = MockModel.ArrangeFixture();

            var settings = new PagedDataSettings()
            {
            };

            settings.Sorting.Add(new SortingSettings()
            {
                Property = "UIFirstProperty"
            });

            settings.Sorting.Add(new SortingSettings()
            {
                Property = "UISecondProperty"
            });

            // Act
            var transformedSettings = DataPagerAdapter.TransformSettings(settings, this.GetType().GetMethod(nameof(MustTranformPagedDataAttributesWhenSorting)));

            // Assert
            Assert.Equal(transformedSettings.Filter.Count, settings.Filter.Count);
            Assert.Equal(transformedSettings.Sorting.Count, settings.Sorting.Count);
            Assert.True(transformedSettings.Sorting.Where(x => x.Property == "BackEndFirstProperty").Any());
            Assert.True(transformedSettings.Sorting.Where(x => x.Property == "BackEndSecondProperty").Any());
        }

        [Fact]
        [PagedDataDefaultSorting("MyDefaultSortingProperty", false)]
        public void ShouldSetDefaultSortingByAttributes()
        {
            // Arrange
            var items = MockModel.ArrangeFixture();

            var settings = new PagedDataSettings()
            {
            };

            // Act
            var transformedSettings = DataPagerAdapter.TransformSettings(settings, this.GetType().GetMethod(nameof(ShouldSetDefaultSortingByAttributes)));

            // Assert
            Assert.Equal(transformedSettings.Sorting.Count, 1);
            Assert.True(transformedSettings.Sorting.Where(x => x.Property == "MyDefaultSortingProperty").Any());
        }
    }
}
