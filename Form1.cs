using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Core.DevToolsProtocolExtension;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebView2.DevTools.Dom;

namespace OtomatisasiTugas
{
    public partial class Form1 : Form
    {
        public Browser Browser;
        public Page Page;
        public WebView2DevToolsContext Driver;
        public bool IsStart=false;
        public string TipeCourse= "quiz";
        public static List<Quiz> answerQuiz=new List<Quiz>();
        public class Quiz
        {
            public string step { get; set; }
            public int answer { get; set; }
        }
        public Form1()
        {
            InitializeComponent();
            initWebview();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            stateButton(true);
            AutoProcess();
        }

        public void stateButton(bool start)
        {
            if (start)
            {
                addLog("Auto Process", "Running");
                button1.Enabled=false;
                button2.Enabled=true;
                IsStart = true;
            }
            else
            {
                addLog("Auto Process", "Stopped");
                button1.Enabled = true;
                button2.Enabled = false;
                IsStart = false;
            }
        }

        private void webView21_Click(object sender, EventArgs e)
        {

        }
        public async void AutoProcess()
        {
            int Step = 0;
            try
            {
                await webView21.CoreWebView2.ExecuteScriptAsync("clear();");
                //var inject = await Driver.QuerySelectorAsync($"div.bar.active a");
                if (webView21.CoreWebView2.Source.Contains("/courses/enrolled"))
                {
                    addLog("Process", "Getting uncompleted course");
                    var inject = await Driver.WaitForSelectorAsync("div.bar.active a");
                    if (inject != null)
                    {
                        addLog("Process", "Success get uncompleted course");
                        addLog("Process", "Getting tipe course");
                        var icon = await inject.QuerySelectorAsync("svg use");
                        if(icon != null)
                        {
                            var attrIcon = await icon.GetAttributeAsync("xlink:href");
                            if (attrIcon == "#icon-text")
                            {
                                TipeCourse = "materi";
                            }
                            else if(attrIcon == "#icon-video")
                            {
                                TipeCourse = "video";
                            }
                            else if (attrIcon == "#icon-quiz")
                            {
                                TipeCourse = "quiz";
                            }
                            addLog("Process", "Tipe course is "+TipeCourse);
                        }
                        else
                        {
                            TipeCourse = "undefined";
                            addLog("Process", "Tipe course is " + TipeCourse);
                        }
                        await inject.ClickAsync();
                    }
                    else
                    {
                        stateButton(false);
                        addLog("Process", "Uncompleted course not found");

                    }
                }else if (webView21.CoreWebView2.Source.Contains("/courses/")&&webView21.CoreWebView2.Source.Contains("/lectures/"))
                {
                    var idSoal = webView21.CoreWebView2.Source.Split('/').Last();
                    var elActive = await Driver.QuerySelectorAsync($"li[data-lecture-id='{idSoal}'] a div span svg use");
                    if(elActive != null)
                    {

                        var attrIcon = await elActive.GetAttributeAsync("xlink:href");
                        if (attrIcon == "#icon__Subject")
                        {
                            TipeCourse = "materi";
                        }
                        else if (attrIcon == "#icon__Video")
                        {
                            TipeCourse = "video";
                        }
                        else if (attrIcon == "#icon__Quiz")
                        {
                            TipeCourse = "quiz";
                        }
                    }
                    var elLocked = await Driver.QuerySelectorAsync("#last_lecture_button");
                    if (elLocked != null)
                    {
                        answerQuiz = new List<Quiz>();
                        addLog("Process", "Course closed back to last course");
                        await elLocked.ClickAsync();
                    }
                    else
                    {
                        if (TipeCourse == "video")
                        {
                            answerQuiz = new List<Quiz>();
                            addLog("Process", "Waiting video complete...");
                            var ele = await webView21.CoreWebView2.ExecuteScriptAsync(@"var iniInterval=setInterval(()=>{
                                if(!$('a.nav-btn.complete').hasClass('disabled')){
                                    window.chrome.webview.postMessage('video-success');
                                    $('a.nav-btn.complete').click();
                                    clearInterval(iniInterval);
                                }
                            },1000);");
                        }
                        if (TipeCourse == "quiz")
                        {
                        balikQuiz:
                            var ele = await Driver.WaitForSelectorAsync(".quiz-progress");
                            if(ele != null)
                            {
                                var textEle = await ele.GetInnerTextAsync();
                                if(textEle != null)
                                {
                                    var valueEle = textEle.Split('/');
                                    if (valueEle.Length == 2)
                                    {
                                        var step = valueEle[0];
                                        var maxStep = valueEle[1];
                                        var answer = answerQuiz.Where(z => z.step == step).ToList();
                                        var answerEl = await Driver.QuerySelectorAllAsync(".quiz-answer");
                                        if (answer.Count == 0)
                                        {
                                            var random = new Random().Next(answerEl.Length - 1);
                                            await answerEl[random].ClickAsync();
                                            var checkBtn = await Driver.WaitForSelectorAsync(".check-answer-button.is-visible");
                                            if (checkBtn != null)
                                            {
                                                await checkBtn.ClickAsync();
                                                var continueBtn = await Driver.WaitForSelectorAsync("button[date-test='continue-button'].is-visible");
                                                if (continueBtn != null)
                                                {
                                                    var resultEl = await Driver.QuerySelectorAllAsync(".quiz-answer-container");
                                                    if (resultEl != null)
                                                    {
                                                        var indexCorrect = 0;
                                                        var isReload = true;
                                                        var tasks = resultEl.ToList().Select(async rel => await rel.GetClassNameAsync());
                                                        var arrTasks = await Task.WhenAll(tasks);
                                                        arrTasks.ToList().ForEach(classRel =>
                                                        {
                                                            var indexBenar = classRel.Split(' ').ToList().FindIndex(nm => nm == "correct");
                                                            var indexSelected = classRel.Split(' ').ToList().FindIndex(nm => nm == "selected");
                                                            if (indexBenar != -1 && indexSelected != -1)
                                                            {
                                                                answerQuiz.Add(new Quiz
                                                                {
                                                                    step = step,
                                                                    answer = indexCorrect - 1
                                                                });
                                                                isReload = false;
                                                            }
                                                            else if (indexBenar != -1 && indexSelected == -1)
                                                            {
                                                                answerQuiz.Add(new Quiz
                                                                {
                                                                    step = step,
                                                                    answer = indexCorrect - 1
                                                                });
                                                                isReload = true;
                                                            }
                                                            indexCorrect++;
                                                        });
                                                        
                                                        //resultEl.ToList().ForEach(rel =>
                                                        //{
                                                        //});
                                                        addLog("Process", $"Quiz question {step} answer = {JsonConvert.SerializeObject(answerQuiz.Where(x => x.step == step).Select(sap=>sap.answer).ToList())}");
                                                        if (answerQuiz.Where(x => x.step == step).Count() > 1)
                                                        {
                                                            webView21.CoreWebView2.Reload();
                                                        }
                                                        else
                                                        {
                                                            if (isReload)
                                                            {
                                                                webView21.CoreWebView2.Reload();
                                                            }
                                                            else
                                                            {
                                                                await Task.Delay(500);
                                                                await continueBtn.ClickAsync();
                                                                await Task.Delay(500); ;
                                                                if (step.Replace(" ", "") == maxStep.Replace(" ", ""))
                                                                {
                                                                    var resultQuiz = await Driver.WaitForSelectorAsync(".quiz-finished-text");
                                                                    if (resultQuiz != null)
                                                                    {
                                                                        var textResultQuiz = await resultQuiz.GetInnerTextAsync();
                                                                        addLog("Process", $"Quiz Result = {textResultQuiz}");
                                                                    }
                                                                    var injectResult = await webView21.CoreWebView2.ExecuteScriptAsync(@"var iniInterval=setInterval(()=>{
                                                                        if(!$('a.nav-btn.complete').hasClass('disabled')){
                                                                            window.chrome.webview.postMessage('quiz-success');
                                                                            $('a.nav-btn.complete').click();
                                                                            clearInterval(iniInterval);
                                                                        }
                                                                    },1000);");
                                                                }
                                                                else
                                                                {
                                                                    goto balikQuiz;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            var injectClickAnswer = "";

                                            answer.ForEach(asw =>
                                            {
                                                injectClickAnswer += $"$('.quiz-answer')[{asw.answer}].click();";
                                                //await answerEl[asw.answer].ClickAsync();
                                            });
                                            await webView21.CoreWebView2.ExecuteScriptAsync(injectClickAnswer);
                                            await Task.Delay(500);
                                            var checkBtn = await Driver.WaitForSelectorAsync(".check-answer-button.is-visible");
                                            if (checkBtn != null)
                                            {
                                                await checkBtn.ClickAsync();
                                                var continueBtn = await Driver.WaitForSelectorAsync("button[date-test='continue-button'].is-visible");
                                                if (continueBtn != null)
                                                {
                                                    await Task.Delay(500);
                                                    await continueBtn.ClickAsync();
                                                    await Task.Delay(500);
                                                    if(step.Replace(" ","")== maxStep.Replace(" ", ""))
                                                    {
                                                        var resultQuiz = await Driver.WaitForSelectorAsync(".quiz-finished-text");
                                                        if(resultQuiz != null)
                                                        {
                                                            var textResultQuiz = await resultQuiz.GetInnerTextAsync();
                                                            addLog("Process", $"Quiz Result = {textResultQuiz}");
                                                        }
                                                        var injectResult = await webView21.CoreWebView2.ExecuteScriptAsync(@"var iniInterval=setInterval(()=>{
                                                                        if(!$('a.nav-btn.complete').hasClass('disabled')){
                                                                            window.chrome.webview.postMessage('quiz-success');
                                                                            $('a.nav-btn.complete').click();
                                                                            clearInterval(iniInterval);
                                                                        }
                                                                    },1000);");
                                                    }
                                                    else
                                                    {
                                                        goto balikQuiz;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (TipeCourse == "materi")
                        {
                            answerQuiz = new List<Quiz>();
                            var ele = await webView21.CoreWebView2.ExecuteScriptAsync(@"var iniInterval=setInterval(()=>{
                                if(!$('a.nav-btn.complete').hasClass('disabled')){
                                    window.chrome.webview.postMessage('materi-success');
                                    $('a.nav-btn.complete').click();
                                    clearInterval(iniInterval);
                                }
                            },1000);");
                        }
                    }
                }
                var yes = "oke";
            }

            catch (Exception ex)
            {
                var err = "anjy";
                //AddLog($"AutoLogin step {Step} : {ex.Message}");
            }

        }
        //private void SetupBrowser()
        //{
        //    int Step = 0;

        //    try
        //    {
        //        Step = 1; // Set browser
        //        RootBrowserFolder = $@"{App.PathSessions}{Account.Code}-{Account.Id}\";
        //        var config = new NoboxBrowserConfig() { SessionFolder = RootBrowserFolder, OpeningUrl = UrlUtama, AutoInitBrowser = false, AutoInitDriver = true, AutoOpenDevTools = false, IsAllowImages = true, DeleteFolderBeforeStart = false, IsDebug = IsDebug, AreDevToolsEnabled = true, AreDefaultContextMenusEnabled = true };
        //        Browser = new AxBrowser(config);

        //        Step = 2; // Set properti tambahan
        //        Browser.Cfg.UserAgent = config.UserAgent;

        //        Step = 3; // Set events
        //        Browser.BrowserInitiated += OnBrowserInitiated;
        //        Browser.BrowserReady += OnBrowserReady;
        //        Browser.DriverInitiated += OnDriverInitiated;

        //        Step = 4; // Tambahkan browser ke form
        //        tabProses.Controls.Add(Browser.WebView);

        //    }
        //    catch (Exception ex) { Log.AddData($"SetupBrowser step {Step} : {ex.Message}", "Error"); }
        //}

        public void addLog(string Fungsi,string Ket)
        {
            string[] row = { Fungsi ,Ket };
            var listViewItem = new ListViewItem(row);
            listView1.Items.Insert(0, listViewItem);
        }

        public async void initWebview()
        {
            int Step = 0;
            try
            {
                Step = 1;
                await webView21.EnsureCoreWebView2Async();

                if (webView21 != null && webView21.CoreWebView2 != null)
                {
                    webView21.CoreWebView2.Navigate("https://institut-asia.teachable.com/");
                    addLog("Init Webview","Success");
                }
                else
                {
                    addLog("Init Webview", "Failed");
                }
                webView21.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                webView21.CoreWebView2.SourceChanged += async (sender, args) =>
                {
                    if (Driver!=null)
                    {
                        AutoProcess();
                    }
                };
                webView21.CoreWebView2.NavigationCompleted += async (sender, args) =>
                {
                    if (args.IsSuccess)
                    {
                        Driver = await webView21.CoreWebView2.CreateDevToolsContextAsync();
                        await Driver.DisposeAsync();
                        if (IsStart)
                        {
                            AutoProcess();
                        }
                        else
                        {

                        }
                    }
                };
            }

            catch (Exception ex)
            {
                //Logs("Error", $"initWebview Step {Step} : {ex.Message}");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            stateButton(false);
        }
        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string dt = e.TryGetWebMessageAsString();
                if(dt== "video-success")
                {
                    addLog("Process", "Video complete go to next course");
                }
                if(dt== "materi-success")
                {
                    addLog("Process", "Materi complete go to next course");
                }
                if(dt== "quiz-success")
                {
                    addLog("Process", "Quiz complete go to next course");
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
