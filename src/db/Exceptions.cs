using System;

/// <summary>
/// Represents a non-specific exception encountered during a database operation
/// </summary>
public class DBException : Exception
{
	/// <summary>
	/// Creates a new DBException instance
	/// </summary>
	/// <returns></returns>
	public DBException() : base()
	{

	}

	/// <summary>
	/// Creates a new DBException instance
	/// </summary>
	/// <param name="message"></param>
	/// <returns></returns>
	public DBException(string message) : base(message)
	{

	}
}

/// <summary>
/// Represents an exception encountered when searching for a record that
/// does not exist
/// </summary>
public class RecordNotFoundException : DBException
{
	/// <summary>
	/// Creates a new RecordNotFoundException instance
	/// </summary>
	/// <returns></returns>
	public RecordNotFoundException() : base()
	{

	}

	/// <summary>
	/// Creates a new RecordNotFoundException instance
	/// </summary>
	/// <param name="message"></param>
	/// <returns></returns>
	public RecordNotFoundException(string message) : base(message)
	{

	}
}