namespace Cameca.CustomAnalysis.PythonCore;

public sealed record MassSpectrumData(HistogramData MassHistogram, HistogramData BackgroundModel);

public sealed record HistogramData(double Start, double BinWidth, MemMapArrayInfo Values);
