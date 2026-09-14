namespace POS.Application.Abstractions.Persistence;

// Translates the database's unique access constraint without exposing provider types to handlers.
public sealed class DuplicateStoreAccessException(Exception innerException)
    : Exception("Employee already has access to this store.", innerException);
