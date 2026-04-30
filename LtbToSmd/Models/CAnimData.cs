using System.Collections.Generic;

namespace LtbToSmd.Models;

public class CAnimData
{
    public string? name;
    public uint nkeyframe;
    public List<int>? listkeyframe;
    public List<string>? listsound;
    public float[]? Dim;
    public int interp_time;
    public CFramedata[]? frame;
}
