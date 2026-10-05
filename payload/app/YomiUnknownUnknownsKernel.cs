using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Yomi.ProductShell
{
    internal sealed class BlindSpotCandidate
    {
        public string Id { get; set; }
        public string Kind { get; set; }
        public string Severity { get; set; }
        public string MissionId { get; set; }
        public string BranchId { get; set; }
        public string SubjectId { get; set; }
        public string Label { get; set; }
        public string Basis { get; set; }
        public string SuggestedProbe { get; set; }
        public string Civilization { get; set; }
        public bool StructuralAbsence { get; set; }
        public bool Heuristic { get; set; }
        public double Priority { get; set; }
        public string Fingerprint { get; set; }
        public override string ToString(){return (Severity??"INFO")+"   ·   "+Priority.ToString("000.0",CultureInfo.InvariantCulture)+"   ·   "+(Kind??"BLIND_SPOT")+"   ·   "+(Label??SubjectId??Id)+"   ·   "+(Heuristic?"HEURISTIC":"OBSERVED STRUCTURE");}
    }

    internal sealed class DisagreementCandidate
    {
        public string Id { get; set; }
        public string MissionId { get; set; }
        public string ContradictionId { get; set; }
        public string Kind { get; set; }
        public string Description { get; set; }
        public string Basis { get; set; }
        public bool Heuristic { get; set; }
        public string Fingerprint { get; set; }
        public override string ToString(){return (Kind??"UNCLASSIFIED")+"   ·   "+(Description??ContradictionId)+"   ·   "+(Heuristic?"HEURISTIC CLASSIFICATION":"RECORDED");}
    }

    internal sealed class TheoryStressCandidate
    {
        public string Id { get; set; }
        public string TheoremId { get; set; }
        public string PatternId { get; set; }
        public string Statement { get; set; }
        public string Scope { get; set; }
        public List<string> MissingTests { get; set; }
        public List<string> CounterexampleMissionIds { get; set; }
        public double StressPriority { get; set; }
        public string Fingerprint { get; set; }
        public TheoryStressCandidate(){MissingTests=new List<string>();CounterexampleMissionIds=new List<string>();}
        public override string ToString(){return StressPriority.ToString("000.0",CultureInfo.InvariantCulture)+"   ·   "+(Statement??TheoremId)+"   ·   missing tests "+MissingTests.Count+"   ·   counterexamples "+CounterexampleMissionIds.Count;}
    }

    internal sealed class CoverageCell
    {
        public string Civilization { get; set; }
        public int Missions { get; set; }
        public int Steps { get; set; }
        public int SourceCaptures { get; set; }
        public double CoverageRatio { get; set; }
        public override string ToString(){return (Civilization??"SYSTEM")+"   ·   missions "+Missions+"   ·   steps "+Steps+"   ·   sources "+SourceCaptures+"   ·   coverage "+CoverageRatio.ToString("P0",CultureInfo.InvariantCulture);}
    }

    internal sealed class VoidSnapshot
    {
        public int Schema { get; set; }
        public string BuiltUtc { get; set; }
        public int Missions { get; set; }
        public int Findings { get; set; }
        public int Questions { get; set; }
        public int Contradictions { get; set; }
        public int Assumptions { get; set; }
        public int Decisions { get; set; }
        public int UniqueSources { get; set; }
        public List<BlindSpotCandidate> BlindSpots { get; set; }
        public List<DisagreementCandidate> Disagreements { get; set; }
        public List<TheoryStressCandidate> TheoryStress { get; set; }
        public List<CoverageCell> Coverage { get; set; }
        public string ExpeditionFingerprint { get; set; }
        public string SynthesisFingerprint { get; set; }
        public string Fingerprint { get; set; }
        public VoidSnapshot(){Schema=1;BuiltUtc=DateTime.UtcNow.ToString("o",CultureInfo.InvariantCulture);BlindSpots=new List<BlindSpotCandidate>();Disagreements=new List<DisagreementCandidate>();TheoryStress=new List<TheoryStressCandidate>();Coverage=new List<CoverageCell>();}
        public override string ToString(){return "VOIDΩ SNAPSHOT   ·   missions "+Missions+"   ·   blind spots "+BlindSpots.Count+"   ·   disagreements "+Disagreements.Count+"   ·   theory stress "+TheoryStress.Count+"   ·   fp "+UnknownUnknownsKernel.Short(Fingerprint);}
    }

    internal sealed class VoidValidationFinding
    {
        public string Severity { get; set; }
        public string Kind { get; set; }
        public string SubjectId { get; set; }
        public string Message { get; set; }
        public override string ToString(){return (Severity??"INFO")+"   ·   "+(Kind??"CHECK")+"   ·   "+(SubjectId??"∅")+"   ·   "+(Message??"");}
    }

    internal static class UnknownUnknownsKernel
    {
        private static readonly string[] StopWords=new[]{"about","after","again","against","also","among","another","because","before","being","between","could","every","finding","from","have","into","mission","other","should","their","there","these","they","this","through","under","using","when","where","which","while","with","would","your"};
        private static string E(string x){return x??"";}
        private static bool Eq(string a,string b){return String.Equals(a,b,StringComparison.OrdinalIgnoreCase);}
        private static bool Has(string a,string b){return !String.IsNullOrWhiteSpace(a)&&a.IndexOf(b,StringComparison.OrdinalIgnoreCase)>=0;}
        private static string Join(IEnumerable<string> xs){return String.Join("|",(xs??Enumerable.Empty<string>()).Where(x=>!String.IsNullOrWhiteSpace(x)).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).ToArray());}
        public static string HashText(string text){using(SHA256 sha=SHA256.Create()){byte[] h=sha.ComputeHash(Encoding.UTF8.GetBytes(text??""));StringBuilder b=new StringBuilder(h.Length*2);foreach(byte x in h)b.Append(x.ToString("x2",CultureInfo.InvariantCulture));return b.ToString();}}
        public static string Short(string text){if(String.IsNullOrWhiteSpace(text))return "∅";return text.Substring(0,Math.Min(12,text.Length));}
        private static IEnumerable<string> Tokens(string text){char[] sep=" \\t\\r\\n.,;:!?()[]{}<>/\\|\"'`~@#$%^&*-_=+".ToCharArray();foreach(string raw in (text??"").ToLowerInvariant().Split(sep,StringSplitOptions.RemoveEmptyEntries)){string x=raw.Trim();if(x.Length<4||StopWords.Contains(x))continue;yield return x;}}
        private static void Add(VoidSnapshot s,BlindSpotCandidate b){if(b==null)return;b.Fingerprint=HashText(E(b.Kind)+"|"+E(b.MissionId)+"|"+E(b.BranchId)+"|"+E(b.SubjectId)+"|"+E(b.Label)+"|"+E(b.Basis));b.Id="VOID-"+b.Fingerprint.Substring(0,20).ToUpperInvariant();if(!s.BlindSpots.Any(x=>Eq(x.Fingerprint,b.Fingerprint)))s.BlindSpots.Add(b);}

        public static VoidSnapshot Build(IEnumerable<ExpeditionMission> source,SynthesisSnapshot synthesis,IEnumerable<SynthesisTheoremCandidate> theoremSource,string expeditionFingerprint,string synthesisFingerprint)
        {
            List<ExpeditionMission> missions=(source??Enumerable.Empty<ExpeditionMission>()).Where(x=>x!=null).ToList();List<SynthesisTheoremCandidate> theorems=(theoremSource??Enumerable.Empty<SynthesisTheoremCandidate>()).Where(x=>x!=null).ToList();VoidSnapshot s=new VoidSnapshot{ExpeditionFingerprint=expeditionFingerprint??HashText(""),SynthesisFingerprint=synthesisFingerprint??HashText("")};
            s.Missions=missions.Count;s.Findings=missions.Sum(x=>x.Findings.Count);s.Questions=missions.Sum(x=>x.Questions.Count);s.Contradictions=missions.Sum(x=>x.Contradictions.Count);s.Assumptions=missions.Sum(x=>x.Assumptions.Count);s.Decisions=missions.Sum(x=>x.Decisions.Count);s.UniqueSources=missions.SelectMany(x=>x.Steps).Where(x=>x.Source!=null&&!String.IsNullOrWhiteSpace(x.Source.SourceFingerprint)).Select(x=>x.Source.SourceFingerprint).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            BuildCoverage(s,missions);BuildMissionBlindSpots(s,missions);BuildGlobalBlindSpots(s,missions,synthesis);BuildDisagreements(s,missions);BuildTheoryStress(s,theorems,synthesis);
            s.BlindSpots=s.BlindSpots.OrderByDescending(x=>x.Priority).ThenBy(x=>x.Kind).ThenBy(x=>x.Id).Take(4096).ToList();s.Disagreements=s.Disagreements.OrderBy(x=>x.Kind).ThenBy(x=>x.Id).ToList();s.TheoryStress=s.TheoryStress.OrderByDescending(x=>x.StressPriority).ThenBy(x=>x.Id).ToList();s.Fingerprint=Fingerprint(s);return s;
        }

        private static void BuildCoverage(VoidSnapshot s,List<ExpeditionMission> missions)
        {
            foreach(string civ in ExpeditionKernel.Civilizations)
            {
                List<ExpeditionStep> steps=missions.SelectMany(x=>x.Steps).Where(x=>Eq(x.Civilization,civ)).ToList();int missionCount=missions.Count(m=>m.Steps.Any(x=>Eq(x.Civilization,civ)));CoverageCell c=new CoverageCell{Civilization=civ,Missions=missionCount,Steps=steps.Count,SourceCaptures=steps.Count(x=>x.Source!=null),CoverageRatio=missions.Count==0?0:(double)missionCount/missions.Count};s.Coverage.Add(c);
                if(missions.Count>0&&missionCount==0)Add(s,new BlindSpotCandidate{Kind="CIVILIZATION_COVERAGE_GAP",Severity="HIGH",Civilization=civ,Label=civ+" has never been consulted",Basis="Zero retained EXPΩ missions contain a step in this civilization.",SuggestedProbe="Ask whether this civilization could falsify, qualify or reframe a live frontier before concluding it is irrelevant.",StructuralAbsence=true,Heuristic=false,Priority=86});
                else if(missions.Count>=4&&c.CoverageRatio<0.15)Add(s,new BlindSpotCandidate{Kind="CIVILIZATION_NEGLECT",Severity="MEDIUM",Civilization=civ,Label=civ+" is rarely consulted",Basis="Civilization appears in "+missionCount+" of "+missions.Count+" retained missions.",SuggestedProbe="Sample a high-pressure frontier and test whether this underused lens changes the investigation.",StructuralAbsence=true,Heuristic=true,Priority=55+(0.15-c.CoverageRatio)*100});
            }
            s.Coverage=s.Coverage.OrderBy(x=>x.CoverageRatio).ThenBy(x=>x.Civilization).ToList();
        }

        private static void BuildMissionBlindSpots(VoidSnapshot s,List<ExpeditionMission> missions)
        {
            foreach(ExpeditionMission m in missions)
            {
                int uniqueSources=m.Steps.Where(x=>x.Source!=null&&!String.IsNullOrWhiteSpace(x.Source.SourceFingerprint)).Select(x=>x.Source.SourceFingerprint).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                if(m.Findings.Count>=3&&uniqueSources<=1)Add(s,new BlindSpotCandidate{Kind="SOURCE_MONOCULTURE",Severity="HIGH",MissionId=m.Id,Label=(m.Title??m.Id)+" rests on "+uniqueSources+" distinct source fingerprint(s)",Basis="Mission has "+m.Findings.Count+" findings but only "+uniqueSources+" retained source fingerprint(s).",SuggestedProbe="Seek an independent source path or explicitly downgrade claims that cannot escape the source monoculture.",StructuralAbsence=true,Heuristic=false,Priority=92});
                foreach(ExpeditionQuestion q in m.Questions.Where(x=>Eq(x.Status,"OPEN")))Add(s,new BlindSpotCandidate{Kind="OPEN_FRONTIER",Severity="MEDIUM",MissionId=m.Id,BranchId=q.BranchId,SubjectId=q.Id,Label=q.Text,Basis="Question remains explicitly open in EXPΩ.",SuggestedProbe="Launch a focused expedition branch whose success condition is answering or narrowing this question.",StructuralAbsence=false,Heuristic=false,Priority=60});
                foreach(ExpeditionFinding f in m.Findings.Where(x=>x.SourceStepIds==null||x.SourceStepIds.Count==0))Add(s,new BlindSpotCandidate{Kind="ORPHAN_FINDING",Severity="HIGH",MissionId=m.Id,BranchId=f.BranchId,SubjectId=f.Id,Label=f.Text,Basis="Finding has no explicit source-step lineage.",SuggestedProbe="Attach supporting evidence or mark the finding as interpretation/hypothesis instead of evidence-backed conclusion.",StructuralAbsence=true,Heuristic=false,Priority=88});
                foreach(ExpeditionFinding f in m.Findings.Where(x=>x.SourceStepIds!=null&&x.SourceStepIds.Count==1&&x.Confidence>=0.75))Add(s,new BlindSpotCandidate{Kind="HIGH_CONFIDENCE_SINGLE_PATH",Severity="MEDIUM",MissionId=m.Id,BranchId=f.BranchId,SubjectId=f.Id,Label=f.Text,Basis="High-confidence finding has exactly one explicit source-step path.",SuggestedProbe="Search for independent support and a deliberate disconfirmation path.",StructuralAbsence=true,Heuristic=true,Priority=72});
                foreach(ExpeditionAssumption a in m.Assumptions.Where(x=>Eq(x.Status,"ACTIVE")))
                {
                    bool challenged=m.Contradictions.Any(c=>Has(c.Description,a.Text))||m.Findings.Any(f=>f.Challenges!=null&&f.Challenges.Any(ch=>Has(ch,a.Text)||Has(a.Text,ch)));
                    if(!challenged)Add(s,new BlindSpotCandidate{Kind="UNCHALLENGED_ASSUMPTION",Severity="MEDIUM",MissionId=m.Id,BranchId=a.BranchId,SubjectId=a.Id,Label=a.Text,Basis="Active assumption has no detectable challenge or contradiction link.",SuggestedProbe="Write the strongest plausible failure condition for this assumption and seek evidence that would trigger it.",StructuralAbsence=true,Heuristic=true,Priority=68});
                }
                foreach(ExpeditionDecision d in m.Decisions.Where(x=>x.FindingIds==null||x.FindingIds.Count==0))Add(s,new BlindSpotCandidate{Kind="DECISION_WITHOUT_FINDING_LINEAGE",Severity="HIGH",MissionId=m.Id,BranchId=d.BranchId,SubjectId=d.Id,Label=d.Decision,Basis="Decision records no explicit finding IDs.",SuggestedProbe="Reconstruct the evidence basis or mark the decision as preference/constraint rather than evidence-derived.",StructuralAbsence=true,Heuristic=false,Priority=82});
                int oq=m.Questions.Count(x=>Eq(x.Status,"OPEN")),oc=m.Contradictions.Count(x=>Eq(x.Status,"OPEN"));if(m.Seals.Count>0&&(oq>0||oc>0))Add(s,new BlindSpotCandidate{Kind="SEALED_WITH_OPEN_DEBT",Severity="MEDIUM",MissionId=m.Id,Label=(m.Title??m.Id)+" sealed with unresolved debt",Basis="Mission contains "+oq+" open question(s) and "+oc+" open contradiction(s) after at least one seal.",SuggestedProbe="Treat the seal as integrity only; decide whether unresolved debt requires reopening or explicit scope limitation.",StructuralAbsence=false,Heuristic=false,Priority=75});
                if(m.Branches.Count>1)
                {
                    foreach(ExpeditionBranch b in m.Branches.Where(x=>!Eq(x.Status,"ACTIVE")))if(m.Questions.Any(q=>Eq(q.BranchId,b.Id)&&Eq(q.Status,"OPEN")))Add(s,new BlindSpotCandidate{Kind="ABANDONED_BRANCH_FRONTIER",Severity="MEDIUM",MissionId=m.Id,BranchId=b.Id,SubjectId=b.Id,Label=(b.Name??b.Id)+" retains open questions",Basis="Inactive branch still owns unresolved question(s).",SuggestedProbe="Revisit, explicitly retire with rationale, or merge its unresolved frontier into the active branch.",StructuralAbsence=false,Heuristic=false,Priority=64});
                }
            }
        }

        private static void BuildGlobalBlindSpots(VoidSnapshot s,List<ExpeditionMission> missions,SynthesisSnapshot synthesis)
        {
            var sourceUse=missions.SelectMany(m=>m.Steps.Where(st=>st.Source!=null&&!String.IsNullOrWhiteSpace(st.Source.SourceFingerprint)).Select(st=>new{Mission=m.Id,Fp=st.Source.SourceFingerprint})).GroupBy(x=>x.Fp,StringComparer.OrdinalIgnoreCase).OrderByDescending(g=>g.Select(x=>x.Mission).Distinct(StringComparer.OrdinalIgnoreCase).Count()).ToList();
            foreach(var g in sourceUse.Where(g=>g.Select(x=>x.Mission).Distinct(StringComparer.OrdinalIgnoreCase).Count()>=3).Take(64)){int n=g.Select(x=>x.Mission).Distinct(StringComparer.OrdinalIgnoreCase).Count();Add(s,new BlindSpotCandidate{Kind="CROSS_MISSION_SOURCE_GRAVITY",Severity="MEDIUM",SubjectId=g.Key,Label="One retained source fingerprint appears across "+n+" missions",Basis="Cross-mission reuse can masquerade as independent convergence.",SuggestedProbe="Find genuinely independent evidence before treating recurrence as replication.",StructuralAbsence=false,Heuristic=false,Priority=Math.Min(90,50+n*6)});}
            if(synthesis!=null)
            {
                foreach(SynthesisPattern p in synthesis.Patterns.Where(x=>x.SupportMissions>=2&&x.UniqueSources<=1).Take(256))Add(s,new BlindSpotCandidate{Kind="SYNTHESIS_SOURCE_MONOCULTURE",Severity="HIGH",SubjectId=p.Id,Label=p.Label,Basis="Pattern recurs across "+p.SupportMissions+" missions but has "+p.UniqueSources+" unique retained source fingerprint(s).",SuggestedProbe="Do not treat recurrence as independent support; launch an EXPΩ transfer trial that deliberately seeks new sources.",StructuralAbsence=false,Heuristic=false,Priority=94});
                foreach(SynthesisTransferProbe p in synthesis.TransferProbes.Where(x=>x.MissingPreconditions!=null&&x.MissingPreconditions.Count>=2).Take(256))Add(s,new BlindSpotCandidate{Kind="TRANSFER_PRECONDITION_GAP",Severity="MEDIUM",MissionId=p.TargetMissionId,SubjectId=p.Id,Label=p.TargetTitle,Basis="Transfer probe is missing "+p.MissingPreconditions.Count+" supporting civilization precondition(s).",SuggestedProbe="Test missing contexts before transferring the pattern.",StructuralAbsence=true,Heuristic=true,Priority=58+Math.Min(25,p.MissingPreconditions.Count*5)});
            }
            if(missions.Count>0&&s.UniqueSources==0)Add(s,new BlindSpotCandidate{Kind="ZERO_RETAINED_SOURCE_UNIVERSE",Severity="CRITICAL",Label="The expedition universe has no retained source fingerprints",Basis="All claims would be operating without retained external/source identity.",SuggestedProbe="Capture source snapshots before doing any cross-mission synthesis.",StructuralAbsence=true,Heuristic=false,Priority=100});
        }

        private static void BuildDisagreements(VoidSnapshot s,List<ExpeditionMission> missions)
        {
            foreach(ExpeditionMission m in missions)foreach(ExpeditionContradiction c in m.Contradictions.Where(x=>Eq(x.Status,"OPEN")))
            {
                string text=(c.Description??"")+" "+(c.Resolution??"");string kind="EVIDENCE_OR_MODEL_CONFLICT";string basis="No more specific structural signature was detected.";
                if(Has(text,"definition")||Has(text,"term")||Has(text,"meaning")){kind="TERMINOLOGY_MISMATCH";basis="Contradiction text contains terminology/definition cues.";}
                else if(Has(text,"scope")||Has(text,"context")||Has(text,"population")||Has(text,"condition")){kind="SCOPE_MISMATCH";basis="Contradiction text contains scope/context cues.";}
                else if(Has(text,"assum")||Has(text,"premise")){kind="ASSUMPTION_CONFLICT";basis="Contradiction text contains assumption/premise cues.";}
                else if(Has(text,"stale")||Has(text,"date")||Has(text,"version")||Has(text,"changed")){kind="TEMPORAL_OR_VERSION_MISMATCH";basis="Contradiction text contains time/version cues.";}
                else if(Has(text,"model")||Has(text,"simulation")||Has(text,"estimate")){kind="MODEL_CONFLICT";basis="Contradiction text contains model/estimate cues.";}
                DisagreementCandidate d=new DisagreementCandidate{MissionId=m.Id,ContradictionId=c.Id,Kind=kind,Description=c.Description,Basis=basis,Heuristic=true};d.Fingerprint=HashText(E(m.Id)+"|"+E(c.Id)+"|"+kind+"|"+E(c.Description));d.Id="DIA-"+d.Fingerprint.Substring(0,18).ToUpperInvariant();s.Disagreements.Add(d);
            }
        }

        private static void BuildTheoryStress(VoidSnapshot s,List<SynthesisTheoremCandidate> theorems,SynthesisSnapshot synthesis)
        {
            foreach(SynthesisTheoremCandidate t in theorems)
            {
                SynthesisPattern p=synthesis==null?null:synthesis.Patterns.FirstOrDefault(q=>Eq(q.Id,t.PatternId));TheoryStressCandidate x=new TheoryStressCandidate{TheoremId=t.Id,PatternId=t.PatternId,Statement=t.Statement,Scope=t.Scope};
                if(String.IsNullOrWhiteSpace(t.Scope))x.MissingTests.Add("EXPLICIT_SCOPE");
                if(p==null)x.MissingTests.Add("ORIGIN_PATTERN_PRESENT");
                else {if(p.UniqueSources<2)x.MissingTests.Add("INDEPENDENT_SOURCE_PATH");if(p.CounterexampleMissionIds.Count==0)x.MissingTests.Add("DELIBERATE_COUNTEREXAMPLE_SEARCH");else x.CounterexampleMissionIds.AddRange(p.CounterexampleMissionIds);if(p.Preconditions==null||p.Preconditions.Count==0)x.MissingTests.Add("BOUNDARY_PRECONDITIONS");if(p.TransferRisk>=0.67)x.MissingTests.Add("HIGH_RISK_TRANSFER_TEST");}
                x.MissingTests.Add("EXPLICIT_FALSIFIER");x.MissingTests.Add("COMPETING_EXPLANATION");x.StressPriority=Math.Min(100,35+x.MissingTests.Count*10+x.CounterexampleMissionIds.Count*8);x.Fingerprint=HashText(E(t.Id)+"|"+E(t.PatternId)+"|"+E(t.Statement)+"|"+E(t.Scope)+"|"+Join(x.MissingTests)+"|"+Join(x.CounterexampleMissionIds));x.Id="THEORY-STRESS-"+x.Fingerprint.Substring(0,16).ToUpperInvariant();s.TheoryStress.Add(x);
                if(String.IsNullOrWhiteSpace(t.Scope))Add(s,new BlindSpotCandidate{Kind="THEOREM_CANDIDATE_WITHOUT_SCOPE",Severity="HIGH",SubjectId=t.Id,Label=t.Statement,Basis="Human-promoted theorem candidate has empty scope text.",SuggestedProbe="Specify the domain, conditions and exclusions before further promotion or transfer.",StructuralAbsence=true,Heuristic=false,Priority=95});
            }
        }

        public static List<VoidValidationFinding> Validate(VoidSnapshot s)
        {
            List<VoidValidationFinding> v=new List<VoidValidationFinding>();if(s==null){v.Add(new VoidValidationFinding{Severity="ERROR",Kind="SNAPSHOT",Message="Snapshot is null."});return v;}if(s.BlindSpots==null||s.Disagreements==null||s.TheoryStress==null||s.Coverage==null)v.Add(new VoidValidationFinding{Severity="ERROR",Kind="COLLECTIONS",Message="One or more snapshot collections are null."});
            if(s.BlindSpots!=null&&s.BlindSpots.Any(x=>String.IsNullOrWhiteSpace(x.Fingerprint)||String.IsNullOrWhiteSpace(x.Id)))v.Add(new VoidValidationFinding{Severity="ERROR",Kind="IDENTITY",Message="Blind spot missing deterministic identity."});
            if(s.BlindSpots!=null&&s.BlindSpots.Any(x=>x.Heuristic&&String.IsNullOrWhiteSpace(x.Basis)))v.Add(new VoidValidationFinding{Severity="ERROR",Kind="HEURISTIC_BASIS",Message="Heuristic blind spot missing visible basis."});
            if(s.Coverage!=null&&s.Coverage.Any(x=>x.CoverageRatio<0||x.CoverageRatio>1))v.Add(new VoidValidationFinding{Severity="ERROR",Kind="COVERAGE_RANGE",Message="Coverage ratio outside [0,1]."});
            if(s.BlindSpots!=null&&s.BlindSpots.GroupBy(x=>x.Id,StringComparer.OrdinalIgnoreCase).Any(g=>g.Count()>1))v.Add(new VoidValidationFinding{Severity="ERROR",Kind="DUPLICATE_ID",Message="Duplicate blind-spot identities detected."});
            if(v.Count==0)v.Add(new VoidValidationFinding{Severity="PASS",Kind="STRUCTURE",SubjectId=Short(s.Fingerprint),Message="VOIDΩ snapshot structural invariants pass. This is not a claim that unknown unknowns have been discovered."});return v;
        }

        public static string Fingerprint(VoidSnapshot s)
        {
            if(s==null)return HashText("");StringBuilder b=new StringBuilder();b.Append(s.Schema).Append('|').Append(E(s.ExpeditionFingerprint)).Append('|').Append(E(s.SynthesisFingerprint)).Append('|').Append(s.Missions).Append('|').Append(s.Findings).Append('|').Append(s.UniqueSources);foreach(BlindSpotCandidate x in s.BlindSpots.OrderBy(x=>x.Id))b.Append('|').Append(x.Fingerprint);foreach(DisagreementCandidate x in s.Disagreements.OrderBy(x=>x.Id))b.Append('|').Append(x.Fingerprint);foreach(TheoryStressCandidate x in s.TheoryStress.OrderBy(x=>x.Id))b.Append('|').Append(x.Fingerprint);foreach(CoverageCell x in s.Coverage.OrderBy(x=>x.Civilization))b.Append('|').Append(E(x.Civilization)).Append(':').Append(x.Missions).Append(':').Append(x.Steps);return HashText(b.ToString());
        }
    }
}
