using System;
using UnityEngine;
using UnityEngine.UI;
using SchulteFlow.Core;

namespace SchulteFlow
{
    public sealed class FlowUi : MonoBehaviour
    {
        public static readonly Color Background = new Color(.035f,.075f,.085f);
        public static readonly Color Lime = new Color(.78f,.97f,.48f);
        public static readonly Color White = new Color(.94f,.97f,.92f);
        public static readonly Color Muted = new Color(.53f,.66f,.66f);
        private static readonly Color Panel = new Color(.075f,.14f,.155f);
        public Font Font { get; private set; }
        public Action Primary, Restart, Settings;
        public Action<int> SelectSize;
        public Action<GameMode> SelectMode;
        public bool ModalOpen { get { return modal != null; } }
        public Rect BoardPixels { get; private set; }
        private RectTransform safe, boardSlot;
        private Text next, timer, record, status, progressText, primaryText, footerNote;
        private Image progress;
        private Button primaryButton;
        private readonly Button[] sizes = new Button[3], modes = new Button[2];
        private Sprite rounded;
        private Texture2D roundedTexture;
        private GameObject modal;
        private int width, height;
        private Rect lastSafe;
        private Action layout;

        public void Initialize()
        {
            Font=Resources.Load<Font>("Fonts/FlowSans");
            if(Font==null)throw new InvalidOperationException("Missing bundled font: Resources/Fonts/FlowSans.otf");
            rounded=MakeRounded();
            var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10;
            var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(900,1600);scaler.matchWidthOrHeight=.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            safe=Rect("Safe area",transform);
            var title=Label("循光",safe,48,White,TextAnchor.MiddleLeft);
            var subtitle=Label("SCHULTE  /  FLOW",safe,17,Lime,TextAnchor.MiddleLeft);
            var setting=Button("设置",safe,false,()=>Settings?.Invoke());
            var rule=Label("专注于此刻",safe,19,Muted,TextAnchor.MiddleRight);
            modes[0]=Button("经典练习",safe,true,()=>SelectMode?.Invoke(GameMode.Classic));
            modes[1]=Button("限时挑战",safe,false,()=>SelectMode?.Invoke(GameMode.Sprint));
            for(int i=0;i<3;i++){int n=i+3;sizes[i]=Button(n+" × "+n,safe,false,()=>SelectSize?.Invoke(n));}
            var stats=PanelRect("Round metrics",safe,Panel);
            var nextLabel=Label("下一个目标",stats,18,Muted,TextAnchor.MiddleLeft);
            next=Label("01",stats,58,Lime,TextAnchor.MiddleLeft);
            timer=Label("00:00.00",stats,37,White,TextAnchor.MiddleRight);
            record=Label("最佳  —",stats,18,Muted,TextAnchor.MiddleRight);
            status=Label("让目光落在棋盘中央",safe,22,Muted,TextAnchor.MiddleLeft);
            progressText=Label("0 / 16",safe,20,Muted,TextAnchor.MiddleRight);
            var track=PanelRect("Progress track",safe,Panel);
            var fill=PanelRect("Progress",track,Lime);progress=fill.GetComponent<Image>();
            Stretch(fill);progress.type=Image.Type.Filled;progress.fillMethod=Image.FillMethod.Horizontal;progress.fillAmount=0;
            boardSlot=Rect("3D board viewport",safe);
            var tip=Label("从 1 开始，依次点亮每一格",safe,20,Muted,TextAnchor.MiddleCenter);
            primaryButton=Button("开始专注",safe,true,()=>Primary?.Invoke());primaryText=primaryButton.GetComponentInChildren<Text>();
            var restart=Button("重开",safe,false,()=>Restart?.Invoke());
            footerNote=Label("离线练习  ·  跟随自己的节奏",safe,18,Muted,TextAnchor.MiddleCenter);
            layout=()=>
            {
                float w=Mathf.Min(safe.rect.width,900), h=safe.rect.height, inner=w-64;
                Place(title.rectTransform,-inner/2+130,12,260,65);Place(subtitle.rectTransform,-inner/2+180,75,360,28);
                Place(setting.GetComponent<RectTransform>(),inner/2-49,24,98,56);Place(rule.rectTransform,inner/2-120,87,240,28);
                for(int i=0;i<2;i++)Place(modes[i].GetComponent<RectTransform>(),(i==0?-1:1)*(inner+12)/4,137,(inner-12)/2,62);
                for(int i=0;i<3;i++)Place(sizes[i].GetComponent<RectTransform>(),(i-1)*(inner+12)/3,215,(inner-24)/3,52);
                Place(stats,0,290,inner,132);
                Place(nextLabel.rectTransform,-inner/2+132,14,210,28);Place(next.rectTransform,-inner/2+132,42,210,80);
                Place(timer.rectTransform,inner/2-207,17,360,58);Place(record.rectTransform,inner/2-247,82,440,30);
                Place(status.rectTransform,-65,445,inner-130,38);Place(progressText.rectTransform,inner/2-58,445,116,38);
                Place(track,0,498,inner,5);
                float boardSize=Mathf.Max(200,Mathf.Min(inner,h-730));
                Place(boardSlot,0,524,boardSize,boardSize);
                Place(tip.rectTransform,0,534+boardSize,inner,36);
                Place(primaryButton.GetComponent<RectTransform>(),-75,h-147,inner-150,78);
                Place(restart.GetComponent<RectTransform>(),inner/2-65,h-147,130,78);
                Place(footerNote.rectTransform,0,h-53,inner,30);
            };
            Relayout();
        }
        public bool Relayout()
        {
            if(width==Screen.width && height==Screen.height && lastSafe==Screen.safeArea)return false;
            width=Screen.width;height=Screen.height;lastSafe=Screen.safeArea;
            safe.anchorMin=lastSafe.position/new Vector2(width,height);safe.anchorMax=(lastSafe.position+lastSafe.size)/new Vector2(width,height);
            safe.offsetMin=safe.offsetMax=Vector2.zero;
            Canvas.ForceUpdateCanvases();layout();Canvas.ForceUpdateCanvases();
            var corners=new Vector3[4];boardSlot.GetWorldCorners(corners);
            BoardPixels=new Rect(corners[0].x,corners[0].y,corners[2].x-corners[0].x,corners[2].y-corners[0].y);
            return true;
        }
        public void Refresh(Round game,SaveStore store,long now,bool counting)
        {
            next.text=game.State==RoundState.Won?"完成":game.Next.ToString("00");
            timer.text=TimeText(game.Mode==GameMode.Sprint?game.Remaining(now):game.Elapsed(now));
            timer.color=game.Mode==GameMode.Sprint && game.Remaining(now)<=5000?new Color(1,.61f,.55f):White;
            long best=store.Best(game);record.text="最佳  "+(best==0?"等待你点亮":TimeText(best));
            progressText.text=game.Completed+" / "+game.Count;progress.fillAmount=(float)game.Completed/game.Count;
            for(int i=0;i<3;i++)Style(sizes[i],i+3==game.Size,false);
            for(int i=0;i<2;i++)Style(modes[i],i==(int)game.Mode,true);
            primaryText.text=counting?"准备进入专注":game.State==RoundState.Running?"暂停一下":game.State==RoundState.Paused?"继续专注":"开始专注";
            primaryButton.interactable=!counting;
            footerNote.text=game.Mode==GameMode.Classic?"离线练习  ·  跟随自己的节奏":"限时 "+game.LimitMs/1000+" 秒  ·  点错不额外扣时";
        }
        public void SetStatus(string text,bool error=false) { status.text=text;status.color=error?new Color(1,.61f,.55f):Lime; }
        public static string TimeText(long ms) { return string.Format("{0:00}:{1:00}.{2:00}",ms/60000,ms/1000%60,ms/10%100); }

