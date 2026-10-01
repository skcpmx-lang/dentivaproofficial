namespace DentivaPro.Application.Abstractions;

/// <summary>Loads or creates the random key used for the local encrypted database.</summary>
public interface IDatabaseKeyStore
{
    byte[] GetOrCreateDatabaseKey();
}
