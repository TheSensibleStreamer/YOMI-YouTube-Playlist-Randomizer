#!/usr/bin/env python3
"""Exercise YOMI's REAL fast-path Queue move code, including 2,802-row end moves.

Extracts the production C# block rather than reimplementing it in a test.
The harness intentionally counts ObservableCollection.Move notifications: moving
a single item to the end must never generate thousands of WPF row moves.
"""
from pathlib import Path
import subprocess
import tempfile

source = Path("payload/app/YomiControllerWpf.cs").read_text(encoding="utf-8-sig")
start = source.index("            bool oneMoveApplied = false;")
end = source.index("            if (!oneMoveApplied)\n            for (int target =", start)
production = source[start:end]
assert "bool movedLater" in production and "bool movedEarlier" in production
assert "_queueRows.Move(movedLater ? first : last, movedLater ? last : first);" in production
assert 'if (_queueView != null && (_queueQueryKind != QueueQueryKind.None || _queueScope != QueueScope.All))' in source
assert '_queueView.Refresh();' in source[start:source.index("private void ApplyQueueRowsBudgeted(", start)]
assert 'row.Slot = i + 1;' in source[start:source.index("private void ApplyQueueRowsBudgeted(", start)]

program = """
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
class QueueRow
{
    public int OccurrenceId;
    public QueueRow(int id) { OccurrenceId = id; }
}
class Program
{
    static void Check(int[] oldRows, int[] newRows, bool expectOptimized)
    {
        var _queueRows = new ObservableCollection<QueueRow>(oldRows.Select(i => new QueueRow(i)));
        var desired = newRows.ToList();
        int removedRows = 0, movedRows = 0, notifications = 0;
        _queueRows.CollectionChanged += (sender,e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Move) notifications++;
        };
""" + production + """
        if (oneMoveApplied != expectOptimized)
            throw new Exception("Incorrect optimized move detection");
        if (oneMoveApplied)
        {
            if (notifications > 1 || movedRows > 1)
                throw new Exception("One occurrence caused multiple WPF row moves");
            if (!_queueRows.Select(r=>r.OccurrenceId).SequenceEqual(desired))
                throw new Exception("Displayed Queue does not match engine order");
        }
        else if (notifications != 0)
            throw new Exception("Unsupported move must use the general reconciliation path");
    }
    static void Main()
    {
        var large = Enumerable.Range(1,2802).ToArray();
        Check(large,large.Skip(1).Concat(new[] { 1 }).ToArray(),true);
        Check(large,large.Take(1).Concat(large.Skip(2).Take(5)).Concat(new[] {2}).Concat(large.Skip(7)).ToArray(),true);
        Check(large, new[] {1,6}.Concat(new[]{2,3,4,5}).Concat(large.Skip(6)).ToArray(),true);
        Check(large,large,true);
        Check(new[]{1,2,3,4,5,6},new[]{1,4,5,6,2,3},false);
        Check(new[]{1,2,3,4,5,6},new[]{3,2,1,4,5,6},false);
        Console.WriteLine("PASS: 2,802-row Move-to-End/Move+5/Move-Up use <=1 WPF notification");
        Console.WriteLine("PASS: no-op and multirow changes avoid false single-move detection");
    }
}
"""
with tempfile.TemporaryDirectory(prefix="yomi-queue-visible-") as work:
    root = Path(work)
    (root / "QueueMove.csproj").write_text(
        '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
        '<TargetFramework>net8.0</TargetFramework><Nullable>disable</Nullable></PropertyGroup></Project>',
        encoding="utf-8"
    )
    (root / "Program.cs").write_text(program, encoding="utf-8")
    subprocess.run(["dotnet", "run", "--project", str(root / "QueueMove.csproj"), "-c", "Release"],check=True)
