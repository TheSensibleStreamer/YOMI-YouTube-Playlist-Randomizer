using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Yomi.ProductShell
{
    internal sealed class WorldPassportSpineObject
    {
        public string Id;
        public string Kind;
        public string Title;
        public string Civilization;
        public string RouteId;
        public string SourceId;
        public string SourcePath;
        public string JsonPath;
        public string Fingerprint;
        public string SnapshotJson;
        public string CreatedUtc;
        public string UpdatedUtc;
        public int OpenCount;
        public override string ToString()
        {
            string civ = String.IsNullOrWhiteSpace(Civilization) ? "OBJECT" : Civilization;
            string kind = String.IsNullOrWhiteSpace(Kind) ? "OBJECT" : Kind;
            return civ + "  ·  " + kind + "  ·  " + (Title ?? Id) + "  ·  " + Short(Fingerprint);
        }
        private static string Short(string s)
        {
            if (String.IsNullOrWhiteSpace(s)) return "∅";
            return s.Length <= 12 ? s : s.Substring(0, 12);
        }
    }

    internal sealed class WorldPassportSpineRelation
    {
        public string Id;
        public string FromId;
        public string ToId;
        public string Kind;
        public string Evidence;
        public string CreatedUtc;
        public override string ToString()
        {
            return (Kind ?? "RELATED_TO") + "  ·  " + Short(FromId) + " → " + Short(ToId) + (String.IsNullOrWhiteSpace(Evidence) ? "" : "  ·  " + Evidence);
        }
        private static string Short(string s)
        {
            if (String.IsNullOrWhiteSpace(s)) return "∅";
            return s.Length <= 18 ? s : s.Substring(0, 18);
        }
    }

    internal sealed class WorldPassportSpineStore
    {
        public int Schema = 1;
        public List<WorldPassportSpineObject> Objects = new List<WorldPassportSpineObject>();
        public List<WorldPassportSpineRelation> Relations = new List<WorldPassportSpineRelation>();
        public string UpdatedUtc;
    }

    internal static class WorldPassportSpine
    {
        private const int MaxObjects = 2048;
        private const int MaxRelations = 8192;
        private static readonly object Gate = new object();
        private static WorldPassportSpineStore Store = new WorldPassportSpineStore();
        private static string LoadedPath;
        private static WeakReference HubWindow;

        private static SolidColorBrush B(string hex) { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
        private static readonly Brush Bg = B("#101417");
        private static readonly Brush Surface = B("#171D21");
        private static readonly Brush Raised = B("#20272D");
        private static readonly Brush Border = B("#354048");
        private static readonly Brush Text = B("#F1F4F6");
        private static readonly Brush Muted = B("#9EABB5");
        private static readonly Brush Accent = B("#69E6B4");
        private static readonly Brush Blue = B("#70B7FF");
        private static readonly Brush Amber = B("#FFCA69");
        private static readonly Brush Danger = B("#FF7B7B");

        private static string PathFor(DeepSystemsContext ctx)
        {
            return Path.Combine(ctx.DataRoot, "deep-systems", "world-passport-spine.json");
        }

        private static string Now() { return DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); }

        private static string Hash(string text)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] b = sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? ""));
                StringBuilder s = new StringBuilder(b.Length * 2);
                foreach (byte x in b) s.Append(x.ToString("x2", CultureInfo.InvariantCulture));
                return s.ToString();
            }
        }

        private static string SafeSerialize(DeepSystemsContext ctx, object value)
        {
            try { return ctx.Json.Serialize(value); }
            catch
            {
                try { return ctx.Json.Serialize(new Dictionary<string, object> { { "fallback_text", value == null ? "null" : value.ToString() } }); }
                catch { return "{\"fallback_text\":\"unserializable\"}"; }
            }
        }

        private static object Deserialize(DeepSystemsContext ctx, string json)
        {
            try { return ctx.Json.DeserializeObject(json ?? "null"); }
            catch { return json ?? ""; }
        }

        private static void Ensure(DeepSystemsContext ctx)
        {
            if (ctx == null) return;
            string path = PathFor(ctx);
            lock (Gate)
            {
                if (String.Equals(LoadedPath, path, StringComparison.OrdinalIgnoreCase)) return;
                LoadedPath = path;
                Store = new WorldPassportSpineStore();
                if (File.Exists(path))
                {
                    try
                    {
                        WorldPassportSpineStore loaded = ctx.Json.Deserialize<WorldPassportSpineStore>(File.ReadAllText(path, Encoding.UTF8));
                        if (loaded != null && loaded.Schema <= 1)
                        {
                            Store = loaded;
                            if (Store.Objects == null) Store.Objects = new List<WorldPassportSpineObject>();
                            if (Store.Relations == null) Store.Relations = new List<WorldPassportSpineRelation>();
                        }
                    }
                    catch { Store = new WorldPassportSpineStore(); }
                }
                Normalize();
            }
        }

        private static void Normalize()
        {
            Store.Objects = Store.Objects
                .Where(x => x != null && !String.IsNullOrWhiteSpace(x.Id))
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(x => x.UpdatedUtc ?? "").First())
                .OrderByDescending(x => x.UpdatedUtc ?? x.CreatedUtc ?? "")
                .Take(MaxObjects).ToList();
            HashSet<string> ids = new HashSet<string>(Store.Objects.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
            Store.Relations = Store.Relations
                .Where(x => x != null && !String.IsNullOrWhiteSpace(x.Id) && ids.Contains(x.FromId ?? "") && ids.Contains(x.ToId ?? "") && !String.Equals(x.FromId, x.ToId, StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderByDescending(x => x.CreatedUtc ?? "")
                .Take(MaxRelations).ToList();
        }

        private static void Save(DeepSystemsContext ctx)
        {
            Ensure(ctx);
            lock (Gate)
            {
                Normalize();
                Store.UpdatedUtc = Now();
                string path = PathFor(ctx);
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string temp = path + ".tmp-" + Guid.NewGuid().ToString("N");
                File.WriteAllText(temp, ctx.Json.Serialize(Store), new UTF8Encoding(false));
                if (File.Exists(path))
                {
                    string backup = path + ".previous";
                    try { File.Replace(temp, path, backup, true); try { if (File.Exists(backup)) File.Delete(backup); } catch { } }
                    catch { File.Copy(temp, path, true); File.Delete(temp); }
                }
                else File.Move(temp, path);
            }
        }

        internal static WorldPassportSpineObject Capture(DeepSystemsContext ctx, string title, string kind, string civilization, string routeId, string sourceId, object value, string sourcePath, string jsonPath)
        {
            if (ctx == null) return null;
            Ensure(ctx);
            string snapshot = SafeSerialize(ctx, value);
            string fp = Hash(snapshot);
            string stable = (civilization ?? "OBJECT") + "|" + (sourceId ?? title ?? "UNKNOWN") + "|" + fp;
            string id = "yomi://spine/" + Hash(stable.ToLowerInvariant()).Substring(0, 28);
            WorldPassportSpineObject item;
            lock (Gate)
            {
                item = Store.Objects.FirstOrDefault(x => String.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
                if (item == null)
                {
                    item = new WorldPassportSpineObject
                    {
                        Id = id,
                        Kind = String.IsNullOrWhiteSpace(kind) ? "OBJECT" : kind,
                        Title = String.IsNullOrWhiteSpace(title) ? (sourceId ?? "OBJECT") : title,
                        Civilization = String.IsNullOrWhiteSpace(civilization) ? "OBJECT" : civilization,
                        RouteId = routeId ?? "",
                        SourceId = sourceId ?? "",
                        SourcePath = sourcePath ?? "",
                        JsonPath = jsonPath ?? "$",
                        Fingerprint = fp,
                        SnapshotJson = snapshot,
                        CreatedUtc = Now(),
                        UpdatedUtc = Now(),
                        OpenCount = 0
                    };
                    Store.Objects.Insert(0, item);
                }
                else
                {
                    item.Title = title ?? item.Title;
                    item.Kind = kind ?? item.Kind;
                    item.RouteId = routeId ?? item.RouteId;
                    item.SourcePath = sourcePath ?? item.SourcePath;
                    item.JsonPath = jsonPath ?? item.JsonPath;
                    item.SnapshotJson = snapshot;
                    item.UpdatedUtc = Now();
                }
            }
            Save(ctx);
            return item;
        }

        internal static void CaptureAndOpen(Window owner, DeepSystemsContext ctx, string title, string kind, string civilization, string routeId, string sourceId, object value, string sourcePath, string jsonPath)
        {
            WorldPassportSpineObject item = Capture(ctx, title, kind, civilization, routeId, sourceId, value, sourcePath, jsonPath);
            if (item != null) OpenRecord(owner, ctx, item);
        }

        internal static void CaptureWorldNode(Window owner, DeepSystemsContext ctx, WorldNode node)
        {
            if (node == null) return;
            object value = node.Value ?? (object)node.Properties;
            CaptureAndOpen(owner, ctx, node.Label, node.Kind, "WORLD", "world", node.Id, value, node.SourcePath, node.JsonPath);
        }

        internal static void OpenHub(Window owner, DeepSystemsContext ctx)
        {
            if (ctx == null) return;
            Ensure(ctx);
            Window existing = HubWindow == null ? null : HubWindow.Target as Window;
            if (existing != null)
            {
                try { existing.Show(); if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; } catch { }
            }

            Window w = MakeWindow(owner, "YOMI · WORLD PASSPORT SPINE", 1480, 900);
            HubWindow = new WeakReference(w);
            DockPanel root = new DockPanel { Margin = new Thickness(16) };
            w.Content = root;

            StackPanel header = new StackPanel { Margin = new Thickness(0,0,0,12) };
            DockPanel.SetDock(header, Dock.Top); root.Children.Add(header);
            header.Children.Add(T("WORLD PASSPORT & EVIDENCE SPINE", 12, Accent, FontWeights.Bold));
            header.Children.Add(T("Persistent cross-civilization objects. World Kernel remains graph/identity authority; METAΩ remains mission-semantic authority. The spine preserves selected object snapshots and explicit user-declared relationships between them.", 13, Muted, FontWeights.Normal));
            WrapPanel top = new WrapPanel { Margin = new Thickness(0,10,0,0) };
            header.Children.Add(top);
            top.Children.Add(Btn("WORLD KERNEL", delegate { WorldKernel.Open(w, ctx); }));
            top.Children.Add(Btn("METAΩ", delegate { KnowledgeConstellationMetastructure.Open(w, ctx); }));
            top.Children.Add(Btn("EXPΩ", delegate { GrandUnifiedExpeditionCommand.Open(w, ctx); }));
            top.Children.Add(Btn("WORKSPACE", delegate { ExpeditionWorkspace.Open(w, ctx); }));
            top.Children.Add(Btn("COPY SPINE", delegate { try { Clipboard.SetText(ExportText(ctx)); } catch { } }));

            TextBox search = new TextBox { Height = 34, Margin = new Thickness(0,0,0,10), Background = Raised, Foreground = Text, BorderBrush = Border, Padding = new Thickness(10,6,10,6) };
            DockPanel.SetDock(search, Dock.Top); root.Children.Add(search);

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(420) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(390) });
            root.Children.Add(grid);

            Grid left = new Grid();
            left.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            left.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(left,0); grid.Children.Add(Card(left));
            left.Children.Add(T("CAPTURED PASSPORTS",11,Accent,FontWeights.Bold));
            ListBox objects = List();
            Grid.SetRow(objects,1); left.Children.Add(objects);

            StackPanel center = new StackPanel();
            Grid.SetColumn(center,2); grid.Children.Add(Card(center));
            TextBlock title = T("Select an object",22,Text,FontWeights.Bold);
            TextBlock id = T("",11,Blue,FontWeights.Normal);
            TextBlock meta = T("Capture a dataset, codebase snapshot, living document, expedition step, World Passport, or arbitrary deep object.",12,Muted,FontWeights.Normal);
            meta.Margin = new Thickness(0,8,0,12);
            TextBox snapshot = new TextBox { IsReadOnly=true, MinHeight=360, MaxHeight=520, AcceptsReturn=true, TextWrapping=TextWrapping.Wrap, VerticalScrollBarVisibility=ScrollBarVisibility.Auto, Background=Bg, Foreground=Text, BorderBrush=Border, Padding=new Thickness(10), FontFamily=new FontFamily("Consolas"), FontSize=11 };
            WrapPanel objectTools = new WrapPanel { Margin = new Thickness(0,10,0,0) };
            Button open = Btn("OPEN PASSPORT", delegate { WorldPassportSpineObject x=objects.SelectedItem as WorldPassportSpineObject; if(x!=null) OpenRecord(w,ctx,x); });
            Button world = Btn("WORLD GRAPH", delegate { WorldPassportSpineObject x=objects.SelectedItem as WorldPassportSpineObject; if(x!=null) OpenWorldPassport(w,ctx,x); });
            Button owning = Btn("OWNING SYSTEM", delegate { WorldPassportSpineObject x=objects.SelectedItem as WorldPassportSpineObject; if(x!=null) OpenOwningSystem(w,ctx,x); });
            Button relate = Btn("RELATE…", delegate { WorldPassportSpineObject x=objects.SelectedItem as WorldPassportSpineObject; if(x!=null) RelateInteractive(w,ctx,x); });
            Button remove = Btn("REMOVE", delegate { WorldPassportSpineObject x=objects.SelectedItem as WorldPassportSpineObject; if(x!=null) Remove(w,ctx,x); });
            objectTools.Children.Add(open); objectTools.Children.Add(world); objectTools.Children.Add(owning); objectTools.Children.Add(relate); objectTools.Children.Add(remove);
            center.Children.Add(title); center.Children.Add(id); center.Children.Add(meta); center.Children.Add(snapshot); center.Children.Add(objectTools);

            Grid right = new Grid();
            right.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            right.RowDefinitions.Add(new RowDefinition { Height=new GridLength(1,GridUnitType.Star) });
            Grid.SetColumn(right,4); grid.Children.Add(Card(right));
            right.Children.Add(T("EVIDENCE RELATIONSHIPS",11,Amber,FontWeights.Bold));
            ListBox relations = List(); Grid.SetRow(relations,1); right.Children.Add(relations);

            bool refreshing = false;
            Action refresh = delegate
            {
                if (refreshing) return;
                refreshing = true;
                try
                {
                Ensure(ctx);
                string q = (search.Text ?? "").Trim();
                IEnumerable<WorldPassportSpineObject> seq = Store.Objects;
                if (q.Length > 0) seq = seq.Where(x => ((x.Title??"")+" "+(x.Kind??"")+" "+(x.Civilization??"")+" "+(x.SourceId??"")+" "+(x.Fingerprint??"")).IndexOf(q,StringComparison.OrdinalIgnoreCase)>=0);
                WorldPassportSpineObject selected = objects.SelectedItem as WorldPassportSpineObject;
                objects.ItemsSource = seq.OrderByDescending(x=>x.UpdatedUtc??x.CreatedUtc??"").ToList();
                if (selected != null)
                {
                    WorldPassportSpineObject again = Store.Objects.FirstOrDefault(x=>String.Equals(x.Id,selected.Id,StringComparison.OrdinalIgnoreCase));
                    if (again != null) objects.SelectedItem = again;
                }
                WorldPassportSpineObject cur = objects.SelectedItem as WorldPassportSpineObject;
                if (cur == null)
                {
                    title.Text="Select an object"; id.Text=""; meta.Text=Store.Objects.Count.ToString(CultureInfo.InvariantCulture)+" passports · "+Store.Relations.Count.ToString(CultureInfo.InvariantCulture)+" explicit relations"; snapshot.Text=""; relations.ItemsSource=null;
                    return;
                }
                title.Text=cur.Title??cur.Id; id.Text=cur.Id;
                meta.Text=(cur.Civilization??"OBJECT")+" · "+(cur.Kind??"OBJECT")+" · source "+(cur.SourceId??"∅")+" · fp "+Short(cur.Fingerprint)+" · captured "+(cur.CreatedUtc??"")+" · opened "+cur.OpenCount.ToString(CultureInfo.InvariantCulture)+"×";
                snapshot.Text=PrettyJson(ctx,cur.SnapshotJson);
                relations.ItemsSource=Store.Relations.Where(r=>String.Equals(r.FromId,cur.Id,StringComparison.OrdinalIgnoreCase)||String.Equals(r.ToId,cur.Id,StringComparison.OrdinalIgnoreCase)).OrderByDescending(r=>r.CreatedUtc).ToList();
                }
                finally { refreshing = false; }
            };
            search.TextChanged += delegate { refresh(); };
            objects.SelectionChanged += delegate { refresh(); };
            objects.MouseDoubleClick += delegate { WorldPassportSpineObject x=objects.SelectedItem as WorldPassportSpineObject; if(x!=null) OpenRecord(w,ctx,x); };
            relations.MouseDoubleClick += delegate { WorldPassportSpineRelation r=relations.SelectedItem as WorldPassportSpineRelation; if(r==null)return; WorldPassportSpineObject cur=objects.SelectedItem as WorldPassportSpineObject; string other=cur!=null&&String.Equals(cur.Id,r.FromId,StringComparison.OrdinalIgnoreCase)?r.ToId:r.FromId; WorldPassportSpineObject x=Store.Objects.FirstOrDefault(o=>String.Equals(o.Id,other,StringComparison.OrdinalIgnoreCase)); if(x!=null) OpenRecord(w,ctx,x); };
            refresh();
            w.Closed += delegate { HubWindow = null; };
            w.Show();
        }

        internal static void OpenRecord(Window owner, DeepSystemsContext ctx, WorldPassportSpineObject item)
        {
            if (item == null || ctx == null) return;
            Ensure(ctx);
            lock (Gate)
            {
                WorldPassportSpineObject found = Store.Objects.FirstOrDefault(x=>String.Equals(x.Id,item.Id,StringComparison.OrdinalIgnoreCase));
                if (found != null) { found.OpenCount++; found.UpdatedUtc=Now(); item=found; }
            }
            Save(ctx);
            object value = Deserialize(ctx,item.SnapshotJson);
            Window w=MakeWindow(owner,"YOMI · PASSPORT · "+Trim(item.Title,72),1180,820);
            DockPanel root=new DockPanel{Margin=new Thickness(16)};
            StackPanel head=new StackPanel(); DockPanel.SetDock(head,Dock.Top); root.Children.Add(head);
            head.Children.Add(T((item.Civilization??"OBJECT")+" · "+(item.Kind??"OBJECT")+" · PERSISTENT PASSPORT",11,Accent,FontWeights.Bold));
            head.Children.Add(T(item.Title??item.Id,24,Text,FontWeights.Bold));
            head.Children.Add(T(item.Id,11,Blue,FontWeights.Normal));
            head.Children.Add(T("fingerprint "+item.Fingerprint+" · source "+(item.SourceId??"∅")+" · route "+(item.RouteId??"∅"),11,Muted,FontWeights.Normal));
            WrapPanel tools=new WrapPanel{Margin=new Thickness(0,10,0,8)};
            tools.Children.Add(Btn("WORLD PASSPORT",delegate{OpenWorldPassport(w,ctx,item);}));
            tools.Children.Add(Btn("OWNING SYSTEM",delegate{OpenOwningSystem(w,ctx,item);}));
            tools.Children.Add(Btn("RELATE…",delegate{RelateInteractive(w,ctx,item);}));
            tools.Children.Add(Btn("EXPΩ CAPTURE",delegate{OpenInExpedition(w,ctx,item);}));
            tools.Children.Add(Btn("COPY URN",delegate{try{Clipboard.SetText(item.Id);}catch{}}));
            tools.Children.Add(Btn("SPINE HUB",delegate{OpenHub(w,ctx);}));
            head.Children.Add(tools);
            TextBox box=new TextBox{Text=PrettyJson(ctx,item.SnapshotJson),IsReadOnly=true,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,Background=Bg,Foreground=Text,BorderBrush=Border,Padding=new Thickness(12),FontFamily=new FontFamily("Consolas"),FontSize=11};
            root.Children.Add(box); w.Content=root; w.Show();
        }

        private static void OpenWorldPassport(Window owner, DeepSystemsContext ctx, WorldPassportSpineObject item)
        {
            object value=Deserialize(ctx,item.SnapshotJson);
            WorldKernel.OpenFromExternal(owner,ctx,item.Title,value,String.IsNullOrWhiteSpace(item.SourcePath)?("spine://"+item.Id):item.SourcePath,String.IsNullOrWhiteSpace(item.JsonPath)?"$":item.JsonPath,"SPINE › "+(item.Civilization??"OBJECT")+" › "+item.Id);
        }

        private static void OpenInExpedition(Window owner, DeepSystemsContext ctx, WorldPassportSpineObject item)
        {
            object value=Deserialize(ctx,item.SnapshotJson);
            WorldNode n=WorldKernel.ExternalRegisterObject(ctx,item.Title,value,String.IsNullOrWhiteSpace(item.SourcePath)?("spine://"+item.Id):item.SourcePath,String.IsNullOrWhiteSpace(item.JsonPath)?"$":item.JsonPath,"SPINE › "+(item.Civilization??"OBJECT")+" › "+item.Id);
            if(n!=null)
            {
                WorldIndex index=WorldKernel.ExternalIndex(ctx,false);
                GrandUnifiedExpeditionCommand.OpenFromWorldNode(owner,ctx,index,n.Id);
            }
        }

        private static void OpenOwningSystem(Window owner, DeepSystemsContext ctx, WorldPassportSpineObject item)
        {
            string route=(item.RouteId??"").Trim().ToLowerInvariant();
            if(route.Length>0)
            {
                ExpeditionWorkspace.OpenRoute(owner,ctx,route,"world passport spine");
                return;
            }
            string c=(item.Civilization??"").ToUpperInvariant();
            if(c.Contains("DATA")) DataFoundry.Open(owner,ctx);
            else if(c.Contains("GENOME")||c.Contains("SOFTWARE")) SoftwareGenomeObservatory.Open(owner,ctx);
            else if(c.Contains("DOC")||c.Contains("LIVING")) LivingDocumentIntelligenceFactory.Open(owner,ctx);
            else if(c.Contains("EXP")) GrandUnifiedExpeditionCommand.Open(owner,ctx);
            else WorldKernel.Open(owner,ctx);
        }

        private static void RelateInteractive(Window owner, DeepSystemsContext ctx, WorldPassportSpineObject from)
        {
            Ensure(ctx);
            List<WorldPassportSpineObject> candidates=Store.Objects.Where(x=>!String.Equals(x.Id,from.Id,StringComparison.OrdinalIgnoreCase)).OrderByDescending(x=>x.UpdatedUtc??"").ToList();
            if(candidates.Count==0){MessageBox.Show(owner,"Capture at least one other passport first.","YOMI · Evidence Spine",MessageBoxButton.OK,MessageBoxImage.Information);return;}
            WorldPassportSpineObject to=ChooseObject(owner,candidates);
            if(to==null)return;
            string kind=Prompt(owner,"RELATION KIND","Relationship (examples: DERIVED_FROM, DEPENDS_ON, EVIDENCE_FOR, CONTRADICTS, CAUSED, IMPLEMENTS, DOCUMENTS):","RELATED_TO");
            if(String.IsNullOrWhiteSpace(kind))return;
            string evidence=Prompt(owner,"RELATION EVIDENCE","Why is this relation justified? Keep this as evidence/annotation, not hidden inference:","");
            AddRelation(ctx,from,to,kind,evidence??"");
        }

        internal static WorldPassportSpineRelation AddRelation(DeepSystemsContext ctx, WorldPassportSpineObject from, WorldPassportSpineObject to, string kind, string evidence)
        {
            if(ctx==null||from==null||to==null||String.Equals(from.Id,to.Id,StringComparison.OrdinalIgnoreCase))return null;
            Ensure(ctx);
            string k=String.IsNullOrWhiteSpace(kind)?"RELATED_TO":kind.Trim().ToUpperInvariant().Replace(' ','_');
            string id="rel-"+Hash((from.Id+"|"+to.Id+"|"+k+"|"+(evidence??"")).ToLowerInvariant()).Substring(0,24);
            WorldPassportSpineRelation relation;
            lock(Gate)
            {
                relation=Store.Relations.FirstOrDefault(x=>String.Equals(x.Id,id,StringComparison.OrdinalIgnoreCase));
                if(relation==null)
                {
                    relation=new WorldPassportSpineRelation{Id=id,FromId=from.Id,ToId=to.Id,Kind=k,Evidence=evidence??"",CreatedUtc=Now()};
                    Store.Relations.Insert(0,relation);
                }
            }
            Save(ctx);
            return relation;
        }

        private static void Remove(Window owner, DeepSystemsContext ctx, WorldPassportSpineObject item)
        {
            if(MessageBox.Show(owner,"Remove this persistent passport from the spine?\n\nThe source civilization data is untouched. Explicit spine relations touching this passport are removed with it.","YOMI · World Passport Spine",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
            lock(Gate)
            {
                Store.Objects.RemoveAll(x=>String.Equals(x.Id,item.Id,StringComparison.OrdinalIgnoreCase));
                Store.Relations.RemoveAll(x=>String.Equals(x.FromId,item.Id,StringComparison.OrdinalIgnoreCase)||String.Equals(x.ToId,item.Id,StringComparison.OrdinalIgnoreCase));
            }
            Save(ctx);
        }

        private static WorldPassportSpineObject ChooseObject(Window owner,List<WorldPassportSpineObject> items)
        {
            Window w=MakeWindow(owner,"YOMI · SELECT PASSPORT",980,680);
            DockPanel root=new DockPanel{Margin=new Thickness(14)};
            TextBox q=new TextBox{Height=34,Background=Raised,Foreground=Text,BorderBrush=Border,Padding=new Thickness(10,6,10,6),Margin=new Thickness(0,0,0,8)};
            DockPanel.SetDock(q,Dock.Top);root.Children.Add(q);
            ListBox list=List();root.Children.Add(list);WorldPassportSpineObject selected=null;
            Action refresh=delegate{string s=(q.Text??"").Trim();IEnumerable<WorldPassportSpineObject> seq=items;if(s.Length>0)seq=seq.Where(x=>((x.Title??"")+" "+(x.Civilization??"")+" "+(x.Kind??"")+" "+(x.SourceId??"")).IndexOf(s,StringComparison.OrdinalIgnoreCase)>=0);list.ItemsSource=seq.ToList();};
            q.TextChanged+=delegate{refresh();};list.MouseDoubleClick+=delegate{selected=list.SelectedItem as WorldPassportSpineObject;if(selected!=null){w.DialogResult=true;w.Close();}};refresh();w.Content=root;bool? result=w.ShowDialog();return result==true?selected:null;
        }

        private static string Prompt(Window owner,string title,string label,string initial)
        {
            Window w=MakeWindow(owner,"YOMI · "+title,760,320);DockPanel root=new DockPanel{Margin=new Thickness(18)};TextBlock l=T(label,12,Muted,FontWeights.Normal);DockPanel.SetDock(l,Dock.Top);root.Children.Add(l);
            TextBox box=new TextBox{Text=initial??"",MinHeight=86,Margin=new Thickness(0,10,0,10),AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,Background=Surface,Foreground=Text,BorderBrush=Border,Padding=new Thickness(8)};DockPanel.SetDock(box,Dock.Top);root.Children.Add(box);
            StackPanel p=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};Button cancel=Btn("CANCEL",delegate{w.DialogResult=false;w.Close();});Button ok=Btn("COMMIT",delegate{w.DialogResult=true;w.Close();});p.Children.Add(cancel);p.Children.Add(ok);root.Children.Add(p);w.Content=root;bool? r=w.ShowDialog();return r==true?box.Text:null;
        }

        private static string PrettyJson(DeepSystemsContext ctx,string json)
        {
            try
            {
                object value=ctx.Json.DeserializeObject(json??"null");
                JavaScriptSerializer j=new JavaScriptSerializer{MaxJsonLength=Int32.MaxValue,RecursionLimit=256};
                return j.Serialize(value);
            }
            catch{return json??"";}
        }

        private static string ExportText(DeepSystemsContext ctx)
        {
            Ensure(ctx);
            StringBuilder s=new StringBuilder();
            s.AppendLine("YOMI WORLD PASSPORT & EVIDENCE SPINE");
            s.AppendLine("objects="+Store.Objects.Count.ToString(CultureInfo.InvariantCulture)+" relations="+Store.Relations.Count.ToString(CultureInfo.InvariantCulture));
            s.AppendLine();
            foreach(WorldPassportSpineObject x in Store.Objects){s.AppendLine(x.Id+" | "+(x.Civilization??"")+" | "+(x.Kind??"")+" | "+(x.Title??"")+" | "+(x.Fingerprint??""));}
            s.AppendLine();s.AppendLine("RELATIONS");
            foreach(WorldPassportSpineRelation r in Store.Relations)s.AppendLine(r.FromId+" --"+r.Kind+"--> "+r.ToId+" | "+(r.Evidence??""));
            return s.ToString();
        }

        private static Window MakeWindow(Window owner,string title,double width,double height)
        {
            Window w=new Window{Title=title,Width=width,Height=height,MinWidth=Math.Min(width,900),MinHeight=Math.Min(height,620),Background=Bg,Foreground=Text,WindowStartupLocation=WindowStartupLocation.CenterOwner};
            if(owner!=null)w.Owner=owner;return w;
        }
        private static Border Card(UIElement child){return new Border{Background=Surface,BorderBrush=Border,BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(9),Padding=new Thickness(12),Child=child};}
        private static Button Btn(string text,Action action){Button b=new Button{Content=text,Margin=new Thickness(4),Padding=new Thickness(11,7,11,7),Background=Raised,Foreground=Text,BorderBrush=Border};b.Click+=delegate{if(action!=null)action();};return b;}
        private static ListBox List(){return new ListBox{Background=Surface,Foreground=Text,BorderBrush=Border,BorderThickness=new Thickness(1),FontFamily=new FontFamily("Consolas"),FontSize=11};}
        private static TextBlock T(string text,double size,Brush color,FontWeight weight){return new TextBlock{Text=text??"",FontSize=size,Foreground=color,FontWeight=weight,TextWrapping=TextWrapping.Wrap};}
        private static string Short(string s){if(String.IsNullOrWhiteSpace(s))return"∅";return s.Length<=16?s:s.Substring(0,16);}
        private static string Trim(string s,int n){if(String.IsNullOrWhiteSpace(s))return"";return s.Length<=n?s:s.Substring(0,n-1)+"…";}
    }
}
