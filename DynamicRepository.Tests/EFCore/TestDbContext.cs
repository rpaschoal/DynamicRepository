using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DynamicRepository.Tests.EFCore
{
    public class TestDbContext : DbContext
    {
        public TestDbContext()
        {
        }

        public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options)
        { }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        public class TestEntityClass
        {
            public int Id { get; set; }

            public Guid TenantId { get; set; }

            public string Title { get; set; }
        }

        public DbSet<TestEntityClass> TestEntityClasses { get; set; }
    }
}
