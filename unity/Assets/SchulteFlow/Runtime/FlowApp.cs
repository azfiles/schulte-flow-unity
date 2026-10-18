using UnityEngine;
using UnityEngine.EventSystems;
using SchulteFlow.Core;

namespace SchulteFlow
{
    public sealed class FlowApp : MonoBehaviour
    {
        private enum Overlay { None, Countdown, Pause, Settings, Confirm, Result }
        private Round game;
        private SaveStore store;
        private FlowUi ui;
        private BoardView board;
        private Feedback feedback;
        private Overlay overlay;
        private bool counting, resultHandled, recordImproved, appPaused, focused=true;
        private float countStart, nextHud;
        private int lastCount, pressed;
        private Vector2 pressedAt;
        private static long Now { get { return (long)(Time.realtimeSinceStartupAsDouble*1000); } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if(FindObjectOfType<FlowApp>()==null)new GameObject("Schulte Flow").AddComponent<FlowApp>();
        }
        private void Awake()
        {
            Application.targetFrameRate=60;QualitySettings.vSyncCount=0;QualitySettings.antiAliasing=2;
            Screen.orientation=ScreenOrientation.Portrait;
            var backdrop=new GameObject("Background camera",typeof(Camera));backdrop.transform.SetParent(transform,false);
            var clear=backdrop.GetComponent<Camera>();clear.depth=-10;clear.cullingMask=0;clear.clearFlags=CameraClearFlags.SolidColor;clear.backgroundColor=FlowUi.Background;
            store=new SaveStore();
            var uiObject=new GameObject("Interface",typeof(RectTransform));uiObject.transform.SetParent(transform,false);
            ui=uiObject.AddComponent<FlowUi>();ui.Initialize();
            if(EventSystem.current==null)
            {
                var events=new GameObject("Touch and UI input",typeof(EventSystem),typeof(StandaloneInputModule));events.transform.SetParent(transform,false);
            }
            var boardObject=new GameObject("Board");boardObject.transform.SetParent(transform,false);
            board=boardObject.AddComponent<BoardView>();board.Initialize(ui.Font);board.ReducedMotion=store.Data.reducedMotion;
            board.SetViewport(ui.BoardPixels);
            feedback=gameObject.AddComponent<Feedback>();feedback.Initialize(store.Data);
            ui.Primary=Primary;ui.Restart=()=>ConfirmNew(game.Size,game.Mode,true);ui.Settings=Settings;
            ui.SelectSize=n=>Choose(n,game.Mode);ui.SelectMode=m=>Choose(game.Size,m);
            game=store.Restore() ?? NewRound(store.Data.size,(GameMode)store.Data.mode);
            board.Build(game);Refresh();store.Flush();
            if(game.State==RoundState.Paused)ShowPause();else ui.SetStatus("让目光落在棋盘中央");
        }
        private static Round NewRound(int size,GameMode mode) { return new Round(size,mode,Random.Range(0,int.MaxValue)); }
        private void Update()
        {
            if(ui.Relayout())board.SetViewport(ui.BoardPixels);
            if(!focused || appPaused)return;
            long now=Now;
            if(game.Tick(now))Finish();
            if(counting)
            {
                int n=3-Mathf.FloorToInt((Time.unscaledTime-countStart)/.7f);
                if(n<=0)
                {
                    counting=false;ui.CloseModal();overlay=Overlay.None;game.Start(now);
                    ui.SetStatus("找到 1，让专注发生");store.SaveRound(game,now);Refresh();
                }
                else if(n!=lastCount){lastCount=n;ui.Countdown(n);feedback.Count();}
            }
            if(Time.unscaledTime>=nextHud){nextHud=Time.unscaledTime+.05f;ui.Refresh(game,store,now,counting);}
            if(Input.GetKeyDown(KeyCode.Escape))Back();
            if(game.State!=RoundState.Running || ui.ModalOpen || Input.touchCount>1){pressed=0;return;}
            Vector2 pointer=Input.mousePosition;
            if(Input.GetMouseButtonDown(0)) { pressed=board.Pick(pointer);pressedAt=pointer; }
            if(Input.GetMouseButton(0) && Vector2.Distance(pointer,pressedAt)>24)pressed=0;
            if(Input.GetMouseButtonUp(0))
            {
                int value=pressed;pressed=0;
                if(value>0 && value==board.Pick(pointer))Tap(value,now);
            }
        }
        private void Tap(int value,long now)
        {
            Hit hit=game.Tap(value,now);
            if(hit==Hit.Ignored)return;
            if(hit==Hit.Expired){Finish();return;}
            bool wrong=hit==Hit.Wrong;feedback.Tap(wrong,game.Combo);board.Feedback(value,wrong,game.Combo);board.Refresh(game);
            ui.SetStatus(wrong?"先找 "+game.Next+"  ·  误触 "+game.Mistakes:game.Combo>=3?"连击 ×"+game.Combo+"  节奏正好":"已点亮 "+value,wrong);
            if(hit==Hit.Complete)Finish();else store.SaveRound(game,now);
            Refresh();
        }
        private void Primary()
        {
            if(counting)return;
            if(game.State==RoundState.Running){if(Pause())ShowPause();}
            else if(game.State==RoundState.Paused)Resume();
            else StartRound();
        }
        private void Fresh(int size,GameMode mode)
        {
            CancelInvoke(nameof(ShowResult));
            counting=false;pressed=0;resultHandled=false;overlay=Overlay.None;ui.CloseModal();
            game=NewRound(size,mode);board.Build(game);store.SaveRound(game,Now);ui.SetStatus("让目光落在棋盘中央");Refresh();
        }
        private void StartRound()
        {
            Fresh(game.Size,game.Mode);counting=true;countStart=Time.unscaledTime;lastCount=3;
            overlay=Overlay.Countdown;ui.Countdown(3);feedback.Count();Refresh();
        }
        private void Choose(int size,GameMode mode)
        {
            if(counting || size==game.Size && mode==game.Mode)return;
            if(game.State==RoundState.Running || game.State==RoundState.Paused)ConfirmNew(size,mode,false);
            else Fresh(size,mode);
        }
        private bool Pause()
        {
            game.Pause(Now);pressed=0;
            if(game.State==RoundState.TimedOut){Finish();return false;}
            store.SaveRound(game,Now);Refresh();return true;
        }
        private void Resume()
        {
            ui.CloseModal();overlay=Overlay.None;game.Resume(Now);store.SaveRound(game,Now);
            ui.SetStatus("继续寻找 "+game.Next);Refresh();
        }
        private void ShowPause()
        {
            overlay=Overlay.Pause;
            ui.Dialog("休息片刻","稍作停留","计时已暂停，进度已经保存\n准备好后，继续寻找 "+game.Next,
                "继续专注",Resume,"重开一局",()=>ConfirmNew(game.Size,game.Mode,true));
        }
        private void ConfirmNew(int size,GameMode mode,bool start)
        {
            if(counting)return;
            if(game.State!=RoundState.Running && game.State!=RoundState.Paused){Fresh(size,mode);if(start)StartRound();return;}
            if(!Pause())return;overlay=Overlay.Confirm;
            ui.Dialog("换个节奏","开始新的一局？","当前这一局会结束\n已保存的最佳纪录会保留", "开始新一局",
                ()=>{Fresh(size,mode);if(start)StartRound();},"保留这一局",ShowPause);
        }
        private void Settings()
        {
            if(counting)return;
            if(game.State==RoundState.Running && !Pause())return;
            CancelInvoke(nameof(ShowResult));
            overlay=Overlay.Settings;
            ui.SettingsDialog(store.Data,()=>{board.ReducedMotion=store.Data.reducedMotion;store.Flush();},()=>
            {
                overlay=Overlay.None;if(game.State==RoundState.Paused)ShowPause();
                else if(game.State==RoundState.Won || game.State==RoundState.TimedOut)ShowResult();
            });
        }
        private void Finish()
        {
            if(resultHandled)return;resultHandled=true;pressed=0;
            bool won=game.State==RoundState.Won;recordImproved=store.Finish(game,Now);
            if(won){feedback.Complete();board.Celebrate();}
            ui.SetStatus(won?"全部点亮，做得漂亮！":"时间到，下次再快一点");Refresh();
            // Let the board celebration play before showing results, without changing the scored time.
            Invoke(nameof(ShowResult),won?.85f:.1f);
        }
        private void ShowResult()
        {
            if(game.State!=RoundState.Won && game.State!=RoundState.TimedOut)return;
            if(appPaused || !focused)return;
            overlay=Overlay.Result;bool won=game.State==RoundState.Won;
            string body=(won?FlowUi.TimeText(game.Elapsed(Now)):"点亮 "+game.Completed+" / "+game.Count+" 格")+
                "\n准确率 "+(game.Accuracy*100).ToString("0")+"%    误触 "+game.Mistakes+" 次\n最高连击 ×"+game.MaxCombo;
            ui.Dialog(won?(recordImproved?"新的个人最佳":"每一格，都被你点亮"):"再试一次，找到自己的节奏",
                won?"专注完成":"挑战结束",body,"再来一局",StartRound,"回到准备",()=>Fresh(game.Size,game.Mode));
        }
        private void Back()
        {
            if(counting){Fresh(game.Size,game.Mode);return;}
            if(overlay==Overlay.Settings || overlay==Overlay.Confirm){ui.CloseModal();overlay=Overlay.None;if(game.State==RoundState.Paused)ShowPause();return;}
            if(overlay==Overlay.Result){Fresh(game.Size,game.Mode);return;}
            if(game.State==RoundState.Running){if(Pause())ShowPause();return;}
            store.SaveRound(game,Now);Application.Quit();
        }
        private void Refresh()
        {
            ui.Refresh(game,store,Now,counting);
            Screen.sleepTimeout=focused && !appPaused && (counting || game.State==RoundState.Running)?SleepTimeout.NeverSleep:SleepTimeout.SystemSetting;
        }
        private void Suspend()
        {
            if(store==null || game==null)return;
            pressed=0;
            if(counting){counting=false;ui.CloseModal();overlay=Overlay.None;ui.SetStatus("准备好，再开始");}
            if(game.State==RoundState.Running)game.Pause(Now);
            if(game.State==RoundState.TimedOut && !resultHandled)Finish();
            store.SaveRound(game,Now);Refresh();
            if(game.State==RoundState.Paused)ShowPause();
        }
        private void OnApplicationPause(bool paused)
        {
            appPaused=paused;AudioListener.pause=paused || !focused;
            if(paused)Suspend();else Reenter();
        }
        private void OnApplicationFocus(bool hasFocus)
        {
            focused=hasFocus;AudioListener.pause=appPaused || !hasFocus;
            if(!hasFocus)Suspend();else Reenter();
        }
        private void Reenter()
        {
            if(game==null || appPaused || !focused)return;
            Refresh();if(resultHandled && (game.State==RoundState.Won || game.State==RoundState.TimedOut))ShowResult();
        }
        private void OnApplicationQuit(){if(store!=null && game!=null){game.Pause(Now);store.SaveRound(game,Now);}}
    }
}
