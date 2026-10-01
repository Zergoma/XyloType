using XyloType.Application.Interfaces;
using XyloType.Domain.Music;

namespace XyloType.Application.Services;

/// <summary>
/// Melodies in the public domain (classical, traditional, ragtime, Christmas, American folk), transcribed as note sequences.
/// The octaves are only relative: <see cref="Melody"/> transposes them to the instrument range.
/// Many melodies were converted from ABC notation found on abcnotation.com (collections such as
/// Anamnese, John Chambers' tune archive, the Nottingham Music Database, P. G. Hardy and Colin Hume tunebooks),
/// keeping only the melody line (durations are given by the typing). Transcriptions marked as copyrighted were not used.
/// </summary>
public class ScoreCatalog : IScoreCatalog
{
    private static readonly IReadOnlyList<Score> s_scores =
    [
        // ===== Classical =====

        new("beethoven-ode-to-joy", "Ode à la joie", "Ludwig van Beethoven", Notes.ParseMelody("""
            E5 E5 F5 G5 G5 F5 E5 D5 C5 C5 D5 E5 E5 D5 D5
            E5 E5 F5 G5 G5 F5 E5 D5 C5 C5 D5 E5 D5 C5 C5
            D5 D5 E5 C5 D5 E5 F5 E5 C5 D5 E5 F5 E5 D5 C5 D5 G4
            E5 E5 F5 G5 G5 F5 E5 D5 C5 C5 D5 E5 D5 C5 C5
            """)),

        new("beethoven-fur-elise", "Lettre à Élise", "Ludwig van Beethoven", Notes.ParseMelody("""
            E5 D#5 E5 D#5 E5 B4 D5 C5 A4 C4 E4 A4 B4 E4 G#4 B4 C5 E4
            E5 D#5 E5 D#5 E5 B4 D5 C5 A4 C4 E4 A4 B4 E4 C5 B4 A4
            B4 C5 D5 E5 G4 F5 E5 D5 F4 E5 D5 C5 E4 D5 C5 B4
            E5 D#5 E5 D#5 E5 B4 D5 C5 A4 C4 E4 A4 B4 E4 G#4 B4 C5 E4
            E5 D#5 E5 D#5 E5 B4 D5 C5 A4 C4 E4 A4 B4 E4 C5 B4 A4
            """)),

        new("mozart-ah-vous-dirai-je-maman", "Ah ! vous dirai-je, maman", "Wolfgang Amadeus Mozart", Notes.ParseMelody("""
            C5 C5 G5 G5 A5 A5 G5 F5 F5 E5 E5 D5 D5 C5
            G5 G5 F5 F5 E5 E5 D5 G5 G5 F5 F5 E5 E5 D5
            C5 C5 G5 G5 A5 A5 G5 F5 F5 E5 E5 D5 D5 C5
            """)),

        new("grieg-mountain-king", "Dans l'antre du roi de la montagne", "Edvard Grieg", Notes.ParseMelody("""
            A4 B4 C5 D5 E5 C5 E5 Eb5 B4 Eb5 D5 Bb4 D5
            A4 B4 C5 D5 E5 C5 E5 A5 G5 E5 C5 E5 G5
            E5 F#5 G5 A5 B5 G5 B5 Bb5 F#5 Bb5 A5 F5 A5
            E5 F#5 G5 A5 B5 G5 B5 E6 D6 B5 G5 B5 D6
            """)),

        new("beethoven-fifth-symphony", "Symphonie n° 5", "Ludwig van Beethoven", Notes.ParseMelody("""
            G4 G4 G4 Eb4 F4 F4 F4 D4
            G4 G4 G4 Eb4 Ab4 Ab4 Ab4 G4 Eb5 Eb5 Eb5 C5
            """)),

        new("mozart-eine-kleine-nachtmusik", "Une petite musique de nuit", "Wolfgang Amadeus Mozart", Notes.ParseMelody("""
            G5 D5 G5 D5 G5 D5 G5 B5 D6
            C6 A5 C6 A5 C6 A5 F#5 A5 D5
            """)),

        new("mozart-turkish-march", "Marche turque", "Wolfgang Amadeus Mozart", Notes.ParseMelody("""
            B4 A4 G#4 A4 C5 D5 C5 B4 C5 E5
            F5 E5 D#5 E5 B5 A5 G#5 A5 B5 A5 G#5 A5 C6
            """)),

        new("bach-petzold-minuet-in-g", "Menuet en sol", "Christian Petzold (attribué à J.-S. Bach)", Notes.ParseMelody("""
            D5 G4 A4 B4 C5 D5 G4 G4 E5 C5 D5 E5 F#5 G5 G4 G4
            C5 D5 C5 B4 A4 B4 C5 B4 A4 G4 F#4 G4 A4 B4 G4 A4
            D5 G4 A4 B4 C5 D5 G4 G4 E5 C5 D5 E5 F#5 G5 G4 G4
            C5 D5 C5 B4 A4 B4 C5 B4 A4 G4 A4 B4 A4 G4 F#4 G4
            """)),

        new("brahms-lullaby", "Berceuse", "Johannes Brahms", Notes.ParseMelody("""
            E5 E5 G5 E5 E5 G5 E5 G5 C6 B5 A5 A5 G5
            D5 E5 F5 D5 D5 E5 F5 D5 F5 B5 A5 G5 B5 C6
            C5 C5 C6 A5 F5 G5 E5 C5 F5 G5 A5 G5
            C5 C5 C6 A5 F5 G5 E5 C5 F5 E5 D5 C5
            """)),

        new("strauss-blue-danube", "Le Beau Danube bleu", "Johann Strauss II", Notes.ParseMelody("""
            D4 D4 F#4 A4 A4 A5 A5 F#5 F#5
            D4 D4 F#4 A4 A4 A5 A5 G5 G5
            C#4 C#4 E4 B4 B4 B5 B5 G5 G5
            C#4 C#4 E4 B4 B4 B5 B5 F#5 F#5
            """)),

        new("dvorak-new-world-largo", "Symphonie du Nouveau Monde (Largo)", "Antonín Dvořák", Notes.ParseMelody("""
            E5 G5 G5 E5 D5 C5 D5 E5 G5 E5 D5
            E5 G5 G5 E5 D5 C5 D5 E5 D5 C5 C5
            """)),

        new("grieg-morning-mood", "Au matin", "Edvard Grieg", Notes.ParseMelody("""
            G5 E5 D5 C5 D5 E5 G5 E5 D5 C5 D5 E5
            D5 E5 G5 E5 G5 A5 E5 A5 G5 E5 D5 C5
            """)),

        new("pachelbel-canon", "Canon en ré", "Johann Pachelbel", Notes.ParseMelody("""
            F#5 E5 D5 C#5 B4 A4 B4 C#5 D5 C#5 B4 A4 G4 F#4 G4 E4
            D4 F#4 A4 G4 F#4 D4 F#4 E4 D4 B4 D4 A4 G4 B4 A4 G4
            F#4 D4 E4 C#5 D5 F#5 A5 A4 B4 G4 A4 F#4 D4 D5 D5 C#5
            D5 C#5 D5 D5 C#5 A4 E4 F#4 D4 D5 C#5 B4 C#5 F#5 A5 B5
            G5 F#5 E5 G5 F#5 E5 D5 C#5 B4 A4 G4 F#4 E4 G4 F#4 E4
            D4 E4 F#4 G4 A4 E4 A4 G4 F#4 B4 A4 G4 A4 G4 F#4 E4
            D4 B4 B4 C#5 D5 C#5 B4 A4 G4 F#4 E4 B4 A4 B4 A4 G4
            F#4 F#5 E5 D5 F#5 B4 A4 B4
            """), ScoreCategories.Classical),

        new("rossini-william-tell", "Guillaume Tell (ouverture)", "Gioachino Rossini", Notes.ParseMelody("""
            G4 G4 G4 G4 G4 G4 G4 C5 D5 E5 G4 G4 G4 G4 G4 C5
            E5 D5 B4 G4 G4 G4 G4 G4 G4 G4 G4 C5 D5 E5 C5 E5
            G5 F5 E5 D5 C5 E5 C5 G4 G4 G4 G4 G4 G4 G4 C5 D5
            E5 G4 G4 G4 G4 G4 C5 E5 D5 B4 G4 G4 G4 G4 G4 G4
            G4 G4 C5 D5 E5 C5 E5 G5 F5 E5 D5 C5 E5 C5 E5 E5
            E5 E5 E5 E5 E5 E5 A5 E5 A5 E5 A5 E5 D5 C5 B4 A4
            E5 E5 E5 E5 E5 E5 E5 E5 A5 E5 A5 E5 A5 G5 F#5 G5
            D5 D5 D5 D5 D5 E5 F5 D5 F5 E5 C5 E5 D5 G4 D5 D5
            D5 D5 D5 E5 F5 D5 F5 E5 C5 E5 D5 G4 G4 G4 G4 G4
            G4 G4 G4 C5 D5 E5 G4 G4 G4 G4 G4 C5 E5 D5 B4 G4
            G4 G4 G4 G4 G4 G4 G4 C5 D5 E5 C5 E5 G5 F5 E5 D5
            C5 E5 C5 G4 G4 G4 G4 G4 G4 G4 C5 D5 E5 G4 G4 G4
            G4 G4 C5 E5 D5 B4 G4 G4 G4 G4 G4 G4 G4 G4 C5 D5
            E5 C5 E5 G5 F5 E5 D5 C5 E5 C5 C5 C5 C5 C5 C5 E5
            D5 C5 B4 C5 A4 G4 A4 G4 A4 G4 A4 B4 C5 F4 G4 F4
            G4 F4 G4 A4 B4 E4 F4 E4 F4 E4 F4 G4 A4 D4 E4 D4
            E4 D4 E4 D4 E4 D4 G4 G4 G4 G4 C5 C5 C5 C5 C5 E5
            D5 C5 B4 C5 A4 G4 A4 G4 A4 G4 A4 B4 C5 F4 G4 F4
            G4 F4 G4 A4 B4 E4 F4 E4 F4 E4 F4 G4 A4 D4 E4 G4
            F4 E4 D4 C4 G4 G4 G4 G4 G4 G4 G4 C5 D5 E5 G4 G4
            G4 G4 G4 G4 G4 E5 F5 G5 C5 D5 E5 E4 F4 G4 B4 C5
            B4 C5 B4 C5 B4 C5 B4 C5 C5 C5 C5 C5 C5 E5 E5 C5
            C5
            """), ScoreCategories.Classical),

        new("bach-jesu-joy", "Jésus que ma joie demeure", "Jean-Sébastien Bach", Notes.ParseMelody("""
            G4 A4 B4 D5 C5 C5 E5 D5 D5 G5 F#5 G5 D5 B4 G4 A4
            B4 C5 D5 E5 D5 C5 B4 A4 B4 G4 F#4 G4 A4 D4 F#4 A4
            C5 B4 A4 B4 G4 A4 B4 D5 C5 C5 E5 D5 D5 G5 F#5 G5
            D5 B4 G4 A4 B4 A4 D5 C5 B4 A4 G4 D4 G4 F#4 G4 B4
            D5 G5 D5 B4 G4 B4 D5 G5 D4 E4 F#4 A4 G4 A4 C5 B4
            C5 A4 F#4 D4 F#4 A4 C5 B4 A4 B4 G4 A4 B4 D5 C5 C5
            E5 D5 D5 G5 F#5 G5 D5 B4 G4 A4 B4 E4 D5 C5
            """), ScoreCategories.Classical),

        // ===== Traditional =====

        new("trad-au-clair-de-la-lune", "Au clair de la lune", "Chanson traditionnelle française", Notes.ParseMelody("""
            C5 C5 C5 D5 E5 D5 C5 E5 D5 D5 C5
            C5 C5 C5 D5 E5 D5 C5 E5 D5 D5 C5
            D5 D5 D5 D5 A4 A4 D5 C5 B4 A4 G4
            C5 C5 C5 D5 E5 D5 C5 E5 D5 D5 C5
            """), ScoreCategories.Traditional),

        new("trad-frere-jacques", "Frère Jacques", "Chanson traditionnelle française", Notes.ParseMelody("""
            C5 D5 E5 C5 C5 D5 E5 C5
            E5 F5 G5 E5 F5 G5
            G5 A5 G5 F5 E5 C5 G5 A5 G5 F5 E5 C5
            C5 G4 C5 C5 G4 C5
            """), ScoreCategories.Traditional),

        new("trad-greensleeves", "Greensleeves", "Chanson traditionnelle anglaise", Notes.ParseMelody("""
            A4 C5 D5 E5 F5 E5 D5 B4 G4 A4 B4 C5 A4 A4 G#4 A4 B4 G#4 E4
            A4 C5 D5 E5 F5 E5 D5 B4 G4 A4 B4 C5 B4 A4 G#4 F#4 G#4 A4 A4
            """), ScoreCategories.Traditional),

        new("trad-korobeiniki", "Korobeïniki", "Chanson traditionnelle russe", Notes.ParseMelody("""
            E5 B4 C5 D5 C5 B4 A4 A4 C5 E5 D5 C5 B4 C5 D5 E5 C5 A4 A4
            D5 F5 A5 G5 F5 E5 C5 E5 D5 C5 B4 B4 C5 D5 E5 C5 A4 A4
            """), ScoreCategories.Traditional),

        new("trad-sur-le-pont-d-avignon", "Sur le pont d'Avignon", "Chanson traditionnelle française", Notes.ParseMelody("""
            C5 C5 C5 D5 D5 D5 E5 F5 G5 C5 B4 C5 D5 G4 C5 C5
            C5 D5 D5 D5 E5 F5 G5 C5 D5 B4 C5
            """), ScoreCategories.Traditional),

        new("trad-a-la-claire-fontaine", "À la claire fontaine", "Chanson traditionnelle française", Notes.ParseMelody("""
            G4 G4 B4 B4 A4 B4 A4 G4 G4 B4 B4 A4 B4 G4 G4 B4
            B4 A4 B4 A4 G4 G4 B4 B4 A4 B4 B4 B4 A4 G4 B4 D5
            B4 D5 D5 B4 G4 B4 A4 G4 G4 B4 B4 A4 G4 B4 G4 B4
            B4 A4 G4 B4 A4 G4 B4 B4 A4 G4 B4 D5 B4 D5 D5 B4
            G4 B4 A4 G4 G4 B4 B4 A4 G4 B4 G4 B4 B4 A4 G4 B4
            A4 G4
            """), ScoreCategories.Traditional),

        new("trad-j-ai-du-bon-tabac", "J'ai du bon tabac", "Chanson traditionnelle française", Notes.ParseMelody("""
            C4 D4 E4 C4 D4 D4 E4 F4 F4 E4 E4 C4 D4 E4 C4 D4
            D4 E4 F4 G4 C4 C4 D4 E4 C4 D4 D4 E4 F4 F4 E4 E4
            C4 D4 E4 C4 D4 D4 E4 F4 G4 C4 G4 G4 F4 E4 D4 E4
            F4 G4 F4 E4 G4 G4 F4 E4 D4 E4 F4 G4 D4 C4 D4 E4
            C4 D4 D4 E4 F4 F4 E4 E4 C4 D4 E4 C4 D4 D4 E4 F4
            G4 C4
            """), ScoreCategories.Traditional),

        new("trad-le-bon-roi-dagobert", "Le bon roi Dagobert", "Chanson traditionnelle française", Notes.ParseMelody("""
            B4 B4 A4 A4 G4 G4 A4 B4 C5 B4 A4 G4 A4 G4 B4 B4
            A4 A4 G4 G4 A4 B4 C5 B4 A4 G4 A4 G4 G4 A4 B4 B4
            B4 C5 D5 A4 A4 A4 G4 A4 B4 B4 B4 C5 D5 A4 A4 A4
            B4 B4 A4 A4 G4 G4 A4 B4 C5 B4 A4 G4 A4 G4
            """), ScoreCategories.Traditional),

        new("trad-malbrough", "Malbrough s'en va-t-en guerre", "Chanson traditionnelle française", Notes.ParseMelody("""
            G4 A4 B4 B4 B4 A4 B4 C5 B4 C5 B4 A4 A4 A4 G4 A4
            B4 G4 A4 B4 B4 B4 A4 B4 D5 C#5 C5 B4 C5 B4 A4 A4
            A4 B4 A4 G4 G4 A4 B4 B4 B4 A4 B4 C5 B4 C5 B4 A4
            A4 A4 G4 A4 B4 G4 A4 B4 B4 B4 A4 B4 D5 C#5 C5 B4
            C5 B4 A4 A4 A4 B4 A4 G4 B4 C5 D5 B4 E5 E5 D5 B4
            C5 D5 D5 E5 F#5 G5 D5 C#5 C5 B4 B4 B4 A4 B4 C5 B4
            C5 B4 A4 A4 A4 G4 A4 B4 G4 A4 B4 B4 B4 A4 B4 D5
            C#5 C5 B4 C5 B4 A4 A4 A4 B4 A4 G4 B4 C5 D5 B4 E5
            E5 D5 B4 C5 D5 D5 E5 F#5 G5 D5 C#5 C5 B4 B4 B4 A4
            B4 C5 B4 C5 B4 A4 A4 A4 G4 A4 B4 G4 A4 B4 B4 B4
            A4 B4 D5 C#5 C5 B4 C5 B4 A4 A4 A4 B4 A4 G4
            """), ScoreCategories.Traditional),

        new("trad-meunier-tu-dors", "Meunier, tu dors", "Chanson traditionnelle française", Notes.ParseMelody("""
            G4 C5 E5 C5 B4 C5 D5 D5 D5 D5 C5 D5 E5 C5 G4 C5
            E5 C5 B4 C5 D5 D5 D5 D5 E5 D5 C5 E5 E5 E5 E5 E5
            E5 E5 E5 G5 G5 D5 D5 D5 D5 D5 D5 G5 G5 E5 E5 E5
            E5 E5 E5 E5 E5 E5 G5 G5 D5 D5 D5 D5 D5 D5 G5 G5
            C5 G4 C5 C5 C5 C5 C5 E5 E5 E5 C5 B4 C5 D5 D5 D5
            D5 D5 C5 D5 E5 C5 G4 C5 C5 C5 C5 C5 E5 E5 E5 C5
            B4 C5 D5 D5 D5 D5 F5 E5 D5 C5
            """), ScoreCategories.Traditional),

        new("trad-danny-boy", "Danny Boy", "Air traditionnel irlandais", Notes.ParseMelody("""
            B3 C4 D4 E4 D4 E4 A4 G4 E4 D4 C4 A3 C4 E4 F4 G4
            A4 G4 E4 C4 E4 D4 B3 C4 D4 E4 D4 E4 A4 G4 E4 D4
            C4 A3 B3 C4 D4 E4 F4 E4 D4 C4 D4 C4 G4 A4 B4 C5
            B4 B4 A4 G4 A4 G4 E4 C4 G4 A4 B4 C5 B4 B4 A4 G4
            E4 D4 G4 G4 G4 E5 D5 D5 C5 A4 C5 G4 E4 C4 B3 C4
            D4 E4 A4 G4 E4 D4 C4 A3 B3 C4
            """), ScoreCategories.Traditional),

        new("trad-kalinka", "Kalinka", "Chanson traditionnelle russe", Notes.ParseMelody("""
            A4 G4 E4 F4 G4 E4 F4 G4 F4 E4 D4 A4 A4 G4 F4 E4
            F4 G4 E4 F4 G4 F4 E4 D4 A4 G4 E4 F4 G4 E4 F4 G4
            F4 E4 D4 A4 A4 G4 F4 E4 F4 G4 E4 F4 G4 F4 E4 D4
            D5 C5 A4 C5 Bb4 A4 G4 F4 C4 A4 C5 Bb4 A4 G4 F4 C4
            D4 D4 E4 G4 F4 E4 D4 C4 C4 C4 C5 A4 C5 G4 A4 F4
            C4 A4 C5 G4 A4 F4 C4 D4 D4 E4 G4 F4 E4 D4 C5 Bb4
            A4
            """), ScoreCategories.Traditional),

        new("trad-auld-lang-syne", "Auld Lang Syne", "Air traditionnel écossais", Notes.ParseMelody("""
            D4 G4 G4 G4 B4 A4 G4 A4 B4 G4 G4 B4 D5 E5 G5 D5
            B4 B4 G4 A4 G4 A4 B4 G4 E4 E4 D4 G4 D4 G4 G4 G4
            B4 A4 G4 A4 B4 G4 G4 B4 D5 E5 G5 D5 B4 B4 G4 A4
            G4 A4 B4 G4 E4 E4 D4 G4 E5 D5 B4 B4 G4 A4 G4 A4
            B4 D5 B4 B4 G4 B4 E5 D5 B4 B4 G4 A4 G4 A4 B4 G4
            E4 E4 D4 G4 E5 D5 B4 B4 G4 A4 G4 A4 B4 D5 B4 B4
            G4 B4 E5 D5 B4 B4 G4 A4 G4 A4 B4 G4 E4 E4 D4 G4
            """), ScoreCategories.Traditional),

        new("trad-la-cucaracha", "La Cucaracha", "Chanson traditionnelle mexicaine", Notes.ParseMelody("""
            A3 A3 D4 D4 F#4 F#4 A4 F#4 A4 B4 A4 G4 F#4 A4 G4 E4
            A3 A3 C#4 C#4 E4 E4 G4 E4 A4 B4 A4 G4 F#4 E4 D4 A3
            A3 A3 D4 F#4 A3 A3 A3 D4 F#4 D4 D4 C#4 C#4 B3 B3 A3
            A3 A3 A3 C#4 E4 A3 A3 A3 C#4 E4 A4 B4 A4 G4 F#4 E4
            D4 A3 A3 A3 D4 F#4 A3 A3 A3 D4 F#4 D4 D4 C#4 C#4 B3
            B3 A3 A3 A3 A3 C#4 E4 A3 A3 A3 C#4 E4 A4 B4 A4 G4
            F#4 E4 D4
            """), ScoreCategories.Traditional),

        // ===== Ragtime =====

        new("joplin-the-entertainer", "The Entertainer", "Scott Joplin", Notes.ParseMelody("""
            D5 D#5 E5 C6 E5 C6 E5 C6
            C6 D6 D#6 E6 C6 D6 E6 B5 D6 C6
            D5 D#5 E5 C6 E5 C6 E5 C6
            A5 G5 F#5 A5 C6 E6 D6 C6 A5 D6
            """), ScoreCategories.Ragtime),

        new("joplin-maple-leaf-rag", "Maple Leaf Rag", "Scott Joplin", Notes.ParseMelody("""
            F4 C5 F4 A4 C5 F4 C5 E4 G4 C5 F4 C5 F4 A4 C5 F4 C5 E4 G4 C5 F4 G#4 C#5 C5 C5 F4 G#4 C#5 C5 F4 G#4 C5 F5 F5 G#5 C6
            """), ScoreCategories.Ragtime),

        // ===== Christmas =====

        new("gruber-silent-night", "Douce nuit", "Franz Xaver Gruber", Notes.ParseMelody("""
            G4 A4 G4 E4 G4 A4 G4 E4
            D5 D5 B4 C5 C5 G4
            A4 A4 C5 B4 A4 G4 A4 G4 E4
            A4 A4 C5 B4 A4 G4 A4 G4 E4
            D5 D5 F5 D5 B4 C5 E5
            C5 G4 E4 G4 F4 D4 C4
            """), ScoreCategories.Christmas),

        new("pierpont-jingle-bells", "Vive le vent (Jingle Bells)", "James Lord Pierpont", Notes.ParseMelody("""
            E5 E5 E5 E5 E5 E5 E5 G5 C5 D5 E5
            F5 F5 F5 F5 F5 E5 E5 E5 E5 D5 D5 E5 D5 G5
            E5 E5 E5 E5 E5 E5 E5 G5 C5 D5 E5
            F5 F5 F5 F5 F5 E5 E5 E5 G5 G5 F5 D5 C5
            """), ScoreCategories.Christmas),

        new("trad-o-tannenbaum", "Mon beau sapin", "Chant traditionnel allemand", Notes.ParseMelody("""
            C5 F5 F5 F5 G5 A5 A5 A5 G5 A5 Bb5 E5 G5 F5
            C5 F5 F5 F5 G5 A5 A5 A5 G5 A5 Bb5 E5 G5 F5
            """), ScoreCategories.Christmas),

        new("trad-les-anges-dans-nos-campagnes", "Les anges dans nos campagnes", "Chant traditionnel français", Notes.ParseMelody("""
            A4 A4 A4 A4 C5 C5 Bb4 A4 F4 A4 A4 G4 A4 A4 C5 C5
            Bb4 A4 A4 A4 A4 A4 C5 C5 Bb4 A4 F4 A4 A4 G4 A4 A4
            C5 C5 Bb4 A4 C5 D5 C5 Bb4 A4 Bb4 C5 Bb4 A4 G4 A4 Bb4
            A4 G4 F4 G4 G4 C4 F4 G4 A4 Bb4 A4 G4 C5 D5 C5 Bb4
            A4 Bb4 C5 Bb4 A4 G4 A4 Bb4 A4 G4 F4 G4 G4 C4 F4 G4
            A4 Bb4 A4 G4 F4
            """), ScoreCategories.Christmas),

        new("trad-il-est-ne-le-divin-enfant", "Il est né le divin enfant", "Chant traditionnel français", Notes.ParseMelody("""
            D4 G4 G4 B4 G4 D4 G4 G4 G4 G4 A4 B4 C5 B4 A4 G4
            A4 F#4 D4 D4 G4 G4 B4 G4 D4 G4 G4 G4 G4 A4 B4 C5
            B4 A4 D5 G4 B4 C5 D5 C5 B4 C5 E5 D5 B4 C5 D5 E5
            D5 C5 B4 B4 A4 B4 C5 D5 C5 B4 C5 E5 D5 B4 C5 D5
            E5 D5 C5 B4 A4
            """), ScoreCategories.Christmas),

        new("trad-deck-the-halls", "Deck the Halls", "Chant traditionnel gallois", Notes.ParseMelody("""
            C5 Bb4 A4 G4 F4 G4 A4 F4 G4 A4 Bb4 G4 A4 G4 F4 E4
            F4 C5 Bb4 A4 G4 F4 G4 A4 F4 G4 A4 Bb4 G4 A4 G4 F4
            E4 F4 G4 A4 Bb4 G4 A4 Bb4 C5 G4 A4 Bb4 C5 D5 E5 F5
            E5 D5 C5 C5 Bb4 A4 G4 F4 G4 A4 F4 D5 D5 D5 D5 C5
            Bb4 A4 G4 F4
            """), ScoreCategories.Christmas),

        new("trad-adeste-fideles", "Adeste Fideles", "John Francis Wade", Notes.ParseMelody("""
            F4 F4 C4 F4 G4 C4 A4 G4 A4 Bb4 A4 G4 F4 F4 E4 D4
            E4 F4 G4 A4 E4 D4 C4 C4 C5 Bb4 A4 Bb4 A4 G4 A4 F4
            G4 E4 D4 C4 F4 F4 E4 F4 G4 F4 C4 A4 A4 G4 A4 Bb4
            A4 G4 A4 Bb4 A4 G4 F4 E4 F4 Bb4 A4 G4 F4 F4
            """), ScoreCategories.Christmas),

        new("trad-we-wish-you-a-merry-christmas", "We Wish You a Merry Christmas", "Chant traditionnel anglais", Notes.ParseMelody("""
            D4 G4 G4 A4 G4 F#4 E4 C4 E4 A4 A4 B4 A4 G4 F#4 D4
            D4 B4 B4 C5 B4 A4 G4 E4 D4 D4 E4 A4 F#4 G4 D4 G4
            G4 G4 F#4 F#4 G4 F#4 E4 D4 A4 B4 A4 A4 G4 D5 D4 D4
            D4 E4 A4 F#4 G4
            """), ScoreCategories.Christmas),

        new("trad-god-rest-ye-merry-gentlemen", "God Rest Ye Merry Gentlemen", "Chant traditionnel anglais", Notes.ParseMelody("""
            E4 E4 B4 B4 A4 G4 F#4 E4 D4 E4 F#4 G4 A4 B4 E4 E4
            B4 B4 A4 G4 F#4 E4 D4 E4 F#4 G4 A4 B4 B4 C5 A4 B4
            C5 D5 E5 B4 A4 G4 E4 F#4 G4 A4 G4 A4 B4 C5 B4 B4
            A4 G4 F#4 E4 G4 F#4 E4 A4 G4 A4 B4 C5 D5 E5 B4 A4
            G4 F#4 E4
            """), ScoreCategories.Christmas),

        // ===== American folk =====

        new("trad-when-the-saints", "When the Saints Go Marching In", "Spiritual traditionnel", Notes.ParseMelody("""
            C5 E5 F5 G5 C5 E5 F5 G5 C5 E5 F5 G5 E5 C5 E5 D5
            E5 E5 D5 C5 C5 E5 G5 G5 G5 F5
            E5 F5 G5 E5 C5 D5 C5
            """), ScoreCategories.AmericanFolk),

        new("trad-amazing-grace", "Amazing Grace", "Hymne traditionnel", Notes.ParseMelody("""
            D4 G4 B4 G4 B4 A4 G4 E4 D4
            D4 G4 B4 G4 B4 A4 D5
            B4 D5 B4 D5 B4 G4 D4 E4 G4 G4 E4 D4
            D4 G4 B4 G4 B4 A4 G4
            """), ScoreCategories.AmericanFolk),

        new("foster-oh-susanna", "Oh! Susanna", "Stephen Foster", Notes.ParseMelody("""
            C5 D5 E5 G5 G5 A5 G5 E5 C5 D5 E5 E5 D5 C5 D5
            C5 D5 E5 G5 G5 A5 G5 E5 C5 D5 E5 E5 D5 D5 C5
            F5 F5 A5 A5 A5 G5 G5 E5 C5 D5
            C5 D5 E5 G5 G5 A5 G5 E5 C5 D5 E5 E5 D5 D5 C5
            """), ScoreCategories.AmericanFolk),
        new("trad-swing-low-sweet-chariot", "Swing Low, Sweet Chariot", "Spiritual traditionnel", Notes.ParseMelody("""
            F#4 D4 F#4 D4 D4 B3 A3 D4 D4 D4 D4 F#4 F#4 A4 A4 B4
            A4 F#4 A4 D4 D4 B3 A3 D4 D4 D4 D4 F#4 F#4 E4 D4 F#4
            A4 D4 A3 D4 D4 D4 D4 D4 D4 B3 A3 D4 D4 D4 D4 F#4
            F#4 A4 A4 A4 B4 A4 F#4 F#4 D4 D4 D4 D4 D4 B3 A3 D4
            D4 D4 D4 F#4 F#4 E4 D4
            """), ScoreCategories.AmericanFolk),

        new("trad-shenandoah", "Shenandoah", "Chanson traditionnelle américaine", Notes.ParseMelody("""
            D4 G4 G4 G4 A4 B4 C5 E5 D5 G5 F#5 E5 D5 E5 D5 B4
            D5 D5 E5 E5 E5 B4 D5 B4 A4 G4 G4 A4 B4 G4 B4 E5
            D5 G4 A4 B4 G4 A4 G4
            """), ScoreCategories.AmericanFolk),

        new("foster-camptown-races", "Camptown Races", "Stephen Foster", Notes.ParseMelody("""
            A4 A4 A4 F#4 A4 B4 A4 F#4 F#4 E4 F#4 E4 A4 A4 A4 F#4
            A4 B4 A4 F#4 E4 F#4 E4 D4 A4 A4 A4 F#4 A4 B4 A4 F#4
            F#4 E4 F#4 E4 A4 A4 A4 F#4 A4 B4 A4 F#4 E4 F#4 E4 D4
            D4 D4 F#4 A4 D5 B4 B4 D5 B4 A4 F#4 A4 A4 F#4 A4 B4
            A4 F#4 E4 F#4 G4 F#4 E4 E4 D4 D4 D4 F#4 A4 D5 B4 B4
            D5 B4 A4 F#4 A4 A4 F#4 A4 B4 A4 F#4 E4 F#4 G4 F#4 E4
            E4 D4
            """), ScoreCategories.AmericanFolk),

        new("trad-red-river-valley", "Red River Valley", "Chanson traditionnelle américaine", Notes.ParseMelody("""
            D4 G4 B4 B4 B4 A4 B4 A4 G4 D4 G4 B4 G4 B4 D5 C5
            B4 A4 D5 C5 B4 B4 A4 G4 A4 B4 D5 C5 E4 E4 D4 F#4
            G4 A4 B4 A4 G4
            """), ScoreCategories.AmericanFolk),

        new("montrose-clementine", "Clementine", "Percy Montrose", Notes.ParseMelody("""
            D4 D4 D4 A3 F#4 F#4 F#4 D4 D4 F#4 A4 A4 G4 F#4 E4 E4
            F#4 G4 G4 F#4 E4 F#4 D4 D4 F#4 E4 A3 C#4 E4 D4
            """), ScoreCategories.AmericanFolk),

    ];

    public IReadOnlyList<Score> GetAll() => s_scores;
}
