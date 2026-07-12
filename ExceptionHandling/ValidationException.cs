namespace PowerPlantChallenge.ExceptionHandling;

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }
}