param(
    [int]$Iterations=25000,
    [int]$Seed=4208,
    [switch]$Quick
)

$ErrorActionPreference='Stop'
if($Quick){$Iterations=[Math]::Min($Iterations,4000)}
if($Iterations -lt 100){$Iterations=100}

$failures=New-Object System.Collections.Generic.List[string]
$checks=0
$sw=[Diagnostics.Stopwatch]::StartNew()

function Assert-Yomi([bool]$Condition,[string]$Message){
    $script:checks++
    if(-not $Condition){$script:failures.Add($Message)}
}
function Source-Key([string]$Identity){
    $sha=[Security.Cryptography.SHA256]::Create()
    try{
        $bytes=[System.Text.Encoding]::UTF8.GetBytes($Identity)
        return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-','').ToLowerInvariant()
    }finally{$sha.Dispose()}
}
function Clone-Array($a){return @($a|ForEach-Object{[int]$_})}
function Valid-State($order,$registry,[int]$current){
    if($order.Count -lt 1){return $false}
    $seen=New-Object 'System.Collections.Generic.HashSet[System.Int32]'
    foreach($id in $order){
        $n=[int]$id
        if(-not $registry.ContainsKey($n)){return $false}
        if(-not $seen.Add($n)){return $false}
    }
    return $seen.Contains($current)
}
function Trim-Stack([System.Collections.ArrayList]$stack){
    while($stack.Count -gt 10){$stack.RemoveAt(0)}
}
function Snapshot-Push([System.Collections.ArrayList]$stack,$order){
    [void]$stack.Add((Clone-Array $order))
    Trim-Stack $stack
}

Assert-Yomi ((Source-Key 'youtube:AbC123') -ne (Source-Key 'youtube:abc123')) 'Case-sensitive source IDs collided.'
Assert-Yomi ((Source-Key 'youtube:AbC123') -eq (Source-Key 'youtube:AbC123')) 'Stable source hash was not deterministic.'
Assert-Yomi ((Source-Key 'video-v4|240p|maximum|30') -ne (Source-Key 'video-v4|360p|maximum|30')) 'Video policy signatures collided.'

$base=128
$registry=New-Object 'System.Collections.Generic.Dictionary[System.Int32,System.Object]'
for($i=1;$i -le $base;$i++){
    $src='youtube:'+('{0:X11}' -f $i)
    $registry[$i]=[PSCustomObject]@{id=$i;source=$src;source_key=(Source-Key $src);origin=$i}
}
$order=[System.Collections.ArrayList]@(1..$base)
$current=1
$nextId=$base+1
$undo=New-Object System.Collections.ArrayList
$redo=New-Object System.Collections.ArrayList
$random=[Random]::new($Seed)
$freeze=$false

function Commit-Order($candidate){
    if(-not(Valid-State $candidate $registry $current)){return $false}
    Snapshot-Push $undo $order
    $redo.Clear()
    $script:order=[System.Collections.ArrayList]@(Clone-Array $candidate)
    return $true
}
function Future-Ids(){
    $slot=$order.IndexOf($current)
    if($slot -lt 0 -or $slot -ge $order.Count-1){return @()}
    return @($order[($slot+1)..($order.Count-1)])
}

