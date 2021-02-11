module Mixi

open Common
open System
open System.Text

open HtmlAgilityPack
open HtmlAgilityPack.CssSelectors.NetCore
open System.Text.RegularExpressions

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)


let mixiOrigPath = $"{unimemoRoot}/original/mixi"

module Download =

    // EUC対応
    let loadHtml (path:string) =
        let doc = HtmlDocument()
        doc.Load(path, Encoding.GetEncoding("EUC-JP"))
        doc

    // pageは1が無いと同じ。
    let listDiaryUrl year month page =
        let burl = $"https://mixi.jp/list_diary.pl?year={year}&month={month}"
        if page = 1 then
            burl
        else
            $"{burl}&page={page}"

    let listFileName year month page = $"list_diary_{year}_{month}_{page}.html"

    let mixiGetPath = $"{mixiOrigPath}/mixi_get.sh"

    let mixiGet url fname =
        shellExecute mixiGetPath $"{url} {fname}" |> ignore

    let downloadListDiaryOne year month page =
        let url = listDiaryUrl year month page
        let fname = listFileName year month page
        let fpath = $"list_diary/{fname}"
        mixiGet url fpath
        fpath

    let seqAHref (doc:HtmlDocument) =
        doc.QuerySelectorAll("a")
       |> Seq.map (fun a -> a.GetAttributeValue("href", ""))

    // patはグループが1つ、Group.[1].Valueのseqを返す。
    let seqMatches (pat:Regex) (hrefs: string seq) =
        hrefs
       |> Seq.filter  (fun href -> pat.Match(href).Success)
       |> Seq.map (fun href -> pat.Match(href).Groups.[1].Value)

    let seqAHrefMatches (pat:Regex) (doc:HtmlDocument) =
        seqAHref doc
        |> seqMatches pat

    let pagePat = Regex(".*list_diary.pl?.*page=([0-9]+).*")

    let hasNext (doc:HtmlDocument) (page:int)=
        let allA = seqAHrefMatches pagePat doc
                    |> Seq.map (fun pageId -> System.Int32.Parse(pageId))
                    |> Seq.filter (fun num -> num > page)
                    |> Seq.toList 
        allA.Length <>0

    open System.Threading
    let waitTime = 2000

    let downloadListDiaryMonth year month =
        let rec downloadRec (fs: string list) page =
            Thread.Sleep(waitTime)
            let fname = downloadListDiaryOne year month page
            printfn $"Download {fname}"
            let doc = loadHtml fname
            let newFs = fname::fs
            if not (hasNext doc page) then
                newFs
            else
                downloadRec newFs (page+1)
        downloadRec [] 1

    let downloadListDiaryMonthList year monthList =
        monthList
        |> List.map (downloadListDiaryMonth year)

    let downloadListDiaryYear year =
        downloadListDiaryMonthList year [1..12]



    // 各日記のダウンロード
    open System.IO

    let ownerIdPat = Regex("https://mixi\.jp/view_diary\.pl\?.*owner_id=([0-9]+).*")

    let getOwnerId (ldDoc:HtmlDocument) =
        seqAHrefMatches ownerIdPat ldDoc
        |> Seq.head

    let editPat = Regex("https://mixi\.jp/edit_diary\.pl\?id=([0-9]+).*")

    let seqDiaryIds (ldDoc:HtmlDocument) =
        seqAHrefMatches editPat ldDoc

    let seqViewDiaryUrls (ownerId:string) (ldDoc:HtmlDocument) =
        let idSeq = seqDiaryIds ldDoc
        idSeq
        |> Seq.map (fun id-> (id, $"https://mixi.jp/view_diary.pl?id={id}&owner_id={ownerId}&full=1"))

    let diaryDir year month page =
        let fpath = $"diary/{year}/%02d{month}_{page}"
        let di = DirectoryInfo(fpath)
        ensureDir di
        di

    let getOneLDDiary year month page =
        let fname = listFileName year month page
        let destDI = diaryDir year month page
        let ldpath = $"{unimemoRoot}/original/mixi/list_diary/{fname}"
        let ldoc = loadHtml ldpath
        let ownerId = getOwnerId ldoc
        seqViewDiaryUrls ownerId ldoc
        |> Seq.iter (fun (id, url)->
                let dest = Path.Combine(destDI.FullName, $"{id}.html")
                mixiGet url dest
                Thread.Sleep(waitTime)
                )

    let listLD year =
        let di = DirectoryInfo  $"{mixiOrigPath}/list_diary/"
        di.GetFiles()
        |> Array.filter (fun fi -> fi.Name.StartsWith($"list_diary_{year}"))

    let ldPat = Regex("list_diary_\d*_(\d*)_(\d*).html")

    let ld2monthPage (fi:FileInfo) =
        let m = ldPat.Match(fi.Name)
        if not m.Success then
            failwith($"listdiary not match(never happen): {fi.FullName}")
        System.Int32.Parse(m.Groups.[1].Value), System.Int32.Parse(m.Groups.[2].Value)

    let getOneYearDiary year =
        listLD year
        |> Array.map ld2monthPage
        |> Array.iter (fun (month, page)->
            getOneLDDiary year month page)

    // ボイスのダウンロード

    let nextHrefLV (lvdoc:HtmlDocument) =
        lvdoc.QuerySelector(".pageList02").QuerySelectorAll("a") |> Seq.map (fun a->a.GetAttributeValue("href", ""))
        |> Seq.filter (fun href-> href.Contains("direction=next"))
        |> Seq.toList
        |> Seq.tryHead

    let lvBPTPat = Regex("list_voice\.pl\?.*base_post_time=([0-9]+).*")

    let saveLV startUrl =
        let rec saveRec url bpt =
            let path = $"list_voice/{bpt}.html"
            printfn $"Downlaoding: {path}"
            Thread.Sleep(waitTime)
            mixiGet url
            
             path
            let lvdoc = loadHtml path
            match nextHrefLV lvdoc with
            | Some href->
                let m = lvBPTPat.Match(href)
                let nextBpt = m.Groups.[1].Value
                saveRec $"https://mixi.jp/{href}" nextBpt
            | _-> ()
        
        let now = DateTime.Now
        let nowBPT = now.ToString("yyyyMMddHHmmss")

        saveRec startUrl nowBPT


    let voicePTPat = Regex("https://mixi.jp/view_voice\.pl\?.*post_time=([0-9]+).*")

    let saveVoices (lvdoc:HtmlDocument) =
        lvdoc.QuerySelectorAll(".moreLink01.hrule a") 
        |> Seq.map (fun a->a.GetAttributeValue("href", ""))
        |> Seq.map (fun href-> 
                    voicePTPat.Match(href).Groups.[1].Value, href)
        |> Seq.toList
        |> List.iter (fun (pt, url) ->
            let fname = $"view_voice/{pt}_vv.html"
            printfn $"Downloading {fname}"
            Thread.Sleep(waitTime)
            mixiGet url fname)

    let listLV () =
        let di = DirectoryInfo  $"{mixiOrigPath}/list_voice/"
        di.GetFiles()
        |> Array.filter (fun fi -> fi.Name.EndsWith($"html"))

    let saveAllVoices () =
        listLV ()
        |> Array.map (fun fi -> loadHtml fi.FullName)
        |> Array.map saveVoices 
