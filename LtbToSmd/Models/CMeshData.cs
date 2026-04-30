using System.Collections.Generic;

namespace LtbToSmd.Models;

public class CMeshData
{
    public string? name;
    public uint nvertices;
    public uint nIdx;
    public List<float[]>? vertices;
    public List<float[]>? normals;
    public List<float[]>? uvs;
    public List<float[]>? weights;
    public List<int[]>? weightsets;
    public List<int>? weightsets_output;
    public List<int>? triangles;
    public uint type;
}
