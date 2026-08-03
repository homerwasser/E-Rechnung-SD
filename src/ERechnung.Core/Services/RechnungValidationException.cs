namespace ERechnung.Core.Services;

public sealed class RechnungValidationException : Exception
{
    public RechnungValidationException(IEnumerable<string> errors)
        : this(CreateErrorList(errors))
    {
    }

    private RechnungValidationException(IReadOnlyList<string> errors)
        : base(string.Join(Environment.NewLine, errors))
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }

    public IReadOnlyList<string> Fehler => Errors;

    private static IReadOnlyList<string> CreateErrorList(IEnumerable<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var errorList = errors
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .ToArray();

        if (errorList.Length == 0)
        {
            throw new ArgumentException("Mindestens ein Validierungsfehler ist erforderlich.", nameof(errors));
        }

        return Array.AsReadOnly(errorList);
    }
}
