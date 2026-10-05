using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Yomi.ProductShell
{
    internal sealed class ConstellationNode
    {
        public string Id { get; set; }
        public string Kind { get; set; }
        public string MissionId { get; set; }
        public string BranchId { get; set; }
        public string StepId { get; set; }
        public string Label { get; set; }
        public string Detail { get; set; }
        public string Fingerprint { get; set; }
        public double Weight { get; set; }
        public string Status { get; set; }
        public override string ToString() { return (Kind ?? "NODE") + "   ·   " + (Status ?? "") + "   ·   " + (Label ?? Id) + "   ·   " + KnowledgeConstellationKernel.Short(Fingerprint); }
    }

    internal sealed class ConstellationEdge
    {
        public string Id { get; set; }
        public string FromId { get; set; }
        public string ToId { get; set; }
        public string Kind { get; set; }
        public string Basis { get; set; }
        public double Strength { get; set; }
        public bool Heuristic { get; set; }
        public string Fingerprint { get; set; }
        public override string ToString() { return (Heuristic ? "≈ " : "→ ") + (Kind ?? "EDGE") + "   ·   " + ShortId(FromId) + " → " + ShortId(ToId) + "   ·   " + Strength.ToString("0.00", CultureInfo.InvariantCulture) + "   ·   " + (Basis ?? ""); }
        private static string ShortId(string s) { if (String.IsNullOrWhiteSpace(s)) return "∅"; return s.Length > 24 ? s.Substring(0,24) : s; }
    }

    internal sealed class MissionDigest
    {
        public string MissionId { get; set; }
        public string Title { get; set; }
        public string Question { get; set; }
        public string Fingerprint { get; set; }
        public int Branches { get; set; }
        public int Steps { get; set; }
        public int Findings { get; set; }
        public int OpenQuestions { get; set; }
        public int OpenContradictions { get; set; }
        public int UniqueSources { get; set; }
        public int Civilizations { get; set; }
        public double FrontierPressure { get; set; }
        public override string ToString() { return (Title ?? MissionId) + "   ·   steps " + Steps + "   ·   findings " + Findings + "   ·   open Q " + OpenQuestions + "   ·   conflicts " + OpenContradictions + "   ·   frontier " + FrontierPressure.ToString("0.00", CultureInfo.InvariantCulture); }
    }

    internal sealed class KnowledgeFrontier
    {
        public string Id { get; set; }
        public string MissionId { get; set; }
        public string BranchId { get; set; }
        public string QuestionId { get; set; }
        public string Question { get; set; }
        public string Origin { get; set; }
        public int SupportingFindings { get; set; }
        public int RelatedMissions { get; set; }
        public int Contradictions { get; set; }
        public double Pressure { get; set; }
        public string Fingerprint { get; set; }
        public override string ToString() { return Pressure.ToString("000.0", CultureInfo.InvariantCulture) + "   ·   " + (Origin ?? "FRONTIER") + "   ·   " + (Question ?? Id) + "   ·   related missions " + RelatedMissions; }
    }

    internal sealed class ConvergenceCluster
    {
        public string Id { get; set; }
        public string Token { get; set; }
        public List<string> MissionIds { get; set; }
        public List<string> FindingIds { get; set; }
        public double Score { get; set; }
        public bool Heuristic { get; set; }
        public string Fingerprint { get; set; }
        public ConvergenceCluster() { MissionIds = new List<string>(); FindingIds = new List<string>(); Heuristic = true; }
        public override string ToString() { return Score.ToString("000.0", CultureInfo.InvariantCulture) + "   ·   token " + (Token ?? "∅") + "   ·   missions " + MissionIds.Count + "   ·   findings " + FindingIds.Count + "   ·   HEURISTIC"; }
    }

    internal sealed class SourceReuseCluster
    {
        public string SourceFingerprint { get; set; }
        public List<string> MissionIds { get; set; }
        public List<string> StepIds { get; set; }
        public List<string> Civilizations { get; set; }
        public SourceReuseCluster() { MissionIds = new List<string>(); StepIds = new List<string>(); Civilizations = new List<string>(); }
        public override string ToString() { return KnowledgeConstellationKernel.Short(SourceFingerprint) + "   ·   missions " + MissionIds.Count + "   ·   steps " + StepIds.Count + "   ·   systems " + Civilizations.Count; }
    }

    internal sealed class MissionAffinity
    {
        public string LeftMissionId { get; set; }
        public string RightMissionId { get; set; }
        public double Score { get; set; }
        public int SharedSources { get; set; }
        public int SharedTokens { get; set; }
        public string Basis { get; set; }
        public bool Heuristic { get; set; }
        public override string ToString() { return Score.ToString("0.000", CultureInfo.InvariantCulture) + "   ·   " + (LeftMissionId ?? "∅") + " ↔ " + (RightMissionId ?? "∅") + "   ·   shared sources " + SharedSources + "   ·   shared tokens " + SharedTokens + "   ·   " + (Heuristic ? "HEURISTIC" : "OBSERVED"); }
    }

    internal sealed class ConstellationSnapshot
    {
        public int Schema { get; set; }
        public string BuiltUtc { get; set; }
        public string ExpeditionStoreFingerprint { get; set; }
        public List<MissionDigest> Missions { get; set; }
        public List<ConstellationNode> Nodes { get; set; }
        public List<ConstellationEdge> Edges { get; set; }
        public List<KnowledgeFrontier> Frontiers { get; set; }
        public List<ConvergenceCluster> Convergences { get; set; }
        public List<SourceReuseCluster> SourceReuse { get; set; }
        public List<MissionAffinity> Affinities { get; set; }
        public string Fingerprint { get; set; }
        public ConstellationSnapshot() { Schema=1; BuiltUtc=DateTime.UtcNow.ToString("o",CultureInfo.InvariantCulture); Missions=new List<MissionDigest>();Nodes=new List<ConstellationNode>();Edges=new List<ConstellationEdge>();Frontiers=new List<KnowledgeFrontier>();Convergences=new List<ConvergenceCluster>();SourceReuse=new List<SourceReuseCluster>();Affinities=new List<MissionAffinity>(); }
        public override string ToString() { return "METAΩ SNAPSHOT   ·   missions " + Missions.Count + "   ·   nodes " + Nodes.Count + "   ·   edges " + Edges.Count + "   ·   frontiers " + Frontiers.Count + "   ·   fp " + KnowledgeConstellationKernel.Short(Fingerprint); }
    }

    internal sealed class ConstellationValidationFinding
    {
        public string Severity { get; set; }
        public string Kind { get; set; }
        public string SubjectId { get; set; }
        public string Message { get; set; }
        public override string ToString() { return (Severity ?? "INFO") + "   ·   " + (Kind ?? "CHECK") + "   ·   " + (SubjectId ?? "∅") + "   ·   " + (Message ?? ""); }
    }

    internal static class KnowledgeConstellationKernel
    {
        private static readonly HashSet<string> Stop = new HashSet<string>(new[]{"the","and","for","that","with","from","this","into","what","when","where","which","while","have","has","are","was","were","will","would","could","should","about","under","over","between","through","without","within","than","then","them","they","their","there","does","did","can","why","how","who","its","our","your","not","but","all","any","each","one","two","new","use","using","used","system","mission","finding","question"},StringComparer.OrdinalIgnoreCase);
        public static string HashText(string text){using(SHA256 sha=SHA256.Create()){byte[] h=sha.ComputeHash(Encoding.UTF8.GetBytes(text??""));StringBuilder b=new StringBuilder(h.Length*2);foreach(byte x in h)b.Append(x.ToString("x2",CultureInfo.InvariantCulture));return b.ToString();}}
        public static string Short(string hash){return String.IsNullOrWhiteSpace(hash)?"∅":hash.Substring(0,Math.Min(12,hash.Length));}
        private static string E(string s){return s??"";}
        private static string Join(IEnumerable<string> xs){return xs==null?"":String.Join("\n",xs.Where(x=>!String.IsNullOrWhiteSpace(x)).OrderBy(x=>x,StringComparer.Ordinal).ToArray());}
        private static string NodeId(string kind,string id){return (kind??"NODE")+":"+(id??"∅");}
        private static void AddEdge(ConstellationSnapshot s,string from,string to,string kind,string basis,double strength,bool heuristic){if(String.IsNullOrWhiteSpace(from)||String.IsNullOrWhiteSpace(to))return;ConstellationEdge e=new ConstellationEdge{Id="EDGE-"+HashText(from+"|"+to+"|"+kind+"|"+basis).Substring(0,20).ToUpperInvariant(),FromId=from,ToId=to,Kind=kind,Basis=basis,Strength=Math.Max(0,Math.Min(1,strength)),Heuristic=heuristic};e.Fingerprint=HashText(e.Id+"|"+e.FromId+"|"+e.ToId+"|"+e.Kind+"|"+e.Basis+"|"+e.Strength.ToString("R",CultureInfo.InvariantCulture)+"|"+e.Heuristic);s.Edges.Add(e);}
        private static IEnumerable<string> Tokens(string text){if(String.IsNullOrWhiteSpace(text))yield break;StringBuilder b=new StringBuilder();foreach(char c in text.ToLowerInvariant())b.Append(Char.IsLetterOrDigit(c)?c:' ');foreach(string x in b.ToString().Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries)){if(x.Length<4||Stop.Contains(x))continue;yield return x;}}

        public static ConstellationSnapshot Build(IEnumerable<ExpeditionMission> source,string storeFingerprint)
        {
            List<ExpeditionMission> missions=(source??Enumerable.Empty<ExpeditionMission>()).Where(x=>x!=null).ToList();ConstellationSnapshot s=new ConstellationSnapshot{ExpeditionStoreFingerprint=storeFingerprint??HashText("")};
            Dictionary<string,ConstellationNode> nodes=new Dictionary<string,ConstellationNode>(StringComparer.OrdinalIgnoreCase);
            Action<ConstellationNode> addNode=n=>{if(n==null||String.IsNullOrWhiteSpace(n.Id))return;if(!nodes.ContainsKey(n.Id)){nodes[n.Id]=n;s.Nodes.Add(n);}};
            foreach(ExpeditionMission m in missions)
            {
                string mid=NodeId("MISSION",m.Id);addNode(new ConstellationNode{Id=mid,Kind="MISSION",MissionId=m.Id,Label=m.Title,Detail=m.Question,Fingerprint=ExpeditionKernel.Fingerprint(m),Weight=2.0,Status=m.Status});
                HashSet<string> civ=new HashSet<string>(m.Steps.Select(x=>x.Civilization??""),StringComparer.OrdinalIgnoreCase);HashSet<string> src=new HashSet<string>(m.Steps.Where(x=>x.Source!=null&&!String.IsNullOrWhiteSpace(x.Source.SourceFingerprint)).Select(x=>x.Source.SourceFingerprint),StringComparer.OrdinalIgnoreCase);
                int oq=m.Questions.Count(x=>String.Equals(x.Status,"OPEN",StringComparison.OrdinalIgnoreCase));int oc=m.Contradictions.Count(x=>String.Equals(x.Status,"OPEN",StringComparison.OrdinalIgnoreCase));
                MissionDigest d=new MissionDigest{MissionId=m.Id,Title=m.Title,Question=m.Question,Fingerprint=ExpeditionKernel.Fingerprint(m),Branches=m.Branches.Count,Steps=m.Steps.Count,Findings=m.Findings.Count,OpenQuestions=oq,OpenContradictions=oc,UniqueSources=src.Count,Civilizations=civ.Count,FrontierPressure=oq*2.0+oc*3.0+Math.Max(0,17-civ.Count)*0.25};s.Missions.Add(d);
                foreach(ExpeditionBranch b in m.Branches){string bid=NodeId("BRANCH",b.Id);addNode(new ConstellationNode{Id=bid,Kind="BRANCH",MissionId=m.Id,BranchId=b.Id,Label=b.Name,Detail=b.Question,Fingerprint=HashText(E(b.Id)+"|"+E(b.ParentBranchId)+"|"+E(b.Question)+"|"+E(b.Hypothesis)),Weight=1.2,Status=b.Status});AddEdge(s,mid,bid,"OWNS_BRANCH","recorded mission membership",1,false);if(!String.IsNullOrWhiteSpace(b.ParentBranchId))AddEdge(s,NodeId("BRANCH",b.ParentBranchId),bid,"FORKED_TO","recorded branch ancestry",1,false);}
                foreach(ExpeditionStep st in m.Steps){string sid=NodeId("STEP",st.Id);addNode(new ConstellationNode{Id=sid,Kind="STEP",MissionId=m.Id,BranchId=st.BranchId,StepId=st.Id,Label=(st.Civilization??"STEP")+" · "+(st.Chamber??"ENTRY"),Detail=st.QuestionAtEntry,Fingerprint=ExpeditionKernel.FingerprintStep(st),Weight=1.0,Status=st.Status});AddEdge(s,NodeId("BRANCH",st.BranchId),sid,"CONTAINS_STEP","recorded branch step",1,false);if(!String.IsNullOrWhiteSpace(st.ParentStepId))AddEdge(s,NodeId("STEP",st.ParentStepId),sid,"NEXT_STEP","recorded step ancestry",1,false);if(st.Source!=null&&!String.IsNullOrWhiteSpace(st.Source.SourceFingerprint)){string so=NodeId("SOURCE",st.Source.SourceFingerprint);addNode(new ConstellationNode{Id=so,Kind="SOURCE",MissionId=m.Id,BranchId=st.BranchId,StepId=st.Id,Label=st.Source.Label??st.Source.SourceId,Detail=st.Source.Civilization,Fingerprint=st.Source.SourceFingerprint,Weight=1.4,Status="CAPTURED"});AddEdge(s,sid,so,"CAPTURED_SOURCE","exact retained source fingerprint",1,false);}}
                foreach(ExpeditionFinding f in m.Findings){string fid=NodeId("FINDING",f.Id);addNode(new ConstellationNode{Id=fid,Kind="FINDING",MissionId=m.Id,BranchId=f.BranchId,StepId=f.StepId,Label=f.Text,Detail=(f.Kind??"FINDING")+" confidence "+f.Confidence.ToString("0.00",CultureInfo.InvariantCulture),Fingerprint=HashText(E(f.Id)+"|"+E(f.Text)+"|"+f.Confidence.ToString("R",CultureInfo.InvariantCulture)+"|"+Join(f.SourceStepIds)),Weight=1.5,Status=f.Status});AddEdge(s,NodeId("STEP",f.StepId),fid,"ASSERTED_FINDING","recorded finding at step",1,false);foreach(string x in f.SourceStepIds)AddEdge(s,NodeId("STEP",x),fid,"EVIDENCE_FOR","explicit source-step lineage",1,false);}
                foreach(ExpeditionQuestion q in m.Questions){string qid=NodeId("QUESTION",q.Id);addNode(new ConstellationNode{Id=qid,Kind="QUESTION",MissionId=m.Id,BranchId=q.BranchId,Label=q.Text,Detail="investigation frontier",Fingerprint=HashText(E(q.Id)+"|"+E(q.BranchId)+"|"+E(q.ParentQuestionId)+"|"+E(q.Text)+"|"+E(q.Status)),Weight=1.3,Status=q.Status});AddEdge(s,NodeId("BRANCH",q.BranchId),qid,"ASKS","recorded branch question",1,false);if(!String.IsNullOrWhiteSpace(q.ParentQuestionId))AddEdge(s,NodeId("QUESTION",q.ParentQuestionId),qid,"CHILD_QUESTION","recorded question ancestry",1,false);if(!String.IsNullOrWhiteSpace(q.ResolutionFindingId))AddEdge(s,NodeId("FINDING",q.ResolutionFindingId),qid,"RESOLVES","explicit question resolution",1,false);if(String.Equals(q.Status,"OPEN",StringComparison.OrdinalIgnoreCase)){KnowledgeFrontier f=new KnowledgeFrontier{Id="FRONTIER-"+q.Id,MissionId=m.Id,BranchId=q.BranchId,QuestionId=q.Id,Question=q.Text,Origin=m.Title??m.Id,SupportingFindings=m.Findings.Count(x=>x.BranchId==q.BranchId),Contradictions=m.Contradictions.Count(x=>x.BranchId==q.BranchId&&String.Equals(x.Status,"OPEN",StringComparison.OrdinalIgnoreCase))};s.Frontiers.Add(f);}}
                foreach(ExpeditionContradiction c in m.Contradictions){string cid=NodeId("CONTRADICTION",c.Id);addNode(new ConstellationNode{Id=cid,Kind="CONTRADICTION",MissionId=m.Id,BranchId=c.BranchId,Label=c.Description,Detail=c.Resolution,Fingerprint=HashText(E(c.Id)+"|"+E(c.LeftFindingId)+"|"+E(c.RightFindingId)+"|"+E(c.Description)+"|"+E(c.Status)+"|"+E(c.Resolution)),Weight=1.8,Status=c.Status});AddEdge(s,NodeId("FINDING",c.LeftFindingId),cid,"CONTRADICTS","explicit contradiction left member",1,false);AddEdge(s,NodeId("FINDING",c.RightFindingId),cid,"CONTRADICTS","explicit contradiction right member",1,false);}
                foreach(ExpeditionDecision d0 in m.Decisions){string did=NodeId("DECISION",d0.Id);addNode(new ConstellationNode{Id=did,Kind="DECISION",MissionId=m.Id,BranchId=d0.BranchId,StepId=d0.StepId,Label=d0.Decision,Detail=d0.Rationale,Fingerprint=HashText(E(d0.Id)+"|"+E(d0.Decision)+"|"+E(d0.Rationale)+"|"+Join(d0.FindingIds)),Weight=1.4,Status="RECORDED"});AddEdge(s,NodeId("STEP",d0.StepId),did,"DECISION_AT","recorded decision step",1,false);foreach(string x in d0.FindingIds)AddEdge(s,NodeId("FINDING",x),did,"INFORMS_DECISION","explicit finding reference",1,false);}
                foreach(ExpeditionCheckpoint c in m.Checkpoints){string cid=NodeId("CHECKPOINT",c.Id);addNode(new ConstellationNode{Id=cid,Kind="CHECKPOINT",MissionId=m.Id,BranchId=c.BranchId,StepId=c.StepId,Label=c.Name,Detail=c.Note,Fingerprint=c.MissionFingerprint,Weight=.8,Status="RETAINED"});AddEdge(s,NodeId("STEP",c.StepId),cid,"CHECKPOINT_AT","recorded exact resume point",1,false);}
            }
            BuildSourceReuse(s,missions);BuildConvergences(s,missions);BuildAffinities(s,missions);ScoreFrontiers(s,missions);
            s.Nodes=s.Nodes.OrderBy(x=>x.Kind).ThenBy(x=>x.MissionId).ThenBy(x=>x.Id,StringComparer.Ordinal).ToList();s.Edges=s.Edges.OrderBy(x=>x.Heuristic).ThenBy(x=>x.Kind).ThenBy(x=>x.Id,StringComparer.Ordinal).ToList();s.Frontiers=s.Frontiers.OrderByDescending(x=>x.Pressure).ThenBy(x=>x.Id).ToList();s.Fingerprint=Fingerprint(s);return s;
        }

        private static void BuildSourceReuse(ConstellationSnapshot s,List<ExpeditionMission> missions)
        {
            var rows=missions.SelectMany(m=>m.Steps.Where(st=>st.Source!=null&&!String.IsNullOrWhiteSpace(st.Source.SourceFingerprint)).Select(st=>new{Mission=m.Id,Step=st.Id,Civ=st.Civilization,Fp=st.Source.SourceFingerprint})).GroupBy(x=>x.Fp,StringComparer.OrdinalIgnoreCase);
            foreach(var g in rows){SourceReuseCluster c=new SourceReuseCluster{SourceFingerprint=g.Key};c.MissionIds.AddRange(g.Select(x=>x.Mission).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x=>x));c.StepIds.AddRange(g.Select(x=>x.Step).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x=>x));c.Civilizations.AddRange(g.Select(x=>x.Civ??"").Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x=>x));if(c.MissionIds.Count>1)s.SourceReuse.Add(c);List<string> steps=c.StepIds.ToList();for(int i=0;i<steps.Count;i++)for(int j=i+1;j<steps.Count;j++)AddEdge(s,NodeId("STEP",steps[i]),NodeId("STEP",steps[j]),"SHARED_SOURCE_FINGERPRINT","exact same retained source fingerprint",1,false);}
            s.SourceReuse=s.SourceReuse.OrderByDescending(x=>x.MissionIds.Count).ThenByDescending(x=>x.StepIds.Count).ToList();
        }

        private static void BuildConvergences(ConstellationSnapshot s,List<ExpeditionMission> missions)
        {
            var tokenRows=new Dictionary<string,List<Tuple<string,string>>>(StringComparer.OrdinalIgnoreCase);
            foreach(ExpeditionMission m in missions)foreach(ExpeditionFinding f in m.Findings)foreach(string t in Tokens(f.Text).Distinct(StringComparer.OrdinalIgnoreCase)){List<Tuple<string,string>> list;if(!tokenRows.TryGetValue(t,out list)){list=new List<Tuple<string,string>>();tokenRows[t]=list;}list.Add(Tuple.Create(m.Id,f.Id));}
            foreach(var kv in tokenRows){List<string> mids=kv.Value.Select(x=>x.Item1).Distinct(StringComparer.OrdinalIgnoreCase).ToList();if(mids.Count<2)continue;ConvergenceCluster c=new ConvergenceCluster{Id="CONV-"+HashText(kv.Key+"|"+Join(mids)).Substring(0,16).ToUpperInvariant(),Token=kv.Key,Score=mids.Count*10+kv.Value.Count};c.MissionIds.AddRange(mids.OrderBy(x=>x));c.FindingIds.AddRange(kv.Value.Select(x=>x.Item2).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x=>x));c.Fingerprint=HashText(c.Id+"|"+c.Token+"|"+Join(c.MissionIds)+"|"+Join(c.FindingIds));s.Convergences.Add(c);}
            s.Convergences=s.Convergences.OrderByDescending(x=>x.Score).ThenBy(x=>x.Token).Take(512).ToList();
        }

        private static void BuildAffinities(ConstellationSnapshot s,List<ExpeditionMission> missions)
        {
            for(int i=0;i<missions.Count;i++)for(int j=i+1;j<missions.Count;j++)
            {
                ExpeditionMission a=missions[i],b=missions[j];HashSet<string> sa=new HashSet<string>(a.Steps.Where(x=>x.Source!=null&&!String.IsNullOrWhiteSpace(x.Source.SourceFingerprint)).Select(x=>x.Source.SourceFingerprint),StringComparer.OrdinalIgnoreCase);HashSet<string> sb=new HashSet<string>(b.Steps.Where(x=>x.Source!=null&&!String.IsNullOrWhiteSpace(x.Source.SourceFingerprint)).Select(x=>x.Source.SourceFingerprint),StringComparer.OrdinalIgnoreCase);int sharedSources=sa.Intersect(sb,StringComparer.OrdinalIgnoreCase).Count();HashSet<string> ta=new HashSet<string>(Tokens(E(a.Question)+" "+String.Join(" ",a.Findings.Select(x=>x.Text??"").ToArray())),StringComparer.OrdinalIgnoreCase);HashSet<string> tb=new HashSet<string>(Tokens(E(b.Question)+" "+String.Join(" ",b.Findings.Select(x=>x.Text??"").ToArray())),StringComparer.OrdinalIgnoreCase);int sharedTokens=ta.Intersect(tb,StringComparer.OrdinalIgnoreCase).Count();double denom=Math.Max(1,ta.Union(tb,StringComparer.OrdinalIgnoreCase).Count());double lexical=sharedTokens/denom;double score=Math.Min(1,sharedSources*.35+lexical*.65);if(score<=0)continue;MissionAffinity af=new MissionAffinity{LeftMissionId=a.Id,RightMissionId=b.Id,Score=score,SharedSources=sharedSources,SharedTokens=sharedTokens,Basis=sharedSources>0?"exact source overlap + lexical overlap":"lexical overlap only",Heuristic=sharedSources==0};s.Affinities.Add(af);AddEdge(s,NodeId("MISSION",a.Id),NodeId("MISSION",b.Id),sharedSources>0?"MISSION_EVIDENCE_OVERLAP":"MISSION_AFFINITY",af.Basis,score,af.Heuristic);
            }
            s.Affinities=s.Affinities.OrderByDescending(x=>x.Score).ThenBy(x=>x.LeftMissionId).ToList();
        }

        private static void ScoreFrontiers(ConstellationSnapshot s,List<ExpeditionMission> missions)
        {
            foreach(KnowledgeFrontier f in s.Frontiers){HashSet<string> t=new HashSet<string>(Tokens(f.Question),StringComparer.OrdinalIgnoreCase);int related=0;foreach(ExpeditionMission m in missions){if(m.Id==f.MissionId)continue;HashSet<string> mt=new HashSet<string>(Tokens(E(m.Question)+" "+String.Join(" ",m.Findings.Select(x=>x.Text??"").ToArray())),StringComparer.OrdinalIgnoreCase);if(t.Overlaps(mt))related++;}f.RelatedMissions=related;f.Pressure=10+f.Contradictions*7+related*3+Math.Max(0,5-f.SupportingFindings);f.Fingerprint=HashText(E(f.Id)+"|"+E(f.MissionId)+"|"+E(f.Question)+"|"+f.Pressure.ToString("R",CultureInfo.InvariantCulture));}
        }

        public static List<ConstellationValidationFinding> Validate(ConstellationSnapshot s)
        {
            List<ConstellationValidationFinding> r=new List<ConstellationValidationFinding>();if(s==null){r.Add(new ConstellationValidationFinding{Severity="ERROR",Kind="SNAPSHOT",Message="Snapshot is null."});return r;}HashSet<string> ids=new HashSet<string>(StringComparer.OrdinalIgnoreCase);foreach(ConstellationNode n in s.Nodes){if(String.IsNullOrWhiteSpace(n.Id))r.Add(new ConstellationValidationFinding{Severity="ERROR",Kind="NODE_ID",Message="Node has no identity."});else if(!ids.Add(n.Id))r.Add(new ConstellationValidationFinding{Severity="ERROR",Kind="DUPLICATE_NODE",SubjectId=n.Id,Message="Node identity appears more than once."});}foreach(ConstellationEdge e in s.Edges){if(!ids.Contains(e.FromId)||!ids.Contains(e.ToId))r.Add(new ConstellationValidationFinding{Severity="ERROR",Kind="DANGLING_EDGE",SubjectId=e.Id,Message="Edge endpoint is missing from snapshot."});if(e.Heuristic&&String.Equals(e.Kind,"SHARED_SOURCE_FINGERPRINT",StringComparison.OrdinalIgnoreCase))r.Add(new ConstellationValidationFinding{Severity="ERROR",Kind="AUTHORITY_BOUNDARY",SubjectId=e.Id,Message="Exact fingerprint edges must not be marked heuristic."});}string fp=Fingerprint(s);if(!String.Equals(fp,s.Fingerprint,StringComparison.OrdinalIgnoreCase))r.Add(new ConstellationValidationFinding{Severity="ERROR",Kind="FINGERPRINT",Message="Snapshot fingerprint does not match retained structure."});if(r.Count==0)r.Add(new ConstellationValidationFinding{Severity="INFO",Kind="STRUCTURE",Message="Constellation structure is internally consistent. This does not certify external truth."});return r;
        }

        public static string Fingerprint(ConstellationSnapshot s)
        {
            if(s==null)return HashText("");StringBuilder b=new StringBuilder();b.Append(s.Schema).Append('|').Append(E(s.ExpeditionStoreFingerprint)).Append('|');foreach(MissionDigest m in s.Missions.OrderBy(x=>x.MissionId))b.Append(E(m.MissionId)).Append(':').Append(E(m.Fingerprint)).Append(';');foreach(ConstellationNode n in s.Nodes.OrderBy(x=>x.Id))b.Append(E(n.Id)).Append(':').Append(E(n.Fingerprint)).Append(';');foreach(ConstellationEdge e in s.Edges.OrderBy(x=>x.Id))b.Append(E(e.Id)).Append(':').Append(E(e.Fingerprint)).Append(';');foreach(KnowledgeFrontier f in s.Frontiers.OrderBy(x=>x.Id))b.Append(E(f.Id)).Append(':').Append(E(f.Fingerprint)).Append(';');return HashText(b.ToString());
        }
    }
}
