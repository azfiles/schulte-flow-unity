using System.Collections.Generic;
using UnityEngine;
using SchulteFlow.Core;

namespace SchulteFlow
{
    public sealed class BoardView : MonoBehaviour
    {
        private sealed class Tile
        {
            public Transform root;
            public Renderer face;
            public TextMesh text;
            public Vector3 rest;
            public int number;
            public float hitAt = -100;
            public bool wrong, done;
        }
        private sealed class Spark
        {
            public Transform root;
            public Renderer renderer;
            public Vector3 start, velocity;
            public float born, life;
            public bool ring, active;
        }
        public Camera ViewCamera { get; private set; }
        public bool ReducedMotion;
        private readonly List<Tile> tiles = new List<Tile>();
        private readonly List<Spark> effects = new List<Spark>();
        private readonly Dictionary<Collider, int> targets = new Dictionary<Collider, int>();
        private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
        private Transform stage, tileGroup;
        private Material tileMaterial, glowMaterial;
        private Mesh tileMesh, ringMesh, sparkMesh;
        private Font font;
        private int effectCursor;
        private static readonly Color Lime = new Color(.78f,.97f,.48f);

        public void Initialize(Font boardFont)
        {
            font = boardFont;
            var cameraObject = new GameObject("Board camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(transform, false);
            ViewCamera = cameraObject.GetComponent<Camera>();
            ViewCamera.orthographic = true; ViewCamera.clearFlags = CameraClearFlags.SolidColor;
            ViewCamera.backgroundColor = new Color(.035f,.075f,.085f); ViewCamera.nearClipPlane = .1f;
            ViewCamera.farClipPlane = 30; cameraObject.transform.localPosition = new Vector3(0,0,-12);
            ViewCamera.allowHDR = false; ViewCamera.allowMSAA = true;
            stage = new GameObject("Tilted board").transform; stage.SetParent(transform, false);
            stage.localRotation = Quaternion.Euler(12,0,0);
            tileMaterial = new Material(Resources.Load<Shader>("Shaders/Tile"));
            glowMaterial = new Material(Resources.Load<Shader>("Shaders/Glow"));
            tileMesh = RoundedTile(); ringMesh = Ring(); sparkMesh = Diamond();
            for (int i = 0; i < 100; i++)
            {
                var t = MeshObject("Pooled feedback", stage, i < 12 ? ringMesh : sparkMesh, glowMaterial);
                t.gameObject.SetActive(false);
                effects.Add(new Spark { root=t, renderer=t.GetComponent<Renderer>(), ring=i < 12 });
            }
        }

        public void Build(Round round)
        {
            if (tileGroup != null) { tileGroup.gameObject.SetActive(false); Destroy(tileGroup.gameObject); }
            foreach (var e in effects) { e.active=false; e.root.gameObject.SetActive(false); }
            tiles.Clear(); targets.Clear();
            tileGroup = new GameObject("Number tiles").transform; tileGroup.SetParent(stage, false);
            var basePlate=MeshObject("Board plinth",tileGroup,tileMesh,tileMaterial);
            basePlate.localPosition=new Vector3(0,-.04f,.29f);
            basePlate.localScale=new Vector3(round.Size+ .32f,round.Size+.32f,1.5f);
            Tint(basePlate.GetComponent<Renderer>(),new Color(.095f,.16f,.17f),0);
            int[] values=round.Board;
            for (int i=0;i<values.Length;i++)
            {
                float x=i%round.Size-(round.Size-1)*.5f, y=(round.Size-1)*.5f-i/round.Size;
                var t=MeshObject("Number "+values[i],tileGroup,tileMesh,tileMaterial);
                t.localPosition=new Vector3(x,y,0); t.localScale=new Vector3(.91f,.91f,1);
                var collider=t.gameObject.AddComponent<BoxCollider>(); collider.size=new Vector3(1,1,.24f);
                targets[collider]=values[i];
                var label=new GameObject("Label",typeof(TextMesh));label.transform.SetParent(t,false);
                label.transform.localPosition=new Vector3(0,.025f,-.145f);
                var text=label.GetComponent<TextMesh>();text.font=font;text.fontSize=100;text.characterSize=.064f;
                text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.text=values[i].ToString();
                label.GetComponent<MeshRenderer>().sharedMaterial=font.material;
                tiles.Add(new Tile { root=t, face=t.GetComponent<Renderer>(), text=text, number=values[i], rest=t.localPosition, done=values[i]<round.Next });
            }
            ViewCamera.orthographicSize=(round.Size+.85f)*.5f;
            Refresh(round); Physics.SyncTransforms();
        }
        public void SetViewport(Rect pixelRect)
        {
            if (pixelRect.width < 1 || pixelRect.height < 1) return;
            ViewCamera.rect=new Rect(pixelRect.x/Screen.width,pixelRect.y/Screen.height,pixelRect.width/Screen.width,pixelRect.height/Screen.height);
        }
        public int Pick(Vector2 screen)
        {
            if (!ViewCamera.pixelRect.Contains(screen)) return 0;
            RaycastHit hit; int value;
            return Physics.Raycast(ViewCamera.ScreenPointToRay(screen),out hit,25) && targets.TryGetValue(hit.collider,out value) ? value : 0;
        }
        public void Refresh(Round round) { foreach (var tile in tiles) tile.done=tile.number<round.Next; }
        public void Feedback(int value,bool wrong,int combo)
        {
            foreach (var tile in tiles) if (tile.number==value)
            {
                tile.hitAt=Time.unscaledTime; tile.wrong=wrong;
                if (!wrong && !ReducedMotion) Burst(tile.rest,Mathf.Min(14,5+combo),false);
                break;
            }
        }
        public void Celebrate() { if (!ReducedMotion) Burst(Vector3.zero,70,true); }
        private void Burst(Vector3 origin,int count,bool big)
        {
            Activate(effects[effectCursor++%12],origin,Vector3.zero,big?1.3f:.65f);
            for(int i=0;i<count;i++)
            {
                var e=effects[12+(effectCursor++%88)];
                float angle=Random.value*Mathf.PI*2, speed=Random.Range(.7f,big?3.3f:1.8f);
                Activate(e,origin,new Vector3(Mathf.Cos(angle)*speed,Mathf.Sin(angle)*speed,-.08f),big?1.4f:.65f);
            }
        }
        private void Activate(Spark e,Vector3 start,Vector3 velocity,float life)
        {
            e.start=start+new Vector3(0,0,-.22f);e.velocity=velocity;e.born=Time.unscaledTime;e.life=life;e.active=true;
            e.root.gameObject.SetActive(true);e.root.localPosition=e.start;
        }
        private void Update()
        {
            float now=Time.unscaledTime;
            foreach(var tile in tiles)
            {
                float age=now-tile.hitAt;float pulse=Mathf.Clamp01(1-age/.45f);
                var color=tile.done?new Color(.14f,.28f,.25f):new Color(.17f,.25f,.27f);
                if(tile.wrong && age<.35f) color=Color.Lerp(color,new Color(.65f,.20f,.24f),pulse);
                Tint(tile.face,color,tile.wrong?0:pulse*.8f);
                tile.text.color=tile.done?new Color(.51f,.81f,.68f):new Color(.94f,.97f,.92f);
                tile.root.localPosition=tile.rest;
                float scale=1;
                if(!ReducedMotion && pulse>0)
                {
                    if(tile.wrong)tile.root.localPosition+=new Vector3(Mathf.Sin(age*65)*.055f*pulse,0,0);
                    else { scale=1-.15f*Mathf.Exp(-age*10)*Mathf.Cos(age*22); tile.root.localPosition+=new Vector3(0,0,-.08f*pulse); }
                }
                tile.root.localScale=new Vector3(.91f*scale,.91f*scale,1);
            }
            foreach(var e in effects)
            {
                if(!e.active)continue;
                float age=now-e.born,t=age/e.life;
                if(t>=1 || ReducedMotion){e.active=false;e.root.gameObject.SetActive(false);continue;}
                e.root.localPosition=e.start+e.velocity*age+(e.ring?Vector3.zero:new Vector3(0,-.6f*age*age,0));
                float scale=e.ring?.3f+t*(e.life>1?5:1.9f):.045f*(1-t);
                e.root.localScale=Vector3.one*scale;
                var color=Lime;color.a=(1-t)*(e.ring?.45f:.9f);Tint(e.renderer,color,0);
            }
        }
        private void Tint(Renderer renderer,Color color,float accent)
        {
            block.Clear();block.SetColor("_Color",color);block.SetFloat("_Accent",accent);renderer.SetPropertyBlock(block);
        }
        private static Transform MeshObject(string name,Transform parent,Mesh mesh,Material material)
        {
            var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);
            g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=material;return g.transform;
        }
        private static Mesh RoundedTile()
        {
            const int steps=6,n=4*(steps+1);var vertices=new List<Vector3>();var triangles=new List<int>();
            float[] radii={.105f,.13f,.13f};float[] halves={.475f,.5f,.5f};float[] depths={-.12f,-.065f,.14f};
            for(int ring=0;ring<3;ring++)for(int corner=0;corner<4;corner++)for(int j=0;j<=steps;j++)
            {
                float angle=(corner*90+j*90f/steps)*Mathf.Deg2Rad;
                float cx=(corner==0 || corner==3?1:-1)*(halves[ring]-radii[ring]);
                float cy=(corner<2?1:-1)*(halves[ring]-radii[ring]);
                vertices.Add(new Vector3(cx+Mathf.Cos(angle)*radii[ring],cy+Mathf.Sin(angle)*radii[ring],depths[ring]));
            }
            for(int i=1;i<n-1;i++){triangles.Add(0);triangles.Add(i+1);triangles.Add(i);}
            for(int ring=0;ring<2;ring++)for(int i=0;i<n;i++)
            {
                int a=ring*n+i,b=ring*n+(i+1)%n,c=a+n,d=b+n;
                triangles.Add(a);triangles.Add(b);triangles.Add(c);triangles.Add(b);triangles.Add(d);triangles.Add(c);
            }
            var mesh=new Mesh { name="Rounded bevel tile" };mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static Mesh Ring()
        {
            const int n=64;var v=new Vector3[n*2];var t=new int[n*6];
            for(int i=0;i<n;i++)
            {
                float a=i*Mathf.PI*2/n;v[2*i]=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);v[2*i+1]=v[2*i]*.975f;
                int j=(i+1)%n;int k=i*6;t[k]=i*2;t[k+1]=j*2;t[k+2]=i*2+1;t[k+3]=i*2+1;t[k+4]=j*2;t[k+5]=j*2+1;
            }
            var mesh=new Mesh { name="Feedback ring",vertices=v,triangles=t };mesh.RecalculateBounds();return mesh;
        }
        private static Mesh Diamond()
        {
            return new Mesh { name="Spark",vertices=new[]{Vector3.up,Vector3.right,Vector3.down,Vector3.left},triangles=new[]{0,1,2,0,2,3} };
        }
        private void OnDestroy()
        {
            if(tileMaterial!=null)Destroy(tileMaterial);if(glowMaterial!=null)Destroy(glowMaterial);
            if(tileMesh!=null)Destroy(tileMesh);if(ringMesh!=null)Destroy(ringMesh);if(sparkMesh!=null)Destroy(sparkMesh);
        }
    }
}
