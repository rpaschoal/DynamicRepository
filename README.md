# DynamicRepository

Dynamic repository with general/common methods for repository pattern on ASP NET 5 projects using EF (Both Core and .NET CLR). 

With this package you get full CRUD data access for your repository + advanced filtering methods with almost no additional code.

You can find the NuGet packages here: https://www.nuget.org/packages?q=DynamicRepository

## Usage

Follow the steps to configure DynamicRepository on your ASP NET 5 project.

### Installing package

On your project.json file, add DynamicRepository to your package dependencies (Core if you are on CoreCLR, or .NET if you are on CLR).

```cs
"DynamicRepository.Core": "1.0.2-*"
```

### Usage

To leverage use of the package, just create a class for your entity repository (EG: MyEntityRepository.cs) and inherit from DynamicRepository.Core.Repository as following:

```cs
public class MyEntityRepository : Repository<int, MyEntity>
{
}
```

* The first type parameter is your database-entity key type. You can use a custom class type for composite primary keys or "object[]" if you prefer.
* The second type paremeter should be your entity type class.
