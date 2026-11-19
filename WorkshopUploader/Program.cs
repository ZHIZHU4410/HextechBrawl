using Steamworks;
using System.Text.Json;
const ulong workshopId = 3841855747;
const uint appId = 588650;
Environment.SetEnvironmentVariable("SteamAppId", appId.ToString());
Environment.SetEnvironmentVariable("SteamGameId", appId.ToString());
if (SteamAPI.InitEx(out var error) != ESteamAPIInitResult.k_ESteamAPIInitResult_OK) { Console.WriteLine("Steam init failed: " + error); return 1; }
try
{
    var id = new PublishedFileId_t(workshopId);
    var query = SteamUGC.CreateQueryUGCDetailsRequest(new[] { id }, 1);
    SteamUGC.SetReturnKeyValueTags(query, true);
    var task = new TaskCompletionSource<SteamUGCQueryCompleted_t>();
    using var callback = CallResult<SteamUGCQueryCompleted_t>.Create((r, failed) => { if (failed) task.TrySetException(new Exception("Steam query I/O failure")); else task.TrySetResult(r); });
    callback.Set(SteamUGC.SendQueryUGCRequest(query));
    var queryDeadline = DateTime.UtcNow.AddSeconds(45);
    while (!task.Task.IsCompleted) { SteamAPI.RunCallbacks(); Thread.Sleep(100); if(DateTime.UtcNow>queryDeadline)throw new Exception("Workshop inspection timed out"); }
    var result = await task.Task;
    if (result.m_eResult != EResult.k_EResultOK || !SteamUGC.GetQueryUGCResult(query, 0, out var info)) throw new Exception("Cannot inspect target item: " + result.m_eResult);
    var tags = new Dictionary<string,string>();
    for(uint n=0; n<SteamUGC.GetQueryUGCNumKeyValueTags(query, 0); n++)
        if(SteamUGC.GetQueryUGCKeyValueTag(query,0,n,out var key,512,out var value,1024)) tags[key]=value;
    Console.WriteLine(JsonSerializer.Serialize(new { id=info.m_nPublishedFileId.m_PublishedFileId,title=info.m_rgchTitle,owner=info.m_ulSteamIDOwner,current=SteamUser.GetSteamID().m_SteamID,app=info.m_nConsumerAppID.m_AppId,updatedUtc=DateTimeOffset.FromUnixTimeSeconds(info.m_rtimeUpdated).ToString("O"),bytes=info.m_nFileSize,tags }));
    SteamUGC.ReleaseQueryUGCRequest(query);
    if(args.Length==0 || args[0]!="--upload") return 0;
    if(info.m_nPublishedFileId.m_PublishedFileId!=workshopId || info.m_nConsumerAppID.m_AppId!=appId || info.m_ulSteamIDOwner!=SteamUser.GetSteamID().m_SteamID)
        throw new Exception("Target ID, application, or ownership mismatch; no changes submitted");
    var path=Path.GetFullPath(args[1]);
    var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(path,"modinfo.json")));
    if(manifest.RootElement.GetProperty("name").GetString()!="HextechBrawl") throw new Exception("Unexpected mod manifest");
    var update = SteamUGC.StartItemUpdate(new AppId_t(appId),id);
    if(!SteamUGC.SetItemContent(update,path)) throw new Exception("SetItemContent failed");
    var submitted = new TaskCompletionSource<SubmitItemUpdateResult_t>();
    using var uploadCallback=CallResult<SubmitItemUpdateResult_t>.Create((r,failed)=>{if(failed)submitted.TrySetException(new Exception("Steam upload I/O failure"));else submitted.TrySetResult(r);});
    uploadCallback.Set(SteamUGC.SubmitItemUpdate(update,"v3.5.0: 选中武器生成在脚边，不自动替换装备；修复过高伤害转整数溢出，保留收藏家阶段保护。"));
    var deadline=DateTime.UtcNow.AddMinutes(5);
    DateTime next=DateTime.MinValue;
    while(!submitted.Task.IsCompleted)
    {
        SteamAPI.RunCallbacks(); Thread.Sleep(100);
        if(DateTime.UtcNow>deadline)throw new Exception("Upload timed out; verify Workshop update status before retrying");
        if(DateTime.UtcNow>=next){var status=SteamUGC.GetItemUpdateProgress(update,out var done,out var total);Console.WriteLine($"Upload {status}: {done}/{total}");next=DateTime.UtcNow.AddSeconds(5);}
    }
    var uploaded=await submitted.Task;
    Console.WriteLine(JsonSerializer.Serialize(new { result=uploaded.m_eResult.ToString(),needsAgreement=uploaded.m_bUserNeedsToAcceptWorkshopLegalAgreement,id=workshopId }));
    if(uploaded.m_eResult!=EResult.k_EResultOK)return 2;
    return 0;
}
catch(Exception ex){Console.WriteLine("ERROR: "+ex.Message);return 1;}
finally{SteamAPI.Shutdown();}
