// MarkerService.cs
using System;
using System.Collections.Generic;
using System.Drawing;

public sealed class MarkerService
{
    public struct Marker { public double T; public string Id; public string Note; public Color Color; }
    private readonly List<Marker> _markers = new List<Marker>();
    public event Action<Marker> MarkerAdded;

    public void AddMarker(double tSeconds, string id, string note = "", Color? color = null)
    {
        var m = new Marker { T = tSeconds, Id = id ?? "", Note = note ?? "", Color = color ?? Color.Black };
        _markers.Add(m);
        if (MarkerAdded != null) MarkerAdded(m);
    }

    public IReadOnlyList<Marker> All() { return _markers.AsReadOnly(); }
    public void Clear() { _markers.Clear(); }
}
