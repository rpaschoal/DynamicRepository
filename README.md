# DynamicRepository

Dynamic repository with general/common methods for repository pattern on ASP NET 5 projects using EF (Both Core and .NET CLR). 

With this package you get full CRUD data access for your repository + advanced filtering methods with almost no additional code.

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

### Filtering

```cs
MyEntityRepository.List(x => x.Name.Contains("Rafael"), x => x.OrderBy(y => y.Name));
```

Filters by data containing "Rafael", sorts by name property

### Paged filtering

In most applications there is a need to filter paged-indexed collections (Useful for grids/lists). You can use it as following:

```cs
MyEntityRepository.GetPagedDataSource(settings);
```

Where "settings" is of "PagedDataSourceSettings" class. In this class you can define filters (+ conjunctions), sort, page and batch size.

## Usage

Follow the next steps to configure DynamicRepository on your ASP NET 5 project.

### Installing package

On your project.json file, add DynamicRepository to your package dependencies (Core if you are on CoreCLR, or .NET if you are on CLR).

```cs
"DynamicRepository.Core": "1.0.2-*"
```

### Implementation

To leverage use of the package, just create a class for your entity repository (EG: MyEntityRepository.cs) and inherit from DynamicRepository.Core.Repository as following:

```cs
public class MyEntityRepository : Repository<int, MyEntity>
{
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
