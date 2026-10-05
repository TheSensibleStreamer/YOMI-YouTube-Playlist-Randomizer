using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Yomi.ProductShell
{
    internal sealed class SynthesisPattern
    {
        public string Id { get; set; }
        public string Kind { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public string Basis { get; set; }
        public List<string> Tokens { get; set; }
        public List<string> MissionIds { get; set; }
        public List<string> FindingIds { get; set; }
        public List<string> SourceFingerprints { get; set; }
        public List<string> CounterexampleMissionIds { get; set; }
        public List<string> Preconditions { get; set; }
        public int SupportMissions { get; set; }
        public int SupportFindings { get; set; }
        public int UniqueSources { get; set; }
        public int ContradictionLoad { get; set; }
        public double Independence { get; set; }
        public double Stability { get; set; }
        public double TransferRisk { get; set; }
        public bool Heuristic { get; set; }
        public string Fingerprint { get; set; }
        public SynthesisPattern(){Tokens=new List<string>();MissionIds=new List<string>();FindingIds=new List<string>();SourceFingerprints=new List<string>();CounterexampleMissionIds=new List<string>();Preconditions=new List<string>();Heuristic=true;}
        public override string ToString(){return (Kind??"PATTERN")+"   ·   "+Stability.ToString("0.000",CultureInfo.InvariantCulture)+" stability   ·   missions "+SupportMissions+"   ·   sources "+UniqueSources+"   ·   counterexamples "+CounterexampleMissionIds.Count+"   ·   "+(Label??Id)+"   ·   HEURISTIC";}
    }

    internal sealed class SynthesisTransferProbe
    {
        public string Id { get; set; }
        public string PatternId { get; set; }
        public string TargetMissionId { get; set; }
        public string TargetTitle { get; set; }
        public double ContextOverlap { get; set; }
        public List<string> MissingPreconditions { get; set; }
        public double Risk { get; set; }
        public string RiskClass { get; set; }
        public string Basis { get; set; }
        public string Fingerprint { get; set; }
        public SynthesisTransferProbe(){MissingPreconditions=new List<string>();}
        public override string ToString(){return (RiskClass??"UNKNOWN")+"   ·   risk "+Risk.ToString("0.00",CultureInfo.InvariantCulture)+"   ·   overlap "+ContextOverlap.ToString("0.00",CultureInfo.InvariantCulture)+"   ·   missing "+MissingPreconditions.Count+"   ·   "+(TargetTitle??TargetMissionId)+"   ·   pattern "+CivilizationalSynthesisKernel.Short(PatternId);}
    }

    internal sealed class SynthesisSnapshot
    {
        public int Schema { get; set; }
        public string BuiltUtc { get; set; }
        public string ConstellationFingerprint { get; set; }
        public string ExpeditionStoreFingerprint { get; set; }
        public List<SynthesisPattern> Patterns { get; set; }
        public List<SynthesisTransferProbe> TransferProbes { get; set; }
        public int Missions { get; set; }
        public int Findings { get; set; }
        public int OpenContradictions { get; set; }
        public int UniqueSources { get; set; }
        public string Fingerprint { get; set; }
        public SynthesisSnapshot(){Schema=1;BuiltUtc=DateTime.UtcNow.ToString("o",CultureInfo.InvariantCulture);Patterns=new List<SynthesisPattern>();TransferProbes=new List<SynthesisTransferProbe>();}
        public override string ToString(){return "SYNTHΩ SNAPSHOT   ·   missions "+Missions+"   ·   patterns "+Patterns.Count+"   ·   transfer probes "+TransferProbes.Count+"   ·   fp "+CivilizationalSynthesisKernel.Short(Fingerprint);}
    }

    internal sealed class SynthesisValidationFinding
    {
        public string Severity { get; set; }
        public string Kind { get; set; }
        public string SubjectId { get; set; }
        public string Message { get; set; }
        public override string ToString(){return (Severity??"INFO")+"   ·   "+(Kind??"CHECK")+"   ·   "+(SubjectId??"∅")+"   ·   "+(Message??"");}
    }

    internal static class CivilizationalSynthesisKernel
    {
        private sealed class Acc
        {
            public string Key;
            public string Kind;
            public HashSet<string> Missions=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> Findings=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> Sources=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> Civilizations=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> Counterexamples=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public List<string> Tokens=new List<string>();
            public int Contradictions;
        }

        private static readonly HashSet<string> Stop=new HashSet<string>(new[]{"the","and","for","that","with","from","this","into","what","when","where","which","while","have","has","are","was","were","will","would","could","should","about","under","over","between","through","without","within","than","then","them","they","their","there","does","did","can","why","how","who","its","our","your","not","but","all","any","each","one","two","new","use","using","used","system","systems","mission","missions","finding","findings","question","questions","evidence","result","results"},StringComparer.OrdinalIgnoreCase);
        public static string HashText(string text){using(SHA256 sha=SHA256.Create()){byte[] h=sha.ComputeHash(Encoding.UTF8.GetBytes(text??""));StringBuilder b=new StringBuilder(h.Length*2);foreach(byte x in h)b.Append(x.ToString("x2",CultureInfo.InvariantCulture));return b.ToString();}}
        public static string Short(string text){if(String.IsNullOrWhiteSpace(text))return "∅";return text.Substring(0,Math.Min(12,text.Length));}
        private static IEnumerable<string> Tokens(string text){if(String.IsNullOrWhiteSpace(text))yield break;StringBuilder b=new StringBuilder();foreach(char c in text.ToLowerInvariant())b.Append(Char.IsLetterOrDigit(c)?c:' ');foreach(string x in b.ToString().Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries)){if(x.Length<4||Stop.Contains(x))continue;yield return x;}}
        private static string Join(IEnumerable<string> xs){return xs==null?"":String.Join("\n",xs.Where(x=>!String.IsNullOrWhiteSpace(x)).OrderBy(x=>x,StringComparer.Ordinal).ToArray());}
        private static string Pair(string a,string b){return String.Compare(a,b,StringComparison.Ordinal)<=0?a+" ∧ "+b:b+" ∧ "+a;}
        private static ExpeditionStep StepById(ExpeditionMission m,string id){return m==null||String.IsNullOrWhiteSpace(id)?null:m.Steps.FirstOrDefault(x=>String.Equals(x.Id,id,StringComparison.OrdinalIgnoreCase));}
        private static IEnumerable<string> FindingSources(ExpeditionMission m,ExpeditionFinding f){foreach(string id in f.SourceStepIds??new List<string>()){ExpeditionStep s=StepById(m,id);if(s!=null&&s.Source!=null&&!String.IsNullOrWhiteSpace(s.Source.SourceFingerprint))yield return s.Source.SourceFingerprint;}}
        private static IEnumerable<string> FindingCivilizations(ExpeditionMission m,ExpeditionFinding f){foreach(string id in f.SourceStepIds??new List<string>()){ExpeditionStep s=StepById(m,id);if(s!=null&&!String.IsNullOrWhiteSpace(s.Civilization))yield return s.Civilization;}}
        private static bool FindingInOpenContradiction(ExpeditionMission m,string findingId){return m!=null&&m.Contradictions.Any(c=>String.Equals(c.Status,"OPEN",StringComparison.OrdinalIgnoreCase)&&(String.Equals(c.LeftFindingId,findingId,StringComparison.OrdinalIgnoreCase)||String.Equals(c.RightFindingId,findingId,StringComparison.OrdinalIgnoreCase)));}

        public static SynthesisSnapshot Build(ConstellationSnapshot constellation,IEnumerable<ExpeditionMission> source,string expeditionFingerprint)
        {
            List<ExpeditionMission> missions=(source??Enumerable.Empty<ExpeditionMission>()).Where(x=>x!=null).ToList();
            SynthesisSnapshot snap=new SynthesisSnapshot{ConstellationFingerprint=constellation==null?HashText(""):constellation.Fingerprint,ExpeditionStoreFingerprint=expeditionFingerprint??HashText(""),Missions=missions.Count,Findings=missions.Sum(m=>m.Findings.Count),OpenContradictions=missions.Sum(m=>m.Contradictions.Count(c=>String.Equals(c.Status,"OPEN",StringComparison.OrdinalIgnoreCase))),UniqueSources=missions.SelectMany(m=>m.Steps).Where(s=>s.Source!=null&&!String.IsNullOrWhiteSpace(s.Source.SourceFingerprint)).Select(s=>s.Source.SourceFingerprint).Distinct(StringComparer.OrdinalIgnoreCase).Count()};
            Dictionary<string,Acc> single=new Dictionary<string,Acc>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string,Acc> pairs=new Dictionary<string,Acc>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string,Acc> failures=new Dictionary<string,Acc>(StringComparer.OrdinalIgnoreCase);

            foreach(ExpeditionMission m in missions)
            {
                foreach(ExpeditionFinding f in m.Findings.Where(x=>x!=null&&!String.IsNullOrWhiteSpace(x.Text)&&!String.Equals(x.Status,"RETRACTED",StringComparison.OrdinalIgnoreCase)))
                {
                    List<string> ts=Tokens(f.Text).Distinct(StringComparer.OrdinalIgnoreCase).Take(18).ToList();
                    List<string> src=FindingSources(m,f).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    List<string> civ=FindingCivilizations(m,f).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    bool contrad=FindingInOpenContradiction(m,f.Id);
                    foreach(string t in ts)
                    {
                        Acc a;if(!single.TryGetValue(t,out a)){a=new Acc{Key=t,Kind="RECURRENCE"};a.Tokens.Add(t);single[t]=a;}a.Missions.Add(m.Id);a.Findings.Add(f.Id);foreach(string s in src)a.Sources.Add(s);foreach(string c in civ)a.Civilizations.Add(c);if(contrad){a.Contradictions++;a.Counterexamples.Add(m.Id);}
                        if(contrad){Acc z;if(!failures.TryGetValue(t,out z)){z=new Acc{Key=t,Kind="FAILURE_SIGNATURE"};z.Tokens.Add(t);failures[t]=z;}z.Missions.Add(m.Id);z.Findings.Add(f.Id);foreach(string s in src)z.Sources.Add(s);foreach(string c in civ)z.Civilizations.Add(c);z.Contradictions++;z.Counterexamples.Add(m.Id);}
                    }
                    for(int i=0;i<ts.Count;i++)for(int j=i+1;j<ts.Count&&j<i+7;j++)
                    {
                        string key=Pair(ts[i],ts[j]);Acc a;if(!pairs.TryGetValue(key,out a)){a=new Acc{Key=key,Kind="MOTIF"};a.Tokens.Add(ts[i]);a.Tokens.Add(ts[j]);pairs[key]=a;}a.Missions.Add(m.Id);a.Findings.Add(f.Id);foreach(string s in src)a.Sources.Add(s);foreach(string c in civ)a.Civilizations.Add(c);if(contrad){a.Contradictions++;a.Counterexamples.Add(m.Id);}
                    }
                }
            }

            List<Acc> all=new List<Acc>();all.AddRange(single.Values.Where(a=>a.Missions.Count>=2));all.AddRange(pairs.Values.Where(a=>a.Missions.Count>=2));all.AddRange(failures.Values.Where(a=>a.Missions.Count>=2));
            foreach(Acc a in all)
            {
                double breadth=Math.Min(1.0,a.Missions.Count/5.0);double independence=a.Missions.Count==0?0:Math.Min(1.0,(double)Math.Max(1,a.Sources.Count)/a.Missions.Count);double conflict=a.Findings.Count==0?0:Math.Min(1.0,(double)a.Contradictions/a.Findings.Count);double stability=Math.Max(0,Math.Min(1,0.48*breadth+0.37*independence+0.15*Math.Min(1,a.Findings.Count/8.0)-0.35*conflict));
                string kind=a.Kind;if(kind=="RECURRENCE"&&a.Missions.Count>=3&&a.Sources.Count>=2&&conflict<0.15)kind="INVARIANT_CANDIDATE";if(kind=="MOTIF"&&conflict>0.34)kind="ANTI_PATTERN_CANDIDATE";
                SynthesisPattern p=new SynthesisPattern{Id="SYN-"+HashText(kind+"|"+a.Key+"|"+Join(a.Missions)).Substring(0,20).ToUpperInvariant(),Kind=kind,Label=a.Key,Description=Describe(kind,a.Key),Basis="Cross-mission mining of retained EXPΩ findings; recurrence is not proof of universality or causation.",Tokens=a.Tokens.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x=>x,StringComparer.Ordinal).ToList(),MissionIds=a.Missions.OrderBy(x=>x,StringComparer.Ordinal).ToList(),FindingIds=a.Findings.OrderBy(x=>x,StringComparer.Ordinal).ToList(),SourceFingerprints=a.Sources.OrderBy(x=>x,StringComparer.Ordinal).ToList(),CounterexampleMissionIds=a.Counterexamples.OrderBy(x=>x,StringComparer.Ordinal).ToList(),Preconditions=a.Civilizations.OrderBy(x=>x,StringComparer.Ordinal).Take(8).ToList(),SupportMissions=a.Missions.Count,SupportFindings=a.Findings.Count,UniqueSources=a.Sources.Count,ContradictionLoad=a.Contradictions,Independence=independence,Stability=stability,TransferRisk=Math.Max(0,Math.Min(1,1-stability+0.25*conflict)),Heuristic=true};
                p.Fingerprint=HashText(p.Id+"|"+p.Kind+"|"+p.Label+"|"+Join(p.MissionIds)+"|"+Join(p.FindingIds)+"|"+Join(p.SourceFingerprints)+"|"+Join(p.CounterexampleMissionIds)+"|"+Join(p.Preconditions)+"|"+p.Stability.ToString("R",CultureInfo.InvariantCulture));snap.Patterns.Add(p);
            }
            snap.Patterns=snap.Patterns.OrderByDescending(p=>p.Stability).ThenByDescending(p=>p.SupportMissions).ThenBy(p=>p.Label,StringComparer.OrdinalIgnoreCase).Take(768).ToList();

            foreach(SynthesisPattern p in snap.Patterns.Take(96))
            {
                foreach(ExpeditionMission m in missions.Where(x=>!p.MissionIds.Contains(x.Id,StringComparer.OrdinalIgnoreCase)))
                {
                    HashSet<string> mt=new HashSet<string>(Tokens((m.Title??"")+" "+(m.Question??"")+" "+(m.Objective??"")+" "+String.Join(" ",m.Findings.Select(f=>f.Text??""))),StringComparer.OrdinalIgnoreCase);
                    int overlap=p.Tokens.Count(t=>mt.Contains(t));if(overlap==0)continue;double context=(double)overlap/Math.Max(1,p.Tokens.Count);HashSet<string> targetCiv=new HashSet<string>(m.Steps.Where(s=>!String.IsNullOrWhiteSpace(s.Civilization)).Select(s=>s.Civilization),StringComparer.OrdinalIgnoreCase);List<string> missing=p.Preconditions.Where(c=>!targetCiv.Contains(c)).ToList();double missingRatio=p.Preconditions.Count==0?0:(double)missing.Count/p.Preconditions.Count;double risk=Math.Max(0,Math.Min(1,0.55*p.TransferRisk+0.30*missingRatio+0.15*(1-context)));SynthesisTransferProbe q=new SynthesisTransferProbe{Id="XFER-"+HashText(p.Id+"|"+m.Id).Substring(0,20).ToUpperInvariant(),PatternId=p.Id,TargetMissionId=m.Id,TargetTitle=m.Title,ContextOverlap=context,MissingPreconditions=missing,Risk=risk,RiskClass=risk<0.34?"LOW":risk<0.67?"MEDIUM":"HIGH",Basis="Lexical context overlap plus explicit missing supporting-civilization preconditions. Transfer remains untested."};q.Fingerprint=HashText(q.Id+"|"+q.PatternId+"|"+q.TargetMissionId+"|"+q.Risk.ToString("R",CultureInfo.InvariantCulture)+"|"+Join(q.MissingPreconditions));snap.TransferProbes.Add(q);
                }
            }
            snap.TransferProbes=snap.TransferProbes.OrderBy(x=>x.Risk).ThenByDescending(x=>x.ContextOverlap).Take(512).ToList();
            snap.Fingerprint=HashText("SYNTHΩ|"+snap.ConstellationFingerprint+"|"+snap.ExpeditionStoreFingerprint+"|"+Join(snap.Patterns.Select(p=>p.Fingerprint))+"|"+Join(snap.TransferProbes.Select(p=>p.Fingerprint)));
            return snap;
        }

        private static string Describe(string kind,string label)
        {
            if(kind=="INVARIANT_CANDIDATE")return "A broad low-conflict recurrence candidate around “"+label+"”. It is not a theorem until scope, counterexamples and transfer tests survive explicit review.";
            if(kind=="FAILURE_SIGNATURE")return "A recurring token associated with explicit unresolved contradiction contexts: “"+label+"”.";
            if(kind=="ANTI_PATTERN_CANDIDATE")return "A repeated co-occurrence with elevated contradiction load: “"+label+"”. Treat as an anti-pattern candidate, not a causal diagnosis.";
            if(kind=="MOTIF")return "A recurring cross-mission finding motif: “"+label+"”.";
            return "A recurring cross-mission finding structure around “"+label+"”.";
        }

        public static List<SynthesisValidationFinding> Validate(SynthesisSnapshot s)
        {
            List<SynthesisValidationFinding> r=new List<SynthesisValidationFinding>();if(s==null){r.Add(new SynthesisValidationFinding{Severity="ERROR",Kind="SNAPSHOT",Message="Snapshot is null."});return r;}HashSet<string> ids=new HashSet<string>(StringComparer.OrdinalIgnoreCase);foreach(SynthesisPattern p in s.Patterns){if(String.IsNullOrWhiteSpace(p.Id)||!ids.Add(p.Id))r.Add(new SynthesisValidationFinding{Severity="ERROR",Kind="PATTERN_ID",SubjectId=p.Id,Message="Pattern id missing or duplicated."});if(!p.Heuristic)r.Add(new SynthesisValidationFinding{Severity="ERROR",Kind="AUTHORITY",SubjectId=p.Id,Message="Automatically mined pattern must remain heuristic."});if(p.Stability<0||p.Stability>1||p.TransferRisk<0||p.TransferRisk>1)r.Add(new SynthesisValidationFinding{Severity="ERROR",Kind="RANGE",SubjectId=p.Id,Message="Pattern score outside [0,1]."});if(p.SupportMissions<2)r.Add(new SynthesisValidationFinding{Severity="WARN",Kind="BREADTH",SubjectId=p.Id,Message="Cross-mission pattern has fewer than two supporting missions."});}HashSet<string> pids=new HashSet<string>(s.Patterns.Select(p=>p.Id),StringComparer.OrdinalIgnoreCase);foreach(SynthesisTransferProbe q in s.TransferProbes){if(!pids.Contains(q.PatternId))r.Add(new SynthesisValidationFinding{Severity="ERROR",Kind="TRANSFER_PATTERN",SubjectId=q.Id,Message="Transfer probe references missing pattern."});if(q.Risk<0||q.Risk>1)r.Add(new SynthesisValidationFinding{Severity="ERROR",Kind="TRANSFER_RANGE",SubjectId=q.Id,Message="Transfer risk outside [0,1]."});}if(r.Count==0)r.Add(new SynthesisValidationFinding{Severity="PASS",Kind="STRUCTURE",SubjectId=s.Fingerprint,Message="Snapshot structure and heuristic-authority invariants hold."});return r;
        }
    }
}
