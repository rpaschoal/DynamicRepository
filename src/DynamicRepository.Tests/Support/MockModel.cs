using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicRepository.Tests.Support
{
    /// <summary>
    /// Simple POCO to be used under this test project.
    /// </summary>
    public sealed class MockModel
    {
        public long Id { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Provides a collection of data to be used in arrange phase of unit tests.
        /// </summary>
        /// <remarks>
        /// TODO: Integrate with AutoFixture project for future test growth.
        /// </remarks>
        /// <returns>
        /// Collection of fixtured data for tests.
        /// </returns>
        public static IList<MockModel> ArrangeFixture()
        {
            return new List<MockModel>()
            {
                new MockModel()
                {
                    Id = 1,
                    Label = "First Label"
                },
                new MockModel()
                {
                    Id = 1,
                    Label = "Second Label"
                },
                new MockModel()
                {
                    Id = 1,
                    Label = "Third Label"
                },
                new MockModel()
                {
                    Id = 1,
                    Label = "Fourth Label"
                },
                new MockModel()
                {
                    Id = 1,
                    Label = "Fifth Label"
                },
                new MockModel()
                {
                    Id = 1,
                    Label = "Sixth Label"
                },
                new MockModel()
                {
                    Id = 1,
                    Label = "Seventh Label"
                },
                new MockModel()
                {
                    Id = 1,
                    Label = "Eigth Label"
                },
                new MockModel()
                {
                    Id = 1,
                    Label = "Nineth Label"
                }
            };
        }
    }
}
