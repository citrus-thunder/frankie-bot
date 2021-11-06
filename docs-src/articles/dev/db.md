# Database Design and Operation

## Introduction

Many of Frankie's services involve storing information about a community or its members. Frankie accomplishes this by storing data for each community in its own separate SQLite database file.

This article walks through the process for storing and retrieving this information and touches on the Model/View/ViewModel paradigm used in Frankie's database API.

## The MVVM Pattern
Frankie's database API subscribes to the MVVM pattern (Model/View/ViewModel), which separates the application (the View) from the data objects used directly by the database (the Model) via an intermediary object called a ViewModel. The ViewModel facilitates interaction between the application and the database by translating raw database model information into useful application-ready properties, and by translating operations on these properties into equivalent database operations.

We'll illustrate this paradigm with Frankie's quote module.

### The Quote Model
The Quote model is a simple class containing properties that represent each field of a quote record in the database, such as the quote's author, the recorder, and when the quote was recorded. The sole responsibility of this class, as for all models, is simply to represent the data of a single quote record; it contains no methods to convert, update, or even save the data to the database.

### The Quote ViewModel
The Quote viewmodel is far more interesting than its model counterpart. It contains properties equivalent to those in the Quote model, but these properties are not necessarily identical. Where in the model, we are restricted to properties that explicitly reflect exactly the data in the database, we do not face such restrictions in the viewmodel.

For example, the `Author` property in the Quote model contains nothing more than a string representation of the quote author's Discord user ID. The equivalent property in the viewmodel contains an actual reference to an `IUser` object representing the Author, which can be consumed directly by the application without doing any lookups or conversions!

Further, if we were to want to change the author of a quote, we wouldn't have to find the new author's id. Instead, we could simply assign a new `IUser` to the viewmodel's `Author` property and the viewmodel would automatically perform the correct conversion when saving.

## The DataBaseService
Discord's API is built upon two primary concepts: Modules and Services. Where Modules purely provide the command interface to the users, the Services contain the core business logic of the application.

The `DataBaseService` is one such service, and serves as the endpoint with which to access database resources. Any module that requires access to read from or update the database should reference this service, and all database transactions should take place through `DataBaseService.RunDBAction()`. This ensures that the database is configured and ready to receive actions and that the actions are peformed in the proper database.

## Frankie's Database API
To best utilize the MVVM pattern, Frankie's Database workflow revolves around the use of `ViewModel` and `ViewModelContainer` instances. Generally, unless working deep under the hood, one should avoid ever working directly with `Model` instances, or directly using the `ViewModel.Model` property.

The common workflow looks something like this:

```csharp
// Get reference to DataBaseService
DataBaseService _db;

// Wrap database action in DataBaseService.RunDBAction()
await _db.RunDBAction(Context, (context) => 
{
	// Establish database connection via new DBConnection instance
	using (var connection = new DBConnection(context, _db.GetServerDBFilePath(context.Guild))
	{
	// Example 1: Get Existing Quotes from the user who sent the command

		// We convert the Author.Id to a string, as that is how it is stored in the database
		// (ulong isn't yet supported in our SQLite connector)
		var authorID = context.Message.Author.Id;

		// We use As<Quote> since otherwise this would return a generic ViewModel<Quote>
		// object when we specifically want a Quote object
		var quote = Quote.FindOne(connection, (q) => q.AuthorID == authorID).As<Quote>();

	// Example 2: Update existing quote's recorded timestamp

		// Edit the Quote's RecordTimeStamp property.
		quote.RecordTimeStamp = DateTime.UtcNow;

		// Save the edit. That's it!
		quote.Save();

	// Example 3: Create a new "Hello World" quote by the user who sent the command
		
		// Create new Quote object
		var newQuote = new Quote(connection)
		{
			Author = context.Message.Author,
			Content = "Hello, World!",
		}

		// Save quote object to database
		newQuote.Save();
	}
});
```
As illustrated, we are able to retrieve viewmodels by searching for them through the database connection, and we can even create our own new viewmodels which will become new models in the database upon saving. We are able to update records by simply altering the viewmodel's properties which converts those alterations to equivalent database operations.

### ViewModelContainers
Where the `ViewModel` class allows us to easily perform edit, update, and delete operations upon the model it represents, the `ViewModelContainer` class takes it one step further. With a container, mass operations can be performed on all viewmodels contained within.

### Getting a ViewModelContainer
In the above example, we used `Quote.FindOne()` to find the first record matching our expression. This returns a `ViewModel` instance directly, as there isn't a need for a container containing a single viewmodel.

In most cases, we will be using the `Find()` method instead, which will return *all* records matching the given expression. These records are returned as a `ViewModelContainer` containing viewmodels matching the expression.

Right away, we can use this collection to access the matching viewmodels through its `Content` property, and mass-apply `Save()` or `Delete()` to every viewmodel within. Coupling a ViewModelContainer with linq's `ForEach()` method makes mass-updating the models contained within easy -- with a single caveat.

It is important to note that the viewmodels contained within a container upon creation are simple, generic viewmodels which only contain default operations (Save, Delete, and a direct reference to the underlying Model, *which should not be manipulated directly*). To access properties and methods defined in concrete viewmodels (such as `Quote`), the container's contents must be converted to the concrete implementation via `ContentAs<T>()`.
```csharp
// Example: Getting a container of Quote viewmodels -- not "ViewModel<Quote>" viewmodels
var quote = Quote.Find(connection, (q) => q.AuthorID = authorID).ContentAs<Quote>();
```
Now, with the ViewModelContainer's contents casted as `Quote` viewmodels, one can access each item's `Quote` properties

### Custom ViewModelContainer Implementations
As with viewmodels, while the generic `ViewModelContainer` class provides useful default operations, much more useful containers can be created by deriving `ViewModelContainer` and adding additional properties and methods.

One such example of this is the `Options` class, which is derived from `ViewModelContainer<Option>`. With the `Options` container, not only can we mass save and delete options, we can use `Options.Get()` and `Options.Set()` to easily update options records without having to manually find and manipulate individual `Option` viewmodels.

```csharp
// Example: Updating options records with the Options container

// We use As<C, O>() to convert the results from a generic ViewModelContainer<Option> 
// containing generic ViewModel<Option> objects to an Options container, which must contain Option objects
var options = Option.FindAll(connection).As<Options, Option>();

// Update option titled "command_prefix" to value "!"
options.Set("command_prefix", "!");

// Save updated option value
options.Save();
```

## Conclusion
Grasping the MVVM pattern, becoming familiar with the `Model`, `ViewModel`, and `ViewModelContainer` classes and their more concrete subclasses, and getting comfortable with the `Find()`, `FindOne()`, `FindAll()`, `ContentAs<T>()`, `ContainerAs<T>()`, and `As<C,O>()` methods will make understanding and contributing to Frankie much easier. We hope this guide can help get you on your way to making the most of Frankie.