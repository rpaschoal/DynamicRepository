# DynamicRepository

Dynamic repository with many built-in data access and handling methods for Repository Pattern on ASP NET Core projects for EF6 (CoreCLR and .NET Framework 4.5.1), EF Core and MongoDB. 

With this package you get instant full CRUD data access for your project + advanced filtering methods with almost no additional code.

You can find the NuGet packages here: https://www.nuget.org/packages?q=DynamicRepository

## Features

### Full Crud Behavior

```cs
MyEntityRepository.Insert(MyEntity);
MyEntityRepository.Get(1);
MyEntityRepository.Update(MyEntity);
MyEntityRepository.Delete(MyEntity);
MyEntityRepository.Delete(1);
```

### Basic Filtering

```cs
MyEntityRepository.List(x => x.Name.Contains("Rafael"), x => x.OrderBy(y => y.Name));
```

Filters by data containing "Rafael", sorts by name property

### Advanced Search Engine

In most applications there is a need to filter paged-indexed collections (Useful for grids/lists). You can use it as following:

```cs
MyEntityRepository.GetPagedData(this.List(), settings);
```

Where "settings" is an instance of "PagedDataSettings" class. In this class you can define filters (+ conjunctions), sort, current page and result set size.

More on the usage of this can be found later on this documentation on the "Advanced Search Usage" section bellow.

## Usage

Follow the next steps to configure the DynamicRepository on your ASP NET Core project.

### Installing package

On your project.json file, add DynamicRepository to your package dependencies (Core if you are on CoreCLR, or .NET if you are on CLR).

```cs
"DynamicRepository.Core": "1.1.0-*"
```

### Implementation

To leverage the use of the package, just create a class for your entity repository (EG: MyEntityRepository.cs) and inherit from DynamicRepository.Core.Repository as following:

```cs
public class MyEntityRepository : Repository<int, MyEntity>
{
  // This is where you provide EF DB Context to "Repository" class. This should be your custom EF context. 
  public MyEntityRepository(DbContext context) : base(context)
  {
  }

  /* Any specialized methods comes here */
}
```

* The first type parameter is your database-entity key type. You can use a custom class type for composite primary keys or "object[]" if you prefer.
* The second type paremeter should be your entity type class.

### Implementation (With Contracts)

If you use interfaces to define your repositories, follow this approach:

```cs
public class MyEntityRepository : Repository<int, MyEntity>, IMyEntityRepository
{
  // This is where you provide EF DB Context to "Repository" class. This should be your custom EF context.
  // As you are using interfaces I may assume you are also using DI. You can set DbContext to be inject through DI.
  public MyEntityRepository(DbContext context) : base(context)
  {
  }

  /* Any specialized methods comes here */
}
```

And on your interface define it as this:

```cs
public interface IMyEntityRepository :  IRepository<int, MyEntity>
{
  /* Any additional methods definitions comes here */
}
```

### Advanced Search Usage

On your WebAPI or MVC Controller call your Repository implementation search as following:

```cs
MyEntityRepository.GetPagedData(this.List(), settings);
```

* The first argument is your IQueryable data source. For most cases just use the repository's list method which will return the current DbSet as the source.
* The second argument is a payload with all the settings for the advanced search defined by the "PagedDataSettings" class. This payload will be discussed further bellow.

#### The PagedDataSettings class and relevant configuration

The "PagedDataSettings" defines the payload for the Advanced Search Engine. Following you can find the details of all relevant configuration:

* "Filter" Property: This is a collection of "FilterSettings" class which holds all properties definitions and values to be applied as search arguments.
* "Order" Property: This is a collection of "SortingSettings" with all properties that should be sorted by in the search result set.
* "Page" Property: This is a number that identifies the current page of the result set. This starts by 1.
* "TotalPerPage" Property: The total rows that will be returned per page on the result set. Default is 20.

#### The FilterSettings class and relevant configuration

* "Property" Property: This is the name of the property you want to filter in your search. You can use dot notation as you would use in any lambda expression. EG: MyEntityRelation.ChildEntityProperty (No nesting limit if there are no collections involved in the path). You can also use deep filtering into child collections (one level of nesting limit) as following EG: MyEntityRelationCollection.ChildEntityProperty.
* "Value" Property: This is any value to be matched as a "Contains" filter on the search. If you want full match of this value, you need to set the "IsExactMatch" Property to TRUE.
* "IsExactMatch" Property: If setted to TRUE will match the "Value" property completely. Defaults to FALSE.
* "Conjunction" Property: This defines how this filter will be aggregated among many other filters on the payload. Most searchs on common scenarios use "AND" and this is the default but if you want you can use an "OR" conjunction as available in the "LogicalConjunctionEnum".
* "PostQueryFilterPath" Property: This is useful when you use deep nested collection filtering. This will remove the result value of a resulting Select expression in a child colletion of the main result set batch. In EF you always get the full object graph so this is useful to remove undesired child collections content after executing the IQueryable search on your database.

## Dependencies

This project was implemented on top of these dependencies:

* https://github.com/kahanu/System.Linq.Dynamic
* https://github.com/StefH/System.Linq.Dynamic.Core
* https://github.com/scottksmith95/LINQKit

Many thanks!