for($iter=1;$iter -le $Iterations;$iter++){
    if($iter % 233 -eq 0){$freeze=-not $freeze}
    $before=(Clone-Array $order) -join ','
    $op=$random.Next(0,8)

    if($op -eq 0 -and $order.Count -gt 1){
        $future=Future-Ids
        if($future.Count -gt 0 -and -not $freeze){
            $selected=[int]$future[$random.Next($future.Count)]
            $candidate=[System.Collections.ArrayList]@(Clone-Array $order)
            $selectedSlot=$candidate.IndexOf($selected)
            $currentSlot=$candidate.IndexOf($current)
            $candidate.RemoveAt($selectedSlot)
            if($selectedSlot -lt $currentSlot){$currentSlot--}
            $candidate.Insert($currentSlot+1,$selected)
            [void](Commit-Order $candidate)
        }
    }elseif($op -eq 1 -and $order.Count -gt 2){
        $future=Future-Ids
        if($future.Count -gt 1 -and -not $freeze){
            $selected=[int]$future[$random.Next($future.Count)]
            $candidate=[System.Collections.ArrayList]@(Clone-Array $order)
            $slot=$candidate.IndexOf($selected)
            $candidate.RemoveAt($slot)
            $candidate.Insert([Math]::Min($candidate.Count,$slot+$random.Next(1,9)),$selected)
            [void](Commit-Order $candidate)
        }
    }elseif($op -eq 2 -and $order.Count -gt 2){
        $future=Future-Ids
        if($future.Count -gt 0 -and -not $freeze){
            $selected=[int]$future[$random.Next($future.Count)]
            $candidate=[System.Collections.ArrayList]@(Clone-Array $order)
            [void]$candidate.Remove($selected)
            [void](Commit-Order $candidate)
        }
    }elseif($op -eq 3 -and -not $freeze){
        $sourceId=[int]$order[$random.Next($order.Count)]
        $source=$registry[$sourceId]
        $id=$nextId;$nextId++
        $registry[$id]=[PSCustomObject]@{id=$id;source=$source.source;source_key=$source.source_key;origin=$sourceId}
        $candidate=[System.Collections.ArrayList]@(Clone-Array $order)
        $candidate.Insert($candidate.IndexOf($current)+1,$id)
        if(Commit-Order $candidate){
            Assert-Yomi ($registry[$id].source_key -eq $source.source_key) "Replay $id did not reuse source/media identity."
        }else{
            [void]$registry.Remove($id);$nextId--
        }
    }elseif($op -eq 4 -and -not $freeze){
        $candidate=[System.Collections.ArrayList]@(Clone-Array $order)
        $start=$candidate.IndexOf($current)+1
        for($i=$candidate.Count-1;$i -gt $start;$i--){
            $j=$random.Next($start,$i+1)
            $tmp=$candidate[$i];$candidate[$i]=$candidate[$j];$candidate[$j]=$tmp
        }
        [void](Commit-Order $candidate)
    }elseif($op -eq 5 -and $undo.Count -gt 0 -and -not $freeze){
        $target=$undo[$undo.Count-1];$undo.RemoveAt($undo.Count-1)
        if(Valid-State $target $registry $current){
            Snapshot-Push $redo $order
            $order=[System.Collections.ArrayList]@(Clone-Array $target)
        }
    }elseif($op -eq 6 -and $redo.Count -gt 0 -and -not $freeze){
        $target=$redo[$redo.Count-1];$redo.RemoveAt($redo.Count-1)
        if(Valid-State $target $registry $current){
            Snapshot-Push $undo $order
            $order=[System.Collections.ArrayList]@(Clone-Array $target)
        }
    }elseif($op -eq 7 -and -not $freeze){
        $candidate=[System.Collections.ArrayList]@(1..$base)
        if($candidate.Contains($current)){[void](Commit-Order $candidate)}
    }

    Assert-Yomi (Valid-State $order $registry $current) "Invalid occurrence state after iteration $iter / op $op."
    Assert-Yomi ($undo.Count -le 10) "Undo stack exceeded 10 at iteration $iter."
    Assert-Yomi ($redo.Count -le 10) "Redo stack exceeded 10 at iteration $iter."
    if($freeze){Assert-Yomi (((Clone-Array $order) -join ',') -eq $before) "Freeze allowed mutation at iteration $iter."}
}

$snapshot=[ordered]@{
    schema=2;session_id='self-test';revision=42;base_count=$base;next_occurrence_id=$nextId;
    order=Clone-Array $order;inserted=@($registry.Keys|Where-Object{$_ -gt $base}|Sort-Object);
    undo=@($undo);redo=@($redo)
}
$json=$snapshot|ConvertTo-Json -Depth 12
$round=$json|ConvertFrom-Json
Assert-Yomi ([int]$round.schema -eq 2) 'Order snapshot JSON schema did not round trip.'
Assert-Yomi (@($round.order).Count -eq $order.Count) 'Order snapshot JSON cardinality changed.'

$tempRoot=Join-Path ([IO.Path]::GetTempPath()) ('yomi-self-test-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot|Out-Null
try{
    $currentPath=Join-Path $tempRoot 'config.json'
    $previousPath=Join-Path $tempRoot 'config.previous.json'
    '{"value":1}'|Set-Content $currentPath -Encoding ASCII
    Copy-Item $currentPath $previousPath
    '{"value":2}'|Set-Content $currentPath -Encoding ASCII
    '{BROKEN'|Set-Content $currentPath -Encoding ASCII
    $recovered=Get-Content $previousPath -Raw|ConvertFrom-Json
    Assert-Yomi ([int]$recovered.value -eq 1) 'Previous-good recovery simulation failed.'
}finally{Remove-Item $tempRoot -Recurse -Force -ErrorAction SilentlyContinue}

$sw.Stop()
$result=[ordered]@{
    product='YOMI';suite='YOMI deterministic self-test';seed=$Seed;
    iterations=$Iterations;assertions=$checks;failures=$failures.Count;
    elapsed_ms=$sw.ElapsedMilliseconds;status=$(if($failures.Count -eq 0){'PASS'}else{'FAIL'})
}
$text=($result|ConvertTo-Json -Compress)+"`r`n"
if($failures.Count -gt 0){$text+=($failures -join "`r`n")+"`r`n"}
try{Set-Clipboard $text}catch{}
Write-Host '===== YOMI CROWN JEWEL SELF TEST ====='
Write-Host $text
Write-Host 'Result copied to clipboard.'
if($failures.Count -gt 0){exit 1}
exit 0
