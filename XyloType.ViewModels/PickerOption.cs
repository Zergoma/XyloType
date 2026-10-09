namespace XyloType.ViewModels;

/// <summary>
/// A choice of a picker: the label is shown, the value is used.
/// </summary>
public record PickerOption<T>(string Label, T Value)
{
    public override string ToString() => Label;
}
