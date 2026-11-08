# audio-render

Offline renderer for the kit sounds (dotnet, no Unity). Compiles `Synth.cs`, `SfxCatalog.cs` and `SfxMeasure.cs` with a stand-in for `Mathf`, renders every catalogue clip to WAV and writes `report.csv` and `catalog.csv`.

    dotnet run -c Release --project tools/audio-render -- --out <folder> [--rec <folder of decoded recordings named like footstep_concrete_1.wav>] [--filter name]
    node tools/audio-render/make-review.mjs <Unity SfxReport folder> <folder with catalog.csv>   # rebuilds docs/audio-review

Its random streams differ slightly from Unity (Mono), so reverberant sounds differ by a few dB: use it for quick iteration, and `SfxReport` inside Unity (`-batchmode -nographics`) for the clips the game really plays. It cannot hear; the numbers are measurements.