        public void Dialog(string kicker,string title,string body,string action,Action onAction,string secondary=null,Action onSecondary=null)
        {
            var panel=NewModal(590);
            var k=Label(kicker,panel,19,Lime,TextAnchor.MiddleCenter);Place(k.rectTransform,0,32,620,36);
            var t=Label(title,panel,42,White,TextAnchor.MiddleCenter);Place(t.rectTransform,0,83,620,65);
            var b=Label(body,panel,24,Muted,TextAnchor.MiddleCenter);b.lineSpacing=1.5f;Place(b.rectTransform,0,163,panel.rect.width-56,210);
            var primary=Button(action,panel,true,()=>{CloseModal();onAction?.Invoke();});Place(primary.GetComponent<RectTransform>(),0,397,panel.rect.width-64,72);
            if(secondary!=null){var other=Button(secondary,panel,false,()=>{CloseModal();onSecondary?.Invoke();});Place(other.GetComponent<RectTransform>(),0,485,panel.rect.width-64,62);}
        }
        public void SettingsDialog(Profile profile,Action changed,Action closed)
        {
            var panel=NewModal(625);
            var title=Label("属于你的节奏",panel,37,White,TextAnchor.MiddleCenter);Place(title.rectTransform,0,30,panel.rect.width-40,65);
            Button sound=null,haptics=null,motion=null;
            sound=Button("",panel,false,()=>{profile.sound=!profile.sound;SettingsLabels(sound,haptics,motion,profile);changed();});
            haptics=Button("",panel,false,()=>{profile.haptics=!profile.haptics;SettingsLabels(sound,haptics,motion,profile);changed();});
            motion=Button("",panel,false,()=>{profile.reducedMotion=!profile.reducedMotion;SettingsLabels(sound,haptics,motion,profile);changed();});
            Place(sound.GetComponent<RectTransform>(),0,123,panel.rect.width-64,68);Place(haptics.GetComponent<RectTransform>(),0,207,panel.rect.width-64,68);
            Place(motion.GetComponent<RectTransform>(),0,291,panel.rect.width-64,68);SettingsLabels(sound,haptics,motion,profile);
            var note=Label("减少动态效果会关闭光波与粒子\n已完成 "+profile.rounds+" 次练习 · 成绩保存在本机",panel,20,Muted,TextAnchor.MiddleCenter);
            Place(note.rectTransform,0,383,panel.rect.width-48,88);
            var done=Button("回到棋盘",panel,true,()=>{CloseModal();closed();});Place(done.GetComponent<RectTransform>(),0,506,panel.rect.width-64,72);
        }
        private static void SettingsLabels(Button sound,Button haptics,Button motion,Profile p)
        {
            sound.GetComponentInChildren<Text>().text="轻柔音效     "+(p.sound?"开启":"关闭");
            haptics.GetComponentInChildren<Text>().text="点按触感     "+(p.haptics?"开启":"关闭");
            motion.GetComponentInChildren<Text>().text="减少动态效果     "+(p.reducedMotion?"开启":"关闭");
        }
        public void Countdown(int number)
        {
            var panel=NewModal(360);
            var value=Label(number.ToString(),panel,110,Lime,TextAnchor.MiddleCenter);Place(value.rectTransform,0,48,300,150);
            var hint=Label("从 1 开始，依次寻找",panel,24,Muted,TextAnchor.MiddleCenter);Place(hint.rectTransform,0,235,panel.rect.width-32,55);
        }
        public void CloseModal() { if(modal!=null){modal.SetActive(false);Destroy(modal);modal=null;} }
        private RectTransform NewModal(float heightValue)
        {
            CloseModal();var backdrop=PanelRect("Modal",transform,Background);Stretch(backdrop);backdrop.GetComponent<Image>().raycastTarget=true;modal=backdrop.gameObject;
            var panel=PanelRect("Dialog panel",backdrop,Panel);panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);
            panel.sizeDelta=new Vector2(Mathf.Min(safe.rect.width-64,720),heightValue);panel.anchoredPosition=Vector2.zero;
            return panel;
        }
        private void Style(Button button,bool selected,bool strong)
        {
            button.GetComponent<Image>().color=selected?(strong?Lime:new Color(.16f,.26f,.24f)):Panel;
            button.GetComponentInChildren<Text>().color=selected?(strong?Background:Lime):Muted;
        }
        private Button Button(string text,Transform parent,bool primary,Action action)
        {
            var rect=PanelRect(text,parent,primary?Lime:Panel);var image=rect.GetComponent<Image>();image.raycastTarget=true;
            var b=rect.gameObject.AddComponent<Button>();b.targetGraphic=image;
            var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.9f,.96f,.92f);colors.pressedColor=new Color(.72f,.82f,.72f);colors.fadeDuration=.08f;b.colors=colors;
            b.navigation=new Navigation { mode=Navigation.Mode.None };b.onClick.AddListener(()=>action());
            var label=Label(text,rect,25,primary?Background:White,TextAnchor.MiddleCenter);Stretch(label.rectTransform);
            label.rectTransform.offsetMin=new Vector2(10,3);label.rectTransform.offsetMax=new Vector2(-10,-3);return b;
        }
        private Text Label(string value,Transform parent,int size,Color color,TextAnchor alignment)
        {
            var rect=Rect(value,parent);var label=rect.gameObject.AddComponent<Text>();label.font=Font;label.fontSize=size;
            label.color=color;label.alignment=alignment;label.text=value;label.raycastTarget=false;
            label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;return label;
        }
        private RectTransform PanelRect(string name,Transform parent,Color color)
        {
            var rect=Rect(name,parent);var image=rect.gameObject.AddComponent<Image>();image.sprite=rounded;image.type=Image.Type.Sliced;
            image.color=color;image.raycastTarget=false;return rect;
        }
        private static RectTransform Rect(string name,Transform parent)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);return rect;
        }
        private static void Stretch(RectTransform r) { r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero; }
        private static void Place(RectTransform r,float x,float top,float w,float h)
        {
            r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,1);r.anchoredPosition=new Vector2(x,-top);r.sizeDelta=new Vector2(w,h);
        }
        private Sprite MakeRounded()
        {
            const int size=96;const float radius=23;
            roundedTexture=new Texture2D(size,size,TextureFormat.RGBA32,false);roundedTexture.name="Rounded UI mask";roundedTexture.filterMode=FilterMode.Bilinear;roundedTexture.wrapMode=TextureWrapMode.Clamp;
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=Mathf.Max(Mathf.Abs(x-(size-1)*.5f)-(size*.5f-radius),0),dy=Mathf.Max(Mathf.Abs(y-(size-1)*.5f)-(size*.5f-radius),0);
                pixels[y*size+x]=new Color(1,1,1,Mathf.Clamp01(radius-Mathf.Sqrt(dx*dx+dy*dy)));
            }
            roundedTexture.SetPixels(pixels);roundedTexture.Apply(false,true);
            return Sprite.Create(roundedTexture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(24,24,24,24));
        }
        private void OnDestroy(){if(rounded!=null)Destroy(rounded);if(roundedTexture!=null)Destroy(roundedTexture);}
    }
}
