using RAMbo.App.Models;
using RAMbo.App.Services.Interfaces;

namespace RAMbo.App.Services.Algorithms;

/// <summary>
/// Optimal (Belady) sayfa degistirme algoritmasi.
/// Gelecekteki referans dizisine bakarak en uzun sure kullanilmayacak sayfayi cikarir.
/// Bu, teorik minimum page fault sayisini uretir (referans deger olarak kullanilir).
/// </summary>
public class OptimalAlgorithm : IPageReplacementAlgorithm
{
    public string Name => "Optimal";

    public SimulationResult Run(int[] referenceString, int frameCount)
    {
        ArgumentNullException.ThrowIfNull(referenceString);

        if (frameCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(frameCount));

        var result = new SimulationResult
        {
            AlgorithmName = Name,
            FrameCount = frameCount,
            ReferenceString = referenceString.ToArray()
        };

        var frames = new List<int>(); // Anlik frame durumu

        for (int i = 0; i < referenceString.Length; i++)
        {
            int page = referenceString[i];
            bool isFault = !frames.Contains(page);
            int? victimPage = null;

            if (isFault)
            {
                if (frames.Count < frameCount)
                {
                    // Frames dolmadi, direkt ekle
                    frames.Add(page);
                }
                else
                {
                    // Frames dolu: gelecege bakarak optimal victim sec
                    victimPage = FindOptimalVictim(frames, referenceString, i + 1);
                    int idx = frames.IndexOf(victimPage.Value);
                    frames[idx] = page;
                }
            }

            result.Steps.Add(new StepResult
            {
                StepNumber = i + 1,
                PageNumber = page,
                IsFault    = isFault,
                FrameState = string.Join(",", frames),
                VictimPage = victimPage
            });
        }

        return result;
    }

    /// <summary>
    /// Frames icerisindeki hangi sayfanin gelecekte en gec (veya hic) kullanilacagini bulur.
    /// O sayfa victim olarak secilir.
    /// </summary>
    private static int FindOptimalVictim(List<int> frames, int[] referenceString, int futureStart)
    {
        int victimPage = frames[0];
        int farthest   = -1; // En uzak kullanim pozisyonu

        foreach (int frame in frames)
        {
            // Bu sayfa gelecekte ilk ne zaman kullanilacak?
            int nextUse = FindNextUse(referenceString, frame, futureStart);

            // Hic kullanilmayacaksa -> kesin victim
            if (nextUse == int.MaxValue)
                return frame;

            // En gec kullanilacak olani sec
            if (nextUse > farthest)
            {
                farthest   = nextUse;
                victimPage = frame;
            }
        }

        return victimPage;
    }

    /// <summary>
    /// Referans dizisinde, verilen baslangic noktasindan itibaren
    /// belirtilen sayfanin bir sonraki kullanim indeksini doner.
    /// Sayfa hic kullanilmayacaksa int.MaxValue doner.
    /// </summary>
    private static int FindNextUse(int[] referenceString, int page, int startFrom)
    {
        for (int i = startFrom; i < referenceString.Length; i++)
        {
            if (referenceString[i] == page)
                return i;
        }
        return int.MaxValue; // Bu sayfa artik hic kullanilmayacak
    }
}
