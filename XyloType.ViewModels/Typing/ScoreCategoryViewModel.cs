using System.ComponentModel;

using CommunityToolkit.Mvvm.ComponentModel;

namespace XyloType.ViewModels.Typing;

/// <summary>
/// A kind of music in the settings: one switch for all its pieces, and the pieces themselves (folded).
/// </summary>
public partial class ScoreCategoryViewModel : ObservableObject
{
    // true while the category switch changes its pieces: they must not update the switch back
    private bool _isApplyingToPieces;

    public ScoreCategoryViewModel(string name, IReadOnlyList<ScoreOptionViewModel> pieces)
    {
        Name = name;
        Pieces = pieces;

        foreach (ScoreOptionViewModel piece in pieces)
            piece.PropertyChanged += OnPiecePropertyChanged;
    }

    public string Name { get; }

    public IReadOnlyList<ScoreOptionViewModel> Pieces { get; }

    public int EnabledCount => Pieces.Count(p => p.IsEnabled);

    public string CountText => $"{EnabledCount}/{Pieces.Count}";

    /// <summary>
    /// On when at least one piece is played: switching it on or off applies to every piece.
    /// </summary>
    public bool IsEnabled
    {
        get => EnabledCount > 0;
        set
        {
            if (value == IsEnabled && (value == false || EnabledCount == Pieces.Count))
                return;

            _isApplyingToPieces = true;
            try
            {
                foreach (ScoreOptionViewModel piece in Pieces)
                    piece.IsEnabled = value;
            }
            finally
            {
                _isApplyingToPieces = false;
            }

            RefreshCounts();
        }
    }

    private void OnPiecePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScoreOptionViewModel.IsEnabled) && !_isApplyingToPieces)
            RefreshCounts();
    }

    private void RefreshCounts()
    {
        OnPropertyChanged(nameof(IsEnabled));
        OnPropertyChanged(nameof(EnabledCount));
        OnPropertyChanged(nameof(CountText));
    }
}
