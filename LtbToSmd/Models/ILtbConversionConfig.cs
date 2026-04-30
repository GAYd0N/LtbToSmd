namespace LtbToSmd.Models;

public interface ILtbConversionConfig
{
    string? InputPath { get; }
    string? OutputPath { get; }
    bool IsGenerateQCEnabled { get; }
    bool IsExtractAnimEnabled { get; }
    bool IsSeparateArmEnabled { get; }
    bool IsSeparateSmdEnabled { get; }
    bool IsCreateSeparateFolders { get; }
    bool IsForceAnimOrigin { get; }
    bool IsBatch { get; }
}
