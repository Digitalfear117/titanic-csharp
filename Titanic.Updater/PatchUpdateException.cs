namespace Titanic.Updater;

public class PatchUpdateException : Exception
{
    public PatchUpdateException(string message) : base(message)
    {}

    public PatchUpdateException(string message, Exception innerException) : base(message, innerException)
    {}
}
